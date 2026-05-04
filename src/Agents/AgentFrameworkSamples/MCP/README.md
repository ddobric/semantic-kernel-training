# MCP (Model Context Protocol) Samples

Demonstrates integrating **MCP servers** as tool providers for AI agents using the Microsoft Agent Framework. MCP is an open protocol that allows AI models to discover and invoke tools exposed by external servers via a standardized JSON-RPC interface.

## Two Transport Scenarios

These samples show the two primary ways to connect to MCP servers:

```
???????????????????????????????????????????????????????????????
?                        AI Agent                             ?
?                  (Azure OpenAI model)                       ?
???????????????????????????????????????????????????????????????
           ?                                  ?
    STDIO Transport                    HTTP Transport
    (Scenario 1)                       (Scenario 2)
           ?                                  ?
           ?                                  ?
????????????????????                ????????????????????
?  Local Process   ?                ?  Remote Server   ?
?  (spawned by     ?                ?  (runs           ?
?   the client)    ?                ?   independently) ?
?                  ?                ?                  ?
?  stdin ???? stdout                ?  HTTP streaming  ?
?  (JSON-RPC)      ?                ?  (JSON-RPC)      ?
????????????????????                ????????????????????
```

---

## Scenario 1 — STDIO Transport (`LocalHostedMcpTool.cs`)

Connects to MCP servers that run as **local processes**. The `McpClient` spawns the executable and communicates over **stdin/stdout** using JSON-RPC messages.

### MCP Servers Bound

| Server | Transport | How It's Hosted | Description |
|---|---|---|---|
| **MonkeyMCP** | STDIO | .NET console app (compiled `.exe`) | A custom MCP server built as a .NET project. Located at `src\Mcp\MonkeyMCP`. The client spawns the executable directly. |
| **GitHub MCP** | STDIO | npm package (via `npx`) | The official `@modelcontextprotocol/server-github` package. Launched via `npx -y` which downloads and runs it on demand. Provides tools for repository operations (commits, files, search). |

### How STDIO Hosting Works

1. The `StdioClientTransport` is configured with the executable path (or `npx` command) and arguments.
2. `McpClient.CreateAsync()` spawns the process in the background.
3. The client sends JSON-RPC requests to the process's **stdin** and reads responses from **stdout**.
4. When the `McpClient` is disposed (`await using`), the process is terminated.

```csharp
// .NET executable — spawned directly
await using var client = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPMonkey",
    Command = "path/to/MonkeyMCP.exe",
    Arguments = [],
}));

// npm package — spawned via npx
await using var client = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "GitHub",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"],
}));
```

### Example Prompts

```
> Summarize the last four commits to the microsoft/semantic-kernel repository
> What files changed in the latest commit?
> Search for "workflow" in the microsoft/agent-framework repo
```

---

## Scenario 2 — HTTP Streamable Transport (`HttpHostedMcpTool.cs`)

Connects to MCP servers that run as **remote web services**. Communication happens over HTTP streaming — no local process is spawned.

### MCP Servers Bound

| Server | Transport | How It's Hosted | Description |
|---|---|---|---|
| **MonkeyMCP** | HTTP | ASP.NET web app (`https://localhost:7133`) | The same MonkeyMCP server as Scenario 1, but hosted as a web service instead of a console app. Useful for shared/team scenarios. |
| **Microsoft Learn** | HTTP | Public API (`https://learn.microsoft.com/api/mcp`) | The official Microsoft Learn MCP endpoint. Provides tools for searching documentation, retrieving articles, and querying learning paths. No authentication required. |

### How HTTP Hosting Works

1. The `HttpClientTransport` is configured with the server's HTTP endpoint URL.
2. `McpClient.CreateAsync()` connects to the running server over HTTP.
3. The client sends JSON-RPC requests as HTTP POST bodies and receives streaming responses.
4. The server must already be running — the client does not manage its lifecycle.

```csharp
// Local web service
await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
{
    Name = "MCPMonkey",
    Endpoint = new Uri("https://localhost:7133")
}));

// Public API
await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
{
    Name = "MSLearning",
    Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
}));
```

### Example Prompts

```
> Search Microsoft Learn for articles about Azure Functions
> What documentation is available for the Agent Framework?
> Find tutorials about deploying to Azure Container Apps
```

---

## STDIO vs HTTP — When to Use Which

| | STDIO | HTTP |
|---|---|---|
| **Server lifecycle** | Client spawns and manages the process | Server runs independently |
| **Deployment** | Single machine, dev/local scenarios | Shared, remote, cloud-hosted |
| **Setup** | Just need the executable or `npx` command | Server must be running and reachable |
| **Security** | Process isolation on same machine | Network-level security (TLS, auth) |
| **Scalability** | One process per client | Many clients share one server |

## Prerequisites

- Azure OpenAI endpoint with a deployed chat model
- Azure CLI logged in (`az login`)
- **For STDIO (Scenario 1):**
  - MonkeyMCP built: `dotnet build src\Mcp\MonkeyMCP`
  - Node.js installed (for `npx` / GitHub MCP)
- **For HTTP (Scenario 2):**
  - MonkeyMCP web app running on `https://localhost:7133`
  - Internet access for `https://learn.microsoft.com/api/mcp`

## Configuration

| Environment Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Model deployment name |

## Project Structure

| File | Description |
|---|---|
| `LocalHostedMcpTool.cs` | Scenario 1 — STDIO transport (MonkeyMCP + GitHub MCP) |
| `HttpHostedMcpTool.cs` | Scenario 2 — HTTP transport (MonkeyMCP + Microsoft Learn) |
