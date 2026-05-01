namespace A2AConsumerApp
{
    /// <summary>
    /// Entry point for the A2A consumer application.
    /// Discovers a remote agent via its agent card and sends a request using the A2A protocol.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            PrintBootstrapMessage();

            // Discover the remote agent and send a sample request.
            await ConsumerSamples.RunAsync();

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadLine();
        }

        private static void PrintBootstrapMessage()
        {
            // Display a startup banner explaining what this application does.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║            A2A Protocol – Agent Consumer Client              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("This application discovers and communicates with a remote AI agent");
            Console.WriteLine("using the A2A (Agent-to-Agent) protocol.");
            Console.WriteLine();
            Console.WriteLine("What happens next:");
            Console.WriteLine("  1. Connect to the agent host at http://localhost:5000");
            Console.WriteLine("  2. Fetch the agent card from /.well-known/agent.json");
            Console.WriteLine("  3. Create a local AIAgent proxy backed by the remote endpoint.");
            Console.WriteLine("  4. Send a sample message ('Hello!') to the weather agent.");
            Console.WriteLine("  5. Print the agent's response below.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Make sure the A2AHost server is running before continuing.");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Press any key to start...");
            Console.ReadKey(true);
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
