namespace RagAgent
{
    /// <summary>
    /// Entry point for the RAG Agent console application.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Display a styled startup banner.
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔══════════════════════════════════════════════╗");
            Console.WriteLine("║           🤖  RAG Agent Sample  🤖          ║");
            Console.WriteLine("║  Retrieval-Augmented Generation with Azure   ║");
            Console.WriteLine("║       OpenAI + In-Memory Vector Store        ║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Initializing agent, embedding model, and knowledge base...");
            Console.ResetColor();
            Console.WriteLine();

            // Launch the RAG agent interactive session.
            await RagAgentSample.RunAsync();
        }
    }
}
