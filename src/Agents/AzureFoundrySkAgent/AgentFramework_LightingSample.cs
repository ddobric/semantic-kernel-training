using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ComponentModel;

namespace AzureFoundrySkAgent
{
    internal class AgentFramework_LightingSample
    {
        public static async Task RunAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!;
            var deploymentName =  "gpt-4o";

            // Create a service collection to hold the agent plugin and its dependencies.
            ServiceCollection services = new();
            services.AddSingleton<LightPlugin>();
         
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureCliCredential())
                .GetChatClient(deploymentName)
                .CreateAIAgent(
                    instructions: "You are a helpful assistant that helps people find information.",
                    name: "Assistant",
                    tools: [.. serviceProvider.GetRequiredService<LightPlugin>().AsAITools()],
                    services: serviceProvider); // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.

            await RunConversationLoopAsync(agent);
        }

        private static async Task RunConversationLoopAsync(AIAgent agent)
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
    }

    internal class LightPlugin
    {
        public bool IsOn { get; set; } = false;
      
        //[KernelFunction]
        [Description("Gets the state of the light.")]
        public string GetState() => IsOn ? "on" : "off";

        //[KernelFunction]
        [Description("Changes the state of the light.'")]
        public string ChangeState(bool newState)
        {
            this.IsOn = newState;
            var state = GetState();

            PaintBox(newState);

            // Print the state to the console
            Console.WriteLine($"[Light is now {state}]");

            return state;
        }

        //[KernelFunction]
        [Description("Invoked for any intent to repair the car. For the given service and the cpdm product k-type specified by user, it locates the product information inside Herth-Bush CPDM system and looksup the product information.")]
        public Task<string> LookupProduct(
          [Description("The name of the service, for which the user is interested.")] string serviceName,
          [Description("The name of the product inside CPDM. Also called k-type")] string productName,
          [Description("The user's ask or intent")] string intent)
        {
            return Task.FromResult<string>("VW Bremse. https://cpdmurl.com/bremse/77");
        }


        protected static void PaintBox(bool onoff)
        {
            // Define box dimensions
            int boxWidth = 2;
            int boxHeight = 2;

            // Define the color of the box
            ConsoleColor boxColor = onoff ? ConsoleColor.Green : ConsoleColor.Gray;

            // Get the dimensions of the console window
            int consoleWidth = Console.WindowWidth;
            int consoleHeight = Console.WindowHeight;

            // Calculate the top-right position for the box
            int startX = consoleWidth - boxWidth;
            int startY = 0;

            var pos = Console.GetCursorPosition();

            // Draw the box
            Console.ForegroundColor = boxColor;

            for (int y = startY; y < startY + boxHeight; y++)
            {
                Console.SetCursorPosition(startX, y);
                Console.Write(new string('█', boxWidth)); // Use █ for a solid block
            }

            // Reset console color
            Console.ResetColor();

            Console.SetCursorPosition(pos.Left, pos.Top);

        }


        /// <summary>
        /// Returns the functions provided by this plugin.
        /// </summary>
        /// <remarks>
        /// In real world scenarios, a class may have many methods and only a subset of them may be intended to be exposed as AI functions.
        /// This method demonstrates how to explicitly specify which methods should be exposed to the AI agent.
        /// </remarks>
        /// <returns>The functions provided by this plugin.</returns>
        public IEnumerable<AITool> AsAITools()
        {
            yield return AIFunctionFactory.Create(this.ChangeState);
            yield return AIFunctionFactory.Create(this.GetState);
        }
    }
}
