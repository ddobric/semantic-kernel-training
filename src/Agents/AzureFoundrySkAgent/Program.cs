using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AzureFoundrySkAgent
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient("https://ddobric-agents-samples-resource.services.ai.azure.com/api/projects/ddobric-agents-samples", new AzureCliCredential());

            // 1. Define an agent on the Azure AI agent service
            PersistentAgent definition = await client.Administration.CreateAgentAsync(
                "gpt-4o",
                name: "SkAgent01",
                description: "Sample Agent",
                instructions: "Helper");

            // 2. Create a Semantic Kernel agent based on the agent definition
            AzureAIAgent agent = new(definition, client);
        }

     //   static async Task Main(string[] args)
     //   {

     //       IKernelBuilder builder = Kernel.CreateBuilder();

     //       Initialize multiple chat - completion services.
     //      builder.AddAzureOpenAIChatCompletion(
     //         deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,
     //         endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
     //         apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);

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

      //  }
        
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
