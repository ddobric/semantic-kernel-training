using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using HostedAgentsWithAzureFoundryModels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System;
using System.Linq;


namespace AgentFramework_Samples.MCP
{
    /// <summary>
    /// Scenario 1: MCP tools via STDIO transport (locally hosted).
    /// Connects to two MCP servers running as local processes:
    ///   1. MonkeyMCP — a custom .NET MCP server (compiled executable)
    ///   2. GitHub MCP — the official @modelcontextprotocol/server-github (via npx)
    /// Tools from both servers are merged and registered with a single AI agent.
    /// </summary>
    internal class LocalHostedMcpTool
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // ── MCP Server 1: MonkeyMCP (local .NET executable) ──
            // This is a custom MCP server built as a .NET console app.
            // It communicates over STDIO — the Agent Framework spawns the process
            // and exchanges JSON-RPC messages via stdin/stdout.
            // See project: src\Mcp\MonkeyMCP
            await using var mcpSampleClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "MCPViaSTDIO",
                Command = "C:\\dev\\git\\semantic-kernel-training\\src\\Mcp\\MonkeyMCP\\bin\\Debug\\net9.0\\MonkeyMCP.exe",
                Arguments = [],
            }));

            await ListMcpToolsAsync(mcpSampleClient);
            var sampleServerMcpTools = await mcpSampleClient.ListToolsAsync().ConfigureAwait(false);

            // ── MCP Server 2: GitHub MCP (via npx) ──
            // The official GitHub MCP server provides tools for repository operations
            // (list repos, read files, search code, etc.). It is launched via npx
            // which downloads and runs the package on demand.
            await using var mcpGitHubClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "MCPServer",
                Command = "npx",
                Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
            }));

            await using var mcpMsLearningClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MSLearning",
                Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
            }));

            await ListMcpToolsAsync(mcpGitHubClient);
            var mcpGithubTools = await mcpGitHubClient.ListToolsAsync().ConfigureAwait(false);

            // Merge tools from both MCP servers into a single array for the agent.
            var allTools = sampleServerMcpTools.Concat(mcpGithubTools).Cast<AITool>().ToArray();

            var mcpMsLearningTools = await mcpMsLearningClient.ListToolsAsync().ConfigureAwait(false);
            allTools = allTools.Concat(mcpMsLearningTools.Cast<AITool>()).ToArray();

            // Create an agent with all MCP tools registered.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                 .GetChatClient(deploymentName)
                 .AsAIAgent(instructions: "You are helpful agent who answers only questions which can be answered with the help of loaded tools.",
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
