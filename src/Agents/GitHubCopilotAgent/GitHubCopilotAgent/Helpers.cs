using Microsoft.Agents.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitHubCopilotAgent
{
    internal class Helpers
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
                    int spinnerDelayMs = 200;
                    ConsoleColor spinnerColor = ConsoleColor.DarkYellow;

                    var spinnerTask = Task.Run(async () =>
                    {
                        string[] frames =
                        [
                            "/ ", "- ", "\\ ", "| "
                        ];
                        int i = 0;
                        try
                        {
                            while (!spinnerCts.Token.IsCancellationRequested)
                            {
                                string frame = frames[i % frames.Length];
                                Console.ForegroundColor = spinnerColor;
                                Console.Write(frame);
                                Console.CursorLeft -= frame.Length;
                                i++;
                                await Task.Delay(spinnerDelayMs, spinnerCts.Token);
                            }
                        }
                        catch (OperationCanceledException) { }
                    });

                    await foreach (var update in agent.RunStreamingAsync(userInput, session))
                    {
                        //TraceMessage("stream-update", update);

                        if (!firstTokenReceived)
                        {
                            firstTokenReceived = true;
                            spinnerColor = ConsoleColor.DarkGray;
                            spinnerDelayMs = 500;
                        }

                        Console.Write(update);
                    }

                    spinnerCts.Cancel();
                    await spinnerTask;
                    Console.ForegroundColor = AgentColor;
                    Console.Write("  ");
                    Console.CursorLeft -= 2;

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

        private static void TraceMessage(string category, AgentResponseUpdate? message)
        {
            string timestamp = DateTimeOffset.Now.ToString("O");
            string messageType = message?.GetType().Name ?? "null";

            if (message is null)
            {
                Console.WriteLine($"[{timestamp}] [TRACE] [{category}] Type={messageType}; Message=<null>");
                return;
            }

            List<string> meaningfulProperties = GetMeaningfulTextProperties(message);
            if (meaningfulProperties.Count == 0)
            {
                Console.WriteLine($"[{timestamp}] [TRACE] [{category}] Type={messageType}; Message=<no meaningful text properties>");
                return;
            }

            Console.WriteLine($"[{timestamp}] [TRACE] [{category}] Type={messageType}; {string.Join("; ", meaningfulProperties)}");
        }

        private static List<string> GetMeaningfulTextProperties(AgentResponseUpdate message)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            List<string> result = [];
            Type? currentType = message.GetType();

            while (currentType is not null && typeof(AgentResponseUpdate).IsAssignableFrom(currentType))
            {
                foreach (PropertyInfo property in currentType.GetProperties(Flags))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    object? value;
                    try
                    {
                        value = property.GetValue(message);
                    }
                    catch
                    {
                        continue;
                    }

                    if (value is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        result.Add($"{currentType.Name}.{property.Name}=\"{NormalizeForTrace(text)}\"");
                        continue;
                    }

                    if (value is IEnumerable enumerable && value is not string)
                    {
                        int index = 0;
                        foreach (object? item in enumerable)
                        {
                            if (item is string itemText && !string.IsNullOrWhiteSpace(itemText))
                            {
                                result.Add($"{currentType.Name}.{property.Name}[{index}]=\"{NormalizeForTrace(itemText)}\"");
                            }

                            index++;
                        }
                    }
                }

                if (currentType == typeof(AgentResponseUpdate))
                {
                    break;
                }

                currentType = currentType.BaseType;
            }

            return result;
        }

        private static string NormalizeForTrace(string value)
        {
            string normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            const int MaxLength = 200;
            return normalized.Length <= MaxLength
                ? normalized
                : normalized[..MaxLength] + "…";
        }

    }
}
