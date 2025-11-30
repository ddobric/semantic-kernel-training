using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ComponentModel;

namespace AzureFoundrySkAgent
{
    internal class AgentFramework_WorkflowSample
    {
        public static async Task RunAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!;
            
            var deploymentName = "gpt-4o";

            // Create a service collection to hold the agent plugin and its dependencies.
            ServiceCollection services = new();
            services.AddSingleton<WorkflowPlugin>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureCliCredential())
                .GetChatClient(deploymentName)
                .CreateAIAgent(
                    instructions: "You are a helpful assistant that runs workflow. The workflow is a sequence of execution of tasks, which are implemented as plugin functions.",
                    name: "Assistant",
                    tools: [.. serviceProvider.GetRequiredService<WorkflowPlugin>().AsAITools()],
                    services: serviceProvider); // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.

            await RunConversationLoopAsync(agent);
        }

        private static async Task RunConversationLoopAsync(AIAgent agent)
        {
            Microsoft.Agents.AI.AgentThread thread = agent!.GetNewThread();

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    await foreach (var update in agent.RunStreamingAsync(userInput, thread))
                    {
                        Console.Write(update);
                    }
                }
                finally
                {

                }
            }
        }

        /// <summary>
        /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input "C".
        /// 
        /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input of the result of step 2.
        /// 
        /// execute the step1 with input "A", then the step2 with input "B" and then pass to step3 the result of step 2.
        ///
        /// execute the step1 with input "A", then the step2 with input "B" and then step3 with input of the result of step 2 if the result of step1 is 1.
        /// </summary>
        internal class WorkflowPlugin
        {
            [Description("Executes the task 1.")]
            public Task<int> Task1([Description("Input of task.")] string input)
            {
                Console.WriteLine($"\nTask 1 - input='{input}'\n");
                return Task.FromResult<int>(1);
            }


            [Description("Executes the task 2.")]
            public Task<int> Task2([Description("The input of the task.")] string input)
            {
                Console.WriteLine($"\nTask 2 - input='{input}'\n");
                return Task.FromResult<int>(2);
            }

            [Description("Executes the step 3.")]
            public Task<int> Task3([Description("The input for the task.")] string input)
            {
                Console.WriteLine($"\nTask 3 - input='{input}'\n");
                return Task.FromResult<int>(3);
            }

            public IEnumerable<AITool> AsAITools()
            {
                yield return AIFunctionFactory.Create(this.Task1);
                yield return AIFunctionFactory.Create(this.Task2);
                yield return AIFunctionFactory.Create(this.Task3);
            }
        }
    }
}
