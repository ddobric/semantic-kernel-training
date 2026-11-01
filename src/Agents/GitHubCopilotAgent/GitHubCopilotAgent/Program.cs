#pragma warning disable GHCP001 // Evaluation-only Copilot SDK API usage acknowledged
using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Agents.AI;
using System.Reflection;
using System.Text.Json;


namespace GitHubCopilotAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string providerType = Environment.GetEnvironmentVariable("BYOK_PROVIDER_TYPE") ?? "azure";
            string baseUrl = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("The AZURE_OPENAI_ENDPOINT environment variable is not set.");
            string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("The AZURE_OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o";

            // Create and start a Copilot client
            await using CopilotClient copilotClient = new();
            await copilotClient.StartAsync();

            // Provider routes the session through a custom endpoint instead of the GitHub Copilot backend.
            // Type is "openai", "azure", or "anthropic". WireApi "completions" is the broadly compatible
            // choice; use "responses" for providers that support the OpenAI Responses API. BYOK also
            // requires Model to be set at the session level.
            SessionConfig sessionConfig = new()
            {
                Model = modelId,
                Provider = new GitHub.Copilot.ProviderConfig
                {
                    Type = providerType,
                    WireApi = "completions",
                    BaseUrl = baseUrl,
                    ApiKey = apiKey,
                    ModelId = modelId,
                },
                OnPermissionRequest = PromptPermission,
                McpServers = new Dictionary<string, McpServerConfig>
                {
                    ["filesystem"] = new McpStdioServerConfig
                    {
                        Command = "npx",
                        Args = ["-y", "@modelcontextprotocol/server-filesystem", "C:\\dev\\git\\daenet-homepage\\public"],
                        Tools = ["*"],
                    },
                    ["microsoft-learn"] = new McpHttpServerConfig
                    {
                        Url = "https://learn.microsoft.com/api/mcp",
                        Tools = ["*"],
                    },
                    ["playwright"] = new McpStdioServerConfig
                    {
                        Command = "npx",
                        Args = ["-y", "@playwright/mcp@latest", "--user-data-dir", "c:\\temp\\pwsession"],
                        Tools = ["*"],
                    },

                },
            };

            AIAgent agent = copilotClient.AsAIAgent(sessionConfig, ownsClient: true);

            await Helpers.RunConversationLoopAsync(agent);

            Console.WriteLine();
        }

        static Task<PermissionDecision> PromptPermission(PermissionRequest request, PermissionInvocation invocation)
        {
            Console.WriteLine($"[Permission Request: {request.Kind}]");
            Console.WriteLine("The following action is requesting approval:");
            string? intention = TryGetIntention(request) ?? TryGetIntention(invocation);
            Console.WriteLine($"Intention: {(!string.IsNullOrWhiteSpace(intention) ? intention : "(not provided)")}");
            Console.WriteLine(JsonSerializer.Serialize(invocation, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

            return Task.FromResult(PermissionDecision.ApproveOnce());

            Console.Write("Approve? (y/n): ");
            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
            return Task.FromResult(input is "Y" or "YES"
                ? PermissionDecision.ApproveOnce()
                : PermissionDecision.Reject());
        }

        private static string? TryGetIntention(object source)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            string[] candidateNames = ["Intention", "Intent", "Purpose", "Description"];

            foreach (string propertyName in candidateNames)
            {
                var property = source.GetType().GetProperty(propertyName, Flags);
                if (property?.GetValue(source) is string value && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

    }
}

#pragma warning restore GHCP001
