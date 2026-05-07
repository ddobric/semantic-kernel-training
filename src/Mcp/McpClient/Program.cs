using Azure;
using Azure.AI.Inference;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Text.Json;

namespace McpClientSample
{
    /// <summary>
    /// Demonstrates how to connect an MCP (Model Context Protocol) client to a remote MCP server
    /// and use its tools inside an Azure OpenAI chat completion loop.
    /// 
    /// Two transport modes are shown:
    ///   1. <see cref="HttpClientTransport"/> – connects to a remote MCP server over HTTP (Streamable HTTP).
    ///   2. <see cref="StdioClientTransport"/> – spawns a local MCP server process via stdio.
    /// 
    /// See https://github.com/modelcontextprotocol/csharp-sdk for the MCP C# SDK.
    /// </summary>
    internal class Program
    {
        // ──────────────────────────────────────────────
        //  Configuration constants
        // ──────────────────────────────────────────────
        private const string McpServerEndpoint = "https://localhost:7133";
        private const string AzureOpenAiEndpoint = "https://ddobric-agents-samples-resource.cognitiveservices.azure.com/openai/deployments/gpt-4.1";
        private const string ModelName = "gpt-4.1";

        // ──────────────────────────────────────────────
        //  Entry point
        // ──────────────────────────────────────────────

        /// <summary>
        /// Application entry point.  
        /// Connects to a remote MCP server, lists available tools, executes a quick
        /// "echo" smoke-test, and then enters an interactive chat loop.
        /// </summary>
        static async Task Main(string[] args)
        {
            // --- 1. Configure the MCP transport --------------------------------
            var transportOptions = new HttpClientTransportOptions
            {
                TransportMode = HttpTransportMode.StreamableHttp,
                Endpoint = new Uri(McpServerEndpoint),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    { "ApiKey", "123" }
                }
            };

            IClientTransport transport = new HttpClientTransport(transportOptions);

            // --- 2. Create the MCP client and connect --------------------------
            var mcpClient = await McpClient.CreateAsync(transport);

            // --- 3. Enumerate available tools ----------------------------------
            var tools = await mcpClient.ListToolsAsync();
            foreach (var tool in tools)
            {
                Console.WriteLine($"{tool.Name} ({tool.Description})");
            }

            // --- 4. Quick smoke-test: call the "echo" tool ---------------------
            var echoResult = await mcpClient.CallToolAsync(
                "echo",
                new Dictionary<string, object?> { ["message"] = "Hello MCP!" },
                cancellationToken: CancellationToken.None);

            Console.WriteLine(echoResult.Content.First(c => c.Type == "text").ToString());

            // --- 5. Enter interactive conversation loop ------------------------
            await RunConversationLoop(tools, mcpClient);
        }

        // ──────────────────────────────────────────────
        //  Alternative: local stdio MCP server
        // ──────────────────────────────────────────────

