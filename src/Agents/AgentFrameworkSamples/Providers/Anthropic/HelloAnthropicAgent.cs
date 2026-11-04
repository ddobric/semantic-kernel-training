using Anthropic;
using Anthropic.Foundry;
using Azure.AI.OpenAI;
using Azure.Identity;
using HostedAgentsWithAzureFoundryModels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework_Samples.Providers.Anthropic
{
    internal class HelloAnthropicAgent
    {
        /// <summary>
        /// dotnet add Microsoft.Agents.AI.Anthropic
        /// dotnet add package Anthropic.Foundry --prerelease
        /// dotnet add package Azure.Identity
        /// </summary>
        /// <returns></returns>
        public static async Task RunHostedAtAnthropicAsync()
        {
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            var deploymentName = Environment.GetEnvironmentVariable("ANTHROPIC_DEPLOYMENT_NAME") ?? "claude-haiku-4-5";

            AnthropicClient client = new() { ApiKey = apiKey };

            AIAgent agent = client.AsAIAgent(
                model: deploymentName,
                name: "HelpfulAssistant",
                instructions: "You are a helpful assistant.");

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Hello, how can you help me?"));
        }

        /// <summary>
        /// Same sample as <see cref="RunHostedAtAnthropicAsync"/>, but using a model
        /// hosted in Azure AI Foundry (Azure OpenAI endpoint + deployment).
        /// Required env vars: AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_DEPLOYMENT_NAME.
        /// </summary>
        public static async Task RunFoundryAsync()
        {
            Helpers.GetAnthropicModelAndKey(out string apiKey, out string model, out string resourceName);

            //AnthropicClient client = new AnthropicFoundryClient(
            //     new AnthropicFoundryApiKeyCredentials(apiKey, "ddobric-agent-samples-resource"!));

            AnthropicClient client = new AnthropicFoundryClient(
                new AnthropicFoundryIdentityTokenCredentials(new AzureCliCredential(),
                resourceName!));

            AIAgent agent = client.AsAIAgent(
                model: "claude-sonnet-5",
                name: "HelpfulAssistant",
                instructions: "You are a helpful assistant.");

            Console.WriteLine(await agent.RunAsync("Hello, how can you help me?"));
        }
    }
}
