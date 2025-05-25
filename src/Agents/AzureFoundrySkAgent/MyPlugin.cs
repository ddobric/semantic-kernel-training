using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFoundrySkAgent
{
    public class MyPlugin
    {
        [KernelFunction]
        public string GetWeather()
        {
            return "hello";
        }
    }
}
