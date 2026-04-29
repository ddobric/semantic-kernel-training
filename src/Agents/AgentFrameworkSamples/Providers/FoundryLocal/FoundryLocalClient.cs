using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentFramework_Samples.Providers.FoundryLocal
{
    /// <summary>
    /// Top-level client for Foundry Local, analogous to <c>OpenAIClient</c>.
    /// Stores shared configuration (HTTP client, base URL, logger) and produces
    /// model-specific <see cref="IChatClient"/> instances via <see cref="GetChatClient"/>.
    /// </summary>
    public class FoundryLocalClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly ILogger<FoundryLocalChatClient>? _logger;

        /// <summary>
        /// Initializes a new Foundry Local client.
        /// </summary>
        /// <param name="httpClient">Optional injected HttpClient (for testing or reuse).</param>
        /// <param name="baseUrl">
        /// The root URL of the Foundry Local endpoint (e.g. <c>http://localhost:5272</c>).
        /// Do NOT include <c>/openai</c> or <c>/v1</c>.
        /// </param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public FoundryLocalClient(HttpClient? httpClient = null, string? baseUrl = null, ILogger<FoundryLocalChatClient>? logger = null)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl), "Foundry baseUrl must be provided.");
            _http = httpClient ?? new HttpClient();
            _logger = logger;
        }

        /// <summary>
        /// Returns an <see cref="IChatClient"/> configured for the specified model deployment.
        /// The returned client shares the HTTP client and base URL of this instance.
        /// </summary>
        /// <param name="model">The model identifier (e.g. <c>qwen2.5-1.5b-instruct</c>).</param>
        public IChatClient GetChatClient(string model)
        {
            return new FoundryLocalChatClient(model, _http, _baseUrl, _logger);
        }
    }
}
