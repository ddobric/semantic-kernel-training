# MonkeyMCPSSE — MCP Server over HTTP Transport

## Overview

**MonkeyMCPSSE** is a .NET 9 ASP.NET Core application that hosts a [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server over HTTP using the **Streamable HTTP** transport. Unlike the `MonkeyMCP` project (which uses stdio), this project exposes the MCP server as a network-accessible HTTP endpoint, making it suitable for remote clients and cloud deployments.

## How It Works

The MCP server is hosted inside an ASP.NET Core web application. The key setup in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(httpTransportOptions =>
    {
        httpTransportOptions.Stateless = true;
    })
    .WithTools<MonkeyTools>()
    .WithTools<EchoTool>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<MonkeyService>();

var app = builder.Build();
app.MapMcp();
app.Run();
```

### Key Concepts

| Concept | Description |
|---|---|
| **`WithHttpTransport()`** | Registers the MCP server to use HTTP as the transport layer instead of stdio. The server listens for MCP requests on an HTTP endpoint. |
| **`Stateless = true`** | Each HTTP request is handled independently without maintaining server-side session state. This makes the server horizontally scalable and suitable for load-balanced or serverless environments. |
| **`app.MapMcp()`** | Maps the MCP HTTP endpoint (default: `/mcp`) into the ASP.NET Core routing pipeline. Clients send MCP JSON-RPC messages to this endpoint. |
| **`WebApplication.CreateBuilder`** | Uses the standard ASP.NET Core host (with Kestrel), providing full support for HTTPS, middleware, logging, and configuration. |

### HTTP Transport vs. Stdio Transport

| Feature | Stdio (`MonkeyMCP`) | HTTP (`MonkeyMCPSSE`) |
|---|---|---|
| Communication | stdin / stdout | HTTP(S) over the network |
| Client launch | Client starts the server process | Server runs independently; client connects via URL |
| Use case | Local tool (VS Code, Claude Desktop) | Remote / cloud-hosted MCP server |
| Scalability | Single client | Multiple concurrent clients |
| Configuration | Command + args in MCP client config | Base URL in MCP client config |

## Available Tools

### Monkey Tools (`MonkeyTools`)

| Tool | Description |
|---|---|
| `GetMonkeys` | Returns a JSON list of all monkeys fetched from an external API. |
| `GetMonkey` | Returns details for a specific monkey by name. |

### Echo Tools (`EchoTool`)

| Tool | Description |
|---|---|
| `Echo` | Echoes the message back with a "Hello from C#:" prefix. |
| `ReverseEcho` | Returns the message reversed. |

## Services

- **`MonkeyService`** — Fetches monkey data from `https://www.montemagno.com/monkeys.json` and caches the results in memory.

## Project Structure

```
MonkeyMCPSSE/
??? Program.cs              # App entry point, MCP server + HTTP transport setup
??? EchoTool.cs             # Echo and ReverseEcho tool definitions
??? MonkeyTools.cs          # Monkey-related tool definitions
??? MonkeyService.cs        # Service for fetching monkey data + Monkey model
??? MonkeyMCPSSE.csproj     # Project file (net9.0, PublishAot enabled)
??? appsettings.json        # ASP.NET Core configuration
??? Properties/
    ??? launchSettings.json # Launch profiles (HTTPS on port 7133)
```

## Running the Server

```bash
cd MonkeyMCPSSE
dotnet run
```

The server starts on `https://localhost:7133` (as configured in `launchSettings.json`). The MCP endpoint is available at:

```
https://localhost:7133/mcp
```

## Connecting an MCP Client

Configure your MCP client (e.g., VS Code with GitHub Copilot, Claude Desktop) to connect via HTTP:

```json
{
    "servers": {
        "monkeymcp-sse": {
            "url": "https://localhost:7133/mcp"
        }
    }
}
```

## Dependencies

| Package | Version |
|---|---|
| `ModelContextProtocol.AspNetCore` | 0.3.0-preview.3 |
| `System.Text.Json` | 10.0.0-preview.6 |
