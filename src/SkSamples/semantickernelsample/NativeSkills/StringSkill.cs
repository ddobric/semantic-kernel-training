using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.Skills
{
    /// <summary>
    /// Implements the string manipulation functionalities.
    /// </summary>
    public class StringSkill
    {
        [SKFunction, Description("Uppers the string.")]
        public string ToUpper([Description("some string as input")] string input, SKContext context)
        {
            return input.ToUpper();
        }

        [SKFunction, Description("Adds spaces between characters.")]
        public string AddSpaces([Description("some string as input")] string input, SKContext context)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in input)
            {
                sb.Append(item);
                sb.Append(" ");
            }

            return sb.ToString();
        }


        [SKFunction, Description("Remove whitespaces from the start end end of the string.")]
        public string Trim([Description("Text with whitespaces.")] string input, SKContext context)
        {           
            return input.Trim();
        }
    }
}
