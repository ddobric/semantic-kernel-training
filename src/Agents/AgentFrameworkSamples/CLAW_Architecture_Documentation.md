# CLAW Architecture: Three-Agent Workflow System

## Architecture Overview

The **CLAW (Command Line Agent Workflow)** system implements a sophisticated three-agent architecture for decomposing, orchestrating, and executing complex user tasks. This document describes the architecture visualized in the generated sequence diagram.

---

## 🏗️ Architecture Components

### 1. **Intent Agent** 🧠 (Decompose)
**Primary Role:** Analyze user intent and create execution plans

**Instructions:**
- Analyze the user's intent carefully
- Decompose tasks into sequential plan of concrete steps
- Call `CreateAndExecutePlanAsync` tool ONCE with the list of steps
- Each step must contain:
  - `instructions`: Detailed execution instructions
  - `description`: Human-readable summary
  - `type`: Classification (cli/browser/reasoning)
- After execution, summarize results to user
- Be thorough: each step should be atomic and self-contained
- Include installation or prerequisite steps if needed
- ⚠️ Never plan destructive commands without clear warnings

**Tools Available:**
- `CreateAndExecutePlanAsync` (via IntentOrchestrator)

---

### 2. **Plan Agent** ⚙️ (Orchestrate)
**Primary Role:** Execute plan steps sequentially and coordinate Task Agent

**Instructions:**
- Receive plan (list of steps) from Intent Agent
- Call `ExecutePlan` tool ONCE with the complete plan
- Each step will be executed by Task Agent (has CLI and browser tools)
- After execution, summarize results of all steps
- **Alternative Path:** For CLI-only plans, may call `RunCommandLineAsync` to build Agent Framework Workflow
- When executing CLI commands: Use cmd with `/c <command>` or PowerShell with `-NoProfile -Command <cmd>`
- Cannot directly call CLI tools like 'git' or 'dotnet' - must be invoked through cmd/pwsh

**Tools Available:**
- `ExecutePlanAsync` (via PlanOrchestrator)
- `RunCommandLineAsync` (for CLI-only workflow path)

---

### 3. **Task Agent** 🔨 (Execute)
**Primary Role:** Execute individual tasks using available tools

**Instructions:**
- Receive specific task to execute along with context from previous steps
- Execute task using best available tools:
  - `ExecuteCliCommandAsync` for CLI/PowerShell commands
  - Playwright tools for browser automation tasks
  - MS Learn tools for accessing Microsoft documentation
  - Memory tools (RememberKey, PrintRememberKeyValues)
- For reasoning or analysis tasks, perform them directly
- Always return clear, concise result describing what was done and output
- If execution fails, explain error and suggest alternatives

