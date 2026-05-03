using Microsoft.Agents.AI;

namespace AgentFramework_Samples
{
    /// <summary>
    /// Provides console UI helpers for interactive agent conversations.
    /// </summary>
    internal static class Helpers
    {
        private const ConsoleColor UserColor = ConsoleColor.Cyan;
        private const ConsoleColor AgentColor = ConsoleColor.Green;
        private const ConsoleColor PromptColor = ConsoleColor.Yellow;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor InfoColor = ConsoleColor.DarkGray;

        /// <summary>
        /// Runs an interactive streaming conversation loop with colored output.
        /// </summary>
        public static async Task RunConversationLoopAsync(AIAgent agent)
        {
            AgentSession session = await agent.CreateSessionAsync();

            WriteLineColored(InfoColor, "Chat session started. Type 'exit' or press Enter on an empty line to quit.");

            while (true)
            {
                Console.WriteLine();
                WriteColored(PromptColor, "You > ");

                Console.ForegroundColor = UserColor;
                string? userInput = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrEmpty(userInput) || userInput == "exit")
                {
                    WriteLineColored(InfoColor, "Session ended.");
                    break;
                }

                WriteColored(AgentColor, "Agent > ");

                try
                {
                    Console.ForegroundColor = AgentColor;

                    // Show a rotating dots animation while waiting for the first token.
                    using var spinnerCts = new CancellationTokenSource();
                    bool firstTokenReceived = false;

                    var spinnerTask = Task.Run(async () =>
                    {
                        string[] frames = ["?", "?", "?", "?", "?", "?", "?", "?", "?", "?"];
                        int i = 0;
                        try
                        {
                            while (!spinnerCts.Token.IsCancellationRequested)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(frames[i % frames.Length]);
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                i++;
                                await Task.Delay(80, spinnerCts.Token);
                            }
                        }
                        catch (OperationCanceledException) { }
                    });

                    await foreach (var update in agent.RunStreamingAsync(userInput, session))
                    {
                        if (!firstTokenReceived)
                        {
                            firstTokenReceived = true;
                            spinnerCts.Cancel();
                            await spinnerTask;
                            // Clear the spinner character and restore agent color.
                            Console.Write(" ");
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            Console.ForegroundColor = AgentColor;
                        }

                        Console.Write(update);
                    }

                    Console.ResetColor();
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ResetColor();
                    WriteLineColored(ErrorColor, $"Error: {ex.Message}");
                }
            }
        }

        public static void GetModelAndKey(out string apiKey, out string model)
        {
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL_NAME") ?? "gpt-5.4-mini";
        }

        public static void GetAzureEndpointAndModelDeployment(out string apiKey, out string model)
        {
            apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            model = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";
        }

        private static void WriteColored(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        private static void WriteLineColored(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
