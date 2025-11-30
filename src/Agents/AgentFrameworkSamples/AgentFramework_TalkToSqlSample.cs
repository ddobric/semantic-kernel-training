using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ComponentModel;

namespace AzureFoundrySkAgent
{
    internal class AgentFramework_TalkToSqlSample
    {
        public static async Task RunAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("AgentFrameworkOpenAIEndpointUrl")!;
            
            var deploymentName = "gpt-4o";

            // Create a service collection to hold the agent plugin and its dependencies.
            ServiceCollection services = new();
            services.AddSingleton<SqlServerTool>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureCliCredential())
                .GetChatClient(deploymentName)
                .CreateAIAgent(
                    instructions: "You are a helpful assistant that transforms the user's prompt into the SQL statements.",
                    name: "Assistant",
                    tools: [.. serviceProvider.GetRequiredService<SqlServerTool>().AsAITools()],
                    services: serviceProvider); // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.

            await RunConversationLoopAsync(agent);
        }

        private static async Task RunConversationLoopAsync(AIAgent agent)
        {
            Microsoft.Agents.AI.AgentThread thread = agent!.GetNewThread();

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");

                string? userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput) || userInput == "exit")
                    break;

                try
                {
                    await foreach (var update in agent.RunStreamingAsync(userInput, thread))
                    {
                        Console.Write(update);
                    }
                }
                finally
                {

                }
            }
        }

        public class SqlServerTool
        {            
            public SqlServerTool()
            {
                var sqlCOnnStr = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!;

            }
         
         
            [Description("Get the current UTC date and time in ISO format. Useful for date-based queries, filtering recent data, or understanding the current context for time-sensitive analysis.")]
            public string GetCurrentUtcDate()
            {
                return $"Current UTC Date/Time: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffffZ}";
            }


            [Description("This tool is allways called bevore executing T-SQL. Store the scheme for already abtained usecases in the cache and do not invoke this tool over and over again for the same usecase. The scheme is described in Markdown.")]
            public string GetScheme([Description("The usecase identifier, which describes which scheme must be loaded. ")]
        string useCase)
            {
                return """
                                        # Table: ai.ChatMessage

                    Stores individual messages exchanged within a chat conversation (both user messages and AI responses).

                    ## Table Details

                    | Property              | Value                                      |
                    |-----------------------|--------------------------------------------|
                    | Schema                | `ai`                                       |
                    | Table Name            | `ChatMessage`                              |
                    | Primary Key           | `Id` (clustered)                           |
                    | Constraint Name       | `PK_ChatMessage_Id`                        |

                    ## Columns

                    | Column Name       | Data Type          | Nullable | Description                                                                                   | Notes / Typical Values |
                    |-------------------|--------------------|----------|-----------------------------------------------------------------------------------------------|------------------------|
                    | **Id**            | `uniqueidentifier` | NOT NULL | Unique identifier for the chat message (GUID). Serves as the primary key.                     | Generated via NEWID() or NEWSEQUENTIALID() |
                    | **ChatContextId** | `uniqueidentifier` | NULL     | Foreign key referencing the parent chat session/conversation. Allows grouping messages.       | NULL only for orphaned/orphan messages (rare) |
                    | **Role**          | `tinyint`          | NOT NULL | Indicates who sent the message.                                                               | Suggested enum values:<br>0 = System<br>1 = User<br>2 = Assistant<br>3 = Tool/Function |
                    | **Message**       | `nvarchar`(max)    | NULL     | The actual text/content of the message. Can store very large messages (images as base64, JSON payloads, etc.). | NULL when message contains only non-text parts (e.g., pure file upload) |
                    | **CreatedAt**     | `datetime2`(7)     | NOT NULL | UTC timestamp when the message was first created.                                             | Usually set with DEFAULT (SYSUTCDATETIME()) |
                    | **ChangedAt**     | `datetime`         | NOT NULL | UTC timestamp of the last modification (for editable messages, e.g., user editing their prompt). | Updated via triggers or application logic |
                    | **CreatedBy**     | `nvarchar`(255)    | NOT NULL | Identifier of the principal that created the message (user ID, service account, etc.).       | Examples: user email, Azure AD objectId, "system", "assistant" |
                    | **ChangedBy**     | `nvarchar`(255)    | NOT NULL | Identifier of the principal that last modified the message.                                   | Same format as CreatedBy |

                    ## Indexes

                    - **Clustered Index**: `PK_ChatMessage_Id` on column `Id`
                    - **Recommended Additional Indexes** (not in the provided script but commonly added):
                      - Non-clustered index on `(ChatContextId, CreatedAt)` – for efficient retrieval of conversation history in chronological order
                      - Non-clustered index on `CreatedAt` – for global recent messages queries
                      - Non-clustered index on `Role` – if filtering by role is frequent

                    ## Relationships

                    - **Foreign Key** (typically added separately):  
                      `FK_ChatMessage_ChatContext` → `ai.ChatContext(Id)` on `ChatContextId`
                    """;
            }

            [Description("Run a T-SQL query query against the database database specified in the connection string. If the query depends on the current date or time, call GetCurrentUtcDate() to get the current UTC date/time. Before calling this tool first load the database scheme provided by the tool GetScheme(usecase) to retrieve schemas for any tables you haven't yet obtained. Then compose your SQL statement using the exact table and column names from retrieved schemas, and pass the query to this tool for execution. For more readable results, \r\njoin related tables to show descriptive fields. Prefer also aggregated results using functions like SUM, AVG, COUNT, and GROUP BY. ALWAYS Limit the number of rows returned to 20 or fewer to avoid overwhelming the user with too much data and explain that results are limited for performance and readability.")]
            public async Task<string> QuerySqlTable(
            [Description("The use-case identifier as defined in instructions.")] string useCase,
            [Description("A well-formed t-SQL query.")] string sqlQuery)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!))
                    {
                        await conn.OpenAsync();

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = sqlQuery;

                        using var reader = await cmd.ExecuteReaderAsync();
                        var result = new System.Text.StringBuilder();

                        // Write column headers
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            result.Append(reader.GetName(i));
                            if (i < reader.FieldCount - 1)
                                result.Append(" | ");
                        }
                        result.AppendLine();

                        int rowCount = 0;

                        while (await reader.ReadAsync() && rowCount < 20)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                result.Append(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString());
                                if (i < reader.FieldCount - 1)
                                    result.Append(" | ");
                            }
                            result.AppendLine();
                            rowCount++;
                        }

                        if (rowCount == 20)
                        {
                            result.AppendLine("Results limited to 20 rows for performance and readability.");
                        }
                        else if (rowCount == 0)
                        {
                            result.AppendLine("No results returned.");
                        }

                        return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    return $"Error executing SQL query: {ex.Message}";
                }
            }

            public IEnumerable<AITool> AsAITools()
            {
                yield return AIFunctionFactory.Create(this.GetCurrentUtcDate);
                yield return AIFunctionFactory.Create(this.GetScheme);
                yield return AIFunctionFactory.Create(this.QuerySqlTable);
            }
        }

    }
}
