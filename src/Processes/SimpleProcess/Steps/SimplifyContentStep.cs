using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SimpleProcess.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ProzessFrameworkSamples.Steps
{
    public class SimplifyContentStep : KernelProcessStep
    {
        public async Task<string> SimplifyAsyn(Kernel kernel, KernelProcessStepContext context, string complexContent)
        {
            ChatHistory chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(complexContent);

            // Use structured output to ensure the response format is easily parsable
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings();
            settings.ResponseFormat = typeof(ProofreadingResponse);

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var proofreadResponse = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings: settings);
            var formattedResponse = JsonSerializer.Deserialize<ProofreadingResponse>(proofreadResponse.Content!.ToString());

            return "";
        }
    }
}

#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
