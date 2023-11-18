using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
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
        [SKFunction, Description("Gets the current time.")]
        public string Now()
        { 
            return DateTime.Now.ToString();
        }

        [SKFunction, Description("Gets the UTC current time.")]
        public string UtcNow()
        {
            return DateTime.UtcNow.ToString();
        }

        [SKFunction]
        [Description("Gets the day of today")]
        public string DayOfWeek(string input, SKContext context)
        {
            return Enum.GetName(DateTime.Now.DayOfWeek)!;
        }

        [SKFunction]
        [Description("Get the current day")]
        public string Today(string input, SKContext context)
        {
            return DateTime.Now.ToString("MMM/dd")!;
        }
    }
}
