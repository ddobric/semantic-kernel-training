# Agent Framework Samples

This solution demonstrates how to build AI agents using the **Microsoft Agent Framework** with Azure OpenAI and OpenAI backends. The entry point for the hosted Azure scenarios is the `HelloAgent` class in `HostedAgentsWithAzureFoundryModels/1_HelloAgent.cs`.

## Prerequisites

- .NET 10
- An Azure OpenAI resource with a deployed chat model
- Environment variables configured (see [Configuration](#configuration))
- Azure CLI logged in (`az login`) for `DefaultAzureCredential`

## Configuration

| Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `AZURE_OPENAI_MODEL_DEPLOYMENT` | Model deployment name |
| `OPENAI_API_KEY` | OpenAI API key (for non-Azure samples) |
| `OPENAI_CHAT_MODEL_NAME` | OpenAI model name (defaults to `gpt-5.4-mini`) |

---

## Agent Scenarios

### Scenario 1 — Agent Construction and Basic Usage (`RunAsync`)

The simplest scenario: create an agent and invoke it.

**How it works:**

1. An `AzureOpenAIClient` is created using `DefaultAzureCredential` for authentication.
2. A `ChatClient` is obtained for the target model deployment.
3. The `AsAIAgent()` extension method wraps the `ChatClient` into an `AIAgent`, which is the central abstraction in the Agent Framework. You provide `instructions` (the system prompt) and a `name`.
4. The agent is invoked in two ways:
   - **Non-streaming** (`RunAsync`) — sends the prompt and waits for the complete `AgentResponse`.
   - **Streaming** (`RunStreamingAsync`) — returns an `IAsyncEnumerable<AgentResponseUpdate>`, yielding incremental text chunks as they arrive from the model. This is ideal for real-time console or UI output.

```
AzureOpenAIClient → ChatClient → AIAgent
                                     ↓
                              RunAsync("prompt")  →  AgentResponse (full text)
                              RunStreamingAsync()  →  AgentResponseUpdate (chunks)
```

---

### Scenario 2 — Sessions and Multi-turn Conversations (`RunMultiturnAsync`)

Demonstrates the difference between stateless and stateful (session-based) conversations.

**The problem with stateless calls:**

By default, each call to `RunAsync` is independent. The agent has no memory of what was said before. If you ask *"Calculate 1+2+...+10"* and then *"Now add 1"*, the second call has no idea what *"all"* refers to — it lacks conversational context.

**The solution — `AgentSession`:**

An `AgentSession` object accumulates the conversation history (all user and assistant messages) across multiple `RunAsync` calls. When you pass the same session to each call, the agent sees the full conversation and can correctly interpret follow-up prompts.

```
Without session:                    With session:
┌─────────────┐                     ┌─────────────┐
│ "Sum 1..10" │ → 55                │ "Sum 1..10" │ → 55        ← history: [turn 1]
│ "Add 1"     │ → ??? (no context)  │ "Add 1"     │ → 56        ← history: [turn 1, 2]
│ "Divide /2" │ → ??? (no context)  │ "Divide /2" │ → 28        ← history: [turn 1, 2, 3]
└─────────────┘                     └─────────────┘
```

**Key API:**

- `agent.CreateSessionAsync()` — creates a new empty session.
- `agent.RunAsync(prompt, session)` — invokes the agent within the session context.

---

### Scenario 3 — Function Tools (`RunWithToolsAsync`)

Shows how to give the agent access to local C# methods it can call autonomously.

**How it works:**

1. A regular C# method (`GetProcessInfo`) is decorated with `[Description]` attributes that tell the agent what the function does and what each parameter means.
2. `AIFunctionFactory.Create(GetProcessInfo)` wraps the method into an `AITool` that the Agent Framework can register with the model.
3. The `tools` parameter of `AsAIAgent()` registers the tool. When the agent determines a user question requires the tool (e.g., *"Show me running processes"*), it automatically:
   - Generates a function call with the appropriate arguments.
   - Executes the local C# method.
   - Incorporates the result into its response.

**Tool definition pattern:**

```csharp
[Description("What this tool does — visible to the model")]
static string MyTool(
    [Description("Parameter description — visible to the model")] string param)
{
    // Your logic here
    return result;
}
```

The `[Description]` attributes are critical — they are the only way the model knows when and how to use the tool.

---

## Project Structure

| Path | Description |
|---|---|
| `HostedAgentsWithAzureFoundryModels/1_HelloAgent.cs` | Core scenarios (this document) |
| `HostedAgentsWithAzureFoundryModels/AgentWithMemory.cs` | Custom memory / context providers |
| `HostedAgentsWithAzureFoundryModels/AgentsInWorkflow.cs` | Multi-agent workflow orchestration |
| `OpenAIAgents/OpenAISamples.cs` | Direct OpenAI (non-Azure) samples |
| `OpenAIAgents/OpenAIReasoningSamples.cs` | Reasoning models with streaming |
| `MCP/` | Model Context Protocol tool integration |
| `Helpers.cs` | Shared utilities (console loop, config) |
