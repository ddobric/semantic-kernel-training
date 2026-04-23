using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

//using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System.ClientModel;
using System.ComponentModel;
using System.Reflection;



namespace AgentFramework_Samples.OpenAIAgents
{
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    /// <summary>
    /// Demonstrates the Agent Framework with direct OpenAI API access (non-Azure).
    /// Covers basic chat, function tools, and streaming.
    /// Requires environment variable: OPENAI_API_KEY (and AgentFrameworkOpenAIEndpointUrl for Azure variants).
    /// </summary>
    internal class OpenAIReasoningSamples
    {
        private static void GetModelAndKey(out string apiKey, out string model)
        {
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL_NAME") ?? "gpt-5.4-mini";
        }

        public static async Task RunReasoningAsync()
        {
            GetModelAndKey(out var apiKey, out var model);

            var client = new OpenAIClient(apiKey)
            .GetResponsesClient()
            .AsIChatClient(model).AsBuilder()
            .ConfigureOptions(o =>
            {
                o.Reasoning = new()
                {
                    Effort = ReasoningEffort.Medium,
                    Output = ReasoningOutput.Full,
                };
            }).Build();

            AIAgent agent = new ChatClientAgent(client);

            Console.WriteLine("1. Non-streaming:");
            var response = await agent.RunAsync("Solve this problem step by step: If a train travels 60 miles per hour and needs to cover 180 miles, how long will the journey take? Show your reasoning.");

            Console.WriteLine(response.Text);

            Console.WriteLine("Token usage:");
            Console.WriteLine($"Input: {response.Usage?.InputTokenCount}, Output: {response.Usage?.OutputTokenCount}, {string.Join(", ", response.Usage?.AdditionalCounts ?? [])}");
            Console.WriteLine();
        }

        public static async Task RunReasoningWithStreamingAsync()
        {
            GetModelAndKey(out var apiKey, out var model);

            var client = new OpenAIClient(apiKey)
            .GetResponsesClient()
            .AsIChatClient(model).AsBuilder()
            .ConfigureOptions(o =>
            {
                o.Reasoning = new()
                {
                    Effort = ReasoningEffort.Medium,
                    Output = ReasoningOutput.Full,
                };
            }).Build();

            AIAgent agent = new ChatClientAgent(client);

        
            await foreach (var update in agent.RunStreamingAsync("Explain the theory of relativity in simple terms."))
            {
                foreach (var item in update.Contents)
                {
                    if (item is TextReasoningContent reasoningContent)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(reasoningContent.Text);
                        Console.ResetColor();
                    }
                    else if (item is TextContent textContent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(textContent.Text);
                        Console.ResetColor();
                    }
                }
            }
        }
    }
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

}
