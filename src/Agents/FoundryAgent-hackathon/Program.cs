using Azure;
using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;
using System.Text.Json;

namespace FoundryAgent
{
    internal class Program
    {
        /// <summary>
        /// Demonstrates how to create and use the agent.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            await Run();

            // How to create the Function definition ot the tool.
            // This is the definition only, not a code!
            // FunctionToolDefinition myFnc = new("LightSwitch", "Switch on the light.");

            RegisterTelemetry();

            string agentName = "DemoAgent2";
            string modelName = "gpt-4o";
            string connectionString = Environment.GetEnvironmentVariable("AgentEndpointUrl")!;

            PersistentAgentsClient client = new PersistentAgentsClient(connectionString, new AzureCliCredential());

            PersistentAgent? agent = null;

            Pageable<PersistentAgent> agentListResponse = client.Administration.GetAgents();

            Console.WriteLine("Listing agents in the foundry project...");

            foreach (var foundyAgent in agentListResponse)
            {
                if (foundyAgent.Name == agentName)
                {
                    agent = foundyAgent;
                    break;
                }
                ;
                Console.WriteLine($"Agent: {foundyAgent.Name} - {foundyAgent.Id}");
            }

            Console.WriteLine("------------------------");

            agent = agentListResponse.FirstOrDefault(a => a.Name == agentName);

            if (agent == null)
            {
                // Create an agent
                Response<PersistentAgent> agentResponse = await client.Administration.CreateAgentAsync(
                    model: modelName,
                    name: agentName,
                    instructions: "You are a helpful agent who helps answering Math and city related questions in a sarcastic way.",

                    tools: new List<ToolDefinition>
                    {
                        new CodeInterpreterToolDefinition() ,
                        GetUserFavoriteCityTool,
                        GetCityNicknameTool,
                        //MyQueueFunctionTool
                    });

                agent = agentResponse.Value;
            }

            //  Create a thread
            Response<PersistentAgentThread> threadResponse = await client.Threads.CreateThreadAsync();
            PersistentAgentThread thread = threadResponse.Value;

            // Add a message to a thread
            //Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
            //    thread.Id,
            //    MessageRole.User,
            //    "I need to solve the equation `3x + 11 = 14`. Can you help me?");

            //ThreadMessage message = messageResponse.Value;

            //var messageResponse2 = await client.CreateMessageAsync(
            //    thread.Id,
            //    MessageRole.User,
            //    "Tell me the nick name of the city Sarajevo.");

            PrintThreadMessages(client, thread.Id);

            while (true)
            {
                Console.Write("> ");
                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userInput);

                string runId = await RunOnThreadAndWait(client, agent, thread);

                PrintConversationResult(client, thread, runId);
            }

