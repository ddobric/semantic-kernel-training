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

### Scenario 4 — Agent with Memory / Context Providers (`2_AgentWithMemory.cs`)

Demonstrates how to give an agent **persistent memory** across turns using `AIContextProvider`.

**The problem:**

By default, agents have no way to "remember" user-specific facts (name, preferences, etc.) beyond the current session's chat history. If you serialize/deserialize a session or start a new one, that implicit context is lost.

**The solution — `AIContextProvider`:**

A custom `UserInfoMemory` component that:
1. **Extracts** the user's name and age from conversation messages (using the LLM itself for extraction).
2. **Injects** remembered facts into the agent's context on every invocation — so the agent always knows the user's name even in a brand-new session.
3. **Serializes** with the session — `SerializeSessionAsync` / `DeserializeSessionAsync` preserve the memory state.

```
 Turn 1: "Hello" ──► Agent asks for name (memory empty)
 Turn 2: "I'm Damir" ──► UserInfoMemory extracts name via LLM
 Turn 3: "What's my name?" ──► Agent reads from memory: "Damir"
          │
    Serialize session ──► JSON includes memory state
          │
    Deserialize ──► Memory restored, agent still knows "Damir"
```

**Key concepts demonstrated:**

| Concept | How it's shown |
|---|---|
| **`AIContextProvider`** | `UserInfoMemory` extends `AIContextProvider` with `ProvideAIContextAsync` (inject) and `StoreAIContextAsync` (extract). |
| **`ProviderSessionState<T>`** | Type-safe, per-session state storage. Each session gets its own `UserInfo` instance. |
| **LLM-based extraction** | Uses `GetResponseAsync<UserInfo>()` with structured output to extract name/age from messages. |
| **Session serialization** | `SerializeSessionAsync` / `DeserializeSessionAsync` round-trip the session including memory state. |
| **Cross-session memory** | `SetValue()` copies memory from one session to a new session, sharing facts without sharing chat history. |

---

## Workflows

The Agent Framework includes a lightweight workflow engine that connects **executors** (processing units) via typed message edges into a directed graph. Workflows can range from simple deterministic pipelines to complex AI-driven feedback loops.

Ref: 
https://kowshik.github.io/JPregel/pregel_paper.pdf

### Sequential Workflow  (`RunAsync`)

The simplest possible workflow — a **Sequential Workflow** with no AI involved. It demonstrates the core workflow mechanics in isolation.

**Two Step Workflow:**

```
Input: "Hello, World!"
       │
       ▼
┌──────────────────┐       ┌──────────────────────┐
│ UppercaseExecutor│──────▶│ ReverseTextExecutor  │──── Output
│  (lambda)        │       │  (custom Executor)   │
└──────────────────┘       └──────────────────────┘
       ↓                          ↓
  "HELLO, WORLD!"           "!DLROW ,OLLEH"
```

**Key concepts demonstrated:**

| Concept | How it's shown |
|---|---|
| **Lambda executor** | A `Func<string, string>` bound as an executor via `BindAsExecutor()` — no subclassing needed for simple transforms. |
| **Delegate executor** | `ReverseTextExecutor` extends `Executor<string, string>` with a typed `HandleAsync` method. |
| **WorkflowBuilder** | `AddEdge(a, b)` connects executors; `WithOutputFrom(b)` marks which executor yields the final result. |
| **Synchronous run** | `InProcessExecution.RunAsync` runs the workflow to completion and returns a `Run` object with all `NewEvents`. |
| **ExecutorCompletedEvent** | Each executor emits this event when it finishes, carrying the executor ID and output data. |

**When to use this pattern:** Pre/post-processing pipelines, data transformation chains, or any workflow where you want deterministic step-by-step processing without LLM calls.

---

### Inter-Executor Messaging (`RunWithMessagingAsync`)

Extends the linear pipeline to a **four-step chain** and introduces two advanced workflow features: **inter-executor messaging** via `SendMessageAsync` and **custom domain events** via `AddEventAsync`.

**Pipeline:**

```
Input: "Hello, World!"
       │
       ▼
┌──────────────────┐     ┌─────────────────────┐     ┌────────────────┐     ┌────────────────┐
│ UppercaseExecutor│────▶│ ReverseTextExecutor │────▶│  MyExecutor2   │────▶│  MyExecutor1   │── Output
│  (lambda)        │     │  (Executor<s,s>)    │     │  (Executor<s,s>)│     │  (Executor)    │
└──────────────────┘     └─────────────────────┘     └───────┬────────┘     └───────┬────────┘
                                                             │                      │
                                                     SendMessageAsync ──────▶ [MessageHandler]
                                                       (MyEvent1)          Handle2Async(MyEvent1)
                                                                                    │
                                                                             AddEventAsync
                                                                              (MyEvent2)
```

