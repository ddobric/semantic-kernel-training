using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ProzessFrameworkSamples.Steps.StepProcess
{
    public sealed class StatelessStep1 : KernelProcessStep
    {
        public override ValueTask ActivateAsync(KernelProcessStepState state)
        {
            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async Task<string> ExecuteAsync(KernelProcessStepContext context, string previousStepResult)
        {
            Console.WriteLine("StatelessStep 1 - Start\n");
            Console.WriteLine($"Result from Previous step {previousStepResult}");
            await Task.Delay(1000);

            return "From 1";
        }
    }
}
#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
