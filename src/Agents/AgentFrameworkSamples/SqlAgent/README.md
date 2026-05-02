# SQL Agent Sample

A natural-language-to-SQL agent that translates conversational prompts into T-SQL queries, executes them against a SQL Server database, and returns human-readable results.

## How It Works

```
User: "How many messages were created today?"
         ?
         ?
???????????????????
?   Agent (LLM)   ?
???????????????????
         ?
         ??? 1. Calls GetScheme("chat") ? returns Markdown table schema
         ??? 2. Calls GetCurrentUtcDate() ? "2025-01-15T14:30:00.00000Z"
         ??? 3. Composes T-SQL using exact column names from schema
         ??? 4. Calls QuerySqlTable("chat", "SELECT COUNT(*) FROM ai.ChatMessage WHERE ...")
                      ?
                      ?
              ????????????????
              ?  SQL Server  ? ? pipe-delimited results (max 20 rows)
              ????????????????
                      ?
                      ?
         Agent formats results into natural language response
```

## Tools

The agent has access to three tools:

| Tool | Purpose |
|---|---|
| **`GetCurrentUtcDate`** | Returns the current UTC timestamp in ISO format. Called before date-filtered queries. |
| **`GetScheme`** | Returns the database schema as Markdown (table names, columns, types, relationships). The agent caches this per use-case to avoid repeated calls. |
| **`QuerySqlTable`** | Executes a T-SQL query and returns results as a pipe-delimited table. Capped at 20 rows. |

The agent is instructed to always call `GetScheme` first to learn the exact table/column names, then optionally `GetCurrentUtcDate` for time-sensitive queries, and finally `QuerySqlTable` with a well-formed T-SQL statement.

## Prerequisites

- Azure OpenAI endpoint with a `gpt-4o` deployment
- SQL Server database with the `ai.ChatMessage` table (or modify `GetScheme` to return your schema)
- Azure CLI logged in (`az login`) for `AzureCliCredential`

## Configuration

| Environment Variable | Description |
|---|---|
| `AgentFrameworkOpenAIEndpointUrl` | Azure OpenAI endpoint URL |
| `SQL_CONNECTION_STRING` | SQL Server connection string |

## Running

1. Set the environment variables
2. Uncomment `await AgentFramework_TalkToSqlSample.RunAsync();` in `Program.cs`
3. Run the project
4. Type natural language questions at the `>` prompt

## Example Prompts

```
> How many messages are in the database?
> Show me the 10 most recent messages
> How many messages were created today?
> What is the breakdown of messages by role?
> Who are the most active users in the last week?
> Show me conversations that have more than 5 messages
> What is the average number of messages per conversation?
```

## Key Design Decisions

- **Schema-first approach** — The agent always retrieves the schema before querying, ensuring it uses correct table/column names and never hallucinates field names.
- **20-row limit** — Results are capped to avoid token overflow and keep responses readable.
- **Session-based** — The conversation loop uses an `AgentSession` so follow-up questions can reference prior queries (e.g., "now filter that by today's date").
- **Streaming output** — Responses are streamed token-by-token for responsive UX.
