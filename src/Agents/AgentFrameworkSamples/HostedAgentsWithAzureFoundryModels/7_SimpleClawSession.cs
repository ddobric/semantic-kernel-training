using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentFramework_Samples.GettingStarted
{
    /*
     
                 User Intent: "Check disk space and find large files"
                                │
                                ▼
                        ┌──────────────┐
                        │  Agent (LLM) │  ── decomposes into ClawTask[]
                        └──────┬───────┘
                               │
                               ▼
                ┌──────────────────────────────────────────┐
                │         RunCommandLineAsync              │
                │  builds workflow from ClawTask[]         │
                └──────────────────────────────────────────┘
                               │
                 ┌─────────────┼─────────────┐
                 ▼             ▼             ▼
             ┌────────┐   ┌────────┐   ┌────────┐
             │ Step 1 │──▶│ Step 2 │──▶│ Step 3 │──▶ Output
             │Executor│   │Executor│   │Executor│
             └────────┘   └────────┘   └────────┘
              intercept    intercept    intercept
              execute      execute      execute
     
     */
    /// <summary>
    /// Demonstrates a CLAW (Command Line Agent Workflow) session where the agent
    /// decomposes a user's complex intent into a list of CLI tasks, then executes
    /// them as a dynamically built Microsoft Agents Workflow — one Executor per task.    /// 
    /// </summary>
    public class SimpleClawSession
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Connect to the Playwright MCP server via stdio transport.
            // Requires: npx @playwright/mcp@latest
            await using var playwrightMcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "Playwright",
                Command = "npx",
                Arguments = ["@playwright/mcp@latest"],
            }));

            var playwrightTools = await playwrightMcpClient.ListToolsAsync();

            // Log available Playwright tools.
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Playwright MCP: {playwrightTools.Count} tool(s) available");
            Console.ResetColor();

            // Combine CLI workflow tool with Playwright MCP tools.
            AITool[] tools = [AIFunctionFactory.Create(RunCommandLineAsync), .. playwrightTools.Cast<AITool>()];

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(
                    instructions: """
                        You are a command-line automation agent running on Windows. When the user describes a task:
                        1. Decompose the intent into one or more sequential CLI sub-tasks.
                        2. Call the RunCommandLineAsync tool ONCE with the complete list of tasks.
                           Each task must specify:
                             - 'executable': the CLI program to run (e.g. "git", "dotnet", "az", "kubectl", "docker", "npm", "curl", "ping", "ipconfig", "pwsh", "cmd").
                             - 'arguments': the full argument string to pass to the executable.
                             - 'description': what this step does.
                        3. The tool will build a workflow and execute all tasks sequentially.
                        4. After execution, summarize the results to the user.
                        
                        If you use any instaed tool, make it to be executed as a Executor superstep in the workflow, and make sure to return the output as a string to be passed to the next step.
                        
                        You also have access to Playwright browser automation tools.
                        Use them when the user asks to interact with web pages, scrape content, take screenshots,
                        fill forms, or perform any browser-based task.However, always as a step in executor.
                        
                        Do not use the actual CLI executable directly (e.g. executable="git", arguments="log --oneline -5").
                        Use it via CMD command. For example, if "git" is not found, you can try executable="cmd", arguments="/c git --version" to check if it's available via the command prompt.
                        If a CLI cannot be executed at all, then add an installation step first.
                        You may use "pwsh" or "cmd" as the executable when the command requires shell features
                        like piping, redirection, or built-in cmdlets (Get-Process, Get-ChildItem, etc.).
                        In that case, pass the shell command via arguments (e.g. executable="pwsh", arguments="-NoProfile -Command Get-Process").
                        
                       
                        If the execution of some task fails, analyze the error and retry with a better approach.
                        
                        IMPORTANT: Never execute destructive commands (rm -rf, format, del /s, etc.) without 
                        making it clear what will be deleted. Prefer read-only or non-destructive commands.
                        """,
                    name: nameof(SimpleClawSession),
                    tools: tools);

            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Receives the full list of decomposed tasks from the agent, builds a
        /// Microsoft Agents Workflow with one CommandExecutor per task, and runs it.
        /// Each executor prompts the user for approval before executing its command.
        /// </summary>
        [Description("""
            Execute a plan of CLI commands as a sequential workflow.
            Provide ALL tasks at once. Each task specifies the executable, its arguments, and a description.
            Tasks are executed sequentially — each receives the output of the previous one.
            """)]
        static async Task<string> RunCommandLineAsync(
            [Description("The list of tasks to execute. Each task has 'executable' (the CLI program), 'arguments' (the arguments string), and 'description' (what it does).")] ClawTask[] tasks)
        {
            if (tasks is null || tasks.Length == 0)
                return "ERROR: No tasks provided.";

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"┌── Workflow Plan: {tasks.Length} task(s)");
            for (int i = 0; i < tasks.Length; i++)
            {
                string prefix = i == tasks.Length - 1 ? "└──" : "├──";
                Console.WriteLine($"{prefix} [{i + 1}] {tasks[i].Description}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"    {tasks[i].Executable} {tasks[i].Arguments}");
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.ResetColor();
            Console.WriteLine();

            // Build a dynamic workflow: one CommandExecutor per task, chained sequentially.
            var executors = tasks.Select((task, index) =>
                new CommandExecutor($"Step{index + 1}", task.Executable, task.Arguments, task.Description, index + 1, tasks.Length))
                .ToArray();

            // Chain: first executor is the entry point, each feeds into the next.
            var builder = new WorkflowBuilder(executors[0]);
            for (int i = 0; i < executors.Length - 1; i++)
            {
                builder.AddEdge(executors[i], executors[i + 1]);
            }
            builder.WithOutputFrom(executors[^1]);
            var workflow = builder.Build();

            // Run the workflow. The input is an empty string since the first task
            // generates its own context from the command.
            StringBuilder results = new();

            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input: "");
            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is CommandCompletedEvent cmdEvt)
                {
                    results.AppendLine($"--- Step {cmdEvt.StepNumber}/{cmdEvt.TotalSteps}: {cmdEvt.StepDescription} ---");
                    results.AppendLine(cmdEvt.Output);
                    results.AppendLine();
                }
                else if (evt is WorkflowErrorEvent errorEvt)
                {
                    results.AppendLine($"WORKFLOW ERROR: {errorEvt.Exception?.Message}");
                }
            }

            return results.ToString();
        }

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength] + $"\n... (truncated, {text.Length - maxLength} chars omitted)";
    }

    /// <summary>
    /// Represents a single task in the CLAW plan, decomposed from the user's intent.
    /// </summary>
    public sealed class ClawTask
    {
        [JsonPropertyName("executable")]
        public required string Executable { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public required string Description { get; set; }
    }

    /// <summary>
    /// Custom workflow event emitted when a command executor completes,
    /// carrying the output for observability and result aggregation.
    /// </summary>
    internal sealed class CommandCompletedEvent(string output, int stepNumber, int totalSteps, string stepDescription)
        : WorkflowEvent(output)
    {
        public string Output => output;
        public int StepNumber => stepNumber;
        public int TotalSteps => totalSteps;
        public string StepDescription => stepDescription;

        public override string ToString() => $"[Step {stepNumber}/{totalSteps}] {stepDescription}: {Truncate(output, 200)}";

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    /// <summary>
    /// Workflow executor that runs a single CLI command.
    /// Each instance represents one step in the CLAW workflow pipeline.
    /// Prompts the user for approval before execution (interceptor pattern).
    /// </summary>
    internal sealed class CommandExecutor : Executor<string, string>
    {
        private readonly string _executable;
        private readonly string _arguments;
        private readonly string _description;
        private readonly int _stepNumber;
        private readonly int _totalSteps;

        public CommandExecutor(string id, string executable, string arguments, string description, int stepNumber, int totalSteps)
            : base(id)
        {
            _executable = executable;
            _arguments = arguments;
            _description = description;
            _stepNumber = stepNumber;
            _totalSteps = totalSteps;
        }

        public override async ValueTask<string> HandleAsync(string previousOutput, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // Display the step and command to the user.
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"┌── Step {_stepNumber}/{_totalSteps}: {_description}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"│   {_executable} {_arguments}");

            // Show prior output if available.
            if (!string.IsNullOrWhiteSpace(previousOutput) && _stepNumber > 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"│   Previous output: {Truncate(previousOutput, 200)}");
            }
            Console.ResetColor();

            // Interceptor: prompt user for approval.
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("│   Execute? [Y]es / [S]kip / [A]bort > ");
            Console.ResetColor();

            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "A")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("└── Aborted by user.");
                Console.ResetColor();
                return "ABORTED: The user chose to abort. Remaining steps were not executed.";
            }

            if (input == "S")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("└── Skipped.");
                Console.ResetColor();
                string skipResult = $"SKIPPED: Step {_stepNumber} was skipped by the user.";
                await context.AddEventAsync(new CommandCompletedEvent(skipResult, _stepNumber, _totalSteps, _description), cancellationToken);
                return skipResult;
            }

            // Execute the command.
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│   Executing...");
            Console.ResetColor();

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _executable,
                    Arguments = _arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

                bool exited = process.WaitForExit(60_000);

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (!exited)
                {
                    process.Kill(entireProcessTree: true);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("└── Timed out (60s).");
                    Console.ResetColor();
                    string timeoutResult = $"ERROR: Command timed out after 60 seconds.\nPartial output:\n{Truncate(stdout, 2000)}";
                    await context.AddEventAsync(new CommandCompletedEvent(timeoutResult, _stepNumber, _totalSteps, _description), cancellationToken);
                    return timeoutResult;
                }

                // Display output.
                StringBuilder result = new();

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    string truncatedOut = Truncate(stdout, 4000);
                    Console.WriteLine($"│   Output ({stdout.Length} chars):");
                    foreach (var line in truncatedOut.Split('\n').Take(20))
                        Console.WriteLine($"│     {line.TrimEnd()}");
                    if (stdout.Split('\n').Length > 20)
                        Console.WriteLine($"│     ... ({stdout.Split('\n').Length - 20} more lines)");
                    Console.ResetColor();
                    result.AppendLine($"STDOUT:\n{truncatedOut}");
                }

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"│   Errors: {Truncate(stderr, 500)}");
                    Console.ResetColor();
                    result.AppendLine($"STDERR:\n{Truncate(stderr, 1000)}");
                }

                result.AppendLine($"EXIT CODE: {process.ExitCode}");

                string status = process.ExitCode == 0 ? "✓ Completed" : $"✗ Failed (exit {process.ExitCode})";
                Console.ForegroundColor = process.ExitCode == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"└── {status}");
                Console.ResetColor();

                string finalResult = result.ToString();
                await context.AddEventAsync(new CommandCompletedEvent(finalResult, _stepNumber, _totalSteps, _description), cancellationToken);
                return finalResult;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"└── Error: {ex.Message}");
                Console.ResetColor();
                string errorResult = $"ERROR: {ex.Message}";
                await context.AddEventAsync(new CommandCompletedEvent(errorResult, _stepNumber, _totalSteps, _description), cancellationToken);
                return errorResult;
            }
        }

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength] + $"\n... (truncated, {text.Length - maxLength} chars omitted)";
    }
}

