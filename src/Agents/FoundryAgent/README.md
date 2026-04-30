# Azure AI Foundry Agent Samples

This project demonstrates how to build AI agents using the **Azure AI Foundry SDK** and the **Microsoft.Agents.AI** library in .NET 10.

## Prerequisites

- .NET 10 SDK
- An Azure AI Foundry project with a deployed model
- Set the following environment variables:
  - `AZURE_FOUNDRYPROJECT_ENDPOINT` — your Foundry project endpoint URI
  - `AZURE_OPENAI_DEPLOYMENT_NAME` — the model deployment name (defaults to `gpt-5.4-mini`)

Authentication uses `DefaultAzureCredential` (Azure CLI, managed identity, etc.).

## Samples

### 1. FoundryResponsesAgentSample — Local Agent (Responses API)

**File:** `FoundryResponsesAgentSample.cs`

Creates an in-memory AI agent using the Responses API **without** persisting it in Azure Foundry. The agent is configured with a custom tool (`GetProcessInfo`) that lists running processes, and executes a single prompt locally.

This approach is ideal for lightweight, stateless scenarios where you don't need agent management in the Foundry portal.

### 2. FoundryAgentSample — Persistent Agent in Azure Foundry

**File:** `FoundryAgentSample.cs`

Uses the **Projects Agent API** to create agent versions directly inside Azure Foundry. The agents are persisted and can be managed through the Foundry portal.

- **`RunCreateAgentInFoundryAsync`** — Creates an agent version and runs a single-turn conversation.
- **`RunCreateMultiturnAgentInFoundryAsync`** — Creates an agent version and runs a multi-turn conversation using an `AgentSession` to maintain context across turns.

## Project Structure

| File | Description |
|------|-------------|
| `Program.cs` | Entry point — runs all samples sequentially |
| `FoundryResponsesAgentSample.cs` | In-memory agent sample using the Responses API |
| `FoundryAgentSample.cs` | Persistent agent samples using the Projects Agent API |
| `Helper.cs` | Reads configuration from environment variables |
| `Tools.cs` | Tool functions exposed to agents for function calling |
