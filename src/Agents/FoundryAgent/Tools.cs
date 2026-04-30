using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FoundryAgentDemo
{
    /// <summary>
    /// Contains tool functions that can be exposed to AI agents for function calling.
    /// </summary>
    internal static class Tools
    {
        /// <summary>
        /// Tool function: returns a formatted list of running processes.
        /// The [Description] attributes provide the agent with metadata to decide when and how to call it.
        /// </summary>
        [Description("Get the information about running processes.")]
        public static string GetProcessInfo([Description("The location to get the weather for.")] string location)
        {
            StringBuilder sb = new StringBuilder();

            var processses = Process.GetProcesses();

            foreach (var process in processses)
            {
                sb.AppendLine($"{process.Id,8} | {process.ProcessName,-40} | Threads: {process.Threads.Count,4} | Memory: {process.WorkingSet64 / 1024.0 / 1024.0,8:F2} MB");
            }

            return sb.ToString();
        }
    }
}
