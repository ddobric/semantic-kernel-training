using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AzureFoundrySkAgent
{
    internal class AgentFramework_FoundryChatAgent
    {
        private const string _cModelDeploymentName = "gpt-4o";


        public static async Task RunAsync()
        {
            string? agentId = null;

            //var persistentAgentsClient = new Azure.AI.Agents.Persistent.PersistentAgentsClient(
            //    Environment.GetEnvironmentVariable("AgentFrameworkFoundryAgentEndpointUrl")!,
            //    new DefaultAzureCredential() /*new AzureCliCredential()*/);

            // Get a client to create/retrieve/delete server side agents with Azure Foundry Agents.
            AIProjectClient aiProjectClient = new(new Uri(Environment.GetEnvironmentVariable("AgentFrameworkFoundryAgentEndpointUrl")!), 
                new DefaultAzureCredential());

            // Define the agent with function tools.
            AITool tool = AIFunctionFactory.Create(TemperatureTool);

            AIAgent? agent = null;


            if (agentId != null)
                agent = await aiProjectClient.GetProjectOpenAIClient(agentId);

            if (agent == null)
            {
                var newAgent = await aiProjectClient.CreateAIAgentAsync(name: "", model: "", 
                    instructions: "", tools: [tool]);

                // Create a persistent agent
                var agentMetadata = await aiProjectClient.Agents.CreateAgentVersionAsync(
                    model: _cModelDeploymentName,
                    name: nameof(AgentFramework_FoundryChatAgent),
                    instructions: "You are the agent answering questions.", 
                    tools: [tool]);

                // Retrieve the agent that was just created as an AIAgent using its ID
                agent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);
            }

            await RunConversationLoopAsync(agent);            
        }

       

        [Description("Get the weather for a given location.")]
        public static string TemperatureTool(
            [Description("The city")] string? city,
            [Description("the room name in the city")] string? room = null)
        {
            if (room != null && room!.ToLower().StartsWith("stage3"))
                return "hot";
            else
                return "35";
        }

        protected static async Task RunConversationLoopAsync(AIAgent agent)
        {
            Microsoft.Agents.AI.AgentThread thread = agent!.GetNewThread();

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    await foreach (var update in agent.RunStreamingAsync(userInput))
                    {
                        Console.Write(update);
                    }
                }
                finally
                {

                }
            }
        }
    }
}
