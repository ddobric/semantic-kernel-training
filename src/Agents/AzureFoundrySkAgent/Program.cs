using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AzureFoundrySkAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello Foundy Agent by Semantic Kernel!");

            string agentName = "Semantic Kernel Agent Sample";
            string modelName = "gpt-4o";
            string connectionString = Environment.GetEnvironmentVariable("AgentConnStr")!;

            AIProjectClient client = AzureAIAgent.CreateAzureAIClient(connectionString, new AzureCliCredential());

            AgentsClient agentsClient = client.GetAgentsClient();

            Response<PageableList<Agent>> agentListResponse = await agentsClient.GetAgentsAsync();

            Console.WriteLine("Listing agents in the foundry project...");

            Azure.AI.Projects.Agent? foundryAgentDefinition = null;
            AzureAIAgent agent;

            foreach (var foundyAgent in agentListResponse.Value)
            {
                if(foundyAgent.Name == agentName)
                {
                    foundryAgentDefinition = foundyAgent;
                    break;
                }
               
                Console.WriteLine($"Agent: {foundyAgent.Name} - {foundyAgent.Id}");
            }

            Console.WriteLine("------------------------");

            if (foundryAgentDefinition == null)
            {
                foundryAgentDefinition = await agentsClient.CreateAgentAsync(
               modelName,
                name: agentName,
                description: "Sample Agent Created by Semantic Kernel Agent Framework.",
                instructions: "You are the agent who helps answering IT related questions only.");
            }

            agent = new(foundryAgentDefinition, agentsClient);

            Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);

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
                    //await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))

                    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
                    {
                        Console.Write(response.Content);
                    }
                }
                finally
                {
                   
                }
            }

            //await agentThread.DeleteAsync();
            //await agent.Client.DeleteAgentAsync(agent.Id);
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
