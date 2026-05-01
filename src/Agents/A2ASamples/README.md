# A2A Protocol Sample

This solution demonstrates agent-to-agent (A2A) communication using the A2A protocol.
It contains two projects: a hosted agent that exposes an AI agent over HTTP, and a consumer application that discovers and interacts with that agent remotely.

## Architecture

    A2AConsumerApp (Console)  --->  A2AHost (ASP.NET Core Server)
      1. Fetch agent card          /.well-known/agent.json
      2. Create AIAgent proxy
      3. Send message               /a2a/weather-agent (A2A JSON)
      4. Receive response           Backed by Azure AI Foundry

## Projects

### A2AHost

ASP.NET Core app that hosts a weather-agent powered by Azure AI Foundry.

Steps:
  1. Reads Azure AI config from appsettings.json or environment variables
  2. Creates an AIAgent using AIProjectClient with a configurable model
  3. Registers the agent in DI and wires up A2A protocol handling
  4. Publishes agent card at /.well-known/agent.json
  5. Starts Kestrel on http://localhost:5000

Key files:
  - Program.cs: Entry point with startup banner
  - A2AHostSample.cs: Configures web app, registers agent, maps A2A endpoints

### A2AConsumerApp

Console app that acts as an A2A client.

Steps:
  1. Points A2ACardResolver at the host (http://localhost:5000/)
  2. Fetches agent card from /.well-known/agent.json
  3. Creates local AIAgent proxy backed by remote A2A endpoint
  4. Sends sample message to weather agent
  5. Prints agent response

Key files:
  - Program.cs: Entry point with startup banner
  - ConsumerSamples.cs: Resolves agent card and invokes remote agent

## Prerequisites

- .NET 10 SDK
- Azure AI Foundry project endpoint with a deployed model (defaults to gpt-4o-mini)
- Azure credentials for DefaultAzureCredential (e.g. az login)

## Configuration

Set in A2AHost/appsettings.json or as environment variables:

- AZURE_AI_PROJECT_ENDPOINT: Azure AI Foundry project endpoint URL (required)
- AZURE_AI_MODEL_DEPLOYMENT_NAME: Model deployment name (default: gpt-4o-mini)

## Running the Sample

1. Start the host:
   dotnet run --project A2AHost

2. Run the consumer (separate terminal):
   dotnet run --project A2AConsumerApp

## How A2A Works Here

1. Discovery: Consumer fetches Agent Card JSON from /.well-known/agent.json
2. Communication: Consumer sends messages via HTTP+JSON to the A2A endpoint
3. Response: Host forwards to Azure AI model and returns the completion via A2A
