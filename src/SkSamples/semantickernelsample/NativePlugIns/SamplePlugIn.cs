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
        [SKFunction, Description("Adds additional information to the model. Performs grounding.")]
        public string Enrich()
        {
            return "Also mentoin that novemer 6 is day the great Global Azure day when the community learn about Azure across the globe.";
        }


        //[SKFunction("Describe the function")]
        //[SKFunctionInput(Description = "DEscribes the function input")]
        [SKFunction, Description("State machine.")]
        //[SKFunctionContextParameter(Name = "mystate", Description = "Holds the state of the state machine.")]
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
    }
}
