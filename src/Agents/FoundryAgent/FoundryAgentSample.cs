using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace FoundryAgentDemo
{
    internal class FoundryAgentSample
    {
        public async Task RunCreateAgentInFoundryAsync()
        {
            Helper.GetAzureEndpointAndModelDeployment(out var projectEndpoint, out var deploymentName);

            var aiProjectClient = new AIProjectClient(
                new Uri(projectEndpoint),
                new DefaultAzureCredential());

            ProjectsAgentVersion agentVersion = await aiProjectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                        nameof(FoundryAgentSample.RunCreateAgentInFoundryAsync),
                        new ProjectsAgentVersionCreationOptions(
                            new DeclarativeAgentDefinition(model: deploymentName)
                            {
                                Instructions = "You are good at creating analytics."
                            }));

            // Wrap the agent version as a FoundryAgent using the AsAIAgent extension.
            FoundryAgent agent = aiProjectClient.AsAIAgent(agentVersion);

            Console.WriteLine(await agent.RunAsync("Tell me some analytics."));

        }

        public async Task RunCreateMultiturnAgentInFoundryAsync()
        {
            Helper.GetAzureEndpointAndModelDeployment(out var projectEndpoint, out var deploymentName);

            var aiProjectClient = new AIProjectClient(
                new Uri(projectEndpoint),
                new DefaultAzureCredential());

            ProjectsAgentVersion agentVersion = await aiProjectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                        nameof(FoundryAgentSample.RunCreateMultiturnAgentInFoundryAsync),
                        new ProjectsAgentVersionCreationOptions(
                            new DeclarativeAgentDefinition(model: deploymentName)
                            {
                                Instructions = "You are good in calculating."
                            }));

            // Wrap the agent version as a FoundryAgent using the AsAIAgent extension.
            FoundryAgent agent = aiProjectClient.AsAIAgent(agentVersion);

            // Create a session to maintain context across multiple runs.
            AgentSession session = await agent.CreateSessionAsync();

            // First turn
            Console.WriteLine(await agent.RunAsync("What is 1 plus two.", session));

            // Second turn — the agent remembers the first turn via the session.
            Console.WriteLine(await agent.RunAsync("Now add 7.", session));
        }
    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
