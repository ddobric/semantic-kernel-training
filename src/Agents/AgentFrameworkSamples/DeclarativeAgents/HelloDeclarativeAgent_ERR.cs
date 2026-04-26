using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework_Samples.DeclarativeAgents
{
    internal class HelloDeclarativeAgent
    {

        public async Task RunAsyn()
        {
            // Create the chat client
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            IChatClient chatClient = new AIProjectClient(
                new Uri("<your-foundry-project-endpoint>"),
                new DefaultAzureCredential())
                    .GetProjectOpenAIClient()
                    .GetProjectResponsesClient()
                    .AsIChatClient("gpt-4o-mini");
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Define the agent using a YAML definition.
            var yamlDefinition =
                """
    kind: Prompt
    name: Assistant
    description: Helpful assistant
    instructions: You are a helpful assistant. You answer questions in the language specified by the user. You return your answers in a JSON format.
    model:
        options:
            temperature: 0.9
            topP: 0.95
    outputSchema:
        properties:
            language:
                type: string
                required: true
                description: The language of the answer.
            answer:
                type: string
                required: true
                description: The answer text.
    """;

            // Create the agent from the YAML definition.
            var agentFactory = new ChatClientPromptAgentFactory(chatClient);
            var agent = await agentFactory.CreateFromYamlAsync(yamlDefinition);

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent!.RunAsync("Tell me a joke about a pirate in English."));

            // Invoke the agent with streaming support.
            await foreach (var update in agent!.RunStreamingAsync("Tell me a joke about a pirate in French."))
            {
                Console.WriteLine(update);
            }
        }
    }
}
