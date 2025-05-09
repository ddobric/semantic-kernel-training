using Microsoft.SemanticKernel;
using SimpleProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SimpleProcess.StepProcesses;
using ProzessFrameworkSamples.Steps.StepProcess;
using ProzessFrameworkSamples.Steps.TechContentProcess;
using ProzessFrameworkSamples.Plugins;

namespace ProzessFrameworkSamples
{
#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public class TechContentProcess
    {
        public static class TechContentProcessEvents
        {
            public const string StartProcessEvent = nameof(StartProcessEvent);

            public const string ContentSimplifiedEvent = nameof(ContentSimplifiedEvent);

            public const string UserExitRequestEvent = nameof(UserExitRequestEvent);
        }

        public async Task RunAsync(Kernel? kernel = null)
        {
            // Create a simple kernel 
            if (kernel == null)
                kernel = Kernel.CreateBuilder().Build();

            kernel.ImportPluginFromObject(new SkPlugIn(), "SkPlugin");

            // Create a process that will interact with the chat completion service
            ProcessBuilder process = new(nameof(StepProcesses));

            var userStep = process.AddStepFromType<UserIteractionStep>();
            var simplifyContentStep = process.AddStepFromType<SimplifyContentStep>();
            var step3 = process.AddStepFromType<StatefullStep1>();

            // Define the process flow
            process
                .OnInputEvent(ProcessEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(userStep, 
                functionName: nameof(UserIteractionStep.StartConversationAsync)));

            process.
                OnEvent(TechContentProcessEvents.UserExitRequestEvent)
                .StopProcess();

            userStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(simplifyContentStep));

            simplifyContentStep
             .OnFunctionResult()
              .SendEventTo(new ProcessFunctionTargetBuilder(step3));

            simplifyContentStep
             .OnEvent(TechContentProcessEvents.ContentSimplifiedEvent)
             .SendEventTo(new ProcessFunctionTargetBuilder(userStep));

            step3
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(userStep,
                functionName: nameof(UserIteractionStep.StartConversationAsync)));

            // Build the process to get a handle that can be started
            KernelProcess kernelProcess = process.Build();

            // Start the process with an initial external event
            await using var runningProcess = await kernelProcess.StartAsync(
                kernel,
                    new KernelProcessEvent()
                    {
                        Id = ProcessEvents.StartProcess,
                        Data = ""
                    });
        }
    }

#pragma warning restore SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

}
