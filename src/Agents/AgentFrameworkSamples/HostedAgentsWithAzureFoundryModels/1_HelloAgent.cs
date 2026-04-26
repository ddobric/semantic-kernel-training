using AgentFramework_Samples;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace HostedAgentsWithAzureFoundryModels
{
    /// <summary>
    /// Demonstrates core Agent Framework scenarios using Azure OpenAI hosted models.
    /// </summary>
    public class HelloAgent
    {
        /// <summary>
        /// Scenario 1: Agent Construction and Basic Usage.
        /// Creates an AIAgent from an AzureOpenAI ChatClient, then invokes it
        /// with a single prompt (non-streaming) and a streaming call.
        /// </summary>
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Build the agent: AzureOpenAIClient → ChatClient → AIAgent
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good at telling jokes.", name: nameof(HelloAgent));

            // Non-streaming invocation — returns the full response at once.
            AgentResponse agentResp = await agent.RunAsync("Tell me a joke about a pirate.");
            Console.WriteLine(agentResp);

            // Streaming invocation — yields incremental updates as they arrive.
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
            {
                Console.WriteLine(update);
            }
        }

        /// <summary>
        /// Scenario 2: Sessions and Multi-turn Conversations.
        /// Without a session, each RunAsync call is stateless — the agent has no memory of prior turns.
        /// With an AgentSession, conversational context is preserved across calls,
        /// enabling follow-up questions that reference previous answers.
        /// </summary>
        public static async Task RunMultiturnAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good calculator.", name: nameof(HelloAgent));

            // Stateless calls — each request is independent; "Now add 1" has no context.
            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10."));
            Console.WriteLine(await agent.RunAsync("Now add 1"));
            Console.WriteLine(await agent.RunAsync("And divide all by 2"));

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Now let's do it with the session.");
            Console.WriteLine();
            Console.WriteLine();

            // Session-based calls — the session accumulates conversation history,
            // so follow-up prompts can reference previous results.
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10.", session));
            Console.WriteLine(await agent.RunAsync("Now add 1", session));
            Console.WriteLine(await agent.RunAsync("And divide all by 2", session));
        }

        /// <summary>
        /// Scenario 3: Function Tools.
        /// Registers a local C# method as a tool the agent can call.
        /// When the user asks a question that requires the tool, the agent
        /// automatically invokes it and incorporates the result into its response.
        /// </summary>
        public static async Task RunWithToolsAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // The AIFunctionFactory.Create wrapper exposes GetProcessInfo as a callable tool.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are the agent that shares information.", name: nameof(HelloAgent),
                    tools: [AIFunctionFactory.Create(GetProcessInfo)]);

            // Start an interactive conversation loop with streaming output.
            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Tool function: returns a formatted list of running processes.
        /// The [Description] attributes provide the agent with metadata to decide when and how to call it.
        /// </summary>
        [Description("Get the information about running processes.")]
        static string GetProcessInfo([Description("The location to get the weather for.")] string location)
        {
            StringBuilder sb = new StringBuilder();

            var processses = Process.GetProcesses();

            foreach (var process in processses)
            {
                sb.AppendLine($"{process.Id,8} | {process.ProcessName,-40} | Threads: {process.Threads.Count,4} | Memory: {process.WorkingSet64 / 1024.0 / 1024.0,8:F2} MB");
            }

            return sb.ToString();
        }
        
    }
}
