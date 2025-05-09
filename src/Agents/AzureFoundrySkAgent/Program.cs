using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AzureFoundrySkAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello Foundy Agent by Semantic Kernel!");

            string agentName = "Semantic Kernel Agent Sample";
            string modelName = "gpt-4o";
            string connectionString = Environment.GetEnvironmentVariable("AgentConnStr")!;

            AIProjectClient client = AzureAIAgent.CreateAzureAIClient(connectionString, new AzureCliCredential());

            //AgentsClient agentsClient = client.GetAgentsClient(); 
            AgentsClient agentsClient = new AgentsClient(connectionString, new DefaultAzureCredential());

            Response<PageableList<Agent>> agentListResponse = await agentsClient.GetAgentsAsync();

            Console.WriteLine("Listing agents in the foundry project...");

            Azure.AI.Projects.Agent? foundryAgentDefinition = null;
            
            foreach (var foundyAgent in agentListResponse.Value)
            {
                if(foundyAgent.Name == agentName)
                {
                    foundryAgentDefinition = foundyAgent;
                    break;
                }
               
                Console.WriteLine($"Agent: {foundyAgent.Name} - {foundyAgent.Id}");
            }

            Console.WriteLine("------------------------");


            if (foundryAgentDefinition == null)
            {
               foundryAgentDefinition = await agentsClient.CreateAgentAsync(
               modelName,
               name: agentName,
               description: "Sample Agent Created by Semantic Kernel Agent Framework.",
               instructions: "You are the agent who helps answering any question.",
               tools: new List<ToolDefinition>
                    {
                        new CodeInterpreterToolDefinition() ,
                        GetUserFavoriteCityTool,
                        GetCityNicknameTool,
                        //MyQueueFunctionTool
                    });                
            }

            AzureAIAgent agent = new(foundryAgentDefinition, agentsClient);

            Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    ChatMessageContent message = new(AuthorRole.User, userInput);
                    //await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))

                    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
                    {
                        Console.Write(response.Content);
                    }
                }
                finally
                {
                   
                }
            }

            //await agentThread.DeleteAsync();
            //await agent.Client.DeleteAgentAsync(agent.Id);
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

    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
