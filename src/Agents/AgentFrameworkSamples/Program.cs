using AgentFramework_Samples.GettingStarted;
using AgentFramework_Samples.HostedAgentsWithAzureFoundryModels;
using AgentFramework_Samples.MCP;
using AgentFramework_Samples.Providers.Anthropic;
using AgentFramework_Samples.Providers.FoundryLocal;
using AgentFramework_Samples.Providers.Ollama;
using AgentFramework_Samples.Providers.OpenAIAgents;
using AgentFramework_Samples.SqlAgent;
using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using HostedAgentsWithAzureFoundryModels;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AzureFoundrySkAgent
{
    /// <summary>
    /// Entry point that selects which Agent Framework sample(s) to run.
    /// Un/comment calls in Main() to switch between demos.
    /// </summary>
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // ═══════════════════════════════════════════════
            //  HOSTED AGENTS — Azure OpenAI (Foundry Models)
            // ═══════════════════════════════════════════════

            // Scenario 1: Basic agent construction & invocation
            //await HelloAgent.RunAsync();

            // Scenario 2: Multi-turn conversations with sessions
            //await HelloAgent.RunMultiturnAsync();

            // Scenario 3: Agent with function tools
            //await HelloAgent.RunWithToolsAsync();

            // Scenario 4: Agent with custom memory (AIContextProvider)
            //await AgentWithMemory.RunAsync();


            // ═══════════════════════════════════════════════
            //  WORKFLOWS — Agent Framework Workflow Engine
            // ═══════════════════════════════════════════════

            // Simple linear pipeline (Uppercase → Reverse)
            //await HelloWorkflow.RunAsync();

            // Pipeline with inter-executor messaging & custom events
            await HelloWorkflow.RunWithMessagingAsync();

            // AI-driven feedback loop (SloganWriter ↔ FeedbackProvider)
            //await ComplexWorkflow.RunAsync();

            // Multi-agent orchestration as a workflow
            //await AgentsInWorkflow.RunAsync();


            // ═══════════════════════════════════════════════
            //  CLAW — Command Line Agent Workflow
            // ═══════════════════════════════════════════════

            // Three-agent architecture: Intent → Plan → Task execution
            //await SimpleClawSession.RunAsync();

            // ═══════════════════════════════════════════════
            //  Lighting Plugin
            // ═══════════════════════════════════════════════
            //await LightingSample.RunAsync();
            //await LightingSample.RunWithOllamaAsync();

            // ═══════════════════════════════════════════════
            //  MCP — Model Context Protocol Tool Integration
            // ═══════════════════════════════════════════════

            // Local MCP server via stdio transport
            //await LocalHostedMcpTool.RunAsync();

            // Remote MCP server via HTTP (e.g. Microsoft Learn)
            //await HttpHostedMcpTool.RunAsync();


            // ═══════════════════════════════════════════════
            //  PROVIDERS — OpenAI (direct, non-Azure)
            // ═══════════════════════════════════════════════

            // ResponsesClient-based agent
            //await OpenAISamples.RunResponsesClientAsync();

            // ChatClient-based agent
            //await OpenAISamples.RunChatClientAsync();

            // Interactive conversation with tools
            //await OpenAISamples.RunConversationAsync();

            // Reasoning models (non-streaming & streaming)
            //await OpenAIReasoningSamples.RunReasoningAsync();
            //await OpenAIReasoningSamples.RunReasoningWithStreamingAsync();

            // Conversation & code interpreter samples
            //await OpenAIConversationSample.RunAsync();
            //await OpenAICodeInterpreter.RunAsync();


            // ═══════════════════════════════════════════════
            //  PROVIDERS — Anthropic
            // ═══════════════════════════════════════════════

            //await HelloAnthropicAgent.RunAsync();


            // ═══════════════════════════════════════════════
            //  PROVIDERS — Ollama (local models)
            // ═══════════════════════════════════════════════

            // Basic agent (single prompt)
            //await HelloOllamaAgent.RunAsync();

            // Agent with function tools (interactive loop)
            //await HelloOllamaAgent.RunWithToolsAsync();


            // ═══════════════════════════════════════════════
            //  PROVIDERS — Foundry Local (REST-based)
            // ═══════════════════════════════════════════════

            //await HelloFoundryLocalAgent.RunAsync();


            // ═══════════════════════════════════════════════
            //  FOUNDRY AGENTS (Azure AI Foundry)
            //  ⚠️ Moved to: src\Agents\FoundryAgent\FoundryAgent.sln
            //  See: Providers\FoundryAgents\README.md
            // ═══════════════════════════════════════════════


            //await SqlAgentSample.RunAsync();


            // ═══════════════════════════════════════════════
            //  LEGACY / ADDITIONAL SAMPLES
            // ═══════════════════════════════════════════════

            //await AgentFramework_WorkflowSample.RunAsync();

            // Semantic Kernel agent (SK-based, not Agent Framework)
            //await SemanticKernelAgent.RunAsync(args);

            // SK Foundry Agent — DEPRECATED, use AgentFrameworkPersistedAgentSamples
            //await SemanticKernelFoundryAgentSample.RunAsync();

            // Azure OpenAI direct samples
            //await AgentFramework_OpenAISamples.RunOpenAIBasicAsync();
            //await AgentFramework_OpenAISamples.RunWithToolsFuncAsync();
            //await AgentFramework_AzOpenAISamples.RunRAGAsync();
            //await AgentFramework_AzOpenAISamples.RunOpenAIBasicAsync();
            //await AgentFramework_AzOpenAISamples.RunOpenAIAgentStreamedAsync();
            //await AgentFramework_AzOpenAISamples.RunWithToolsFuncAsync();

            // Persistent (server-side) agents
            //await AgentFrameworkPersistedAgentSamples.RunPersistentAgents();

        }

        // Following functions are related to SK Agents



        public static async Task MainOcrSample(string[] args)
        {
            AIProjectClient projectClient =
                new(new Uri(Environment.GetEnvironmentVariable("AgentEndpointUrl")!),
                new DefaultAzureCredential());

            PersistentAgentsClient agentsClient = projectClient.GetPersistentAgentsClient();

            PersistentAgent foundryAgent = agentsClient.Administration.GetAgent("asst_LJaKif5HACvcoHXDxG0ngoEq");

            AzureAIAgent skAgent = new(foundryAgent, agentsClient);

            await RunConversationLoopAsync(skAgent, agentsClient);
        }


        private static async Task RunConversationLoopAsync(AzureAIAgent skAgent, PersistentAgentsClient agentsClient)
        {
            AzureAIAgentThread agentThread = new(skAgent.Client);

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                byte[]? fileContent = null;

                if (userInput.ToLower().StartsWith("file:"))
                {
                    fileContent = LoadFile(userInput);
                    if (fileContent == null)
                    {
                        continue;
                    }
                }

                try
                {
                    ChatMessageContent message;

                    if (fileContent != null)
                    {
                        var imageContent = new ReadOnlyMemory<byte>(fileContent);

                        ChatMessageContentItemCollection contentItems = new ChatMessageContentItemCollection()
                        {
                            new TextContent("Extract the data from the image."),
                            new ImageContent(imageContent, "image/jpg")
                        };
                        message = new ChatMessageContent(AuthorRole.User, contentItems);
                    }
                    else
                        message = new(AuthorRole.User, userInput);

                    await foreach (StreamingChatMessageContent response in skAgent.InvokeStreamingAsync(message, agentThread))
                    {
                        Console.Write(response.Content);
                    }
                }
                finally
                {

                }
            }
        }

        private static byte[]? LoadFile(string userInput)
        {
            var tokens = userInput.Split(":");
            if (tokens.Length == 2)
            {
                string filePath = tokens[1].Trim();
                if (File.Exists(filePath))
                {
                    var fileContent = File.ReadAllBytes(filePath);
                    Console.WriteLine($"[File content loaded from {filePath}]");
                    return fileContent;
                }
                else
                {
                    Console.WriteLine($"[File not found: {filePath}]");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("[Invalid file command. Use 'file:<path>']");
                return null;
            }
        }




        // 2. Create an agent instance
        //     PersistentAgent agent = await client.CreateAgentAsync(def
        //   static async Task Main(string[] args)
        //   {

        //       IKernelBuilder builder = Kernel.CreateBuilder();

        //       // Initialize multiple chat - completion services.
        //       builder.AddAzureOpenAIChatCompletion(
        //          deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,
        //          endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
        //          apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);

        //       builder.Plugins.AddFromObject(new MyPlugin());

        //       Kernel kernel = builder.Build();

        //       ChatCompletionAgent agent =
        //new()
        //{
        //    Name = "aaaa",
        //    Instructions = "your are summarizer",
        //    Kernel = kernel,
        //    Arguments = // Specify the service-identifier via the KernelArguments
        //      new KernelArguments(
        //        new OpenAIPromptExecutionSettings()
        //        {
        //            ServiceId = "service-2" // The target service-identifier.
        //        })
        //};

        //   }

        //static async Task Main(string[] args)
        //{
        //    Console.WriteLine("Hello Foundy Agent by Semantic Kernel!");

        //    string agentName = "Semantic Kernel Agent Sample";
        //    string modelName = "gpt-4o";
        //    string connectionString = Environment.GetEnvironmentVariable("AgentConnStr")!;
        //    Azure.AI.Projects.Agent? foundryAgentDefinition = null;

        //    //AIProjectClient client = AzureAIAgent.CreateAzureAIClient(connectionString, new AzureCliCredential());

        //    ////AgentsClient agentsClient = client.GetAgentsClient(); 
        //    //AgentsClient agentsClient = new AgentsClient(connectionString, new DefaultAzureCredential());

        //    //Response<PageableList<Agent>> agentListResponse = await agentsClient.GetAgentsAsync();

        //    //Console.WriteLine("Listing agents in the foundry project...");


        //    //foreach (var foundyAgent in agentListResponse.Value)
        //    //{
        //    //    if(foundyAgent.Name == agentName)
        //    //    {
        //    //        foundryAgentDefinition = foundyAgent;
        //    //        break;
        //    //    }

        //    //    Console.WriteLine($"Agent: {foundyAgent.Name} - {foundyAgent.Id}");
        //    //}

        //    Console.WriteLine("------------------------");

        //    KernelPlugin plugin = KernelPluginFactory.CreateFromType<object>();
        //    var tools = plugin.Select(f => f.ToToolDefinition(plugin.Name));


        //    if (foundryAgentDefinition == null)
        //    {
        //       foundryAgentDefinition = await agentsClient.CreateAgentAsync(
        //       modelName,
        //       name: agentName,
        //       description: "Sample Agent Created by Semantic Kernel Agent Framework.",
        //       instructions: "You are the agent who helps answering any question.",
        //       tools: new List<ToolDefinition>
        //            {
        //                new CodeInterpreterToolDefinition() ,
        //                GetUserFavoriteCityTool,
        //                GetCityNicknameTool,
        //                //MyQueueFunctionTool
        //            });                
        //    }

        //    AzureAIAgent agent = new(foundryAgentDefinition, agentsClient);

        //    Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);

        //    while (true)
        //    {
        //        Console.WriteLine();
        //        Console.Write("> ");

        //        string? userInput = Console.ReadLine();
        //        if (String.IsNullOrEmpty(userInput) || userInput == "exit")
        //            break;

        //        try
        //        {
        //            ChatMessageContent message = new(AuthorRole.User, userInput);
        //            //await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))

        //            await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
        //            {
        //                Console.Write(response.Content);
        //            }
        //        }
        //        finally
        //        {

        //        }
        //    }

        //    //await agentThread.DeleteAsync();
        //    //await agent.Client.DeleteAgentAsync(agent.Id);
        //}

        ///// <summary>
        ///// Example of the function with no arguments.
        ///// </summary>
        ///// <returns></returns>
        //protected static string GetUserFavoriteCity() => "Frankfurt am Main, Germany";

        //private static FunctionToolDefinition GetUserFavoriteCityTool = new("GetUserFavoriteCity", "Gets the user's favorite city.");

        //// Example of a function with a single required parameter
        //protected static string GetCityNickname(string location)
        //{
        //    if (location.ToLower().Contains("seattle"))
        //        return "The Emerald City";
        //    else if (location.ToLower().Contains("sarajevo"))
        //        return "SA, Bosnian Culture City";
        //    else
        //        return "Unknown City";
        //}

        //private static FunctionToolDefinition GetCityNicknameTool = new(
        //    name: "GetCityNickname",
        //    description: "Gets the nickname of a city, e.g. 'LA' for 'Los Angeles, CA'.",
        //    parameters: BinaryData.FromObjectAsJson(
        //        new
        //        {
        //            Type = "object",
        //            Properties = new
        //            {
        //                Location = new
        //                {
        //                    Type = "string",
        //                    Description = "The city and state, e.g. San Francisco, CA",
        //                },
        //            },
        //            Required = new[] { "location" },
        //        },
        //        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        //    );


    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
