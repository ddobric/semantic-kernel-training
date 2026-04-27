using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Agents.AI.Hosting;

namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    internal class WorkflowAsAgent
    {
        /*
        public static async Task RunAsync()
        {
            // Set up the Azure OpenAI client
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Create agents
            AIAgent frenchAgent = GetTranslationAgent("French", chatClient);
            AIAgent spanishAgent = GetTranslationAgent("Spanish", chatClient);
            AIAgent englishAgent = GetTranslationAgent("English", chatClient);

            var builder = WebApplication.CreateBuilder(args);
            // Build the workflow by adding executors and connecting them
            var builder = new WorkflowBuilder(frenchAgent)
                .AddEdge(frenchAgent, spanishAgent)
                .AddEdge(spanishAgent, englishAgent);
         
            var workflowAsAgent = builder
                .AddWorkflow()
                .AddAsAIAgent();  // Now the workflow can be used as an agent
          
        }

        /// <summary>
        /// Creates a translation agent for the specified target language.
        /// </summary>
        /// <param name="targetLanguage">The target language for translation</param>
        /// <param name="chatClient">The chat client to use for the agent</param>
        /// <returns>A ChatClientAgent configured for the specified language</returns>
        private static ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient) =>
            new(chatClient, 
                $"You are a translation assistant that translates the provided text to {targetLanguage}.");
    }
        */
    }
}

