using AgentsWithSkills.AgentBasedSkills;

namespace AgentsWithSkills
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //await AgentWIthFileSkill.RunAsync();

            await AgentWithClassBasedSkill.RunAsync();
        }
    }
}
