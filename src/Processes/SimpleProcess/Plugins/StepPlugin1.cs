using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzessFrameworkSamples.Plugins
{
    /// <summary>
    /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input "C".
    /// 
    /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input of the result of step 2.
    /// 
    /// execute the step1 with input "A", then the step2 with input "B" and then pass to step3 the result of step 2.
    ///
    /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input of the result of step 2 if the result of step1 is 1.
    /// </summary>
    internal class StepPlugin1
    {
        [KernelFunction(nameof(Step1))]
        [Description("Executes the step 1.")]
        public Task<int> Step1([Description("Input of step.")] string input)
        {
            Console.WriteLine($"\nStep 1 - input='{input}'\n");
            return Task.FromResult<int>(1);
        }

        [KernelFunction(nameof(Step2))]
        [Description("Executes the step 2.")]
        public Task<int> Step2([Description("The input for the step.")]string  input)
        {
            Console.WriteLine($"\nStep 2 - input='{input}'\n");
            return Task.FromResult<int>(2);
        }

        [KernelFunction(nameof(Step3))]
        [Description("Executes the step 3.")]
        public Task<int> Step3([Description("The input for the step.")] string input)
        {
            Console.WriteLine($"\nStep 3 - input='{input}'\n");
            return Task.FromResult<int>(3);
        }
    }
}
