using A2A;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using A2A;
using A2A.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace A2AHost
{
    /// <summary>
    /// Configures and runs an ASP.NET Core host that serves an AI-powered
    /// weather agent over the A2A (Agent-to-Agent) protocol.
    /// </summary>
    internal class A2AHostSample
    {
        /// <summary>
        /// Builds the web application, registers the weather agent, and starts
        /// listening for incoming A2A requests.
        /// </summary>
        public static async Task RunAsync()
        {
            var builder = WebApplication.CreateBuilder();

            // Read required Azure AI configuration from appsettings / environment variables.
            string endpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"]
                ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
            string model = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

            // 1. Create and register the "weather-agent" as a keyed singleton in the DI container.
            //    Keyed services allow multiple AIAgent instances to coexist, each identified by a unique key.
            builder.Services.AddKeyedSingleton<AIAgent>("weather-agent", (sp, _) =>
            {
                // Wrap plain C# methods as AITool instances so the LLM can invoke them.
                // AIFunctionFactory.Create inspects the [Description] attributes and parameter
                // metadata to generate the tool schema automatically.
                AITool getCitiesTool = AIFunctionFactory.Create(GetCities);
                AITool getWeatherTool = AIFunctionFactory.Create(GetWeather);

                // Build the agent using Azure AI Foundry. The AIProjectClient handles
                // authentication, model routing, and tool-calling orchestration.
                return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                    .AsAIAgent(
                        model: model,
                        instructions: "You are a helpful weather assistant. Use the available tools to get cities and weather information.",
                        name: "weather-agent",
                        tools: [getCitiesTool, getWeatherTool]);
            });

            // 2. Register the A2A protocol server middleware for the "weather-agent".
            //    This wires up the A2A message handling pipeline (JSON-RPC over HTTP)
            //    that translates incoming A2A requests into AIAgent invocations.
            builder.AddA2AServer("weather-agent");

            var app = builder.Build();

            // 3. Map the A2A HTTP+JSON endpoint.
            //    This creates a POST route at /a2a/weather-agent that accepts
            //    A2A protocol messages (tasks/send, tasks/sendSubscribe, etc.).
            app.MapA2AHttpJson("weather-agent", "/a2a/weather-agent");

            // 4. Publish the Agent Card at the well-known URL: /.well-known/agent.json
            //    The Agent Card is the discovery mechanism defined by the A2A specification.
            //    Remote consumers fetch this card to learn the agent's name, capabilities,
            //    supported protocol bindings, and endpoint URLs.
            app.MapWellKnownAgentCard(new AgentCard
            {
                Name = "WeatherAgent",
                Description = "A helpful weather assistant.",
                SupportedInterfaces =
                [
                    new AgentInterface
                    {
                        Url = "http://localhost:5000/a2a/weather-agent",
                        ProtocolBinding = ProtocolBindingNames.HttpJson,
                        ProtocolVersion = "1.0",
                    }
                ]
            });

            // Start the Kestrel web server and block until shutdown.
            app.Run();
        }

        private static readonly string[] _cities = ["Seattle", "New York", "London", "Palma", "Sarajevo", "Frankfurt"];
        private static readonly string[] _conditions = ["Sunny", "Cloudy", "Rainy", "Snowy", "Windy", "Foggy", "Partly Cloudy"];
        private static readonly Random _random = new();

        /// <summary>
        /// Returns a list of available cities.
        /// </summary>
        [Description("Get the list of available cities for weather lookup.")]
        public static string GetCities()
        {
            return string.Join(", ", _cities);
        }

        /// <summary>
        /// Returns randomly generated weather for the specified city.
        /// </summary>
        [Description("Get the current weather for a given city.")]
        public static string GetWeather([Description("The city name to get weather for.")] string city)
        {
            int temperature = _random.Next(-10, 40);
            string condition = _conditions[_random.Next(_conditions.Length)];
            int humidity = _random.Next(20, 100);

            return $"Weather in {city}: {condition}, {temperature}°C, Humidity: {humidity}%";
        }
    }
}