        /// <summary>
        /// Connects to the MCP "Everything" reference server via stdio transport.
        /// Useful for local development when no remote MCP server is available.
        /// </summary>
        static async Task UseServerEverything()
        {
            IClientTransport transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "Everything",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            });

            var mcpClient = await McpClient.CreateAsync(transport);

            foreach (var tool in await mcpClient.ListToolsAsync())
            {
                Console.WriteLine($"{tool.Name} ({tool.Description})");
            }

            CallToolResult result = await mcpClient.CallToolAsync(
                "echo",
                new Dictionary<string, object?> { ["message"] = "Hello MCP!" },
                cancellationToken: CancellationToken.None);

            Console.WriteLine(result.Content.First(c => c.Type == "text").ToString());

            await RunConversationLoop(await mcpClient.ListToolsAsync(), mcpClient);
        }

        // ──────────────────────────────────────────────
        //  Conversation loop
        // ──────────────────────────────────────────────

        /// <summary>
        /// Runs an interactive console chat loop that sends user prompts to Azure OpenAI,
        /// detects tool-call requests in the response, forwards them to the MCP server,
        /// and feeds the results back into the conversation history.
        /// </summary>
        /// <param name="tools">MCP tools discovered from the server.</param>
        /// <param name="mcpClient">Connected MCP client used to invoke tools.</param>
        protected static async Task RunConversationLoop(IList<McpClientTool> tools, McpClient mcpClient)
        {
            // --- Azure OpenAI client setup -------------------------------------
            var endpoint = new Uri(AzureOpenAiEndpoint);
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);

            var client = new ChatCompletionsClient(endpoint, credential, new AzureAIInferenceClientOptions());

            // --- Seed the conversation with a system message -------------------
            var chatHistory = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage("You are a helpful assistant that knows about AI")
            };

            // --- Convert MCP tool metadata to Azure AI tool definitions --------
            var mcpToolDefs = await GetMcpTools(tools);

            // --- Main loop: read prompt → call LLM → handle tool calls ---------
            while (true)
            {
                Console.Write("> ");
                var prompt = Console.ReadLine();
                chatHistory.Add(new ChatRequestUserMessage(prompt));

                while (true)
                {
                    // Build the completion request with the current history & tools
                    var options = new ChatCompletionsOptions(chatHistory) { Model = ModelName };
                    mcpToolDefs.ForEach(t => options.Tools.Add(t));

                    ChatCompletions response = await client.CompleteAsync(options);

                    // Append assistant's textual reply to history
                    if (response.Content != null)
                        chatHistory.Add(new ChatRequestAssistantMessage(response.Content));

                    // If the model requested tool calls, execute them via MCP
                    if (response.ToolCalls?.Count() > 0)
                    {
                        var sb = new StringBuilder();

                        for (int i = 0; i < response.ToolCalls.Count; i++)
                        {
                            var call = response.ToolCalls[i];
                            Console.WriteLine($"Tool call {i}: {call.Name} with arguments {call.Arguments}");

                            // Deserialize arguments and invoke the MCP tool
                            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(call.Arguments);

                            var result = await mcpClient.CallToolAsync(
                                call.Name,
                                dict!,
                                cancellationToken: CancellationToken.None);

                            // Extract the text result from the tool response
                            var res = result.Content.First(c => c.Type == "text");
                            var text = ((TextContentBlock)res).Text;

                            sb.AppendLine(text);
                            Console.WriteLine(text);
                            Console.WriteLine("----------------------------------");

                            // Feed the tool result back into the conversation
                            chatHistory.Add(new ChatRequestAssistantMessage(text));
                        }
                    }
                    else
                    {
                        // No tool calls – print the final answer and wait for next prompt
                        Console.WriteLine(response.Content);
                        break;
                    }
                }
            }
        }

        // ──────────────────────────────────────────────
        //  MCP → Azure AI tool conversion helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Converts a list of <see cref="McpClientTool"/> definitions into
        /// <see cref="ChatCompletionsToolDefinition"/> objects that Azure AI Inference understands.
        /// </summary>
        /// <param name="tools">MCP tool metadata retrieved from the server.</param>
        /// <returns>A list of Azure AI tool definitions.</returns>
        protected static async Task<List<ChatCompletionsToolDefinition>> GetMcpTools(IList<McpClientTool> tools)
        {
            var toolDefinitions = new List<ChatCompletionsToolDefinition>();

            foreach (var tool in tools)
            {
                Console.WriteLine($"Registering tool: {tool.Name} – {tool.Description}");

                tool.JsonSchema.TryGetProperty("properties", out JsonElement propertiesElement);

                var def = ConvertToToolDefinition(tool.Name, tool.Description, propertiesElement);
                toolDefinitions.Add(def);
            }

            return toolDefinitions;
        }

        /// <summary>
        /// Creates a single <see cref="ChatCompletionsToolDefinition"/> from raw tool metadata.
        /// </summary>
        /// <param name="name">Tool name (must match the MCP tool name).</param>
        /// <param name="description">Human-readable description of what the tool does.</param>
        /// <param name="propertiesElement">JSON element describing the tool's input parameters.</param>
        /// <returns>An Azure AI tool definition ready to attach to a chat completion request.</returns>
        private static ChatCompletionsToolDefinition ConvertToToolDefinition(
            string name, string description, JsonElement propertiesElement)
        {
            var functionDefinition = new FunctionDefinition(name)
            {
                Description = description,
                Parameters = BinaryData.FromObjectAsJson(
                    new { Type = "object", Properties = propertiesElement },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            return new ChatCompletionsToolDefinition(functionDefinition);
        }
    }
}
