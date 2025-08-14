using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample
{
    public class Sample_SemanticUseCaseSelector
    {
        private Kernel _kernel;

        private KernelPlugin _samplePlugin;

        public Sample_SemanticUseCaseSelector(Kernel kernel) {

            _kernel = kernel;
        }

        public async Task RunAsync()
        {
            ChatHistory history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
          
            _kernel.ImportPluginFromObject(this);

            var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

            // Import the OrchestratorPlugin from the plugins directory.
            _samplePlugin = _kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugin");

            string? userInput;

            while ((userInput = Console.ReadLine()) != null)
            {
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
                    kernel: _kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);

                // Get user input again
                Console.Write("User > ");
            }
        }

        [KernelFunction()]
        [Description("Performs the tranlation of the given text.")]
        public async Task<string> DoTranslation(
            [Description("The destination labguage. Text will be translated to this text.")]string toLanguage, 
            [Description("")]string text)
            {

            var translatedText = await _kernel.InvokeAsync(_samplePlugin["Translator"], 
                new() { ["input"] = text, ["language"] = toLanguage });

            return translatedText.ToString();
        
        }

        [KernelFunction()]
        [Description("Performs the classification.")]
        public Task DoClassification(
          [Description("The expecting class of the given text.")] string theClass,
          [Description("The text to be classified by the given class.")] string text)
        { return Task.CompletedTask; }
    }
}
