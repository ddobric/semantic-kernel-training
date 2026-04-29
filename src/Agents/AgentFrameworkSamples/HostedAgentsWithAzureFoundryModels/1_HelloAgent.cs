using AgentFramework_Samples;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace HostedAgentsWithAzureFoundryModels
{
    /// <summary>
    /// Demonstrates core Agent Framework scenarios using Azure OpenAI hosted models.
    /// </summary>
    public class HelloAgent
    {
        /// <summary>
        /// Scenario 1: Agent Construction and Basic Usage.
        /// Creates an AIAgent from an AzureOpenAI ChatClient, then invokes it
        /// with a single prompt (non-streaming) and a streaming call.
        /// </summary>
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // Build the agent: AzureOpenAIClient → ChatClient → AIAgent
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good at telling jokes.", name: nameof(HelloAgent));
        
            // Non-streaming invocation — returns the full response at once.
            AgentResponse agentResp = await agent.RunAsync("Tell me a joke about a pirate.");
            Console.WriteLine(agentResp);

            // Streaming invocation — yields incremental updates as they arrive.
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
            {
                Console.WriteLine(update);
            }
        }

        /// <summary>
        /// Scenario 2: Sessions and Multi-turn Conversations.
        /// Without a session, each RunAsync call is stateless — the agent has no memory of prior turns.
        /// With an AgentSession, conversational context is preserved across calls,
        /// enabling follow-up questions that reference previous answers.
        /// </summary>
        public static async Task RunMultiturnAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are good calculator.", name: nameof(HelloAgent));

            // Stateless calls — each request is independent; "Now add 1" has no context.
            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10."));
            Console.WriteLine(await agent.RunAsync("Now add 1"));
            Console.WriteLine(await agent.RunAsync("And divide all by 2"));

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Now let's do it with the session.");
            Console.WriteLine();
            Console.WriteLine();

            // Session-based calls — the session accumulates conversation history,
            // so follow-up prompts can reference previous results.
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(await agent.RunAsync("Calculate the sum of numbers: 1,2,3,4,5,6,7,8,9, 10.", session));
            Console.WriteLine(await agent.RunAsync("Now add 1", session));
            Console.WriteLine(await agent.RunAsync("And divide all by 2", session));
        }

        /// <summary>
        /// Scenario 3: Function Tools.
        /// Registers a local C# method as a tool the agent can call.
        /// When the user asks a question that requires the tool, the agent
        /// automatically invokes it and incorporates the result into its response.
        /// </summary>
        public static async Task RunWithToolsAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // The AIFunctionFactory.Create wrapper exposes GetProcessInfo as a callable tool.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are the agent that shares information.", name: nameof(HelloAgent),
                    tools: [AIFunctionFactory.Create(GetProcessInfo), AIFunctionFactory.Create(GetVehicleLocation)]
                    );

            // Start an interactive conversation loop with streaming output.
            await Helpers.RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// Tool function: returns a formatted list of running processes.
        /// The [Description] attributes provide the agent with metadata to decide when and how to call it.
        /// </summary>
        [Description("Get the information about running processes.")]
        static string GetProcessInfo([Description("The location to get the weather for.")] string location)
        {
            StringBuilder sb = new StringBuilder();

            var processses = Process.GetProcesses();

            foreach (var process in processses)
            {
                sb.AppendLine($"{process.Id,8} | {process.ProcessName,-40} | Threads: {process.Threads.Count,4} | Memory: {process.WorkingSet64 / 1024.0 / 1024.0,8:F2} MB");
            }

            return sb.ToString();
        }

        private static readonly string[] VehicleIds = ["TRK-001", "TRK-002", "VAN-010", "VAN-011", "CAR-100", "CAR-101", "BUS-050", "BUS-051"];

        private static readonly (string City, double Lat, double Lon)[] KnownLocations =
        [
            ("Frankfurt", 50.1109, 8.6821),
            ("Berlin", 52.5200, 13.4050),
            ("Munich", 48.1351, 11.5820),
            ("Hamburg", 53.5511, 9.9937),
            ("Sarajevo", 43.8563, 18.4131),
            ("Vienna", 48.2082, 16.3738),
            ("Zurich", 47.3769, 8.5417),
            ("Amsterdam", 52.3676, 4.9041),
        ];

        /// <summary>
        /// Tool function: simulates retrieving the current GPS location of a vehicle.
        /// Returns a random position near one of the known city locations with slight jitter
        /// to simulate real-time movement.
        /// </summary>
        [Description("Get the current GPS location of a vehicle. Returns latitude, longitude, speed, and heading.")]
        static string GetVehicleLocation(
            [Description("The vehicle ID to locate (e.g. TRK-001, VAN-010). If not provided, returns all vehicles.")] string? vehicleId = null)
        {
            var rng = Random.Shared;
            var sb = new StringBuilder();

            var vehiclesToReport = string.IsNullOrWhiteSpace(vehicleId)
                ? VehicleIds
                : VehicleIds.Where(v => v.Equals(vehicleId, StringComparison.OrdinalIgnoreCase)).DefaultIfEmpty(vehicleId).ToArray();

            sb.AppendLine($"{"Vehicle",-10} | {"Latitude",10} | {"Longitude",10} | {"Speed (km/h)",13} | {"Heading",8} | City");
            sb.AppendLine(new string('-', 75));

            foreach (var vid in vehiclesToReport)
            {
                // Pick a random known location and add GPS jitter (±0.05°, ~5 km)
                var loc = KnownLocations[rng.Next(KnownLocations.Length)];
                double lat = loc.Lat + (rng.NextDouble() - 0.5) * 0.1;
                double lon = loc.Lon + (rng.NextDouble() - 0.5) * 0.1;
                int speed = rng.Next(0, 121);
                int heading = rng.Next(0, 360);

                sb.AppendLine($"{vid,-10} | {lat,10:F4} | {lon,10:F4} | {speed,13} | {heading,6}° | {loc.City}");
            }

            return sb.ToString();
        }
        
    }
}
