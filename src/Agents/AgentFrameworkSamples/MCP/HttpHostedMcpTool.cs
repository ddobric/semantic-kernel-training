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
    /// Scenario 2: MCP tools via HTTP Streamable transport (remotely hosted).
    /// Connects to two MCP servers over HTTP:
    ///   1. MonkeyMCP — a custom MCP server hosted as a web service (localhost for dev)
    ///   2. Microsoft Learn — the official MS Learn MCP endpoint (public API)
    /// Unlike STDIO, no local process is spawned — the servers run independently
    /// and communication happens over HTTP streaming (JSON-RPC over HTTP).
    /// </summary>
    internal class HttpHostedMcpTool
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // ── MCP Server 1: MonkeyMCP (HTTP, locally hosted web service) ──
            // Same MonkeyMCP server as in Scenario 1, but hosted as an ASP.NET web app
            // instead of a console STDIO process. Uses HTTP Streamable transport.
            await using var mcpSampleClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MCPMonkey",
                Endpoint = new Uri("https://localhost:7133")
            }));

            // ── MCP Server 2: Microsoft Learn (HTTP, publicly hosted) ──
            // The official Microsoft Learn MCP endpoint exposes tools for searching
            // documentation, retrieving articles, and querying learning paths.
            // This is a public API — no authentication required.
            await using var mcpMsLearningClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MSLearning",
                Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
            }));

            // Discover and merge tools from both HTTP MCP servers.
            var sampleTools = await mcpSampleClient.ListToolsAsync();
            var msLearningTools = await mcpMsLearningClient.ListToolsAsync();
            var allTools = sampleTools.Concat(msLearningTools).Cast<AITool>().ToArray();

            // Display available tools from each server.
            await ListMcpToolsAsync(mcpSampleClient);
            await ListMcpToolsAsync(mcpMsLearningClient);

            // Create an agent with all HTTP MCP tools registered.
            AIAgent agent = new AzureOpenAIClient(
              new Uri(endpoint),
              new DefaultAzureCredential())
               .GetChatClient(deploymentName)
               .AsAIAgent(instructions: "You are helpful agent who answers only questions which can be answered with the help of loaded tools.",
               tools: allTools.ToArray());

            // Interactive conversation loop.
            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Lists all tools exposed by an MCP server in a tree-formatted console output.
        /// Displays the server name, tool count, and each tool's name with description.
        /// </summary>
        private static async Task ListMcpToolsAsync(McpClient mcpClient)
        {
            var tools = await mcpClient.ListToolsAsync();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"┌── MCP Server: {mcpClient.ServerInfo.Name} ({tools.Count} tools)");
            Console.ResetColor();

            for (int i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                bool isLast = i == tools.Count - 1;
                string prefix = isLast ? "└── " : "├── ";

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{prefix}{tool.Name}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(tool.Description))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"  — {tool.Description}");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }
    }
}
