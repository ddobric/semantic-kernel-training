using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpServer.Console
{
    internal class Tools
    {
        [KernelFunction, Description("Retrieves the current date time in UTC.")]
        public static string GetCurrentDateTimeInUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        [KernelFunction, Description("Gets the current weather for the specified city and specified date time.")]
        public static string GetWeatherForCity(string cityName, string currentDateTimeInUtc)
        {
            return cityName switch
            {
                "Frankfurt" => "61 and rainy",
                "Sarajevo" => "55 and cloudy",
                "London" => "55 and cloudy",
                "Paris" => "55 and cloudy",
            };
        }
    }
}