            Console.ReadLine();
        }

        protected static async Task Run()
        {
            var endpoint = new Uri("https://hackathon-munich-resource.services.ai.azure.com/api/projects/hackathon-munich");
            AIProjectClient projectClient = new(endpoint, new AzureCliCredential());

            PersistentAgentsClient agentsClient = projectClient.GetPersistentAgentsClient();

            PersistentAgent agent = agentsClient.Administration.GetAgent("asst_uoqf778cnzUCTeHGyaQJPlF4");

            PersistentAgentThread thread = agentsClient.Threads.GetThread("thread_dI4Ekm1W2zNLSmdRlRxuC14d");

            PersistentThreadMessage messageResponse = agentsClient.Messages.CreateMessage(
                thread.Id,
                MessageRole.User,
                "Hi Reise-Recherche-Agent");

            ThreadRun run = agentsClient.Runs.CreateRun(
                thread.Id,
                agent.Id);

            // Poll until the run reaches a terminal status
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = agentsClient.Runs.GetRun(thread.Id, run.Id);
            }
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress);
            if (run.Status != RunStatus.Completed)
            {
                throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
            }

            Pageable<PersistentThreadMessage> messages = agentsClient.Messages.GetMessages(
                thread.Id, order: ListSortOrder.Ascending);

            // Display messages
            foreach (PersistentThreadMessage threadMessage in messages)
            {
                Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                foreach (MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.Write(textItem.Text);
                    }
                    else if (contentItem is MessageImageFileContent imageFileItem)
                    {
                        Console.Write($"<image from ID: {imageFileItem.FileId}");
                    }
                    Console.WriteLine();
                }
            }


        }
        protected static void PrintThreadMessages(PersistentAgentsClient client, string threadId)
        {
            Pageable<PersistentThreadMessage> threadMessages = client.Messages.GetMessages(threadId);

            foreach (var msg in threadMessages)
            {
                Console.WriteLine($"Thread: {msg.ThreadId}, Run:{msg.RunId},  {msg.Role}, {ToContent(msg.ContentItems)}");
            }

            Console.WriteLine();
        }

        private static string ToContent(IReadOnlyList<MessageContent> contentItems)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in contentItems)
            {
                sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }

        private static void PrintConversationResult(PersistentAgentsClient client, PersistentAgentThread thread, string runId)
        {
            Console.WriteLine("============================================================");

            Pageable<PersistentThreadMessage> afterRunMessagesResponse = client.Messages.GetMessages(thread.Id, runId);

            IReadOnlyList<PersistentThreadMessage> messages = afterRunMessagesResponse.Where(m => m.RunId == runId).ToArray();

            // Note: messages iterate from newest to oldest, with the messages[0] being the most recent
            foreach (PersistentThreadMessage threadMessage in messages)
            {
                Console.WriteLine("----------------------------");
                Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                foreach (MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.Write(textItem.Text);
                    }
                    else if (contentItem is MessageImageFileContent imageFileItem)
                    {
                        Console.Write($"<image from ID: {imageFileItem.FileId}");
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("============================================================");
        }

        private static async Task<string> RunOnThreadAndWait(PersistentAgentsClient client, PersistentAgent? agent, PersistentAgentThread thread)
        {
            // Run the agent
            Response<ThreadRun> runResponse = await client.Runs.CreateRunAsync(
                thread.Id,
                agent?.Id,
                additionalInstructions: "");
            ThreadRun run = runResponse.Value;

            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                // Update the status of the run.
                runResponse = await client.Runs.GetRunAsync(thread.Id, runResponse.Value.Id);

                if (runResponse.Value.Status == RunStatus.RequiresAction &&
                    runResponse.Value.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    List<ToolOutput> toolOutputs = new();
                    foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
                    {
                        toolOutputs.Add(GetResolvedToolOutput(toolCall));
                    }
                    runResponse = await client.Runs.SubmitToolOutputsToRunAsync(runResponse.Value, toolOutputs);
                }
            }
            while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

            return runResponse.Value.Id;
        }

        /// <summary>
        /// Example of the function with no arguments.
        /// </summary>
        /// <returns></returns>
        protected static string GetUserFavoriteCity() => "Frankfurt am Main, Germany";

        private static FunctionToolDefinition GetUserFavoriteCityTool = new("GetUserFavoriteCity", "Gets the user's favorite city.");

        // Example of a function with a single required parameter
        protected static string GetCityNickname(string location)
        {
            if (location.ToLower().Contains("seattle"))
                return "The Emerald City";
            else if (location.ToLower().Contains("sarajevo"))
                return "SA, Bosnian Culture City";
            else
                return "Unknown City";
        }



        private static FunctionToolDefinition GetCityNicknameTool = new(
            name: "GetCityNickname",
            description: "Gets the nickname of a city, e.g. 'LA' for 'Los Angeles, CA'.",
            parameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                    },
                    Required = new[] { "location" },
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            );


        static string _connStr = "UseDevelopmentStorage=true";

        /// <summary>
        /// https://github.com/Azure/azure-docs-sdk-dotnet/blob/main/api/overview/azure/preview/ai.projects-readme.md
        /// </summary>
        protected static AzureFunctionToolDefinition MyQueueFunctionTool =
            new AzureFunctionToolDefinition("Function2",
                "Gets the information related to invoices.",
                new AzureFunctionBinding(new AzureFunctionStorageQueue(_connStr, "input-queue")),
                new AzureFunctionBinding(new AzureFunctionStorageQueue(_connStr, "output-queue")),
                  parameters: BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    query = new
                    {
                        Type = "string",
                        Description = "The question to ask.",
                    },
                    outputqueueuri = new
                    {
                        Type = "string",
                        Description = "The full output queue uri."
                    }
                },
            },
        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
    )
                );






        //new(
        //name: "Function1",
        //description: "Gets the information related to invoices.",

        //parameters: BinaryData.FromObjectAsJson(
        //    new
        //    {
        //        Type = "object",
        //        Parameters = new
        //        {
        //            Location = new
        //            {
        //                Type = "string",
        //                Description = "The city and state, e.g. San Francisco, CA",
        //            },
        //        },
        //        Required = new[] { "location" },

        //    },
        //    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        //);

        private static ToolOutput GetResolvedToolOutput(RequiredToolCall toolCall)
        {
            if (toolCall is RequiredFunctionToolCall functionToolCall)
            {
                if (functionToolCall.Name == GetUserFavoriteCityTool.Name)
                {
                    return new ToolOutput(toolCall, GetUserFavoriteCity());
                }
                using JsonDocument argumentsJson = JsonDocument.Parse(functionToolCall.Arguments);
                if (functionToolCall.Name == GetCityNicknameTool.Name)
                {
                    string locationArgument = argumentsJson.RootElement.GetProperty("location").GetString();
                    return new ToolOutput(toolCall, GetCityNickname(locationArgument));
                }
            }
            return null;
        }

        private static void RegisterTelemetry()
        {
            // Enables experimental Azure SDK observability
            AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

            // By default instrumentation captures chat messages without content
            // since content can be very verbose and have sensitive information.
            // The following AppContext switch enables content recording.
            AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", true);

            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddHttpClientInstrumentation()
                .AddSource("Azure.AI.Inference.*")
                .ConfigureResource(r => r.AddService("sample2"))
                .AddConsoleExporter()
                .AddOtlpExporter()
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddHttpClientInstrumentation()
                .AddMeter("Azure.AI.Inference.*")
                .ConfigureResource(r => r.AddService("sample2"))
                .AddConsoleExporter()
                .AddOtlpExporter()
                .Build();
        }

        /*
        static void Main2(string[] args)
        {
            AIProjectClient projectClient = new AIProjectClient("connectionString");

            var connClient = projectClient.GetConnectionsClient();
            ConnectionResponse connection = connClient.GetDefaultConnection(ConnectionType.AzureAIServices, withCredential: true);
            var properties = connection.Properties as ConnectionPropertiesApiKeyAuth;

            if (properties == null)
            {
                throw new Exception("Invalid auth type, expected API key auth");
            }

            // Create and use an Azure OpenAI client
            AzureOpenAIClient azureOpenAIClient = new(
                new Uri(properties.Target),
                new AzureKeyCredential(properties.Credentials.Key));

            // This must match the custom deployment name you chose for your model
            ChatClient chatClient = azureOpenAIClient.GetChatClient("gpt-4o-mini");

            ChatCompletion completion = chatClient.CompleteChat(
                [
                    new SystemChatMessage("You are a helpful assistant that talks like a pirate."),
        new UserChatMessage("Does Azure OpenAI support customer managed keys?"),
        new AssistantChatMessage("Yes, customer managed keys are supported by Azure OpenAI"),
        new UserChatMessage("Do other Azure AI services support this too?")
                ]);

            Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        }*/
    }
}
