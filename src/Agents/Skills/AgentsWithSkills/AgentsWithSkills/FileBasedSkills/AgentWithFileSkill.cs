using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AgentsWithSkills.FileBasedSkills
{

    // This sample demonstrates how to use file-based Agent Skills with a ChatClientAgent.
    // Skills are discovered from SKILL.md files on disk and follow the progressive disclosure pattern:
    // 1. Advertise — skill names and descriptions in the system prompt
    // 2. Load — full instructions loaded on demand via load_skill tool
    // 3. Read resources — reference files read via read_skill_resource tool
    // 4. Run scripts — scripts executed via run_skill_script tool with a subprocess executor
    //
    // This sample uses a unit-converter skill that converts between miles, kilometers, pounds, and kilograms.

    internal class AgentWIthFileSkill
    {
        public static async Task RunAsync()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";

            var fileOptions = new AgentFileSkillsSourceOptions
            {
                AllowedResourceExtensions = [".md", ".txt"],
                ResourceDirectories = ["docs", "templates"],
            };

            var skillsProvider = new AgentSkillsProvider(
                Path.Combine(AppContext.BaseDirectory, "skills"),
                fileOptions: fileOptions);

            // Discover skills from the 'skills' directory
            //var skillsProvider = new AgentSkillsProvider(
            //    Path.Combine(AppContext.BaseDirectory, "Skills"));

            // --- Agent Setup ---
            AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                .GetResponsesClient()
                .AsAIAgent(new ChatClientAgentOptions
                {
                    Name = "UnitConverterAgent",
                    ChatOptions = new()
                    {
                        Instructions = "You are a helpful assistant that can convert units.",
                    },
                    AIContextProviders = [skillsProvider],
                },
                model: deploymentName);

            // --- Example: Unit conversion ---
            Console.WriteLine("Converting units with file-based skills");
            Console.WriteLine(new string('-', 60));

            AgentResponse response = await agent.RunAsync(
                "How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

            Console.WriteLine($"Agent: {response.Text}");
        }
    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

