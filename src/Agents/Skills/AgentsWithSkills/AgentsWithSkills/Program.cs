using AgentsWithSkills.AgentBasedSkills;
using AgentsWithSkills.FileBasedSkills;

namespace AgentsWithSkills
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //await AgentWithFileSkill.RunAsync();

            await AgentWithClassBasedSkill.RunAsync();
        }
    }
}
