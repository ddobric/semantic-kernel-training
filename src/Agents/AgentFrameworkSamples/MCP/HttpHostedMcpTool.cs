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
    internal class HttpHostedMcpTool
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            await using var mcpSampleClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MCPMonkey",
                Endpoint = new Uri("https://localhost:7133")
            }));

            await using var mcpMsLearningClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = "MSLearning",
                Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
            }));

            //#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            //            var mcpTool = ResponseTool.CreateMcpTool(
            //                serverLabel: "microsoft_learn",
            //                serverUri: new Uri("https://learn.microsoft.com/api/mcp"),
            //                toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
            //#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var sampleTools = await mcpSampleClient.ListToolsAsync();
            var msLearningTools = await mcpMsLearningClient.ListToolsAsync();

            var allTools = sampleTools.Concat(msLearningTools).Cast<AITool>().ToArray();

            await ListMcpToolsAsync(mcpSampleClient);
            await ListMcpToolsAsync(mcpMsLearningClient);

            AIAgent agent = new AzureOpenAIClient(
              new Uri(endpoint),
              new DefaultAzureCredential())
               .GetChatClient(deploymentName)
               .AsAIAgent(instructions: "You answer questions related to GitHub repositories only.",
               tools: allTools.ToArray());

            await Helpers.RunConversationLoopAsync(agent);
        }

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