**How messaging works:**

1. `MyExecutor2` processes the string from its edge input, then calls `context.SendMessageAsync(new MyEvent1(...))` to dispatch a typed message.
2. `MyExecutor1` has two `[MessageHandler]` methods — one for `string` (the edge data) and one for `MyEvent1` (the sent message). The workflow engine routes each message to the matching handler automatically.
3. When `MyExecutor1` handles the `MyEvent1`, it emits a `MyEvent2` via `context.AddEventAsync(...)`. This event appears in the `run.NewEvents` stream for watchers but does **not** trigger any executor — it is purely for observability.

**Key concepts demonstrated:**

| Concept | How it's shown |
|---|---|
| **`SendMessageAsync`** | `MyExecutor2` sends a `MyEvent1` message to any executor that can handle it, independent of the edge graph. |
| **`[MessageHandler]` routing** | `MyExecutor1` uses the untyped `Executor` base with `partial class` and `[MessageHandler]` attributes. The source generator wires up type-based dispatch so each handler receives only its matching message type. |
| **`[SendsMessage]` attribute** | Declared on `MyExecutor2` to tell the workflow engine which message types it may send — enables compile-time/build-time validation. |
| **Custom domain events** | `MyEvent2` is emitted via `AddEventAsync` for observability. Unlike `SendMessageAsync`, events do not route to executors — they are for external watchers only. |
| **Event stream inspection** | The event loop distinguishes `ExecutorCompletedEvent`, custom `MyEvent2` (highlighted in green), and other events. |

---

## Complex Workflow Example (`4_ComplexWorkflow.cs`)

Demonstrates a **multi-agent feedback loop** where two AI executors collaborate iteratively to produce a polished slogan.

### Concept

