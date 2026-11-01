# SimpleClawSession - 3-Agent Workflow Diagram

This diagram summarizes how `SimpleClawSession` coordinates three agents:

1. **Intent Agent** takes the user intent and creates a plan.
2. **Plan Agent** takes that plan and breaks/orchestrates it into executable tasks.
3. **Task Agent** executes each task using installed tools (CLI, Playwright MCP, MS Learn MCP, memory helpers).

## High-level flow

```mermaid
flowchart LR
	U[User Intent]

	IA[Intent Agent\nCreate plan from intent]
	IO[IntentOrchestrator\nCreateAndExecutePlanAsync]

	PA[Plan Agent\nTransform/orchestrate plan into tasks]
	PO[PlanOrchestrator\nExecutePlanAsync]

	TA[Task Agent\nExecute task]

	T1[ExecuteCliCommandAsync]
	T2[Playwright MCP Tools]
	T3[MS Learn MCP Tools]
	T4[RememberKey / PrintRememberKeyValues]

	U --> IA
	IA --> IO
	IO --> PA
	PA --> PO
	PO -->|task per step| TA

	TA --> T1
	TA --> T2
	TA --> T3
	TA --> T4

	TA -->|step result + context| PO
	PO -->|aggregated results| PA
	PA -->|summary| IA
	IA -->|final response| U
```

## Sequence view

```mermaid
sequenceDiagram
	actor User
	participant IntentAgent as Intent Agent
	participant IntentOrch as IntentOrchestrator
	participant PlanAgent as Plan Agent
	participant PlanOrch as PlanOrchestrator
	participant TaskAgent as Task Agent
	participant Tools as Installed Tools

	User->>IntentAgent: Provide intent
	IntentAgent->>IntentOrch: CreateAndExecutePlanAsync(PlanStep[])
	IntentOrch->>PlanAgent: Send generated plan

	PlanAgent->>PlanOrch: ExecutePlanAsync(plan)

	loop For each plan step
		PlanOrch->>TaskAgent: Execute task(step + previous context)
		TaskAgent->>Tools: Use best tool (CLI/Playwright/MS Learn/Memory)
		Tools-->>TaskAgent: Tool output
		TaskAgent-->>PlanOrch: Task result
	end

	PlanOrch-->>PlanAgent: All step results
	PlanAgent-->>IntentAgent: Execution summary
	IntentAgent-->>User: Final answer
```

## Agent responsibilities

- **Intent Agent**
  - Interprets user intent.
  - Produces a structured plan (`PlanStep[]`).
  - Delegates execution via `CreateAndExecutePlanAsync`.

- **Plan Agent**
  - Receives the plan.
  - Orchestrates task creation/execution through `ExecutePlanAsync`.
  - Collects and summarizes step outputs.

- **Task Agent**
  - Executes one task at a time.
  - Selects and invokes installed tools.
  - Returns concrete outputs/errors for each step.
