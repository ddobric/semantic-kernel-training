using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework_Samples.Providers.FoundryLocal
{
    internal class HelloFoundryLocalAgent
    {
        public static async Task RunChatClientAsync()
        {
            Helpers.GetModelAndKey(out var apiKey, out var model);

            AIAgent agent = new OpenAIClient(apiKey)
            .GetChatClient(model)
            .AsAIAgent("You are good at telling jokes.", "JokerAgent");

            AgentResponse agentRes = await agent.RunAsync("Tell me a joke about a politic.");
            Console.WriteLine(agentRes);

            ChatMessage systemMessage = ChatMessage.CreateSystemMessage("If the user asks you to tell a joke, tell the joke.");
            ChatCompletion completionRes = await agent.RunAsync([systemMessage, ChatMessage.CreateUserMessage("Tell me a joke about a politic.")]);

            Console.WriteLine(completionRes.Content.First().Text);
        }
    }
}