The workflow follows a graph-based execution model inspired by the [Pregel BSP pattern](https://kowshik.github.io/JPregel/pregel_paper.pdf). Two executors communicate by passing typed messages along directed edges:

```
  
 ┌─────────────┐                                         ┌─────────────────┐
 │   Caller     │  "Create a slogan for..."  (string)    │  SloganWriter    │
 │ (RunAsync)   │───────────────────────────────────────▶│  Executor        │
 └─────────────┘                                         └────────┬────────┘
                                                                  │
                             SUPERSTEP 1                          │
                             ──────────                           │
                                                                  │ HandleAsync(string)
                                                                  │  → LLM generates slogan
                                                                  │  → emits SloganGeneratedEvent
                                                                  │  → returns SloganResult
                                                                  │
                                                    SloganResult  │
                                                    (edge data)   │
                                                                  ▼
                                                         ┌─────────────────┐
                                                         │ FeedbackProvider │
                                                         │  Executor        │
                                                         └────────┬────────┘
                                                                  │
                             SUPERSTEP 2                          │
                             ──────────                           │
                                                                  │ HandleAsync(SloganResult)
                                                                  │  → LLM reviews slogan
                                                                  │  → emits FeedbackEvent
                                                                  │  → decides next action:
                                                                  │
                          ┌───────────────────────────────────────┼───────────────────────┐
                          │                                       │                       │
                          ▼                                       ▼                       ▼
                   rating ≥ 8                            attempts ≥ 3              otherwise
                   ┌──────────────┐                     ┌──────────────┐      ┌──────────────────┐
                   │ YieldOutput  │                     │ YieldOutput  │      │ SendMessage      │
                   │ "Accepted"   │                     │ "Rejected"   │      │ (FeedbackResult) │
                   │ + slogan     │                     │ + last slogan│      └────────┬─────────┘
                   └──────┬───────┘                     └──────┬───────┘               │
                          │                                    │                       │
                          ▼                                    ▼                       │
                   WorkflowOutputEvent                  WorkflowOutputEvent            │
                   (workflow ends)                      (workflow ends)                │
                                                                                      │
                                                                      FeedbackResult  │
                                                                      (via SendMsg)   │
                                                                                      ▼
                                                                             ┌─────────────────┐
                             SUPERSTEP 3..N                                  │  SloganWriter    │
                             ──────────────                                  │  Executor        │
                                                                             └────────┬────────┘
                                                                                      │
                                                                HandleAsync(FeedbackResult)
                                                                  │  → LLM revises slogan
                                                                  │  → emits SloganGeneratedEvent
                                                                  │  → returns SloganResult
                                                                  │
                                                                  ▼
                                                         (back to FeedbackProvider)
                                                         (loop continues until Accept or Reject)

 MESSAGE TYPES:
   Edge data:     string → SloganResult → FeedbackResult → SloganResult → ...
   Domain events: SloganGeneratedEvent, FeedbackEvent (observability only, no routing)
   Control:       YieldOutputAsync (terminates the workflow with a final string output)
```

### Components

| Component | Role |
|---|---|
| **`SloganWriterExecutor`** | Generates or revises slogans. Has two `[MessageHandler]` methods: one for the initial `string` prompt, one for `FeedbackResult` revisions. Uses structured JSON output (`ResponseFormat`). |
| **`FeedbackExecutor`** | Reviews slogans and produces a `FeedbackResult` with comments, a 1–10 rating, and suggested actions. Decides whether to accept, reject, or loop. |
| **`SloganResult`** | Data contract carrying the task description and generated slogan. |
| **`FeedbackResult`** | Data contract carrying review comments, numeric rating, and improvement actions. |
| **`SloganGeneratedEvent` / `FeedbackEvent`** | Custom `WorkflowEvent` subclasses emitted for observability — watchers see progress in real time. |

### Workflow Lifecycle

1. **Build the graph** — `WorkflowBuilder` connects the two executors with bidirectional edges and marks `FeedbackExecutor` as the output source.
2. **Run with streaming** — `InProcessExecution.RunStreamingAsync` starts the workflow. Events are consumed via `WatchStreamAsync()`.
3. **Iteration loop:**
   - The `SloganWriter` generates a slogan → emits `SloganGeneratedEvent`.
   - The `FeedbackProvider` reviews it → emits `FeedbackEvent`.
   - If the rating ≥ `MinimumRating` (default 8), the slogan is accepted via `YieldOutputAsync`.
   - If `MaxAttempts` (default 3) is reached, the workflow stops with the last slogan.
   - Otherwise, the `FeedbackResult` is sent back to `SloganWriter` for revision.
4. **Output** — The final accepted (or best-effort) slogan is yielded as a `WorkflowOutputEvent`.

### Key Patterns

- **Structured output** — Both executors use `ChatResponseFormat.ForJsonSchema<T>()` to force the model to return valid JSON matching the data contracts.
- **Session persistence** — Each executor lazily creates an `AgentSession`, preserving conversation history across loop iterations so the agent can build on prior feedback.
- **Message-based routing** — The `[MessageHandler]` attribute and `partial class` with source generators wire up type-based message dispatch automatically.
- **Observability** — Custom `WorkflowEvent` subclasses provide a structured event stream that callers can monitor, log, or display.

---

## Agents in Workflow (`5_AgentsInWorkflow.cs`)

Demonstrates using **AIAgents as executors** inside a workflow — each agent is a node in the graph, processing and forwarding messages.

### Concept

Three translation agents (French → Spanish → English) are chained in a linear pipeline. Each agent translates the text it receives into its target language and passes the result to the next.

```
Input: "Hello World!"
       │
       ▼
┌──────────────┐     ┌───────────────┐     ┌───────────────┐
│ French Agent │────▶│ Spanish Agent │────▶│ English Agent │──── Output
│  (translate) │     │  (translate)  │     │  (translate)  │
└──────────────┘     └───────────────┘     └───────────────┘
       ↓                    ↓                    ↓
  "Bonjour le monde!"  "¡Hola Mundo!"     "Hello World!"
```

**Key concepts demonstrated:**

| Concept | How it's shown |
|---|---|
| **Agents as executors** | `AIAgent` implements `IExecutor`, so agents can be added directly to `WorkflowBuilder` as graph nodes. |
| **`TurnToken`** | Agent-executors cache incoming messages and only start processing when they receive a `TurnToken`. This synchronizes multi-agent workflows. |
| **Streaming workflow** | `InProcessExecution.RunStreamingAsync` + `WatchStreamAsync()` provides real-time event observation. |
| **`AgentResponseUpdateEvent`** | Emitted as each agent produces streaming output — callers see which agent is responding and what it said. |
| **Error handling** | `WorkflowErrorEvent` and `ExecutorFailedEvent` provide structured error reporting per-executor. |

---

## Workflow as Agent (`6_WorkflowAsAgent.cs`)

> **⚠️ Work in progress** — This sample is currently commented out and not functional.

Demonstrates the concept of wrapping a workflow as an `AIAgent` so it can be invoked like any other agent (via `RunAsync` / `RunStreamingAsync`), while internally orchestrating a multi-step workflow.

The intended pattern:
```csharp
var workflowAsAgent = new WorkflowBuilder(frenchAgent)
    .AddEdge(frenchAgent, spanishAgent)
    .AddEdge(spanishAgent, englishAgent)
    .AddWorkflow()
    .AddAsAIAgent();  // Workflow exposed as a single AIAgent
```

---

## CLAW — Command Line Agent Workflow (`7_SimpleClawSession.cs`)

Demonstrates a **three-agent architecture** for decomposing and executing complex CLI/automation tasks:

```
User Prompt
     │
     ▼
┌─────────────┐  PlanStep[]   ┌─────────────┐  per step    ┌─────────────┐
│ Intent Agent│──────────────▶│  Plan Agent  │─────────────▶│  Task Agent │
│ (decompose) │               │ (orchestrate)│◀─────────────│  (execute)  │
└─────────────┘               └─────────────┘   result +   └─────────────┘
                                                 context    has: CLI tool
                                                            has: Playwright
```

### The Three Agents

| Agent | Role |
|---|---|
| **Intent Agent** | Takes the user's natural language prompt and decomposes it into a `PlanStep[]` — each step has instructions, a description, and a type (`cli`, `browser`, or `reasoning`). |
| **Plan Agent** | Orchestrates execution. For CLI-only plans, it can build an Agent Framework Workflow with one `CommandExecutor` per step. For mixed plans, it iterates through steps and invokes the Task Agent sequentially. |
| **Task Agent** | Executes a single task. Has access to `ExecuteCliCommandAsync` (runs CLI commands with user approval) and Playwright MCP tools (browser automation). Gets a fresh session per step but receives prior context via the prompt. |

### Key Features

- **Interceptor pattern** — Every step prompts the user for approval (`[Y]es / [S]kip / [A]bort`) before execution.
- **Playwright MCP** — Connected via stdio transport (`npx @playwright/mcp@latest`), giving the agent browser automation capabilities.
- **CLI workflow path** — CLI-only plans use `RunCommandLineAsync` to build a dynamic Agent Framework Workflow with `CommandExecutor` nodes.
- **Context chaining** — Each task result is accumulated as `previousContext` and passed to the next task's prompt.

---

## IoT Lighting Sample (`8_LightingSample.cs`)

Demonstrates **dependency injection (DI)** with the Agent Framework and function tools that simulate IoT device control.

### Concept

An AI agent controls a virtual light bulb through tool calls. The `LightPlugin` is registered in a DI `ServiceCollection` and resolved at runtime, showing how real-world services (databases, APIs, IoT hubs) can be injected into agent tools.

```
User: "Turn on the light"
       │
       ▼
┌─────────────────┐     ┌──────────────┐
│   Agent (LLM)   │────▶│ LightPlugin  │ ← resolved from DI
│                 │     │  ChangeState │ → updates IsOn, draws ASCII bulb
│                 │◀────│  GetState    │ → returns "on" / "off"
└─────────────────┘     └──────────────┘
```

### Tools

| Tool | Description |
|---|---|
| **`GetState`** | Returns the current light state (`"on"` or `"off"`). |
| **`ChangeState`** | Toggles the light and draws an ASCII light bulb in the top-right corner of the console — yellow with rays when on, dark gray when off. |
| **`LookupProduct`** | Demo tool simulating a product catalog lookup (CPDM system). |

### Key Concepts

| Concept | How it's shown |
|---|---|
| **Dependency injection** | `LightPlugin` is registered as a singleton in `ServiceCollection` and resolved via `GetRequiredService<LightPlugin>()`. |
| **`AsAITools()`** | Explicit tool exposure — only `GetState` and `ChangeState` are registered, not `LookupProduct`. Shows how to control which methods the agent can call. |
| **`services` parameter** | The `IServiceProvider` is passed to `AsAIAgent()` so the agent can resolve dependencies for tool invocations. |
| **Visual feedback** | `PaintBox()` draws an ASCII light bulb at a fixed console position, with rays when on and a status label (`ON`/`OFF`). |

### Example Prompts

```
> Turn on the light
> What is the current state of the light?
> Toggle the light off
> Is the light on or off?
```

## Project Structure

| Path | Description |
|---|---|
| `1_HelloAgent.cs` | Core agent scenarios: basic invocation, multi-turn sessions, function tools |
| `2_AgentWithMemory.cs` | Custom memory via `AIContextProvider` with session serialization |
| `3_SimpleWorkflow.cs` | Sequential workflow pipeline and inter-executor messaging |
| `4_ComplexWorkflow.cs` | AI-driven feedback loop (SloganWriter ↔ FeedbackProvider) |
| `5_AgentsInWorkflow.cs` | AIAgents as workflow executors (translation chain) |
| `6_WorkflowAsAgent.cs` | ⚠️ WIP — wrapping a workflow as an AIAgent |
| `7_SimpleClawSession.cs` | Three-agent CLAW session (Intent → Plan → Task execution) |
| `8_LightingSample.cs` | IoT light control with DI and ASCII visual feedback |
