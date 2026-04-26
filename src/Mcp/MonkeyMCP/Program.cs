using Microsoft.Extensions.Hosting;
using MonkeyMCP;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;

// Create a bare host builder without any default configuration.
// This is suitable for console-based MCP servers that communicate via standard I/O (stdin/stdout).
var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// --- Alternative: HTTP Transport (SSE / Streamable HTTP) ---
// Instead of stdio, the MCP server can be hosted over HTTP using ASP.NET Core.
// This is done in the MonkeyMCPSSE project, which uses WebApplication.CreateBuilder()
// and registers the MCP server with .WithHttpTransport().
// The HTTP transport exposes the MCP server as an HTTP endpoint (e.g., /mcp),
// enabling remote clients to connect over the network.
// When Stateless = true, each HTTP request is handled independently without
// server-side session state, making it suitable for scalable, stateless deployments.
// Example:
//   builder.Services
//       .AddMcpServer()
//       .WithHttpTransport(o => o.Stateless = true)
//       .WithTools<Tools>();

// Register the MCP server with the stdio transport.
// Stdio transport communicates via stdin/stdout, which is the standard way
// for local MCP clients (e.g., VS Code, Claude Desktop) to launch and talk to an MCP server.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<EchoTool>()
    .WithTools<MonkeyTools>();

// Register HttpClient factory for outbound HTTP calls (used by MonkeyService).
builder.Services.AddHttpClient();

// Register MonkeyService as a singleton so monkey data is cached across tool invocations.
builder.Services.AddSingleton<MonkeyService>();

Console.WriteLine("Started MCP server");

// Build and run the host. The MCP server listens on stdin and writes responses to stdout.
await builder.Build().RunAsync();

// --- Inline Tool Definition ---
// Tools can also be defined inline in the same file using the [McpServerToolType] attribute.
// Each method marked with [McpServerTool] becomes an invocable tool exposed by the MCP server.
[McpServerToolType]
public class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Hello from C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message) => new string(message.Reverse().ToArray());
}