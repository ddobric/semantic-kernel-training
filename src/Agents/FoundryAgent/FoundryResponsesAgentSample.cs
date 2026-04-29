using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FoundryAgentDemo
{
    internal class FoundryResponsesAgentSample
    {
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
