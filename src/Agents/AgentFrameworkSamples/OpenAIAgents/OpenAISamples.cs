using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System.ClientModel;
using System.ComponentModel;
using System.Reflection;



namespace AgentFramework_Samples.OpenAIAgents
{
    /// <summary>
    /// Demonstrates the Agent Framework with direct OpenAI API access (non-Azure).
    /// Covers basic chat, function tools, and streaming.
    /// Requires environment variable: OPENAI_API_KEY (and AgentFrameworkOpenAIEndpointUrl for Azure variants).
    /// </summary>
    internal class OpenAISamples
    {
        private static void GetModelAndKey(out string apiKey, out string model)
        {
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL_NAME") ?? "gpt-5.4-mini";
        }

        /// <summary>
        /// Creates two agents – one via raw OpenAI key, one via AzureOpenAIClient –
        /// and runs a simple prompt against each to show both auth paths.
        /// </summary>
        public static async Task RunResponsesClientAsync()
        {
            GetModelAndKey(out var apiKey, out var model);

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AIAgent agent =
                new ResponsesClient(new ApiKeyCredential(apiKey))
                .AsAIAgent(model: model, instructions: "You are good at telling jokes.", name: "Joker");
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            AgentResponse agentRes = await agent.RunAsync("Tell me a joke about politic.");

            Console.WriteLine(agentRes);

            ChatMessage systemMessage = ChatMessage.CreateSystemMessage($"Today is {DateTime.Now}");
            ChatCompletion completionRes = await agent.RunAsync([systemMessage, ChatMessage.CreateUserMessage("Tell me a joke about a politic.")]);

            Console.WriteLine(completionRes.Content);
        }


        public static async Task RunChatClientAsync()
        {
            GetModelAndKey(out var apiKey, out var model);

            AIAgent agent = new OpenAIClient(apiKey)
            .GetChatClient(model)
            .AsAIAgent("You are good at telling jokes.", "JokerAgent");

            AgentResponse agentRes = await agent.RunAsync("Tell me a joke about a politic.");
            Console.WriteLine(agentRes);

            ChatMessage systemMessage = ChatMessage.CreateSystemMessage("If the user asks you to tell a joke, tell the joke.");
            ChatCompletion completionRes = await agent.RunAsync([systemMessage, ChatMessage.CreateUserMessage("Tell me a joke about a politic.")]);

            Console.WriteLine(completionRes.Content.First().Text);
        }

        public static async Task RunConversationAsync()
        {
            GetModelAndKey(out var apiKey, out var model);

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AIAgent agent =
                new ResponsesClient(new ApiKeyCredential(apiKey))
                .AsAIAgent(model: model, instructions: "You are good at telling jokes.", name: "Joker", tools: [Microsoft.Extensions.AI.AIFunctionFactory.Create(GetWeather)]);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            await ConsoleHelper.RunConversationLoopAsync(agent);
        }


        [Description("Get the weather for a given location.")]
        public static string GetWeather(
            [Description("The city")] string? city)
        {
            return city?.ToLower() switch
            {
                "frankfurt" => "22°C, partly cloudy",
                "berlin" => "18°C, rainy",
                "munich" => "25°C, sunny",
                "sarajevo" => "15°C, windy",
                "palma" => "28°C, humid",
                _ => "20°C, clear skies"
            };
        }
    }
}
