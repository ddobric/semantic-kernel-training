using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;


namespace McpClient
{
    internal class Program
    {
        /// <summary>
        /// https://github.com/modelcontextprotocol/csharp-sdk
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            SseClientTransportOptions opts = new SseClientTransportOptions { 
             Endpoint = new Uri("http://localhost:3001/sse"),
            };


            SseClientTransport  sseTransport= new SseClientTransport(opts);

            var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "Everything",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            });

            var client = await McpClientFactory.CreateAsync(clientTransport);

            // Print the list of tools available from the server.
            foreach (var tool in await client.ListToolsAsync())
            {
                Console.WriteLine($"{tool.Name} ({tool.Description})");
            }

            // Execute a tool (this would normally be driven by LLM tool invocations).
            var result = await client.CallToolAsync(
                "Echo",
                new Dictionary<string, object?>() { ["message"] = "Hello MCP!" },
                cancellationToken: CancellationToken.None);

            // echo always returns one and only one text content object
            Console.WriteLine(result.Content.First(c => c.Type == "text").Text);
        }
    }
}
