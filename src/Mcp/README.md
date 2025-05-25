# Monkey - Model Context Protocol (MCP) Server

## Overview
This is a Model Context Protocol (MCP) server implementation built with .NET 9.0. The MCP server provides a communication protocol for facilitating interactions between various components in a model-driven system. This implementation demonstrates how to set up a basic MCP server with custom tools and services.

## Try it

Configure in VS Code with GitHub Copilot, Claude Desktop, or other MCP clients:

```json
{
    "inputs": [],
    "servers": {
        "monkeymcp": {
            "command": "docker",
            "args": [
                "run",
                "-i",
                "--rm",
                "jamesmontemagno/monkeymcp"
            ],
            "env": {}
        }
    }
}
```

## Features

### Core Components
- **MCP Server**: Built using the ModelContextProtocol library (version 0.1.0-preview.2)
- **Standard I/O Transport**: Uses stdio for communication with clients
- **Custom Tool Integration**: Includes examples of how to create and register MCP tools

### Services
- **MonkeyService**: A sample service that fetches monkey data from an API endpoint
  - Provides methods to retrieve a list of all monkeys or find a specific monkey by name
  - Caches results for better performance

### Available Tools
The server exposes several tools that can be invoked by clients:

#### Monkey Tools
- **GetMonkeys**: Returns a JSON serialized list of all available monkeys
- **GetMonkey**: Retrieves information about a specific monkey by name

#### Echo Tool
- **Echo**: A simple tool that echoes back the provided message with a "hello" prefix

## Configuration Options

### Hosting Configuration
The server uses Microsoft.Extensions.Hosting (version 9.0.3) which provides:
- Configuration from multiple sources (JSON, environment variables, command line)
- Dependency injection for services
- Logging capabilities

### Logging Options
Several logging providers are available:
- **Console**: Logs to standard output
- **Debug**: Logs for debugging purposes
- **EventLog**: Logs to the system event log (when running on Windows)
- **EventSource**: Provides ETW (Event Tracing for Windows) integration

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Basic understanding of the Model Context Protocol (MCP)

### Running the Server
1. Clone this repository
2. Navigate to the project directory
3. Build the project: `dotnet build`
4. Configure with VS Code or other client:

```json
"monkeyserver": {
    "type": "stdio",
    "command": "dotnet",
    "args": [
        "run",
        "--project",
        "/Users/jamesmontemagno/GitHub/TestMCP/MonkeyMCP/MonkeyMCP.csproj"
    ]
}
```

> Update the path to the project

### Extending the Server
To add custom tools:
1. Create a class and mark it with the `[McpServerToolType]` attribute
2. Add methods with the `[McpServerTool]` attribute
3. Optionally add `[Description]` attributes to provide documentation

Example:
```csharp
[McpServerToolType]
public static class CustomTool
{
    [McpServerTool, Description("Description of what the tool does")]
    public static string ToolMethod(string param) => $"Result: {param}";
}
```

## Server-Sent Events Implementation (MonkeyMCPSSE)

### Overview
The `MonkeyMCPSSE` project provides an alternative implementation of the Monkey MCP server using Server-Sent Events (SSE) over HTTP instead of stdio transport. This implementation runs as a web server, making it ideal for web-based clients and scenarios requiring HTTP-based communication.

> Read more about [SSE best practices for security here](https://modelcontextprotocol.io/docs/concepts/transports#security-warning%3A-dns-rebinding-attacks)

### Features
- **HTTP-based Transport**: Runs on `http://localhost:3001` by default
- **Server-Sent Events**: Enables real-time, one-way communication from server to client
- **ASP.NET Core Integration**: Built using ASP.NET Core's web server capabilities
- **MCP over HTTP**: Implements the Model Context Protocol over HTTP transport

### Running the SSE Server
1. Navigate to the MonkeyMCPSSE directory
2. Build and run the project:
3. Connect and run in VS Code or using MCP Inspector `npx @modelcontextprotocol/inspector`

### Implementation Details
The SSE implementation uses ASP.NET Core's built-in web server capabilities while maintaining the same monkey data service and tools as the stdio version. This makes it easy to switch between transport methods while keeping the core functionality intact.

## Project Structure
- **/MonkeyMCP**: Main project directory
  - **MonkeyService.cs**: Implementation of the service to fetch monkey data
  - **MonkeyTools.cs**: MCP tools for accessing monkey data
  - **Program.cs**: Entry point that configures and starts the MCP server

## Dependencies
- **Microsoft.Extensions.Hosting** (9.0.3): Provides hosting infrastructure
- **ModelContextProtocol** (0.1.0-preview.2): MCP server implementation
- **System.Text.Json** (9.0.3): JSON serialization/deserialization

## License
This project is available under the MIT License.
