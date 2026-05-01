namespace A2AHost
{
    /// <summary>
    /// Entry point for the A2A host application.
    /// Starts an ASP.NET Core server that exposes an AI agent over the A2A protocol.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            PrintBootstrapMessage();

            // Build and run the A2A host server (blocks until the server shuts down).
            await A2AHostSample.RunAsync();

            Console.ReadLine();
        }

        private static void PrintBootstrapMessage()
        {
            // Display a startup banner explaining what this application does.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              A2A Protocol – Agent Host Server                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("This application hosts an AI-powered weather agent and exposes");
            Console.WriteLine("it over the A2A (Agent-to-Agent) protocol.");
            Console.WriteLine();
            Console.WriteLine("What happens next:");
            Console.WriteLine("  1. An ASP.NET Core web server is configured.");
            Console.WriteLine("  2. A 'weather-agent' is created using Azure AI Foundry.");
            Console.WriteLine("  3. The agent is registered with the A2A protocol server.");
            Console.WriteLine("  4. An agent card is published at /.well-known/agent.json");
            Console.WriteLine("     so remote consumers can discover this agent.");
            Console.WriteLine("  5. The server starts listening on http://localhost:5000");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Tip: Run the A2AConsumerApp in a separate terminal to call this agent.");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
