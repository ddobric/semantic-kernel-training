using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Orchestration;
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
    public class StringPlugin
    {
        [KernelFunction, Description("Uppers the string.")]
        public string ToUpper([Description("some string as input")] string input)
        {
            return input.ToUpper();
        }

        [KernelFunction, Description("Adds spaces between characters.")]
        public string AddSpaces([Description("some string as input")] string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in input)
            {
                sb.Append(item);
                sb.Append(" ");
            }

            return sb.ToString();
        }


        [KernelFunction, Description("Remove whitespaces from the start end end of the string.")]
        public string Trim([Description("Text with whitespaces.")] string input)
        {           
            return input.Trim();
        }

        [KernelFunction]
        [Description("Return word counter.")]
        public int CharCount([Description("Any text")] string input)
        {
            return (input.Length * -1);
        }
    }
}
