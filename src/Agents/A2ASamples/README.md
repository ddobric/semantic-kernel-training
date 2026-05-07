# A2A Protocol - Agent-to-Agent Communication Sample

This solution demonstrates how to build, host, and consume an AI agent using the **A2A (Agent-to-Agent) protocol** with .NET 10 and Azure AI Foundry.

> **A2A Specification**: [https://google.github.io/A2A/](https://google.github.io/A2A/)

---

## Solution Overview

The solution contains two projects:

| Project | Role | Description |
|---|---|---|
| **A2AHost** | Server | Hosts an AI-powered weather agent and exposes it over the A2A protocol. |
| **A2AConsumerApp** | Client | Discovers the remote agent via its Agent Card and sends requests. |

---

## Architecture

### High-Level Flow

```
+---------------------------+         +------------------------------------+
|    A2AConsumerApp          |         |         A2AHost                    |
|  (Agent Consumer)          |         |   (Agent Provider / Server)        |
|                            |         |                                    |
|  1. Fetch Agent Card       |-------->|  /.well-known/agent.json           |
|     from well-known URL    |<--------|  (returns AgentCard JSON)          |
|                            |         |                                    |
|  2. Send A2A request       |-------->|  /a2a/weather-agent                |
|     (tasks/send)           |<--------|  (A2A HTTP+JSON endpoint)          |
|                            |         |                                    |
|  3. Receive response       |         |  +----------------------------+    |
|                            |         |  |   Weather AIAgent          |    |
|                            |         |  |  +----------------------+  |    |
|                            |         |  |  | Tool: GetCities      |  |    |
|                            |         |  |  | Tool: GetWeather     |  |    |
|                            |         |  |  +----------------------+  |    |
|                            |         |  +----------------------------+    |
+---------------------------+         +------------------------------------+
```

### Agent Discovery Sequence

```
  Consumer                              Host
     |                                    |
     |  GET /.well-known/agent.json       |
     |----------------------------------->|
     |                                    |
     |  200 OK { AgentCard JSON }         |
     |<-----------------------------------|
     |                                    |
     |  POST /a2a/weather-agent           |
     |  { A2A JSON-RPC message }          |
     |----------------------------------->|
     |                                    |
     |  { A2A JSON-RPC response }         |
     |<-----------------------------------|
```

### Tool-Calling Flow

When the consumer sends a natural-language prompt, the A2A host forwards it to the LLM. The LLM may invoke registered tools during the conversation:

```
Consumer --> A2A Host --> LLM (Azure AI Foundry)
                               |
                          Tool call: GetCities()
                               |
                          <-- ["Seattle","New York",...]
                               |
                          Tool call: GetWeather("Seattle")
                               |
                          <-- "Sunny, 22C, Humidity: 65%"
                               |
                          Final answer
                               |
Consumer <-- A2A Host <-- LLM response
```

---

## What Is an Agent Card?

The **Agent Card** is the discovery mechanism defined by the A2A specification. Every A2A-compliant agent publishes a JSON document at the well-known URL:

```
GET /.well-known/agent.json
```

The Agent Card tells consumers everything they need to interact with the agent:

| Field | Purpose |
|---|---|
| `name` | Human-readable agent name |
| `description` | What the agent does |
| `supportedInterfaces` | List of protocol bindings and their endpoint URLs |
| `supportedInterfaces[].url` | The URL to send A2A messages to |
| `supportedInterfaces[].protocolBinding` | Transport type (e.g., `HttpJson`, `HttpSse`) |
| `supportedInterfaces[].protocolVersion` | A2A protocol version |

**Example Agent Card** (from this sample):

```json
{
  "name": "WeatherAgent",
  "description": "A helpful weather assistant.",
  "supportedInterfaces": [
    {
      "url": "http://localhost:5000/a2a/weather-agent",
      "protocolBinding": "HttpJson",
      "protocolVersion": "1.0"
    }
  ]
}
```

Agent Cards enable a **decentralized discovery model** -- consumers only need to know the host URL and can dynamically learn how to communicate with any agent.

---

## How to Implement an A2A Agent (Step by Step)

### Step 1 - Create the AI Agent with Tools

Define C# methods as tools using `[Description]` attributes and wrap them with `AIFunctionFactory.Create`:

```csharp
AITool getCitiesTool = AIFunctionFactory.Create(GetCities);
AITool getWeatherTool = AIFunctionFactory.Create(GetWeather);

AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: "gpt-4o-mini",
        instructions: "You are a helpful weather assistant.",
        name: "weather-agent",
        tools: [getCitiesTool, getWeatherTool]);
```

### Step 2 - Register the A2A Server

```csharp
builder.Services.AddKeyedSingleton<AIAgent>("weather-agent", (sp, _) => agent);
builder.AddA2AServer("weather-agent");
```

### Step 3 - Map the A2A Endpoint

```csharp
app.MapA2AHttpJson("weather-agent", "/a2a/weather-agent");
```

### Step 4 - Publish the Agent Card

```csharp
app.MapWellKnownAgentCard(new AgentCard
{
    Name = "WeatherAgent",
    Description = "A helpful weather assistant.",
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url = "http://localhost:5000/a2a/weather-agent",
            ProtocolBinding = ProtocolBindingNames.HttpJson,
            ProtocolVersion = "1.0",
        }
    ]
});
```

### Step 5 - Consume the Agent (Client Side)

```csharp
A2ACardResolver resolver = new(new Uri("http://localhost:5000/"));
AIAgent agent = await resolver.GetAIAgentAsync();
Console.WriteLine(await agent.RunAsync("What is the weather in Seattle?"));
```

---

## Running the Sample

### Prerequisites

- .NET 10 SDK
- An Azure AI Foundry project endpoint
- Set the environment variable or `appsettings.json`:
  - `AZURE_AI_PROJECT_ENDPOINT` -- your Azure AI project endpoint
  - `AZURE_AI_MODEL_DEPLOYMENT_NAME` -- (optional, defaults to `gpt-4o-mini`)

### Run

**Terminal 1** - Start the agent host:

```bash
cd A2AHost
dotnet run
```

**Terminal 2** - Run the consumer:

```bash
cd A2AConsumerApp
dotnet run
```

---

## References

- **A2A Protocol Specification**: [https://google.github.io/A2A/](https://google.github.io/A2A/)
- **A2A GitHub Repository**: [https://github.com/google/A2A](https://github.com/google/A2A)
- **Microsoft Agents SDK**: [https://github.com/microsoft/Agents](https://github.com/microsoft/Agents)
