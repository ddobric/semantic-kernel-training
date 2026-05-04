using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

//https://kowshik.github.io/JPregel/pregel_paper.pdf
/*
Superstep N:
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Collect All    │───▶│  Route Messages │───▶│  Execute All    │
│  Pending        │    │  Based on Type  │    │  Target         │
│  Messages       │    │  & Conditions   │    │  Executors      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
                                                       │ (barrier: wait for all)
┌─────────────────┐    ┌─────────────────┐             │
│  Start Next     │◀───│  Emit Events &  │◀────────────┘
│  Superstep      │    │  New Messages   │
└─────────────────┘    └─────────────────┘

 */
namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    /// <summary>
    /// Complex Workflow: a feedback loop between two AI executors.
    /// A SloganWriter generates slogans, a FeedbackProvider reviews them,
    /// and the loop repeats until the rating meets the threshold or max attempts are reached.
    /// Inspired by the Pregel BSP (Bulk Synchronous Parallel) model.
    /// </summary>
    internal class ComplexWorkflow
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Wrap the Azure OpenAI ChatClient as an IChatClient — shared by both executors.
            var chatClient = new AzureOpenAIClient(new Uri(endpoint),
                new DefaultAzureCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Instantiate the two workflow participants (executors).
            var sloganWriterExecutor = new SloganWriterExecutor("SloganWriter", chatClient);
            var feedbackExecutor = new FeedbackExecutor("FeedbackProvider", chatClient);

            // Define the workflow graph:
            //   SloganWriter ──▶ FeedbackProvider ──▶ SloganWriter (loop)
            // Output is yielded by the FeedbackProvider when a slogan is accepted.
            var workflow = new WorkflowBuilder(sloganWriterExecutor)
                .AddEdge(sloganWriterExecutor, feedbackExecutor)
                .AddEdge(feedbackExecutor, sloganWriterExecutor)
                .WithOutputFrom(feedbackExecutor)
                .Build();

            // Run the workflow with streaming — events arrive as they happen.
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input: "Create a slogan for a new electric SUV that is affordable and fun to drive.");
            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                Console.WriteLine($"Event: {evt.GetType().Name}");

                // Custom domain events for observability.
                if (evt is SloganGeneratedEvent or FeedbackEvent)
                {
                    Console.WriteLine($"{evt}");
                }

                // Final accepted output from the workflow.
                if (evt is WorkflowOutputEvent outputEvent)
                {
                    Console.WriteLine($"{outputEvent}");
                }

                // Error handling.
                if (evt is WorkflowErrorEvent errorEvent)
                {
                    Console.WriteLine($"Workflow error: {errorEvent.Exception?.Message}");
                    Console.WriteLine($"Details: {errorEvent.Exception}");
                }
            }
        }
    }


    /// <summary>
    /// Data contract for the slogan writer's structured output.
    /// Contains the original task description and the generated slogan text.
    /// </summary>
    public sealed class SloganResult
    {
        [JsonPropertyName("task")]
        public required string Task { get; set; }

        [JsonPropertyName("slogan")]
        public required string Slogan { get; set; }
    }

    /// <summary>
    /// Data contract for the feedback agent's structured output.
    /// Contains review comments, a numeric rating (1–10), and suggested improvement actions.
    /// </summary>
    public sealed class FeedbackResult
    {
        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("actions")]
        public string Actions { get; set; } = string.Empty;
    }

    /// <summary>
    /// Domain event emitted when the SloganWriter produces a new or revised slogan.
    /// Watchers can observe this in the streaming event loop for real-time progress.
    /// </summary>
    internal sealed class SloganGeneratedEvent(SloganResult sloganResult) : WorkflowEvent(sloganResult)
    {
        public override string ToString() => $"Slogan: {sloganResult.Slogan}";
    }

    /// <summary>
    /// Executor that generates and revises slogans using an AI agent.
    /// Has two message handlers:
    ///   1. <c>HandleAsync(string)</c> — handles the initial user prompt to create the first slogan.
    ///   2. <c>HandleAsync(FeedbackResult)</c> — handles review feedback to produce an improved revision.
    /// Uses structured JSON output via <see cref="ChatResponseFormat"/> to ensure valid deserialization.
    /// </summary>
    internal sealed partial class SloganWriterExecutor : Executor
    {
        private readonly AIAgent _agent;
        private AgentSession? _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="SloganWriterExecutor"/> class.
        /// </summary>
        /// <param name="id">A unique identifier for the executor.</param>
        /// <param name="chatClient">The chat client to use for the AI agent.</param>
        public SloganWriterExecutor(string id, IChatClient chatClient) : base(id)
        {
            ChatClientAgentOptions agentOptions = new()
            {
                ChatOptions = new()
                {
                    Instructions = "You are a professional slogan writer. You will be given a task to create a slogan.",
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<SloganResult>()
                }
            };

            this._agent = new ChatClientAgent(chatClient, agentOptions);
        }

        /// <summary>
        /// Handles the initial user prompt — generates the first slogan.
        /// </summary>
        [MessageHandler]
        public async ValueTask<SloganResult> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // Lazily create a session so the agent remembers prior turns in this workflow run.
            this._session ??= await this._agent.CreateSessionAsync(cancellationToken);

            var result = await this._agent.RunAsync(message, this._session, cancellationToken: cancellationToken);

            // The agent returns structured JSON thanks to ResponseFormat; deserialize it.
            var sloganResult = JsonSerializer.Deserialize<SloganResult>(result.Text) ?? throw new InvalidOperationException("Failed to deserialize slogan result.");

            // Emit a domain event so watchers can observe progress.
            await context.AddEventAsync(new SloganGeneratedEvent(sloganResult), cancellationToken);
         
            return sloganResult;
        }

        /// <summary>
        /// Handles feedback from the FeedbackExecutor — revises the slogan based on review comments.
        /// </summary>
        [MessageHandler]
        public async ValueTask<SloganResult> HandleAsync(FeedbackResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var feedbackMessage = $"""
            Here is the feedback on your previous slogan:
            Comments: {message.Comments}
            Rating: {message.Rating}
            Suggested Actions: {message.Actions}

            Please use this feedback to improve your slogan.
            """;

            var result = await this._agent.RunAsync(feedbackMessage, this._session, cancellationToken: cancellationToken);
            var sloganResult = JsonSerializer.Deserialize<SloganResult>(result.Text) ?? throw new InvalidOperationException("Failed to deserialize slogan result.");

            await context.AddEventAsync(new SloganGeneratedEvent(sloganResult), cancellationToken);
            return sloganResult;
        }


        // Not required if the package Microsoft.Agents.AI.Workflows.Generators is added!
        //protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        //{
        //    protocolBuilder.ConfigureRoutes(routeBuilder =>
        //    {
        //        routeBuilder
        //               .AddHandler<string, SloganResult>(this.HandleAsync)
        //               .AddHandler<FeedbackResult, SloganResult>(this.HandleAsync);
        //    });
        //    return protocolBuilder;
        //}
    }

    /// <summary>
    /// Domain event emitted when the FeedbackProvider completes a review.
    /// Serializes the full <see cref="FeedbackResult"/> as indented JSON for console display.
    /// </summary>
    internal sealed class FeedbackEvent(FeedbackResult feedbackResult) : WorkflowEvent(feedbackResult)
    {
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
        public override string ToString() => $"Feedback:\n{JsonSerializer.Serialize(feedbackResult, this._options)}";
    }

    /// <summary>
    /// Executor that reviews slogans and decides the workflow's next step:
    ///   • <b>Accept</b> — rating ≥ <see cref="MinimumRating"/> → yields the slogan as final output.
    ///   • <b>Reject</b> — attempts ≥ <see cref="MaxAttempts"/> → yields the last slogan with a rejection note.
    ///   • <b>Loop</b> — sends <see cref="FeedbackResult"/> back to the SloganWriter for revision.
    /// </summary>
    [SendsMessage(typeof(FeedbackResult))]
    [YieldsOutput(typeof(string))]
    internal sealed partial class FeedbackExecutor : Executor<SloganResult>
    {
        private readonly AIAgent _agent;
        private AgentSession? _session;

        /// <summary>Minimum acceptable rating (1–10). Slogans rated at or above this are accepted.</summary>
        public int MinimumRating { get; init; } = 8;

        /// <summary>Maximum number of revision attempts before the workflow stops with the last slogan.</summary>
        public int MaxAttempts { get; init; } = 3;

        private int _attempts;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackExecutor"/> class.
        /// </summary>
        /// <param name="id">A unique identifier for the executor.</param>
        /// <param name="chatClient">The chat client to use for the AI agent.</param>
        public FeedbackExecutor(string id, IChatClient chatClient) : base(id)
        {
            ChatClientAgentOptions agentOptions = new()
            {
                ChatOptions = new()
                {
                    Instructions = "You are a professional editor. You will be given a slogan and the task it is meant to accomplish.",
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<FeedbackResult>()
                }
            };

            this._agent = new ChatClientAgent(chatClient, agentOptions);
        }

        /// <summary>
        /// Reviews a slogan and either accepts it, rejects after max attempts, or sends feedback back for revision.
        /// </summary>
        public override async ValueTask HandleAsync(SloganResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            this._session ??= await this._agent.CreateSessionAsync(cancellationToken);

            // Ask the feedback agent to rate the slogan.
            var sloganMessage = $"""
            Here is a slogan for the task '{message.Task}':
            Slogan: {message.Slogan}
            Please provide feedback on this slogan, including comments, a rating from 1 to 10, and suggested actions for improvement.
            """;

            var response = await this._agent.RunAsync(sloganMessage, this._session, cancellationToken: cancellationToken);
            var feedback = JsonSerializer.Deserialize<FeedbackResult>(response.Text) ?? throw new InvalidOperationException("Failed to deserialize feedback.");

            await context.AddEventAsync(new FeedbackEvent(feedback), cancellationToken);

            // Accept: rating meets the threshold.
            if (feedback.Rating >= this.MinimumRating)
            {
                await context.YieldOutputAsync($"The following slogan was accepted:\n\n{message.Slogan}", cancellationToken);
                return;
            }

            // Give up: too many iterations.
            if (this._attempts >= this.MaxAttempts)
            {
                await context.YieldOutputAsync($"The slogan was rejected after {this.MaxAttempts} attempts. Final slogan:\n\n{message.Slogan}", cancellationToken);
                return;
            }

            // Loop: send feedback back to the SloganWriter for another revision.
            await context.SendMessageAsync(feedback, cancellationToken: cancellationToken);
            this._attempts++;
        }
    }

}