**Tools Available:** 🧰
- `ExecuteCliCommandAsync` - Execute CLI/PowerShell commands
- **Playwright MCP** - Browser automation (npx @playwright/mcp)
- **MS Learn MCP** - Microsoft documentation access (https://learn.microsoft.com/api/mcp)
- `RememberKeyAsync` - Store key-value pairs in memory
- `PrintRememberKeyValues` - Display all stored key-value pairs

---

## 🔄 Execution Flow (Sequence)

### Main Flow:

1. **User → Intent Agent**
   - User provides natural language prompt describing task
   - Example: "Clone a git repository and build the solution"

2. **Intent Agent → Plan Agent**
   - Intent Agent analyzes prompt and creates `PlanStep[]`
   - Each step has instructions, description, and type
   - Calls `CreateAndExecutePlanAsync` with plan
   - Plan passed to Plan Agent

3. **Plan Agent → Task Agent** (Per Step Iteration)
   - Plan Agent iterates through each step
   - For each step, creates task-specific prompt with:
	 - Step instructions
	 - Context from previous steps
	 - Step metadata (number, description)
   - User approval required: **[Y]es / [S]kip / [A]bort / [U]nattended**
   - Invokes Task Agent with prompt

4. **Task Agent → Tools**
   - Task Agent analyzes task requirements
   - Selects appropriate tool(s) to execute
   - Calls tool functions (CLI, Playwright, MS Learn, Memory)
   - Each CLI command also requires user approval

5. **Task Agent → Plan Agent**
   - Task Agent returns execution result
   - Result includes output, errors, or completion status
   - Context accumulated for subsequent steps

6. **Plan Agent → Intent Agent**
   - After all steps complete, Plan Agent returns consolidated results
   - Results include all step outputs and context

7. **Intent Agent → User**
   - Intent Agent summarizes execution results
   - Provides final output to user

---

## 📋 Alternative: CLI-Only Workflow Path

When **all steps are simple CLI commands**, Plan Agent can use an alternative execution path:

### RunCommandLineAsync Flow:
- Plan Agent calls `RunCommandLineAsync` with `ClawTask[]`
- Builds **Agent Framework Workflow** with one `CommandExecutor` per step
- Each CommandExecutor:
  - Displays step information
  - Prompts for user approval: [Y]es / [S]kip / [A]bort / [U]nattended
  - Executes CLI command via Process
  - Returns output and exit code
  - Emits `CommandCompletedEvent`
- Workflow executes sequentially with explicit edges
- Results streamed back via `InProcessExecution`

**Advantage:** Better suited for pure CLI automation scenarios with structured workflow tracking.

---

## 🔑 Key Features

### ✓ User Approval (Interceptor Pattern)
- Every step and CLI command requires user confirmation
- Options: **[Y]es** | **[S]kip** | **[A]bort** | **[U]nattended**
- **[U]nattended mode:** Auto-executes all remaining tasks without prompts
- Safety mechanism prevents unintended destructive operations

### ✓ Context Passing
- Results from previous steps passed to subsequent tasks
- Task Agent receives full execution context
- Enables dependent operations and informed decision-making
- Context accumulates as plan progresses

### ✓ MCP Integration (Model Context Protocol)
- **Playwright MCP:** Browser automation via stdio transport
  - Command: `npx @playwright/mcp@latest`
  - Provides web scraping, form filling, navigation tools
- **MS Learn MCP:** Documentation access via HTTP transport
  - Endpoint: `https://learn.microsoft.com/api/mcp`
  - Provides Microsoft documentation search and retrieval

### ✓ Flexible Execution
- **Direct Tool Calls:** Task Agent calls tools directly
- **Agent Framework Workflow:** CLI-only plans use structured workflow
- Plan Agent intelligently chooses execution method

### ✓ Memory Store
- `RememberKeyAsync`: Store information as key-value pairs
- `PrintRememberKeyValues`: Retrieve stored information
- Persistent across task execution within session
- Useful for sharing data between steps

---

## 📊 Data Structures

### PlanStep
```csharp
public sealed class PlanStep
{
	[JsonPropertyName("instructions")]
	public required string Instructions { get; set; }  // Detailed step instructions

	[JsonPropertyName("description")]
	public required string Description { get; set; }   // Human-readable summary

	[JsonPropertyName("type")]
	public string? Type { get; set; }                  // "cli", "browser", "reasoning"
}
```

### ClawTask (CLI Workflow)
```csharp
public sealed class ClawTask
{
	[JsonPropertyName("executable")]
	public required string Executable { get; set; }    // e.g., "cmd", "pwsh"

	[JsonPropertyName("arguments")]
	public string Arguments { get; set; }              // Command arguments

	[JsonPropertyName("description")]
	public required string Description { get; set; }   // Task description
}
```

---

## 🔗 Orchestrator Classes

### IntentOrchestrator
- Bridges Intent Agent and Plan Agent
- Implements `CreateAndExecutePlanAsync` tool
- Maintains Plan Agent session
- Serializes plan to JSON for Plan Agent

### PlanOrchestrator
- Bridges Plan Agent and Task Agent
- Implements `ExecutePlanAsync` tool
- Iterates through plan steps
- Manages context passing between steps
- Handles user approval for each step
- Accumulates and returns consolidated results

---

## ⚙️ Technical Details

### Shared ChatClient
All three agents use the **same Azure OpenAI ChatClient**:
```csharp
ChatClient chatClient = new AzureOpenAIClient(
	new Uri(endpoint),
	new DefaultAzureCredential())
	.GetChatClient(deploymentName);
```

### Agent Configuration
- **Intent Agent:** Tools = [CreateAndExecutePlanAsync]
- **Plan Agent:** Tools = [ExecutePlanAsync, RunCommandLineAsync]
- **Task Agent:** Tools = [ExecuteCliCommand, Playwright tools, MS Learn tools, Memory tools]

### Session Management
- Intent Agent maintains one session with Plan Agent
- Plan Agent creates fresh Task Agent session per step
- Fresh sessions ensure isolation but context passed explicitly via prompts

---

## 🎯 Use Cases

### Example 1: Git Repository Setup
**User:** "Clone the Semantic Kernel repository and build the solution"

**Intent Agent Creates:**
- Step 1: Clone repository (type: cli)
- Step 2: Navigate to solution directory (type: cli)
- Step 3: Restore NuGet packages (type: cli)
- Step 4: Build solution (type: cli)

**Execution:** Each CLI command executed via Task Agent, user approves each step

---

### Example 2: Web Research Task
**User:** "Find the latest Azure OpenAI documentation and summarize pricing"

**Intent Agent Creates:**
- Step 1: Search MS Learn for Azure OpenAI docs (type: browser)
- Step 2: Navigate to pricing page (type: browser)
- Step 3: Extract pricing information (type: reasoning)
- Step 4: Summarize findings (type: reasoning)

**Execution:** Task Agent uses Playwright and MS Learn tools, combines results

---

### Example 3: Complex Multi-Step Workflow
**User:** "Clone repo, find all failing tests, save results, and create summary report"

**Intent Agent Creates:**
- Step 1: Clone repository (type: cli)
- Step 2: Run test suite (type: cli)
- Step 3: Parse test results (type: reasoning)
- Step 4: Store failed tests in memory (type: cli)
- Step 5: Generate summary report (type: reasoning)

**Execution:** Mixed CLI, reasoning, memory tools; context flows between steps

---

## 🛡️ Safety Mechanisms

1. **User Approval:** Every step requires explicit confirmation
2. **Unattended Mode:** Optional [U] for trusted/long-running operations
3. **Destructive Command Warning:** Intent Agent flags dangerous operations
4. **Error Handling:** Task Agent reports errors with suggested alternatives
5. **Timeout Protection:** CLI commands timeout after 60 seconds
6. **Process Isolation:** CLI commands run in separate process with controlled environment

---

## 📈 Benefits of This Architecture

1. **Separation of Concerns:**
   - Intent Agent: Planning and decomposition
   - Plan Agent: Orchestration and coordination
   - Task Agent: Execution and tool invocation

2. **Modularity:**
   - Easy to add new tools to Task Agent
   - Easy to modify orchestration logic in Plan Agent
   - Easy to improve planning in Intent Agent

3. **Reusability:**
   - Task Agent reused for every step
   - Tools available across all task types
   - Orchestrators provide composable patterns

4. **User Control:**
   - Visibility into execution plan before running
   - Granular approval for each step
   - Ability to skip or abort at any point
   - Unattended mode for trusted scenarios

5. **Context Awareness:**
   - Results flow between steps naturally
   - Task Agent makes informed decisions
   - Dependencies handled automatically

6. **Extensibility:**
   - MCP protocol enables easy tool integration
   - New agent types can be added
   - Alternative execution paths supported

---

## 🔧 Configuration

### Required Environment:
- Azure OpenAI endpoint and deployment
- Node.js (for Playwright MCP via npx)
- PowerShell or cmd.exe for CLI execution
- Internet connection for MS Learn MCP

### MCP Servers:
- **Playwright:** stdio transport via `npx @playwright/mcp@latest`
- **MS Learn:** HTTP transport via `https://learn.microsoft.com/api/mcp`

---

## 📝 Summary

The **CLAW Three-Agent Architecture** provides a robust, safe, and flexible system for executing complex user tasks. By separating intent analysis, orchestration, and execution into distinct agents with clear responsibilities, the system achieves:

- **Intelligent task decomposition**
- **Controlled sequential execution**
- **Rich tool integration via MCP**
- **User safety and transparency**
- **Context-aware operation**
- **Extensible and maintainable design**

This architecture is particularly well-suited for:
- CLI automation workflows
- Web research and data extraction tasks
- Multi-step software development operations
- Documentation and learning tasks
- Any scenario requiring structured, supervised automation

---

**Generated for:** `SimpleClawSession.cs`  
**Architecture Type:** Three-Agent CLAW (Command Line Agent Workflow)  
**Framework:** Microsoft Agents AI + Model Context Protocol (MCP)
