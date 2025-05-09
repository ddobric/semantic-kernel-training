using Microsoft.SemanticKernel;
using SimpleProcess.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ProzessFrameworkSamples.Steps.StepProcess
{
    public sealed class StatefullStep1 : KernelProcessStep<StepProcessState>
    {
        private StepProcessState _state;

        public override ValueTask ActivateAsync(KernelProcessStepState<StepProcessState> state)
        {
            if (state.State == null)
            {

            }

            _state = state.State;

            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async Task<string> ExecuteAsync(Kernel kernel, KernelProcessStepContext context, string previousStepResult)
        {
            Console.WriteLine("Statefull Step 1 - Start\n");
            Console.WriteLine($"Result from Previous step {previousStepResult}");
            _state.StartedAt = DateTime.UtcNow;
            _state.State = "Statefull Step 1 entered";

            await Task.Delay(1000);

            _state.State = "Statefull Step 1 exit";
           
            return "From 2";
        }
    }
}
#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
