using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FoundryAgentDemo
{
    /// <summary>
    /// Demonstrates running an AI agent using the Responses API without creating a persistent agent in Azure Foundry.
    /// The agent is created in-memory via <see cref="AIProjectClientExtensions.AsAIAgent"/> and executes a single prompt
    /// with a custom tool (<see cref="Tools.GetProcessInfo"/>) for listing running processes.
    /// </summary>
    internal class FoundryResponsesAgentSample
    {
        /// <summary>
        /// Creates an in-memory AI agent with a process-info tool and runs a single prompt.
        /// </summary>
        public async Task RunAsync()
        {
            Helper.GetAzureEndpointAndModelDeployment(out var projectEndpoint, out var deploymentName);

            AIAgent agent = new AIProjectClient(
                 new Uri(projectEndpoint),
                         new DefaultAzureCredential())
                         .AsAIAgent(
                           model: deploymentName,
                           name:nameof(FoundryResponsesAgentSample),
                           instructions: "You are good at creating analytics.",
                           tools: [AIFunctionFactory.Create(Tools.GetProcessInfo)]);

            Console.WriteLine(await agent.RunAsync("List running processes and create some intersting analytics."));
        }

    }
}
