using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AzureAI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AgentFramework_Samples.Providers.FoundryAgents
{
    internal class HelloFoundryAgent
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

            //const string JokerName = "JokerAgent";

            //// Create the AIProjectClient to manage server-side agents.
            //AIProjectClient aiProjectClient = new(new Uri(endpoint), new AzureCliCredential());

            //// Create a server-side agent version using the native SDK.
            //ProjectsAgentVersion agentVersion = await aiProjectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            //    JokerName,
            //    new ProjectsAgentVersionCreationOptions(
            //        new DeclarativeAgentDefinition(model: deploymentName)
            //        {
            //            Instructions = "You are good at telling jokes.",
            //        }));

            //// Wrap the agent version as a FoundryAgent using the AsAIAgent extension.
            //FoundryAgent agent = aiProjectClient.AsAIAgent(agentVersion);

            //// Once you have the agent, you can invoke it like any other AIAgent.
            //Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

            //// Cleanup: deletes the agent and all its versions.
            //await aiProjectClient.AgentAdministrationClient.DeleteAgentAsync(agent.Name);
        }
    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

