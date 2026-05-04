using A2A;
using Microsoft.Agents.AI;


namespace A2AConsumerApp
{
    /// <summary>
    /// Demonstrates consuming a remote AI agent over the A2A (Agent-to-Agent) protocol.
    /// <para>
    /// The workflow follows the standard A2A discovery pattern:
    /// 1. Point an <see cref="A2ACardResolver"/> at the remote host.
    /// 2. The resolver fetches the Agent Card from <c>/.well-known/agent.json</c>.
    /// 3. The card describes the agent's capabilities and endpoint URL.
    /// 4. A local <see cref="AIAgent"/> proxy is created that forwards calls
    ///    to the remote A2A endpoint transparently.
    /// </para>
    /// </summary>
    internal class ConsumerSamples
    {
        /// <summary>
        /// Discovers the remote weather agent and sends a sample prompt.
        /// </summary>
        public static async Task RunAsync()
        {
            // 1. Initialize the A2A card resolver.
            //    The resolver will fetch the Agent Card from http://localhost:5000/.well-known/agent.json.
            //    The Agent Card contains the protocol binding (HTTP+JSON), endpoint URL, and metadata.
            A2ACardResolver resolver = new(new Uri("http://localhost:5000/"));

            // 2. Resolve the Agent Card and create a local AIAgent proxy.
            //    All subsequent calls on this proxy are transparently forwarded
            //    to the remote A2A server over the HTTP+JSON protocol binding.
            AIAgent agent = await resolver.GetAIAgentAsync();

            // 3. Interact with the remote agent – each RunAsync call sends an A2A
            //    "tasks/send" message and returns the agent's text response.
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
