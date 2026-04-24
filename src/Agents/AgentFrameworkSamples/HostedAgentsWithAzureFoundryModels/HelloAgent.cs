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
    public class HelloAgent
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good at telling jokes.", name: nameof(HelloAgent));

            AgentResponse agentResp = await agent.RunAsync("Tell me a joke about a pirate.");

            // Invoke the agent and output the text result.
            Console.WriteLine(agentResp);

            // Invoke the agent with streaming support.
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
            {
                Console.WriteLine(update);
            }
        }

        public static async Task RunMultiturnAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good calculator.", name: nameof(HelloAgent));

       
            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10."));
            Console.WriteLine(await agent.RunAsync("Now add 1"));
            Console.WriteLine(await agent.RunAsync("And divide all by 2"));


            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("Now let's do it with the session.");
            Console.WriteLine();
            Console.WriteLine();

            // Invoke the agent with a multi-turn conversation, where the context is preserved in the session object.
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10.", session));
            Console.WriteLine(await agent.RunAsync("Now add 1", session));
            Console.WriteLine(await agent.RunAsync("And divide all by 2", session));
        }


        public static async Task RunWithToolsAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are the agent that shares information.", name: nameof(HelloAgent), tools: [AIFunctionFactory.Create(GetProcessInfo)]);

           await Helpers.RunConversationLoopAsync(agent);
        }

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
