using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenTelemetry.Context.Propagation;

namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    /// <summary>
    /// Scenario: Hello Workflow — the simplest possible workflow.
    /// Chains two pure-function executors in a linear pipeline: Uppercase → Reverse.
    /// No AI model is involved; this demonstrates the workflow engine mechanics
    /// (executors, edges, events) in isolation before adding LLM complexity.
    /// </summary>
    internal partial class HelloWorkflow
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // First executor: a plain lambda bound as an executor via BindAsExecutor().
            // Converts input text to uppercase.
            Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
            var uppercaseExecutor = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

            // Second executor: a custom Executor<TIn, TOut> subclass that reverses text.
            ReverseTextExecutor reverseExecutor = new();

            
            // Build the workflow graph: Uppercase → Reverse, output comes from Reverse.
            WorkflowBuilder builder = new(uppercaseExecutor);
            builder.AddEdge(uppercaseExecutor, reverseExecutor).WithOutputFrom(reverseExecutor);
            var workflow = builder.Build();

            // Run synchronously (non-streaming) and inspect completed events.
            await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");

            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
                else
                {
                    Console.WriteLine($"{evt.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Scenario 2: Workflow with Inter-Executor Messaging and Custom Events.
        /// Extends the linear pipeline with two additional executors that demonstrate
        /// how executors can send typed messages to peers via SendMessageAsync and
        /// emit custom WorkflowEvents for observability.
        /// Pipeline: Uppercase → Reverse → MyExecutor2 → MyExecutor1 (output).
        /// MyExecutor2 also sends a MyEvent1 message that MyExecutor1 handles via [MessageHandler].
        /// </summary>
        public static async Task RunWithMessagingAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Lambda executor: converts input text to uppercase.
            Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
            var uppercaseExecutor = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

            // Custom executor: reverses the text.
            ReverseTextExecutor reverseExecutor = new();

            // Custom executor: forwards text and sends a MyEvent1 message to downstream executors.
            MyExecutor2 myxExecutor2 = new();

            // Custom executor: handles both string input (from the edge) and MyEvent1 messages.
            // Emits MyEvent2 as a custom domain event when handling MyEvent1.
            MyExecutor1 myxExecutor1 = new(nameof(MyExecutor1));

            // Build a four-step linear pipeline with output from the last executor.
            WorkflowBuilder builder = new(uppercaseExecutor);
            builder.AddEdge(uppercaseExecutor, reverseExecutor);
            builder.AddEdge(reverseExecutor, myxExecutor2);
            builder.AddEdge(myxExecutor2, myxExecutor1).WithOutputFrom(myxExecutor1);
            var workflow = builder.Build();

            // Run and inspect all events, highlighting custom events in green.
            await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");

            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
                else if (evt is MyEvent2)
                {
                    var clr = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{evt.GetType().Name}");
                    Console.ForegroundColor = clr;
                }
                else
                {
                    Console.WriteLine($"{evt.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Second executor: reverses the input text and completes the workflow.
        /// </summary>
        internal class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
        {
            /// <summary>
            /// Processes the input message by reversing the text.
            /// </summary>
            /// <param name="message">The input text to reverse</param>
            /// <param name="context">Workflow context for accessing workflow services and adding events</param>
            /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
            /// The default is <see cref="CancellationToken.None"/>.</param>
            /// <returns>The input text reversed</returns>
            /// 
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                // Because we do not suppress it, the returned result will be yielded as an output from this executor.
                return ValueTask.FromResult(string.Concat(message.Reverse()));
            }
        }

        /// <summary>
        /// Multi-handler executor: demonstrates [MessageHandler] routing.
        /// Handles two message types: string (from the pipeline edge) and MyEvent1 (from SendMessageAsync).
        /// Uses the untyped Executor base with partial class + source-generated routing.
        /// </summary>
        internal sealed partial class MyExecutor1(string id) : Executor(id)
        {
            /// <summary>
            /// Handles the string message from the pipeline edge by reversing the text.
            /// </summary>
            [MessageHandler]
            public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(string.Concat(message.Reverse()));
            }

            /// <summary>
            /// Handles MyEvent1 sent by MyExecutor2 via SendMessageAsync.
            /// Emits a MyEvent2 domain event for observability, then returns the event content.
            /// </summary>
            [MessageHandler]
            public async ValueTask<string> Handle2Async(MyEvent1 event1, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                // Emit a custom domain event so watchers can observe this handler was invoked.
                await context.AddEventAsync(new MyEvent2("Hello, I'm Event2"), cancellationToken);

                return $"Event Message: {event1}";
            }
        }


        /// <summary>
        /// Message-sending executor: demonstrates inter-executor communication.
        /// Processes the string input from the edge, then sends a MyEvent1 message
        /// to any downstream executor that can handle it (MyExecutor1.Handle2Async).
        /// The [SendsMessage] attribute declares the message type for workflow validation.
        /// </summary>
        [SendsMessage(typeof(MyEvent1))]
        internal sealed partial class MyExecutor2() : Executor<string, string>(nameof(MyExecutor2))
        {
            public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                // Send a typed message to any executor that handles MyEvent1.
                await context.SendMessageAsync(new MyEvent1("My Message to Executors."));

                return $"Result from MyExecutor {message}";
            }
        }

        /// <summary>
        /// Custom message sent between executors via SendMessageAsync.
        /// MyExecutor2 sends it; MyExecutor1 handles it via [MessageHandler].
        /// </summary>
        internal sealed class MyEvent1(string msg) : WorkflowEvent(msg)
        {
            public override string ToString() => $"MyEvent1 {msg}";
        }

        /// <summary>
        /// Custom domain event emitted by MyExecutor1 for observability.
        /// Watchers see this in the event stream to track inter-executor messaging.
        /// </summary>
        internal sealed class MyEvent2(string msg) : WorkflowEvent(msg)
        {
            public override string ToString() => $"MyEvent2 {msg}";
        }
    }
}
