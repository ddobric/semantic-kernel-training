using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;


namespace AzureFoundrySkAgent
{
    internal class AgentFramework_AzOpenAISamples
    {
        private const string _cModelDeploymentName = "gpt-4o-mini";

        public static async Task RunOpenAIBasicAsync()
        {
            var client = new AzureOpenAIClient(new Uri(Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!),
               new DefaultAzureCredential());

            var chatClient = client.GetChatClient(_cModelDeploymentName);             

            var agent = chatClient.CreateAIAgent(instructions: "You are good at telling jokes.", name: "JokerOld");
            ChatMessage systemMessage = new(
                ChatRole.System,
                """
                    If the user asks you to tell a joke, tell the joke.
                    """);

            ChatMessage userMessage = new(ChatRole.User, "Tell me a joke about a croatia.");

            Console.WriteLine(await agent.RunAsync([systemMessage, userMessage]));
        }

        public static async Task RunWithToolsFuncAsync()
        {
            var client = new AzureOpenAIClient(new Uri(Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!),
               new DefaultAzureCredential());

            var chatClient = client.GetChatClient(_cModelDeploymentName);

            var agent = chatClient.CreateAIAgent(instructions: "You are good at telling jokes.", name: "Joker",
                 tools: [AIFunctionFactory.Create(GetWeather)]);

            ChatMessage systemMessage = new(
                ChatRole.System,
                """
                    If the user asks you to tell a joke, refuse to do so, explaining that you are not a clown.
                    Offer the user an interesting fact instead.
                    """);

            ChatMessage userMessage = new(ChatRole.User, "Wie ist das Wetter in Frankfurt?");

            Console.WriteLine(await agent.RunAsync([systemMessage, userMessage]));
        }


        protected static async Task RunConversationLoopAsync(AIAgent agent)
        {
            Microsoft.Agents.AI.AgentThread thread = agent!.GetNewThread();

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    await foreach (var update in agent.RunStreamingAsync(userInput))
                    {
                        Console.Write(update);
                    }
                }
                finally
                {

                }
            }
        }

        public static async Task RunOpenAIAgentStreamedAsync()
        {
            var client = new AzureOpenAIClient(new Uri(Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!), 
                new DefaultAzureCredential());

            var chatClient = client.GetChatClient(_cModelDeploymentName);

            var agent = chatClient.CreateAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
            ChatMessage systemMessage = new(
                ChatRole.System,
                """
                    If the user asks you to tell a joke, refuse to do so, explaining that you are not a clown.
                    Offer the user an interesting fact instead.
                    """);

            ChatMessage userMessage = new(ChatRole.User, "Tell me a joke about a pirate.");

            await RunConversationLoopAsync(agent);
            //await foreach (var update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
            //{
            //    Console.Write(update);
            //}
        }


        [Description("Get the weather for a given location.")]
        public static string GetWeather(
            [Description("The city")] string? city,
            [Description("the room name in the city")] string? room = null)
        {
            if (room != null && room!.ToLower().StartsWith("stage3"))
                return "hot";
            else
                return "35";
        }

        // Moved to AgentFrameworkPersistedAgentSamples
        //public static async Task RunOpenAIAgentWithStateAsync()
        //{
        //    var client = new AzureOpenAIClient(new Uri(Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!),
        //        new DefaultAzureCredential());

        //    var chatClient = client.GetChatClient(_cModelDeploymentName);

        //    var agent = chatClient.CreateAIAgent(instructions: "You are a calculator.", name: "Mathguru");

        //    AgentThread thread = agent.GetNewThread();

        //    ChatMessage systemMessage = new(
        //        ChatRole.System,
        //        """
        //            If the user asks you to tell a joke, refuse to do so, explaining that you are not a clown.
        //            Offer the user an interesting fact instead.
        //            """);

        //    Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Calculate the sum of 100 and 200.")]));
        //    Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Add 7 to the result.")]));
        //    Console.WriteLine();
        //    Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Calculate the sum of 100 and 200.")], thread));
        //    Console.WriteLine(await agent.RunAsync([systemMessage, new(ChatRole.User, "Add 7 to the result.")], thread));

        //}

    }
}
