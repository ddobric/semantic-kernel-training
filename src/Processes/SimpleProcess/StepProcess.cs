using Microsoft.SemanticKernel;
using SimpleProcess.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SimpleProcess
{
    public class StepProcesses
    {
        public static class ProcessEvents
        {
            public const string StartProcess = nameof(StartProcess);
        }

        /// <summary>
        /// Demonstrates the creation of the simplest possible process with multiple steps
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>

        public async Task RunAsync(Kernel? kernel = null)
        {
            // Create a simple kernel 
            if (kernel == null)
                kernel = Kernel.CreateBuilder().Build();

            // Create a process that will interact with the chat completion service
            ProcessBuilder process = new(nameof(StepProcesses));

            var step1 = process.AddStepFromType<StatelessStep1>();
            var step2 = process.AddStepFromType<StatelessStep2>();
            var step3 = process.AddStepFromType<StatefullStep1>();

            // Define the process flow
            process
                .OnInputEvent(ProcessEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(step1));

            step1
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(step2));

            step2
             .OnFunctionResult()
              .SendEventTo(new ProcessFunctionTargetBuilder(step3));

            step3
                .OnFunctionResult()
                .StopProcess();

            // Build the process to get a handle that can be started
            KernelProcess kernelProcess = process.Build();

            // Start the process with an initial external event
            await using var runningProcess = await kernelProcess.StartAsync(
                kernel,
                    new KernelProcessEvent()
                    {
                        Id = ProcessEvents.StartProcess,
                        Data = "My Data"
                    });
        }
    }
}
#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

