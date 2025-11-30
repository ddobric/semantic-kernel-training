using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFoundrySkAgent
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    internal class SemanticKernelFoundryAgentSample
    {
        // NOT COMPLETED. DEPRECTED
        public static async Task RunAsync()
        {
            PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(Environment.GetEnvironmentVariable("AgentEndpointurl")!, new AzureCliCredential());
            var agent = client.GetAIAgentAsync("asst_qOM7Mh9TtFw2tGbjFfP4fGst");

            AIProjectClient projectClient =
                new(new Uri(Environment.GetEnvironmentVariable("AgentEndpointUrl")!),
                new DefaultAzureCredential());

            PersistentAgentsClient agentsClient = projectClient.GetPersistentAgentsClient();


            PersistentAgent foundryAgent = agentsClient.Administration.GetAgent("asst_qOM7Mh9TtFw2tGbjFfP4fGst");

            AzureAIAgent skAgent = new(foundryAgent, agentsClient);

            await RunConversationLoopAsync(skAgent, agentsClient);
        }


        private static async Task RunConversationLoopAsync(AzureAIAgent skAgent, PersistentAgentsClient agentsClient)
        {
            AzureAIAgentThread agentThread = new(skAgent.Client);

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    ChatMessageContent message;

                    message = new(AuthorRole.User, userInput);
                    
                    var response1 = skAgent.InvokeAsync(message, agentThread);
                   


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
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

}
