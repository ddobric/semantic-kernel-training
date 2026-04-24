using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    internal class AgentsInWorkflow
    {
        public static async Task RunAsync()
        {
            // Set up the Azure OpenAI client
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Create agents
            AIAgent frenchAgent = GetTranslationAgent("French", chatClient);
            AIAgent spanishAgent = GetTranslationAgent("Spanish", chatClient);
            AIAgent englishAgent = GetTranslationAgent("English", chatClient);

            // Build the workflow by adding executors and connecting them
            var workflow = new WorkflowBuilder(frenchAgent)
                .AddEdge(frenchAgent, spanishAgent)
                .AddEdge(spanishAgent, englishAgent)
                .Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, "Hello World!"));

            // Must send the turn token to trigger the agents.
            // The agents are wrapped as executors. When they receive messages,
            // they will cache the messages and only start processing when they receive a TurnToken.
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is AgentResponseUpdateEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
                else if (evt is WorkflowErrorEvent workflowError)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");
                    Console.ResetColor();
                }
                else if (evt is ExecutorFailedEvent executorFailed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data == null ? "unknown error" : $"exception {executorFailed.Data}")}.");
                    Console.ResetColor();
                }
            }
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
}

