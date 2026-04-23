using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFoundrySkAgent
{
    /// <summary>
    /// Semantic Kernel plugin used by <see cref="SemanticKernelAgent"/>.
    /// Exposes a weather tool as a KernelFunction that the SK agent can invoke.
    /// </summary>
    public class MyPlugin
    {
        [KernelFunction, Description("Gets the weater conditions.")]
        public string GetWeather(
            [Description("The city")]string? city, 
            [Description("the room name in the city")]string? room = null)
        {
            if (room != null && room!.ToLower().StartsWith("stage3"))
                return "hot";
            else
                return "35";
        }
    }
}
