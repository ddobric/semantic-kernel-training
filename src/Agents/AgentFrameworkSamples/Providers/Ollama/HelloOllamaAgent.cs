using AgentFramework_Samples.Providers.FoundryLocal;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AgentFramework_Samples.Providers.Ollama
{
    /// <summary>
    /// Demonstrates using the Agent Framework with Ollama as the model provider.
    /// Ollama runs models locally, so no cloud API key is needed — only a running Ollama instance.
    /// </summary>
    internal class HelloOllamaAgent
    {
        /// <summary>
        /// Scenario 1: Basic Agent — single prompt, non-streaming.
        /// Creates an AIAgent from the Ollama chat client and invokes it with a simple prompt.
        /// </summary>
        public static async Task RunAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "127.0.0.1:11434";
            var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "gpt-oss:latest";

            // OllamaApiClient implements IChatClient, so it can be wrapped as an AIAgent directly.
            AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName)
                .AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");

            // Non-streaming invocation — returns the full response at once.
            Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));
        }

        /// <summary>
        /// Scenario 2: Agent with Function Tools.
        /// Registers a local C# method (GetProcessInfo) as a tool the agent can call.
        /// When the user asks about processes, the agent autonomously invokes the tool
        /// and incorporates the result into its response.
        /// </summary>
        public static async Task RunWithToolsAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "127.0.0.1:11434";
            var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "gpt-oss:latest";

            // AIFunctionFactory.Create wraps the C# method as a tool the agent can invoke.
            AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName)
                .AsAIAgent(
                    instructions: "You are good at telling jokes.",
                    name: "Joker",
                    tools: [AIFunctionFactory.Create(GetProcessInfo)]);

            // Start an interactive conversation loop with streaming output.
            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Tool function: returns a formatted list of running processes.
        /// The [Description] attributes provide the agent with metadata
        /// to decide when and how to call it.
        /// </summary>
        [Description("Get the information about running processes.")]
        static string GetProcessInfo([Description("The location to get the weather for.")] string location)
        {
            StringBuilder sb = new StringBuilder();

            var processses = Process.GetProcesses();

            foreach (var process in processses)
            {
                sb.AppendLine($"{process.Id,8} | {process.ProcessName,-40} | Threads: {process.Threads.Count,4} | Memory: {process.WorkingSet64 / 1024.0 / 1024.0,8:F2} MB");
            }

            return sb.ToString();
        }
    }
}

