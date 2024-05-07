using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace semantickernelsample.Skills
{
    public class MyDateTimePlugin
    {
        [KernelFunction, Description("Gets the current time.")]
        public string Now()
        {
            return DateTime.Now.ToString();
        }

        [KernelFunction, Description("Gets the UTC current time.")]
        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        [KernelFunction]
        [Description("Gets the day of today")]
        public string DayOfWeek()
        {
            //ExecutionContext context
            return Enum.GetName(DateTime.Now.DayOfWeek)!;
        }

        [KernelFunction]
        [Description("Get the current day")]
        public string Today()
        {
            return DateTime.Now.ToString("MMM/dd")!;
        }

        [KernelFunction]
        [Description("Get the current day on the planet")]
        public string TodayOnPlanet(string planet)
        {
            if (planet.ToLower() == "earth")
                return DateTime.Now.ToString("MMM/dd")!;
            else
                return DateTime.Now.AddMonths(222).ToString("MMM/dd")!;
        }
    }
}
