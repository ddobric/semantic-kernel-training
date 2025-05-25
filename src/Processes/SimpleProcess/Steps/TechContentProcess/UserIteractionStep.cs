using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ProzessFrameworkSamples.Plugins;
using SimpleProcess.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ProzessFrameworkSamples.TechContentProcess;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ProzessFrameworkSamples.Steps.TechContentProcess
{
    public class UserIteractionStep :   KernelProcessStep<StepProcessState>
    {
        private string _sysPrompt = "You are the agent executing user's commands.";

        private StepProcessState? _state;

        public override ValueTask ActivateAsync(KernelProcessStepState<StepProcessState> state)
        {
            _state = state.State;

            return base.ActivateAsync(state);
        }

        //[KernelFunction("StartConversationAsync")]
        //public async Task<string> StartConversationAsync(Kernel kernel, KernelProcessStepContext context, string instruction)
        //{

        //    var clr = Console.ForegroundColor;

        //    StringBuilder result = new StringBuilder();

        //    ChatHistory chatHistory = new ChatHistory(_sysPrompt);

        //    while (true)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Cyan;
        //        Console.WriteLine("Please enter the source of the text you want me to simplify.");
        //        Console.ForegroundColor = ConsoleColor.Yellow;

        //        var intent = Console.ReadLine();
        //        if (string.IsNullOrEmpty(intent))
        //            continue;

        //        if (intent.Equals("exit", StringComparison.OrdinalIgnoreCase))
        //        {
        //            await context.EmitEventAsync(TechContentProcessEvents.UserExitRequestEvent, data: "exit", visibility: KernelProcessEventVisibility.Internal);

        //            break;
        //        }

        //        chatHistory.AddUserMessage(intent);

        //        // Use structured output to ensure the response format is easily parsable
        //        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
        //        {
        //            Temperature = 0.1,
        //            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        //        };

        //        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        //        //var response = await chatCompletionService.GetChatMessageContentsAsync(
        //        // chatHistory,
        //        // executionSettings: settings,
        //        // kernel: kernel);

        //        var stream = chatCompletionService.GetStreamingChatMessageContentsAsync(
        //            chatHistory,
        //            executionSettings: settings,
        //            kernel: kernel);

        //        await foreach (var message in stream)
        //        {
        //            if (message.Content?.Length > 0)
        //            {
        //                result.AppendLine(message.Content);
        //                Console.Write(message.Content);
        //            }
        //            else if (message.Items.Count > 0)
        //            {
        //                foreach (var item in message.Items)
        //                {
        //                    Console.Write(".");
        //                }
        //            }
        //        }


        //        //Console.WriteLine(response.ToString());

        //        Console.ForegroundColor = clr;

        //        // Console.WriteLine($"Result from Previous step {instruction}");

        //    }

        //    return result.ToString();
        //}

        [KernelFunction("StartConversationAsync")]
        public async Task<string> StartConversationAsync(Kernel kernel, KernelProcessStepContext context, string instruction)
        {
            var clr = Console.ForegroundColor;

            string? result = null;

            while (true)
            {

                string intent;

                if (!String.IsNullOrEmpty(instruction))
                {
                    Console.WriteLine($"Where to save the simplified text?");

                    intent = Console.ReadLine();
                    var result1 = await kernel.InvokeAsync(nameof(SkPlugIn), nameof(SkPlugIn.SaveTextAsync),
                        new KernelArguments { ["file"] = intent, ["text"] = instruction });
                    break;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Please enter the source of the text you want me to simplify.");
                Console.ForegroundColor = ConsoleColor.Yellow;


                intent = Console.ReadLine();
                if (string.IsNullOrEmpty(intent))
                    continue;

                if (intent.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    await context.EmitEventAsync(TechContentProcessEvents.UserExitRequestEvent, data: "exit", visibility: KernelProcessEventVisibility.Internal);

                    break;
                }

                try
                {
                    // Enable auto function calling
                    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                    {
                        Temperature = 0.0,
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    };

                    if (IsLocalFilePath(intent) && String.IsNullOrEmpty(instruction))
                    {
                        var result1 = await kernel.InvokeAsync(nameof(SkPlugIn), nameof(SkPlugIn.LoadFileAsync),
                          new KernelArguments { ["url"] = intent, });
                    }
                    else if (IsWebUrl(intent) && String.IsNullOrEmpty(instruction))
                    {
                        result = await kernel.InvokeAsync<string>(nameof(SkPlugIn), nameof(SkPlugIn.LoadWebContentAsync),
                           new KernelArguments { ["url"] = intent, });

                        _state.State = result;

                        //Console.WriteLine($"Reading from web is not supported.");
                    }
                    else
                    {
                        result = intent;
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    continue;
                }

                Console.ForegroundColor = clr;

            }

            return result;
        }

        private bool IsLocalFilePath(string input)
        {
            var regex = new Regex(@"^[a-zA-Z]:\\(?:[^\\\/:*?""<>|\r\n]+\\)*[^\\\/:*?""<>|\r\n]+\.\w+$");
            return regex.IsMatch(input);
        }

        private bool IsWebUrl(string input)
        {
            var regex = new Regex(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+([\/\w\-._~:?#[\]@!$&'()*+,;=]*)?$", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
    }
}

#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

