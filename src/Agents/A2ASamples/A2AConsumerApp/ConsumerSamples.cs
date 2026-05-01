using A2A;
using Microsoft.Agents.AI;


namespace A2AConsumerApp
{
    /// <summary>
    /// Demonstrates consuming a remote AI agent over the A2A protocol.
    /// The class resolves the agent's card, creates a local proxy, and invokes it.
    /// </summary>
    internal class ConsumerSamples
    {
        /// <summary>
        /// Discovers the remote weather agent and sends a sample prompt.
        /// </summary>
        public static async Task RunAsync()
        {
            // Initialize a resolver pointing at the remote agent's host.
            // The resolver fetches the well-known agent card from /.well-known/agent.json.
            A2ACardResolver resolver = new(new Uri("http://localhost:5000/"));

            // Resolve the agent card and create a local AIAgent proxy backed by the remote A2A endpoint.
            AIAgent agent = await resolver.GetAIAgentAsync();

            // Send a message to the remote agent and print its response.
            Console.WriteLine(await agent.RunAsync("Hello!"));

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();

            Console.WriteLine(await agent.RunAsync("For which cities you can provide me a weather?"));
           
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();

            Console.WriteLine(await agent.RunAsync("Provide a weather for sarajevo and palma?"));

        }
    }
}
