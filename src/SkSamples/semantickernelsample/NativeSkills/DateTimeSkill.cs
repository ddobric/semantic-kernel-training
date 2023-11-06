using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.Skills
{
    public  class DateTimeSkill
    {
        [SKFunction("Gets the current time.")]
        public string Now()
        { 
            return DateTime.Now.ToString();
        }

        [SKFunction("Gets the day of today")]
        public string DayOfWeek(string input, SKContext context)
        {
            return Enum.GetName(DateTime.Now.DayOfWeek)!;
        }

        [SKFunction("Gets the day of today")]
        public string Today(string input, SKContext context)
        {
            return DateTime.Now.ToString("MMM/dd")!;
        }
    }
}
