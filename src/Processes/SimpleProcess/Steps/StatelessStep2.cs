using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SimpleProcess.Steps
{
    public sealed class StatelessStep2 : KernelProcessStep
    {
        public override ValueTask ActivateAsync(KernelProcessStepState state)
        {
            return base.ActivateAsync(state);
        }
        
        [KernelFunction]
        public Task<string> ExecuteAsync(KernelProcessStepContext context, string previousStepResult)
        {         
            Console.WriteLine("Step 2 - Start\n");
            Console.WriteLine($"Result from Previous step {previousStepResult}");
            return Task.FromResult<string>("Step 2 Result");
        }
    }
}
#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
