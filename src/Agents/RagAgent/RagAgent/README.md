# RAG Agent Sample

A .NET 10 console application that demonstrates the **Retrieval-Augmented Generation (RAG)** pattern using Azure OpenAI and an in-memory vector store.

## How It Works

1. **Text Chunking** – A sample knowledge-base text (geography facts, people) is split into small paragraphs using `TextChunker` from Semantic Kernel.
2. **Embedding** – Each chunk is converted into a vector embedding via the Azure OpenAI `text-embedding-3-large` model.
3. **In-Memory Storage** – Chunks and their embeddings are kept in a simple `List<MyInMemoryVector>`.
4. **AI Agent with RAG Tool** – An `AIAgent` is created with a `QueryInfo` tool. When the user asks a question, the agent can invoke this tool to perform a cosine-similarity search over the stored embeddings and retrieve the **top 3** most relevant chunks.
5. **Streaming Chat Loop** – The user interacts with the agent in a console chat loop with colored output and a spinner animation while waiting for responses.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure OpenAI resource with:
  - A **chat completion** deployment (e.g. `gpt-4o-mini`)
  - A **text-embedding-3-large** deployment
- Azure CLI or managed identity configured for `DefaultAzureCredential`

## Environment Variables

| Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | The endpoint URL of your Azure OpenAI resource |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | The chat model deployment name (defaults to `gpt-5.4-mini`) |

## Running

```bash
cd RagAgent
dotnet run
```

## Project Structure

| File | Purpose |
|---|---|
| `Program.cs` | Entry point – displays startup banner and launches the agent |
| `RagAgentSample.cs` | Core logic: embedding generation, vector search, agent setup, and chat loop |
| `MyInMemoryVector.cs` | Data model for a text chunk with its embedding vector |
