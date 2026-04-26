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
using System;
using OpenAI.Chat;


namespace AgentFramework_Samples.MCP
{
    /// <summary>
    /// Demonstrates how to invoke MCP tools via STDIO (locally).
    /// </summary>
    internal class LocalHostedMcpTool
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            //
            // To use thiss MCP server see the project src\Mcp\MonkeyMCP.
            // This project demonstrates how to implement the MCP server that can be hosted locally using STDIO transport.
            await using var mcpSampleClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "MCPMonkey",
                Command = "C:\\dev\\git\\semantic-kernel-training\\src\\Mcp\\MonkeyMCP\\bin\\Debug\\net9.0\\MonkeyMCP.exe",
                Arguments = [],
            }));

            await ListMcpToolsAsync(mcpSampleClient);

            var sampleServerMcpTools = await mcpSampleClient.ListToolsAsync().ConfigureAwait(false);

            //
            // Create an MCPClient for the GitHub server
            await using var mcpGitHubClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "MCPServer",
                Command = "npx",
                Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
            }));

            await ListMcpToolsAsync(mcpGitHubClient);

            var mcpGithubTools = await mcpGitHubClient.ListToolsAsync().ConfigureAwait(false);

            var allTools = sampleServerMcpTools.Concat(mcpGithubTools).Cast<AITool>().ToArray();

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                 .GetChatClient(deploymentName)
                 .AsAIAgent(instructions: "You answer questions related to GitHub repositories only.", 
                 tools: allTools);
            Console.WriteLine();

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Summarize the last four commits to the microsoft/semantic-kernel repository?"));

            await Helpers.RunConversationLoopAsync(agent);
        }

        private static async Task ListMcpToolsAsync(McpClient mcpMonkeyClient)
        {
            foreach (var item in await mcpMonkeyClient.ListToolsAsync())
            {
                Console.WriteLine(item.Name);
            }
        }
    }
}
