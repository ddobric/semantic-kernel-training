using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Orchestration;
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
        private readonly Kernel? _kernel;

        public SamplePlugIn(Kernel? kernel = null)
        {
            _kernel = kernel;
        }

        [KernelFunction, Description("Adds additional information to the model. Performs grounding.")]
        public string Enrich()
        {
            return "Also mentoin that november 6 is day the great Global Azure day when the community learn about Azure across the globe.";
        }


        //[KernelFunction, Description("State machine.")]
        //public Task<SKContext> AddValue()
        //{
        //    Debug.WriteLine(context.GetHashCode());

        //    if (!context.Variables.ContainsKey("mystate"))
        //        context.Variables.Set("mystate", "0");

        //    var state = context.Variables["mystate"];

        //    context.Variables.Set("mystate", "stopped!");

        //    return Task.FromResult<SKContext>(context);
        //}


        //[KernelFunction, Description("State machine.")]
        //public string GetValue()
        //{
        //    if (!context.Variables.ContainsKey("mystate"))
        //        context.Variables.Set("mystate", "0");

        //    var state = context.Variables["mystate"];

        //    return state;
        //}


        [KernelFunction, Description("Does nothing. Demonstrates how to access the context.")]
        public string NullFunction()
        {
            return null;
        }

        [KernelFunction, Description("Adds two numbers.")]
        public int AddNumbersFunction(int arg1, int arg2)
        {
            ExecutionContext context = ExecutionContext.Capture();

            return (arg1 + arg2);
        }


        [KernelFunction, Description("Executes the function semantically extracted from prompt.")]
        public async Task<string> ExecuteMathOperationFunction(string prompt)
        {
            var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

            var plugIn = this._kernel?.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugin");

            var res = await _kernel.InvokeAsync<string>(plugIn["MathOperationExtractor"], new() { ["input"] = prompt });

            var tokens = res.Split('|');

            var mathOperator = tokens[0];

            var args = tokens[1].Split(',');

            var arg1 = int.Parse(args[0].Replace("\nArguments: ", String.Empty));
            var arg2 = int.Parse(args[1]);

            switch (mathOperator.Replace("\r\nFunction: ", String.Empty).Trim())
            {
                case "+":
                    return (arg1 + arg2).ToString();

                case "exponent":
                    return (Math.Pow(arg1, arg2)).ToString();
                default:
                    return "unknown operator";
            }
        }

        [KernelFunction, Description("Calculates the fiction function.")]
        public Task<string> FictionFunction(
            [Description("The first argument that describes some entity")] string input,
            [Description("The second argument that describes some entity")] string arg2,
            [Description("The the number that defines the contraction jumping of sausages units between entities used to calculate the fiction.")] int number)
        {
            return Task.FromResult<string>($"{input}-{arg2}-{number.ToString()}");
        }


        [KernelFunction, Description("Berechnet Investment in die Technologie im Kontext eines Portfolios.")]
        public Task<string> WorkshopFunction(
            [Description("Investment Bereich, Sektor usw.")] string input,
            [Description("The second argument that describes the name of the customer")] string customer,
            [Description("Zeitraum")] TimeSpan timeRange)
        {
            return Task.FromResult<string>($"{input}-{timeRange}-{customer}");
        }

        [KernelFunction, Description("Books working hours in Employe service. User wants to commit wotking hours.")]
        public Task<string> EmployeeServiceBookHoursFunction(
        [Description("The first argument that specifies the project name for which the working hours will be booked.")] string project,
        [Description("Number of working hours to be booked on the project")] string workingHours,
        [Description("Number of working hours to be booked on the project")] string date)
        {
            return Task.FromResult<string>($"{project}-{workingHours}-{date}");
        }



      
    }
}
