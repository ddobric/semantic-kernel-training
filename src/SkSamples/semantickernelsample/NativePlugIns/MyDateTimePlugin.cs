using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.Skills
{
    public  class MyDateTimePlugin
    {
        [KernelFunction, Description("Gets the current time.")]
        public string Now()
        { 
            return DateTime.Now.ToString();
        }

        [KernelFunction, Description("Gets the UTC current time.")]
        public string UtcNow()
        {
            return DateTime.UtcNow.ToString();
        }

        [KernelFunction]
        [Description("Gets the day of today")]
        public string DayOfWeek(string input, ExecutionContext context)
        {
            return Enum.GetName(DateTime.Now.DayOfWeek)!;
        }

        [KernelFunction]
        [Description("Get the current day")]
        public string Today(string input, ExecutionContext context)
        {
            return DateTime.Now.ToString("MMM/dd")!;
        }
    }
}
