using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace VibeNetAdapte
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, MCP Server!");

            NetAdapterTool.RunPowerShell("aaa");

            // try also this: https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/
            var builder = WebApplication.CreateBuilder(args);

            McpServerOptions options = new McpServerOptions()
            {

            };

            builder.Services
                .AddMcpServer()
                .WithHttpTransport(httpTransportOptions =>
                {
                    httpTransportOptions.RunSessionHandler = (httpContext, mcpServer, cancellationToken) =>
                    {
                        // TODO...
                        httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader);

                        return mcpServer.RunAsync(cancellationToken);
                    };
                })
                .WithTools<NetAdapterTool>();             

            builder.Services.AddHttpClient();
    
            var app = builder.Build();

            app.MapMcp();

            app.Run();
        }
    }
}
