using Anthropic.SDK;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Net;
using System.Text.Json;

namespace McpClient
{
    internal class Program
    {
        /// <summary>
        /// https://github.com/modelcontextprotocol/csharp-sdk
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            SseClientTransportOptions opts = new SseClientTransportOptions
            {
                //Endpoint = new Uri("http://localhost:3001/sse"),
               
                Endpoint = new Uri("https://localhost:7133/sse"),
                AdditionalHeaders = new Dictionary<string, string>()
                {              
                    {"ApiKey","123" }
                }
            };


            IClientTransport transport;

            transport = new SseClientTransport(opts);

            //transport = new StdioClientTransport(new StdioClientTransportOptions
            //{
            //    Name = "Everything",
            //    Command = "npx",
            //    Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            //});

            var mcpClient = await McpClientFactory.CreateAsync(transport);


            // Print the list of tools available from the server.
            foreach (var tool in await mcpClient.ListToolsAsync())
            {
                Console.WriteLine($"{tool.Name} ({tool.Description})");
            }
            
            // Execute a tool (this would normally be driven by LLM tool invocations).
            //CallToolResponse result = await mcpClient.CallToolAsync(
            //    "Echo",
            //    new Dictionary<string, object?>() { ["message"] = "Hello MCP!" },
            //    cancellationToken: CancellationToken.None);

            // echo always returns one and only one text content object
           // Console.WriteLine(result.Content.First(c => c.Type == "text").Text);

            await RunConversationLoop(await mcpClient.ListToolsAsync(), mcpClient);
        }

        protected static async Task RunConversationLoop(IList<McpClientTool> tools, IMcpClient mcpClient)
        {
            //var client = new ChatCompletionsClient(
            //    new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!),
            //    new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!));

            var endpoint = new Uri("https://ddobric-agents-samples-resource.cognitiveservices.azure.com/openai/deployments/gpt-4.1");
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);
            var model = "gpt-4.1";

            var client = new ChatCompletionsClient(
                endpoint,
                credential,
                new AzureAIInferenceClientOptions()
            );

            var chatHistory = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage("You are a helpful assistant that knows about AI")
            };

            var mcpToolDefs = await GetMcpTools(tools);


            while (true)
            {
                Console.Write(">");

                var prompt = Console.ReadLine();

                chatHistory.Add(new ChatRequestUserMessage(prompt));

                var options = new ChatCompletionsOptions(chatHistory)
                {
                    Model = "gpt-4.1",
                };

                mcpToolDefs.ForEach((tool) => options.Tools.Add(tool));

                ChatCompletions? response = await client.CompleteAsync(options);

                var content = response.Content;

                if (content != null)
                    chatHistory.Add(new ChatRequestAssistantMessage(content));

                if (response.ToolCalls?.Count() > 0)
                {
                    // 5. Check if the response contains a function call
                    ChatCompletionsToolCall? calls = response.ToolCalls.FirstOrDefault();
                    for (int i = 0; i < response.ToolCalls.Count; i++)
                    {
                        var call = response.ToolCalls[i];
                        Console.WriteLine($"Tool call {i}: {call.Name} with arguments {call.Arguments}");
                        //Tool call 0: add with arguments {"a":2,"b":4}

                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(call.Arguments);
                        var result = await mcpClient.CallToolAsync(
                            call.Name,
                            dict!,
                            cancellationToken: CancellationToken.None
                        );

                        Console.WriteLine(result.Content.First(c => c.Type == "text").Text);
                    }
                }
                else
                {
                    Console.WriteLine(content);
                }
            }
        }

        protected static async Task<List<ChatCompletionsToolDefinition>> GetMcpTools(IList<McpClientTool> tools)
        {
            List<ChatCompletionsToolDefinition> toolDefinitions = new List<ChatCompletionsToolDefinition>();

            foreach (var tool in tools)
            {
                Console.WriteLine($"Connected to server with tools: {tool.Name}");
                Console.WriteLine($"Tool description: {tool.Description}");
                Console.WriteLine($"Tool parameters: {tool.JsonSchema}");

                JsonElement propertiesElement;
                tool.JsonSchema.TryGetProperty("properties", out propertiesElement);

                var def = ConvertFrom(tool.Name, tool.Description, propertiesElement);
                Console.WriteLine($"Tool definition: {def}");
                toolDefinitions.Add(def);

                Console.WriteLine($"Properties: {propertiesElement}");
            }

            return toolDefinitions;
        }

        private static ChatCompletionsToolDefinition ConvertFrom(string name, string description, JsonElement jsonElement)
        {
            // convert the tool to a function definition
            FunctionDefinition functionDefinition = new FunctionDefinition(name)
            {
                Description = description,
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = jsonElement
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            // create a tool definition
            ChatCompletionsToolDefinition toolDefinition = new ChatCompletionsToolDefinition(functionDefinition);
            return toolDefinition;
        }

    }
}
