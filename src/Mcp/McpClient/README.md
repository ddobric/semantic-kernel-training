# MCP Client Sample

A .NET 9 console application that demonstrates how to connect to a **Model Context Protocol (MCP)** server and use its tools as part of an **Azure OpenAI** chat completion loop.

## Overview

The sample shows two integration patterns:

| Pattern | Transport | Use case |
|---------|-----------|----------|
| **Remote HTTP server** | `HttpClientTransport` (Streamable HTTP) | Production / shared MCP servers |
| **Local stdio server** | `StdioClientTransport` | Local development with the MCP "Everything" reference server |

### How it works

```
User prompt ??? Azure OpenAI (gpt-4.1)
                      ?
                      ?? text reply ??? console
                      ?
                      ?? tool_call ??? MCP Server ??? tool result
                                                         ?
                                                         ???? fed back into chat history
```

1. The app connects to an MCP server and discovers available tools.
2. Tool metadata is converted into Azure AI `ChatCompletionsToolDefinition` objects.
3. User prompts are sent to Azure OpenAI together with the tool definitions.
4. When the model responds with a tool call, the app forwards it to the MCP server via `CallToolAsync`.
5. The tool result is appended to the conversation history and the loop continues.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An **Azure OpenAI** resource with a `gpt-4.1` deployment
- A running **MCP server** (or Node.js/npx for the stdio variant)

## Configuration

| Setting | Location | Description |
|---------|----------|-------------|
| `AZURE_OPENAI_API_KEY` | Environment variable | API key for your Azure OpenAI resource |
| MCP server endpoint | `Program.McpServerEndpoint` constant | URL of the remote MCP server (default: `https://localhost:7133`) |
| Azure OpenAI endpoint | `Program.AzureOpenAiEndpoint` constant | Full deployment URL for the model |

## Running the sample

```bash
# Set the API key
export AZURE_OPENAI_API_KEY="<your-key>"

# Run the app
dotnet run
```

The app will list the tools exposed by the MCP server, run a quick "echo" smoke-test, and then present an interactive `>` prompt where you can chat with the model.

## Project structure

```
??? Program.cs          # Application entry point, MCP client setup, and chat loop
??? McpClient.csproj    # Project file with NuGet dependencies
??? Properties/
?   ??? launchSettings.json
?   ??? launchSettings.prod.json
??? README.md           # This file
```

## Key dependencies

| Package | Purpose |
|---------|---------|
| [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) | MCP C# SDK – client transports, tool invocation |
| [`Azure.AI.Inference`](https://www.nuget.org/packages/Azure.AI.Inference) | Azure AI Inference client for chat completions |
| [`Anthropic.SDK`](https://www.nuget.org/packages/Anthropic.SDK) | Anthropic types (e.g., `TextContentBlock`) |

## License

See the repository root for license information.
