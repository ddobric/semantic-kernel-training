using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.Skills
{
    public class SamplePlugIn
    {
        private readonly IKernel? _kernel;

        public SamplePlugIn(IKernel? kernel = null)
        {
            _kernel = kernel;
        }

        [SKFunction, Description("Adds additional information to the model. Performs grounding.")]
        public string Enrich()
        {
            return "Also mentoin that november 6 is day the great Global Azure day when the community learn about Azure across the globe.";
        }


        [SKFunction, Description("State machine.")]
        public Task<SKContext> AddValue(SKContext context)
        {
            Debug.WriteLine(context.GetHashCode());

            if (!context.Variables.ContainsKey("mystate"))
                context.Variables.Set("mystate", "0");

            var state = context.Variables["mystate"];

            context.Variables.Set("mystate", "stopped!");

            return Task.FromResult<SKContext>(context);
        }


        [SKFunction, Description("State machine.")]
        public string GetValue(SKContext context)
        {
            if (!context.Variables.ContainsKey("mystate"))
                context.Variables.Set("mystate", "0");

            var state = context.Variables["mystate"];

            return state;
        }


        [SKFunction, Description("Does nothing. Demonstrates how to access the context.")]
        public string NullFunction(SKContext context)
        {
            return context.Result;
        }

        [SKFunction, Description("Adds two numbers.")]
        public string AddNumbersFunction(SKContext context, int arg1, int arg2)
        {
            return (arg1 + arg2).ToString();
        }


        [SKFunction, Description("Executes the function semantically extracted from prompt.")]
        public async Task<string> ExecuteMathOperationFunction(SKContext context, string prompt)
        {
            var mathOperatorExtractorFnc = this._kernel.Functions.GetFunction("SamplePlugin", "MathOperationExtractor");

            var res = await _kernel.RunAsync(prompt, mathOperatorExtractorFnc);

            var val = res.GetValue<string>();

            var tokens = val.Split('|');

            var mathOperator = tokens[0];

            var args = tokens[1].Split(',');

            var arg1 = int.Parse(args[0].Replace("\nArguments: ", String.Empty));
            var arg2 = int.Parse(args[1]);

            switch (mathOperator.Replace("Function: ", String.Empty))
            {
                case "+":
                    return (arg1 + arg2).ToString();

                case "exponent":
                    return (Math.Pow(arg1, arg2)).ToString();
                default:
                    return "unknown operator";
            }
        }

        [SKFunction, Description("Calculates the fiction function.")]
        public Task<string> FictionFunction(SKContext context,
            [Description("The first argument that describes some entity")] string input,
            [Description("The second argument that describes some entity")] string arg2,
            [Description("The the number that defines the contraction jumping of wurstchen units between entities used to calcukate the fiction.")] int number)
        {
            return Task.FromResult<string>($"{input}-{arg2}-{number.ToString()}");
        }
    }
}
