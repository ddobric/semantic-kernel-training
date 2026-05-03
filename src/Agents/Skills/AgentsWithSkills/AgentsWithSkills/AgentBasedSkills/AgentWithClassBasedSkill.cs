using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

using OpenAI.Responses;


#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace AgentsWithSkills.AgentBasedSkills
{
    internal class AgentWithClassBasedSkill
    {
        public static async Task RunAsync()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";

            // --- Skills Provider ---
            var skillsProvider = new AgentSkillsProvider(new UnitConverterSkill(), new FictionSkill());
          

            // --- Agent Setup ---
            AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                .GetResponsesClient()
                .AsAIAgent(new ChatClientAgentOptions
                {
                    Name = "UnitConverterAgent",
                    ChatOptions = new()
                    {
                        Instructions = "You are a helpful assistant that can convert units and calculate the fiction.",
                    },
                    AIContextProviders = [skillsProvider],
                },
                model: deploymentName);

            // --- Example: Unit conversion ---
            WriteHeader("Demonstrating Skills implemented as C# class");

            WriteSection("Unit Conversion");
            WriteUserMessage("How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

            AgentResponse response1 = await agent.RunAsync("How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

            WriteAgentMessage(response1.Text);

            WriteSection("Fiction Calculation");
            WriteUserMessage("What is the fiction between a dog and jupiter?");

            AgentResponse response2 = await agent.RunAsync("What is the fiction between a dog and jupiter?");

            WriteAgentMessage(response2.Text);


            WriteUserMessage("What is the fiction between a dog, human and jupiter?");

            AgentResponse response3 = await agent.RunAsync("What is the fiction between a dog, human and jupiter?");

            WriteAgentMessage(response3.Text);
        }

        private static void WriteHeader(string text)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  {text}");
            Console.WriteLine(new string('═', 60));
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void WriteSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"── {title} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('─', 60));
            Console.ResetColor();
        }

        private static void WriteUserMessage(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  User: ");
            Console.ResetColor();
            Console.WriteLine(text);
            Console.WriteLine();
        }

        private static void WriteAgentMessage(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  Agent: ");
            Console.ResetColor();
            Console.WriteLine(text);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// A class-based Agent Skill that converts between common units (miles/km, pounds/kg).
    /// </summary>
    /// <remarks>
    /// Derives from <see cref="AgentClassSkill{T}"/> so that properties annotated with
    /// <see cref="AgentSkillResourceAttribute"/> are automatically discovered as skill resources,
    /// and methods annotated with <see cref="AgentSkillScriptAttribute"/> are discovered as
    /// skill scripts. The agent uses progressive disclosure: it first sees the skill's name
    /// and description, then loads the full instructions, then reads resources and runs
    /// scripts on demand — keeping the context window lean.
    /// </remarks>
    internal sealed class UnitConverterSkill : AgentClassSkill<UnitConverterSkill>
    {
        /// <inheritdoc/>
        public override AgentSkillFrontmatter Frontmatter { get; } = new(
            "unit-converter",
            "Convert between common units using a multiplication factor. Use when asked to convert miles, kilometers, pounds, or kilograms.");

        /// <inheritdoc/>
        protected override string Instructions => """
        Use this skill when the user asks to convert between units.

        1. Review the conversion-table resource to find the factor for the requested conversion.
        2. Use the convert script, passing the value and factor from the table.
        3. Present the result clearly with both units.
        """;

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used to marshal parameters and return values
        /// for scripts and resources.
        /// </summary>
        /// <remarks>
        /// This override is not necessary for this sample, but can be used to provide custom
        /// serialization options, for example a source-generated <c>JsonTypeInfoResolver</c>
        /// for Native AOT compatibility.
        /// </remarks>
        protected override JsonSerializerOptions? SerializerOptions => null;

        /// <summary>
        /// A conversion table resource providing multiplication factors.
        /// </summary>
        [AgentSkillResource("conversion-table")]
        [Description("Lookup table of multiplication factors for common unit conversions.")]
        public string ConversionTable => """
        # Conversion Tables

        Formula: **result = value × factor**

        | From        | To          | Factor   |
        |-------------|-------------|----------|
        | miles       | kilometers  | 1.60934  |
        | kilometers  | miles       | 0.621371 |
        | pounds      | kilograms   | 0.453592 |
        | kilograms   | pounds      | 2.20462  |
        """;

        /// <summary>
        /// Converts a value by the given factor.
        /// </summary>
        [AgentSkillScript("convert")]
        [Description("Multiplies a value by a conversion factor and returns the result as JSON.")]
        private static string ConvertUnits(double value, double factor)
        {
            double result = Math.Round(value * factor, 4);
            return JsonSerializer.Serialize(new { value, factor, result });
        }
    }

    /// <summary>
    /// A class-based Agent Skill that calculates a fictional "fiction" value between two entities.
    /// </summary>
    /// <remarks>
    /// Demonstrates a skill with entity-specific rules exposed as a resource and a calculation
    /// script. The agent reads the entity-rules resource to determine entity factors, then
    /// invokes the calculate-fiction-script with the computed fictionFactor.
    /// </remarks>
    internal sealed class FictionSkill : AgentClassSkill<FictionSkill>
    {
        /// <inheritdoc/>
        public override AgentSkillFrontmatter Frontmatter { get; } = new(
            "fiction-calculator",
            "Calculates the fiction between two given entities.");

        /// <inheritdoc/>
        protected override string Instructions => """
        Use this skill when the user asks to calculate the fiction between entities.

        1. Review the 'entity-rules' resource and remember all rules.
        2. Use the 'calculate-fiction-script', passing names of entity1 and entity2.
        3. If user specifies more than two entities, aks the user politly to provide two entities only.
        4. If user specifies less than two entities, aks the user politly to provide two entities.
        3. Present the result clearly with both units.
        """;

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used to marshal parameters and return values
        /// for scripts and resources.
        /// </summary>
        /// <remarks>
        /// This override is not necessary for this sample, but can be used to provide custom
        /// serialization options, for example a source-generated <c>JsonTypeInfoResolver</c>
        /// for Native AOT compatibility.
        /// </remarks>
        protected override JsonSerializerOptions? SerializerOptions => null;

        /// <summary>
        /// The resource which defines some entity rules for calculation of fiction.
        /// </summary>
        [AgentSkillResource("entity-rules")]
        [Description("Contains rules related to entities.")]
        public string EntityRules => """
        # Entity Rules

        The default entityFactor = 42
        The entityFactor is not the default on in following cases:
        If the entity is a human, the entityFactor is 100.
        If the entity is planet, the entityFactor is between 1000.
        If the entity is an animal, the entityFactor is between 10000.

        Calculate the fictionFactor as a sum of entityFactors for both entities, plus some random float value between 0..1' .
        """;

        /// <summary>
        /// Converts a value by the given factor.
        /// </summary>
        [AgentSkillScript("calculate-fiction-script")]
        [Description("Calculates the fiction between entity1, and entity2 by using the fictionFactor and returns the result as JSON.")]
        public static string CalculateFiction(string entity1, string entity2, float fictionFactor)
        {
            double result = entity1.Length * entity2.Length * fictionFactor;
            return JsonSerializer.Serialize(new { entity1, entity2, fictionFactor, result });
        }
    }
}


#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

