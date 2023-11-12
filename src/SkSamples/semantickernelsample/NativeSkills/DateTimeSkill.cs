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
    public  class DateTimeSkill
    {
        [SKFunction, Description("Gets the current time.")]
        public string Now()
        { 
            return DateTime.Now.ToString();
        }

        [SKFunction, Description("Gets the day of today")]
        public string DayOfWeek(string input, SKContext context)
        {
            return Enum.GetName(DateTime.Now.DayOfWeek)!;
        }

        [SKFunction, Description("Gets the day of today")]
        public string Today(string input, SKContext context)
        {
            return DateTime.Now.ToString("MMM/dd")!;
        }
    }
}
