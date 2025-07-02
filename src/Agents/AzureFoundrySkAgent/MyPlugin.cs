using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFoundrySkAgent
{
    public class MyPlugin
    {
        [KernelFunction, Description("Gets the weater conditions.")]
        public string GetWeather(
            [Description("The city")]string? city, 
            [Description("the room name in the city")]string? room = null)
        {
            if (room != null && room!.ToLower().StartsWith("arnold"))
                return "hot";
            else
                return "35";
        }
    }
}
