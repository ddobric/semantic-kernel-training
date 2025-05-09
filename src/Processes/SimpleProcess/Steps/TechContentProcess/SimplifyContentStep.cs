using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SimpleProcess.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ProzessFrameworkSamples.TechContentProcess;


#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ProzessFrameworkSamples.Steps.TechContentProcess
{
    public class SimplifyContentStep : KernelProcessStep<StepProcessState>
    {
        private StepProcessState? _state;

        private string _sysPrompt = 
            @"You are the agent that is able to simplify the provided highly complex technical text. 
If the content is HTML or JSON encoded, try to extract the text only.";

        public override ValueTask ActivateAsync(KernelProcessStepState<StepProcessState> state)
        {
            _state = state.State;

            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async Task<string?> SimplifyAsync(Kernel kernel, KernelProcessStepContext context, string complexContent)
        {
            
            ChatHistory chatHistory = new ChatHistory(_sysPrompt);
            chatHistory.AddUserMessage(complexContent);

            // Use structured output to ensure the response format is easily parsable
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings()
            {
                
            };

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var simplified = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings: settings);

            await context.EmitEventAsync(TechContentProcessEvents.ContentSimplifiedEvent, data: simplified.Content, visibility: KernelProcessEventVisibility.Internal);

            _state!.Content = simplified.Content;

            return simplified.Content;
        }
    }
}

#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
