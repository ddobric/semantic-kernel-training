using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.NativePlugIns
{
    internal class LightPlugin
    {
        public bool IsOn { get; set; } = false;

//#pragma warning disable CA1024 // Use properties where appropriate
        [KernelFunction]
        [Description("Gets the state of the light.")]
        public string GetState() => IsOn ? "on" : "off";
//#pragma warning restore CA1024 // Use properties where appropriate

        [KernelFunction]
        [Description("Changes the state of the light.'")]
        public string ChangeState(bool newState)
        {
            this.IsOn = newState;
            var state = GetState();

            // Print the state to the console
            Console.WriteLine($"[Light is now {state}]");

            return state;
        }

        [KernelFunction]
        [Description("Invoked for any intent to repair the car. For the given service and the cpdm product k-type specified by user, it locates the product information inside Herth-Bush CPDM system and looksup the product information.")]
        public Task<string> LookupProduct(
              [Description("The name of the service, for which the user is interested.")]  string serviceName,
              [Description("The name of the product inside CPDM. Also called k-type")] string productName,
              [Description("The user's ask or intent")] string intent)
        {
            return Task.FromResult<string>("VW Bremse. https://cpdmurl.com/bremse/77");
        }
    }
}
