using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AgentFramework_Samples.Providers.FoundryLocal
{
    /// <summary>
    /// Local (REST-based) implementation of <see cref="IAgentModel"/> for Foundry.
    /// 
    /// Responsibilities:
    /// - Communicates with Foundry Local via HTTP
    /// - Sends prompts using OpenAI-compatible Chat Completions format
    /// - Handles multiple possible endpoint paths (compatibility fallback)
    /// - Parses responses into plain text
    /// 
    /// Key Design:
    /// Foundry may expose different API routes depending on configuration,
    /// so this service attempts multiple endpoints to ensure compatibility.
    /// </summary>
    public sealed class FoundryLocalChatClient : IChatClient
    {
        // Toggle for debugging raw HTTP responses
        private const bool Debuglogging = false;

        // Model identifier (e.g., qwen2.5-1.5b-instruct...)
        private readonly string _modelName;

        // HTTP client used for REST communication
        private readonly HttpClient _http;

        // Logger for diagnostics
        private readonly ILogger<FoundryLocalChatClient> _logger;

        // IMPORTANT:
        // Base URL must be the ROOT (e.g., http://localhost:port)
        // Do NOT include /openai or /v1 here.

        /// <summary>
        /// Initializes the Foundry service.
        /// </summary>
        /// <param name="modelName">Model identifier</param>
        /// <param name="httpClient">Optional injected HttpClient (for testing)</param>
        /// <param name="baseUrl">Resolved Foundry base URL (required)</param>
        /// <param name="logger">Optional logger instance</param>
        public FoundryLocalChatClient(string modelName, HttpClient? httpClient = null, string? baseUrl = null, ILogger<FoundryLocalChatClient>? logger = null)
        {
            _modelName = modelName;
            _http = httpClient ?? new HttpClient();
            _logger = logger ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<FoundryLocalChatClient>();

            // Ensure base URL is provided (critical for REST calls)
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException(
                    "Foundry baseUrl must be provided by FoundryEndpointResolver.");

            // Configure HTTP client
            _http.BaseAddress = new Uri(baseUrl);
            _http.Timeout = TimeSpan.FromMinutes(5); // Local models can be slow
        }


        /// <summary>
        /// Extracts text from OpenAI-compatible chat completion response.
        /// 
        /// Handles:
        /// - Standard: choices[0].message.content
        /// - Fallback: choices[0].text (older or alternative formats)
        /// </summary>
        private static string ExtractChatCompletionText(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Standard OpenAI format:
            // { choices: [ { message: { content: "..." } } ] }
            if (root.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                var choice0 = choices[0];

                // Preferred format
                if (choice0.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? string.Empty;
                }

                // Fallback format (less common)
                if (choice0.TryGetProperty("text", out var textProp) &&
                    textProp.ValueKind == JsonValueKind.String)
                {
                    return textProp.GetString() ?? string.Empty;
                }
            }

            // No valid text found
            return string.Empty;
        }

        /// <summary>
        /// Builds the request payload and sends it to Foundry, trying multiple endpoint paths.
        /// Returns the raw JSON response body on success.
        /// </summary>
        private async Task<string> SendChatRequestAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, 
            bool stream, CancellationToken cancellationToken)
        {
            var messageArray = messages.Select(m => new
            {
                role = m.Role.Value,
                content = m.Text ?? string.Empty
            }).ToArray();

            var body = new
            {
                model = _modelName,
                messages = messageArray,
                temperature = options?.Temperature ?? 0.0,
                //stream
            };

            body = new
            {
                model = _modelName,
                messages = new[]
                {
                        new { role = "user", content = "Hello" }
                    },
                temperature = 0.0 // deterministic output for benchmarking
            };

            string json = JsonSerializer.Serialize(body);

            var pathsToTry = new[]
            {
                "/v1/chat/completions",
                "/openai/v1/chat/completions"
            };

            HttpResponseMessage? resp = null;
            string respJson = "";

            foreach (var path in pathsToTry)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, path);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //resp = await _http.SendAsync(req, stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, cancellationToken);
                resp = await _http.SendAsync(req);

                if (!stream || !resp.IsSuccessStatusCode)
                {
                    respJson = await resp.Content.ReadAsStringAsync(cancellationToken);
                }

                _logger.LogDebug("Status={StatusCode} PathTried={Uri}", (int)resp.StatusCode, resp.RequestMessage?.RequestUri);

                if (resp.IsSuccessStatusCode)
                    break;

                _logger.LogDebug("Response Body: {Body}", string.IsNullOrWhiteSpace(respJson) ? "(empty body)" : respJson);
            }

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                int statusCode = resp is not null ? (int)resp.StatusCode : 0;
                throw new HttpRequestException($"Foundry REST error {statusCode}: {respJson}");
            }

            // For streaming, read the response body via the stream path.
            if (stream)
                respJson = await resp.Content.ReadAsStringAsync(cancellationToken);

            return respJson;
        }

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            string respJson = await SendChatRequestAsync(messages, options, stream: false, cancellationToken);

            string text = ExtractChatCompletionText(respJson);

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
            {
                ModelId = _modelName
            };
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Build and send the request with stream=true, reading headers only.
            var messageArray = messages.Select(m => new
            {
                role = m.Role.Value,
                content = m.Text ?? string.Empty
            }).ToArray();

            var body = new
            {
                model = _modelName,
                messages = messageArray,
                temperature = options?.Temperature ?? 0.0,
                stream = true
            };

            string json = JsonSerializer.Serialize(body);

            var pathsToTry = new[]
            {
                "/v1/chat/completions",
                "/openai/v1/chat/completions"
            };

            HttpResponseMessage? resp = null;

            foreach (var path in pathsToTry)
            {
                var req = new HttpRequestMessage(HttpMethod.Post, path);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                _logger.LogDebug("Status={StatusCode} PathTried={Uri}", (int)resp.StatusCode, resp.RequestMessage?.RequestUri);

                if (resp.IsSuccessStatusCode)
                    break;

                string errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Response Body: {Body}", errorBody);
            }

            if (resp is null || !resp.IsSuccessStatusCode)
            {
                int statusCode = resp is not null ? (int)resp.StatusCode : 0;
                throw new HttpRequestException($"Foundry REST error {statusCode}");
            }

            // Read the SSE stream line by line.
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // SSE format: "data: {...}" or "data: [DONE]"
                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                string data = line["data: ".Length..];

                if (data == "[DONE]")
                    yield break;

                // Parse the SSE chunk and extract the delta content.
                string deltaText = ExtractStreamingDelta(data);

                if (!string.IsNullOrEmpty(deltaText))
                {
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent(deltaText)],
                        ModelId = _modelName
                    };
                }
            }
        }

        /// <summary>
        /// Extracts the delta content from a streaming SSE chunk.
        /// Format: { choices: [ { delta: { content: "..." } } ] }
        /// </summary>
        private static string ExtractStreamingDelta(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var choice0 = choices[0];

                    if (choice0.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        return content.GetString() ?? string.Empty;
                    }
                }
            }
            catch (JsonException ex)
            {
                // Malformed chunk — skip it.
                Debug.WriteLine($"Failed to parse streaming chunk: {ex.Message}");
            }

            return string.Empty;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {

        }
    }

}
