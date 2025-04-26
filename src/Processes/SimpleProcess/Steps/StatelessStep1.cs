using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SimpleProcess.Steps
{
    public sealed class StatelessStep1 : KernelProcessStep
    {
        public override ValueTask ActivateAsync(KernelProcessStepState state)
        {
            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async Task<string> ExecuteAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("Step 1 - Start\n");

            await Task.Delay(1000);

            return "Step 1 Result";
        }
    }
}
#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
