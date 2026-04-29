using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AgentFramework_Samples.Providers.FoundryLocal
{
    /// <summary>
    /// Top-level client for Foundry Local, analogous to <c>OpenAIClient</c>.
    /// </summary>
    public class FoundryLocalClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly ILogger<FoundryLocalChatClient>? _logger;

        // Matches status output such as:
        // "running on http://127.0.0.1:61472/openai/status"
        private static readonly Regex UrlRegex =
            new(@"http:\/\/127\.0\.0\.1:\d+\/openai\/status", RegexOptions.Compiled);

        // Cache the last working base URL to avoid repeated service discovery
        private static string? _cachedBaseUrl;

        /// <summary>
        /// Initializes a new Foundry Local client.
        /// </summary>
        /// <param name="httpClient">Optional injected HttpClient (for testing or reuse).</param>
        /// <param name="baseUrl">
        /// The root URL of the Foundry Local endpoint (e.g. <c>http://localhost:5272</c>).
        /// Do NOT include <c>/openai</c> or <c>/v1</c>.
        /// </param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public FoundryLocalClient(string baseUrl, HttpClient? httpClient = null, ILogger<FoundryLocalChatClient>? logger = null)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl), "Foundry baseUrl must be provided.");
            _http = httpClient ?? new HttpClient();
            _logger = logger ?? NullLogger<FoundryLocalChatClient>.Instance;
        }

        public static async Task<FoundryLocalClient> CreateFoundryClientAsync(string? baseUrl = null, HttpClient? httpClient = null, ILogger<FoundryLocalChatClient>? logger = null)
        {
            var resolvedLogger = logger ?? NullLogger<FoundryLocalChatClient>.Instance;
            string resolvedUrl = baseUrl ?? await Task.Run(() => EnsureFoundryReadyAsync(resolvedLogger));
            return new FoundryLocalClient(resolvedUrl, httpClient, logger);
        }

        /// <summary>
        /// Returns an <see cref="IChatClient"/> configured for the specified model deployment.
        /// The returned client shares the HTTP client and base URL of this instance.
        /// </summary>
        /// <param name="model">The model identifier (e.g. <c>qwen2.5-1.5b-instruct</c>).</param>
        public FoundryLocalChatClient GetChatClient(string model)
        {
            return new FoundryLocalChatClient(model, _http, _baseUrl, _logger);
        }


        /// <summary>
        /// Starts the Foundry Local service through the CLI.
        /// 
        /// Flow:
        /// 1. Execute "foundry service start"
        /// 2. Allow a longer timeout for service startup
        /// </summary>
        public static async Task StartServiceAsync(CancellationToken? cancelToken = null)
        {
            // Start the Foundry background service using the CLI
            var (success, stdOut, stdErr) = await TryRunFoundryAsync("service start", cancelToken);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to start Foundry service. StdOut: {stdOut}, StdErr: {stdErr}");
            }
        }


        /// <summary>
        /// Runs a Foundry CLI command and returns the combined output.
        /// 
        /// Flow:
        /// 1. Start a command shell process
        /// 2. Execute "foundry" with the provided arguments
        /// 3. Wait for the process to finish within the timeout
        /// 4. Kill the process if it times out
        /// 5. Read standard output and error output
        /// 6. Return the combined command result
        /// </summary>
        private static async Task<(bool Success, string StdOut, string StdErr)> TryRunFoundryAsync(string args, CancellationToken? cancelToken = null)
        {
            // Run the Foundry CLI through cmd.exe
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c foundry {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = new Process { StartInfo = psi };

            p.Start();

            if (cancelToken == null)
                await p.WaitForExitAsync();
            else
                await p.WaitForExitAsync(cancelToken.Value);

            // Collect both standard output and error output because Foundry may use either
            string stdOut = await p.StandardOutput.ReadToEndAsync();
            string stdErr = await p.StandardError.ReadToEndAsync();

            return (string.IsNullOrEmpty(stdErr), stdOut, stdErr);
        }

        /// <summary>
        /// Ensures that the Foundry service is running and reachable.
        /// 
        /// Steps:
        /// 1. Attempts to start Foundry service via CLI
        /// 2. Waits for initialization
        /// 3. Resolves endpoint to confirm availability
        /// 
        /// Notes:
        /// - Safe to call multiple times (idempotent behavior)
        /// - If already running, start command will not fail critically
        /// </summary>
        private static async Task<string> EnsureFoundryReadyAsync(ILogger logger)
        {
            logger.LogDebug("Starting Foundry service...");

            var (success, stdOut, stdErr) = await TryRunFoundryAsync("service start");

            if (!success)
            {
                throw new InvalidOperationException($"Failed to start Foundry service. StdOut: {stdOut}, StdErr: {stdErr}");
            }

            logger.LogDebug("Waiting for Foundry to initialize...");
           
            try
            {
                // Verify that endpoint can be resolved successfully
                var baseUrl = await ResolveBaseUrlAsync();

                logger?.LogDebug("Foundry endpoint detected: {BaseUrl}", baseUrl);

                return baseUrl;
            }
            catch (Exception ex)
            {
                // Critical failure: service started but endpoint unavailable
                throw new InvalidOperationException("Foundry service started but endpoint could not be resolved.", ex);
            }
        }

        /// <summary>
        /// Resolves the Foundry service base URL.
        /// 
        /// Flow:
        /// 1. Return the cached URL if it is still reachable
        /// 2. Repeatedly call "foundry service status"
        /// 3. Check whether the service is running
        /// 4. Extract the base URL from the status output
        /// 5. Verify that the endpoint is reachable
        /// 6. Cache and return the working base URL
        /// 7. Throw an exception if no reachable endpoint is found
        /// </summary>
        public static async Task<string> ResolveBaseUrlAsync()
        {
            // Reuse the cached endpoint if it still responds
            if (!string.IsNullOrWhiteSpace(_cachedBaseUrl) && IsServiceAlive(_cachedBaseUrl))
                return _cachedBaseUrl;

            // Retry several times because the service may still be starting up
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string status;

                try
                {
                    // Read Foundry service status from the command line
                    var (success, stdOut, stdErr) = await TryRunFoundryAsync("service status");

                    if (!success)
                    {
                        throw new InvalidOperationException($"Failed to start Foundry service. StdOut: {stdOut}, StdErr: {stdErr}");
                    }
                 
                    status = stdOut;
                }
                catch
                {
                    status = string.Empty;
                }

                // If the service is not yet running, wait and retry
                if (string.IsNullOrWhiteSpace(status) || IsStatusNotRunning(status))
                {
                    Thread.Sleep(2000);
                    continue;
                }

                try
                {
                    // Extract the base URL from the status output
                    string baseUrl = ParseBaseUrlFromStatus(status);

                    // Only accept the URL if the REST endpoint is actually reachable
                    if (IsServiceAlive(baseUrl))
                    {
                        _cachedBaseUrl = baseUrl;
                        return baseUrl;
                    }
                }
                catch
                {
                    // Ignore parsing or connection errors and keep retrying
                }

                Thread.Sleep(2000);
            }

            throw new InvalidOperationException("Could not resolve a reachable Foundry service endpoint.");
        }

        /// <summary>
        /// Checks whether the Foundry service status output indicates
        /// that the service is not currently running.
        /// 
        /// Flow:
        /// 1. Read the status text
        /// 2. Look for known "not running" messages
        /// 3. Return true if the service appears inactive
        /// </summary>
        private static bool IsStatusNotRunning(string status)
        {
            // Foundry may report inactivity with different status messages
            return status.Contains("not running", StringComparison.OrdinalIgnoreCase) ||
                   status.Contains("To start the service", StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Checks whether the Foundry REST service is reachable at the given base URL.
        /// 
        /// Flow:
        /// 1. Build an HTTP client with a short timeout
        /// 2. Send a request to /openai/status
        /// 3. Return true if the response is successful
        /// 4. Return false if the request fails
        /// </summary>
        private static bool IsServiceAlive(string baseUrl)
        {
            try
            {
                // Use a short timeout because this is only a liveness check
                using var http = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(3)
                };

                // Check the Foundry health/status endpoint
                var resp = http.GetAsync(baseUrl + "/openai/status").GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                // Any request error means the service is treated as unreachable
                return false;
            }
        }

        /// <summary>
        /// Extracts the base URL from the Foundry service status output.
        /// 
        /// Flow:
        /// 1. Search the status text using the URL regex
        /// 2. Validate that a match was found
        /// 3. Remove the "/openai/status" suffix
        /// 4. Return the cleaned base URL
        /// </summary>
        private static string ParseBaseUrlFromStatus(string status)
        {
            // Try to locate the full service status URL inside the command output
            var m = UrlRegex.Match(status);
            if (!m.Success)
                throw new InvalidOperationException("Could not detect Foundry service URL from: " + status);

            // Convert:
            // "http://127.0.0.1:61472/openai/status"
            // to:
            // "http://127.0.0.1:61472"
            return m.Value.Replace("/openai/status", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        }
    }
}
