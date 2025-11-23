using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Threading;


namespace AzureFoundrySkAgent
{
    /// <summary>
    /// Demonstrates how to use Azure Foundry Persistent Agents with the Agent Framework SDK and multiturn conversation.
    /// </summary>
    internal class AgentFrameworkPersistedAgentSamples
    {
        private const string _cModelDeploymentName = "gpt-4o-mini";

        public static async Task RunPersistentAgents()
        {
            string agentId = "asst_qOM7Mh9TtFw2tGbjFfP4fGst";

            var persistentAgentsClient = new Azure.AI.Agents.Persistent.PersistentAgentsClient(
                Environment.GetEnvironmentVariable("AgentFrameworkFoundryAgentEndpointUrl")!,
                new DefaultAzureCredential() /*new AzureCliCredential()*/);

            AIAgent agent;

            agent = await persistentAgentsClient.Administration.GetAgentAsync(agentId);

            if (agent == null)
            {
                // Create a persistent agent
                var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
                    model: "gpt-4o-mini",
                    name: "AgentFrameworkPersistedAgentSamples",
                    instructions: "You are good at telling jokes.");

                // Retrieve the agent that was just created as an AIAgent using its ID
                agent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);
            }

            Microsoft.Agents.AI.AgentThread thread = agent!.GetNewThread();

            ChatMessage systemMessage = new(
                 ChatRole.System,
                 """
                 Follow instructions carefully.                
                 """);

            Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Calculate the sum of 100 and 200.")]));
            Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Add 7 to the result.")]));
            Console.WriteLine();
            Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Calculate the sum of 100 and 200.")], thread));
            Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Add 7 to the result.")], thread));

            var res = await agent.RunAsync([systemMessage, new(ChatRole.User, "Create a bulk chart with known numbers.")], thread);
            Console.WriteLine(res);
          
        }
    }
}
