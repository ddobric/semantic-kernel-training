using AgentFramework_Samples;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;

namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    /// <summary>
    /// IoT-style demo: an AI agent controls a virtual light through function-calling tools.
    /// Demonstrates dependency injection (DI) with the Agent Framework – the LightPlugin
    /// is registered in a ServiceCollection and resolved at runtime.
    /// Requires environment variable: AgentFrameworkOpenAIEndpointUrl
    /// </summary>
    internal class LightingSample
    {
        /// <summary>
        /// Builds the DI container, registers LightPlugin, creates the agent,
        /// and starts the interactive conversation loop.
        /// </summary>
        public static async Task RunAsync()
        {
            //var endpoint = Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!;
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Create a service collection to hold the agent plugin and its dependencies.
            ServiceCollection services = new();
            services.AddSingleton<LightPlugin>();
         
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureCliCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(
                    instructions: "You are a helpful assistant that helps people find information.",
                    name: "Assistant",
                    tools: [.. serviceProvider.GetRequiredService<LightPlugin>().AsAITools()],
                    services: serviceProvider); // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.

            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Builds the DI container, registers LightPlugin, creates the agent,
        /// and starts the interactive conversation loop.
        /// </summary>
        public static async Task RunWithOllamaAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "127.0.0.1:11434";
            var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "gpt-oss:latest";

            // Create a service collection to hold the agent plugin and its dependencies.
            ServiceCollection services = new();
            services.AddSingleton<LightPlugin>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            AIAgent agent = new OllamaApiClient(new Uri(endpoint), modelName)
                .AsAIAgent(
                    instructions: "You are a helpful assistant that helps people find information.",
                    name: "Assistant",
                    tools: [.. serviceProvider.GetRequiredService<LightPlugin>().AsAITools()],
                    services: serviceProvider); // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.
                      
            await Helpers.RunConversationLoopAsync(agent);
        }


        internal static void SetConsoleColor()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
    }

    /// <summary>
    /// Plugin that simulates an IoT light with on/off state.
    /// Exposes GetState and ChangeState as AI-callable tools, plus a product lookup demo.
    /// </summary>
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

            Console.ForegroundColor = ConsoleColor.Green;

            // Print the state to the console
            Console.WriteLine($"[:))) Light is now {state}]");

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
            // Save current cursor position to restore later.
            var pos = Console.GetCursorPosition();

            // Fixed position: top-right corner of the console.
            int consoleWidth = Console.WindowWidth;
            int bulbX = consoleWidth - 12;
            int bulbY = 0;

            ConsoleColor bulbColor = onoff ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
            ConsoleColor baseColor = ConsoleColor.Gray;
            string status = onoff ? " ON " : " OFF";

            Console.ForegroundColor = bulbColor;

            // Row 0: top of bulb
            Console.SetCursorPosition(bulbX, bulbY);
            Console.Write("  .----.  ");

            // Row 1-2: bulb body
            Console.SetCursorPosition(bulbX, bulbY + 1);
            Console.Write(" /      \\ ");
            Console.SetCursorPosition(bulbX, bulbY + 2);
            Console.Write("|        |");

            // Row 3: lower bulb
            Console.SetCursorPosition(bulbX, bulbY + 3);
            Console.Write(" \\      / ");

            // Row 4: bulb base
            Console.ForegroundColor = baseColor;
            Console.SetCursorPosition(bulbX, bulbY + 4);
            Console.Write("  |====|  ");

            // Row 5: status label
            Console.ForegroundColor = onoff ? ConsoleColor.Green : ConsoleColor.Red;
            Console.SetCursorPosition(bulbX, bulbY + 5);
            Console.Write($"  |{status}|  ");

            // Row 6: base bottom
            Console.ForegroundColor = baseColor;
            Console.SetCursorPosition(bulbX, bulbY + 6);
            Console.Write("  '----'  ");

            // Draw rays if the light is on.
            if (onoff)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.SetCursorPosition(bulbX - 2, bulbY + 1);
                Console.Write("* ");
                Console.SetCursorPosition(bulbX + 10, bulbY + 1);
                Console.Write(" *");
                Console.SetCursorPosition(bulbX - 2, bulbY + 2);
                Console.Write("* ");
                Console.SetCursorPosition(bulbX + 10, bulbY + 2);
                Console.Write(" *");
                Console.SetCursorPosition(bulbX + 3, bulbY > 0 ? bulbY - 1 : bulbY);
                Console.Write("****");
            }
            else
            {
                // Clear ray positions when off.
                Console.SetCursorPosition(bulbX - 2, bulbY + 1);
                Console.Write("  ");
                Console.SetCursorPosition(bulbX + 10, bulbY + 1);
                Console.Write("  ");
                Console.SetCursorPosition(bulbX - 2, bulbY + 2);
                Console.Write("  ");
                Console.SetCursorPosition(bulbX + 10, bulbY + 2);
                Console.Write("  ");
                Console.SetCursorPosition(bulbX + 3, bulbY > 0 ? bulbY - 1 : bulbY);
                Console.Write("    ");
            }

            // Restore cursor and color.
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
