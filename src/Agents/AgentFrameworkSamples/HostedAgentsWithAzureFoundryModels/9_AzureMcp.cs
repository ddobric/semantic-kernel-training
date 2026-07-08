using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentFramework_Samples.HostedAgentsWithAzureFoundryModels
{
    public class AzureMcp
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);           

            // ── MCP Server 2:Azure MCP (via npx) ──
            await using var mcpGitHubClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "AZURE MCPServer",
                Command = "npx",
                //Arguments = ["-y", "--verbose", "@azure/mcp-server"],
                Arguments= [  "-y", "@azure/mcp@latest", "server", "start"
            ]
             //   Arguments = ["Azure.Mcp", "--source", "https://api.nuget.org/v3/index.json", "--yes", "--", "azmcp", "server", "start"],
            }));

            await using var mcpMsLearningClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MSLearning",
                Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
            }));

            await ListMcpToolsAsync(mcpGitHubClient);

            await ListMcpToolsAsync(mcpMsLearningClient);

            var mcpGithubTools = await mcpGitHubClient.ListToolsAsync().ConfigureAwait(false);

            // Merge tools from both MCP servers into a single array for the agent.
            var mcpMsLearningTools = await mcpMsLearningClient.ListToolsAsync().ConfigureAwait(false);

            var allTools = mcpGithubTools.Concat(mcpMsLearningTools.Cast<AITool>()).ToArray();

            // Create an agent with all MCP tools registered.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                 .GetChatClient(deploymentName)
                 .AsAIAgent(instructions: "You are helpful agent who helps managing microsoft azure.",
                 tools: allTools);

            Console.WriteLine();

            // Single-shot invocation to demonstrate tool usage.
            Console.WriteLine(await agent.RunAsync("Summarize the last four commits to the microsoft\\agent-framework repository?"));

            // Interactive conversation loop for follow-up questions.
            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Lists all tools exposed by an MCP server to the console.
        /// </summary>
        private static async Task ListMcpToolsAsync(McpClient mcpMonkeyClient)
        {
            foreach (var item in await mcpMonkeyClient.ListToolsAsync())
            {
                Console.WriteLine(item.Name);
            }
        }
    }
}
