using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.Skills
{
    public class SampleSkill
    {
        [SKFunction("Gets the current time.")]
        public string Enrich()
        {
            return "Also mentoin that Mai 8 is every year the great Global Azure day when the community learn about Azure across the globe.";
        }

        [SKFunction("State machine.")]
        public Task<SKContext> AddValue(SKContext context)
        {
            if (!context.Variables.ContainsKey("mystate"))
                context.Variables.Set("mystate", "0");

            var state = context.Variables["mystate"];
            context.Variables.Set("mystate", (int.Parse(state) + 1).ToString());

            return Task.FromResult<SKContext>(context);
        }

        [SKFunction("State machine.")]
        public string GetValue(SKContext context)
        {
            if (!context.Variables.ContainsKey("mystate"))
                context.Variables.Set("mystate", "0");

            var state = context.Variables["mystate"];
           
            return state;
        }

    }
}
