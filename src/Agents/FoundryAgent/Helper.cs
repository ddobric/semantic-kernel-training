namespace FoundryAgentDemo
{
    /// <summary>
    /// Utility class for reading Azure AI Foundry configuration from environment variables.
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// Reads the Azure Foundry project endpoint and model deployment name from environment variables.
        /// </summary>
        /// <param name="foundryProjectEndpoint">The Foundry project endpoint URI from <c>AZURE_FOUNDRYPROJECT_ENDPOINT</c>.</param>
        /// <param name="deploymentName">The model deployment name from <c>AZURE_OPENAI_DEPLOYMENT_NAME</c>, defaults to <c>gpt-5.4-mini</c>.</param>
        /// <exception cref="InvalidOperationException">Thrown when <c>AZURE_FOUNDRYPROJECT_ENDPOINT</c> is not set.</exception>
        public static void GetAzureEndpointAndModelDeployment(out string foundryProjectEndpoint, out string deploymentName)
        {
            foundryProjectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRYPROJECT_ENDPOINT") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";
        }
    }
}