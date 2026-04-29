using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AgentFramework_Samples.Providers.FoundryAgents
{
    internal class ResponsesAgent
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            AIAgent agent = new AIProjectClient(
            new Uri(endpoint),
            new DefaultAzureCredential())
                .AsAIAgent(
                    model: deploymentName,
                    name: "Joker",
                    instructions: "You are good at telling jokes.");

            Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

        }

    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

