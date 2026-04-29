namespace FoundryAgentDemo
{
    internal static class Helper
    {
        public static void GetAzureEndpointAndModelDeployment(out string foundryProjectEndpoint, out string deploymentName)
        {
            foundryProjectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRYPROJECT_ENDPOINT") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";
        }
    }
}