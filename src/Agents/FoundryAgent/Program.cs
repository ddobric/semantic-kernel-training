using Azure.AI.OpenAI;
using Azure;
using OpenAI.Chat;
using Azure.AI.Projects;
using Azure.Identity;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
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
            //FunctionToolDefinition myFnc = new("LightSwitch", "Switch on the light.");

            RegisterTelemetry();

            string agentName = "Sarcastic Agent";
            string modelName = "gpt-4o";

            var connectionString = Environment.GetEnvironmentVariable("AgentConnStr");

            AgentsClient client = new AgentsClient(connectionString, new DefaultAzureCredential());

            Agent? agent = null;

            Response<PageableList<Agent>> agentListResponse = await client.GetAgentsAsync();

            foreach (var foundyAgent in agentListResponse.Value)
            {
                agent = foundyAgent;
                Console.WriteLine($"Agent: {foundyAgent.Name} - {foundyAgent.Id}");
            }

            agent = agentListResponse.Value.FirstOrDefault(a => a.Name == agentName);

            if (agent == null)
            {
                // Create an agent
                Response<Agent> agentResponse = await client.CreateAgentAsync(
                    model: modelName,
                    name: agentName,
                    instructions: "You are a helpful agent who helps answering Math and city related questions in a sarcastic way.",
                    tools: new List<ToolDefinition>
                    {
                        new CodeInterpreterToolDefinition() ,
                        GetUserFavoriteCityTool,
                        GetCityNicknameTool
                    });

                agent = agentResponse.Value;
            }

            //  Create a thread
            Response<AgentThread> threadResponse = await client.CreateThreadAsync();
            AgentThread thread = threadResponse.Value;

            // Add a message to a thread
            //Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
            //    thread.Id,
            //    MessageRole.User,
            //    "I need to solve the equation `3x + 11 = 14`. Can you help me?");

            //ThreadMessage message = messageResponse.Value;

            var messageResponse2 = await client.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                "Tell me the nick name of the city Sarajevo.");

            // Intermission: message is now correlated with thread
            // Intermission: listing messages will retrieve the message just added

            Response <PageableList<ThreadMessage>> messagesListResponse = await client.GetMessagesAsync(thread.Id);
            //Assert.That(messagesListResponse.Value.Data[0].Id == message.Id);

            // Step 4: Run the agent
            Response<ThreadRun> runResponse = await client.CreateRunAsync(
                thread.Id,
                agent?.Id,
                additionalInstructions: "");
            ThreadRun run = runResponse.Value;

            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);

                if (runResponse.Value.Status == RunStatus.RequiresAction &&
                    runResponse.Value.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    List<ToolOutput> toolOutputs = new();
                    foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
                    {
                        toolOutputs.Add(GetResolvedToolOutput(toolCall));
                    }
                    runResponse = await client.SubmitToolOutputsToRunAsync(runResponse.Value, toolOutputs);
                }
            }
            while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

            Response<PageableList<ThreadMessage>> afterRunMessagesResponse
                = await client.GetMessagesAsync(thread.Id);

            IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

            // Note: messages iterate from newest to oldest, with the messages[0] being the most recent
            foreach (ThreadMessage threadMessage in messages)
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

            //await client.CancelRunAsync(runResponse.Value.ThreadId, runResponse.Value.Id);

            Console.ReadLine();
        }

        /// <summary>
        /// Example of the function with no arguments.
        /// </summary>
        /// <returns></returns>
        protected static string GetUserFavoriteCity() => "Seattle, WA";

        private static FunctionToolDefinition GetUserFavoriteCityTool = new("GetUserFavoriteCity", "Gets the user's favorite city.");

        // Example of a function with a single required parameter
        protected static string GetCityNickname(string location) => location switch
        {
            "Seattle, WA" => "The Emerald City",
            "Sarajavo" => "Bosnian Culture City",
            _ => throw new NotImplementedException(),
        };

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
        }
    }
}
