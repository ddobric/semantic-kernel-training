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
    internal class HelloOllamaAgent
    {
        public static async Task RunAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "127.0.0.1:11434";
            var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "gpt-oss:latest";

            // Get a chat client for Ollama and use it to construct an AIAgent.
            AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName).
                AsAIAgent
                (instructions: "You are good at telling jokes.", name: "Joker");

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));
        }

        public static async Task RunWithToolsAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "127.0.0.1:11434";
            var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "gpt-oss:latest";

            // Get a chat client for Ollama and use it to construct an AIAgent.
            AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName).
                AsAIAgent
                (instructions: "You are good at telling jokes.", name: "Joker",
                 tools: [AIFunctionFactory.Create(GetProcessInfo)]);

            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Tool function: returns a formatted list of running processes.
        /// The [Description] attributes provide the agent with metadata to decide when and how to call it.
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

