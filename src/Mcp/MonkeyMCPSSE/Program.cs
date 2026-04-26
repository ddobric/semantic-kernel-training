using Microsoft.Extensions.Configuration.UserSecrets;
using ModelContextProtocol.Protocol;
using MonkeyMCPSSE;

// Print a startup banner to the console.
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║        🐒  MonkeyMCPSSE — MCP Server Demo  🐒      ║");
Console.WriteLine("║      Hosting MCP over HTTP (Streamable HTTP)        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// Create the ASP.NET Core web application builder.
// Unlike the stdio-based MonkeyMCP project, this uses a full web host (Kestrel)
// so the MCP server is accessible over HTTP(S) by remote clients.
var builder = WebApplication.CreateBuilder(args);

// Register the MCP server and configure it to use the HTTP transport.
// WithHttpTransport() exposes the MCP server as an HTTP endpoint instead of stdin/stdout.
// Clients send JSON-RPC messages to the /mcp endpoint via HTTP POST requests.
builder.Services
    .AddMcpServer()
     .WithHttpTransport(httpTransportOptions =>
     {
         // Stateless mode: each HTTP request is self-contained with no server-side session.
         // This enables horizontal scaling behind a load balancer or in serverless environments.
         httpTransportOptions.Stateless = true;

         // Optional: RunSessionHandler can be used to customize session lifecycle,
         // e.g., for authentication, logging, or custom cancellation handling.
         //httpTransportOptions.RunSessionHandler = (httpContext, mcpServer, cancellationToken) =>
         //{
         //    return mcpServer.RunAsync(cancellationToken);
         //};
     })
    // Register tool classes. Each public method with [McpServerTool] becomes an invocable tool.
    .WithTools<MonkeyTools>()
    .WithTools<EchoTool>();

// Reference: https://github.com/microsoft/mcp-for-beginners/blob/main/03-GettingStarted/06-http-streaming/solution/dotnet/Program.cs

// Register HttpClient factory for outbound HTTP calls (used by MonkeyService to fetch monkey data).
builder.Services.AddHttpClient();

// Register MonkeyService as a singleton to cache monkey data across requests.
builder.Services.AddSingleton<MonkeyService>();

var app = builder.Build();

// Map the MCP endpoint into the ASP.NET Core routing pipeline.
// By default this creates a POST endpoint at /mcp that accepts MCP JSON-RPC messages.
app.MapMcp();

// Print available endpoint info before starting.
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("  ✅ MCP endpoint:  /mcp");
Console.WriteLine("  🔧 Tools:        GetMonkeys, GetMonkey, Echo, ReverseEcho");
Console.WriteLine("  📡 Transport:    HTTP (Stateless)");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("  Connect your MCP client to: https://localhost:<port>/mcp");
Console.WriteLine("  Press Ctrl+C to stop the server.");
Console.WriteLine();

app.Run();