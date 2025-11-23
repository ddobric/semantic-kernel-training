using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFoundrySkAgent
{
    internal class SemanticKernelAgent
    {
        public static async Task RunAsync(string[] args)
        {
            //PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(Environment.GetEnvironmentVariable("AgentEndpointurl")!, new AzureCliCredential());

            //// 1. Define an agent on the Azure AI agent service
            //PersistentAgent definition = await client.Administration.CreateAgentAsync(
            //    "gpt-4o",
            //    name: "SkAgent01",
            //    description: "Sample Agent",
            //    instructions: "Helper");

            //AzureAIAgent foundryAgent = new(definition, client);

            IKernelBuilder builder = Kernel.CreateBuilder();

            // Initialize multiple chat - completion services.
            builder.AddAzureOpenAIChatCompletion(
               deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,
               endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
               apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);

            builder.Plugins.AddFromObject(new MyPlugin());
            // Import plug-in from type
            //kernel.ImportPluginFromType<MyPlugin>();

            Kernel kernel = builder.Build();

            var agent = new ChatCompletionAgent()
            {
                Name = "MySKAgent",
                Instructions = "You are answering only scientific questions.",
                Kernel = kernel,
                Arguments = new KernelArguments(
                      new OpenAIPromptExecutionSettings()
                      {
                          FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                      })
            };

            await RunConversationLoopAsync(agent);
        }

        private static async Task RunConversationLoopAsync(Agent skAgent)
        {
            ChatHistoryAgentThread agentThread = new();

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;
                try
                {
                    ChatMessageContent message = new(AuthorRole.User, userInput);

                    //new ImageContent()
                    await foreach (StreamingChatMessageContent response in skAgent.InvokeStreamingAsync(message, agentThread))
                    {
                        Console.Write(response.Content);
                    }
                }
                finally
                {

                }
            }
        }
    }
}
