using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ProzessFrameworkSamples.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzessFrameworkSamples
{
    internal class ModelOrchestratorSample
    {

        public async Task RunAsync(Kernel kernel)
        {
            // Create chat history
            var history = new ChatHistory();

           // kernel.ImportPluginFromObject(new SkPlugIn(), "SkPlugin");
            kernel.ImportPluginFromObject(new StepPlugin1(), "StepPlugin");
            //kernel.ImportPluginFromObject(new StepPlugin2(), "StepPlugin");

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            string? userInput;

            while (true)
            {
                // Get user input again
                Console.Write("User > ");

                userInput = Console.ReadLine();

                if (userInput == null)
                    return;

                Console.ForegroundColor = ConsoleColor.White;
                
                // Add user input
                history.AddUserMessage(userInput);

                // Enable auto function calling
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    Temperature = 0.0,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                // Get the response from the AI
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: kernel);

                Console.ForegroundColor = ConsoleColor.Yellow;

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);           
            }
        }
    }
}
