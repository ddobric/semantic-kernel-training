using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
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

    internal class AgentWithFileSkill
    {
        public static async Task RunAsync()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";

            var fileOptions = new AgentFileSkillsSourceOptions
            {
                AllowedResourceExtensions = [".md", ".txt", ".py", ".jpg", ".json"],
            };

            var opts = new AgentSkillsProviderOptions()
            {
                 
            };

            //var skillsProvider2 = new AgentSkillsProvider(
            //    Path.Combine(AppContext.BaseDirectory, "FileBasedSkills\\Skills"),
            //    fileOptions: fileOptions);

            // --- Logger Factory Setup ---
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            // --- Skills Provider ---
            // Discovers skills from the 'skills' directory containing SKILL.md files.
            // The script runner runs file-based scripts (e.g. Python) as local subprocesses.
            var skillsProvider = new AgentSkillsProvider(
                Path.Combine(AppContext.BaseDirectory, "FileBasedSkills\\Skills"),  
                SubprocessScriptRunner.RunAsync,
                loggerFactory: loggerFactory,
                options: opts,
                fileOptions: fileOptions);

            // Discover skills from the 'skills' directory
            //var skillsProvider = new AgentSkillsProvider(
            //    Path.Combine(AppContext.BaseDirectory, "Skills"));

            // --- Agent Setup ---
            AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                .GetResponsesClient()
                .AsAIAgent(new ChatClientAgentOptions
                {
                    Name = "SkilledAgent",
                    ChatOptions = new()
                    {
                        Instructions = "You are a helpful assistant that can convert units.",
                    },
                    AIContextProviders = [skillsProvider],
                },
                model: deploymentName);

            // --- Example: Unit conversion ---
            Console.WriteLine(new string('-', 60));

            AgentResponse response = await agent.RunAsync("How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms? Use unit-converter skill.");

            Console.WriteLine($"Agent: {response.Text}");

            response = await agent.RunAsync("List available skills.");

            Console.WriteLine($"Agent: {response.Text}");

            response = await agent.RunAsync("Create a scientific paper from c:\\temp\\brk245_summary.docx. Author/Affiliation Damir Dobric. Title: Crazzy SKill Test. Use default cover: yes. Output path: crazzyskilltest.pdf. If you cannot create it provide exact instructions how to create environment.");

            Console.WriteLine($"Agent: {response.Text}");
        }
    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

