# AgentsWithSkills

A sample project demonstrating **Agent Skills** — portable packages of instructions, resources, and scripts that give agents specialized capabilities — using the [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/agents/skills?pivots=programming-language-csharp).

## What Are Agent Skills?

Agent Skills follow an open specification and implement a **progressive disclosure** pattern so agents load only the context they need, when they need it. Each stage maps directly to a specific member of an `AgentClassSkill<T>` class:

| Stage | Description | Class Member |
|-------|-------------|--------------|
| 1. **Advertise** (~100 tokens) | Skill names and descriptions are injected into the system prompt so the agent knows what skills are available. | `AgentSkillFrontmatter Frontmatter` — provides the skill `name` and `description` used for advertising. |
| 2. **Load Instructions** | When a task matches a skill's domain, the agent calls `load_skill` to retrieve the full step-by-step instructions. | `string Instructions` — contains the detailed guidance the agent follows once the skill is loaded. |
| 3. **Read Resources** | The agent calls `read_skill_resource` to fetch supplementary data (tables, rules, templates) on demand. | Properties annotated with `[AgentSkillResource]` — each property exposes a named resource the agent can read. |
| 4. **Run Scripts** | The agent calls `run_skill_script` to execute logic bundled with the skill. | Methods annotated with `[AgentSkillScript]` — each method implements a named script the agent can invoke. |

This pattern keeps the agent's context window lean while giving it access to deep domain knowledge on demand.

## Class-Based Skills

Class-based skills bundle all skill components — name, description, instructions, resources, and scripts — into a single C# class deriving from `AgentClassSkill<T>`. The four progressive-disclosure stages are implemented as class members described in the table above.

### UnitConverterSkill

A skill that converts between common measurement units using a multiplication factor.

| Component | Name | Description |
|-----------|------|-------------|
| **Resource** | `conversion-table` | Lookup table of multiplication factors (miles ? km, pounds ? kg) |
| **Script** | `convert` | Multiplies a value by a conversion factor and returns a JSON result |

**How it works:** The agent reads the `conversion-table` resource to find the correct factor for the requested conversion, then invokes the `convert` script with the value and factor. The script computes `result = Math.Round(value * factor, 4)` and returns a JSON object.

```csharp
internal sealed class UnitConverterSkill : AgentClassSkill<UnitConverterSkill>
{
    // Stage 1 – Advertise: name and description injected into the system prompt.
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "unit-converter",
        "Convert between common units using a multiplication factor. "
      + "Use when asked to convert miles, kilometers, pounds, or kilograms.");

    // Stage 2 – Load Instructions: detailed guidance loaded on demand via load_skill.
    protected override string Instructions => """
        Use this skill when the user asks to convert between units.

        1. Review the conversion-table resource to find the factor for the requested conversion.
        2. Use the convert script, passing the value and factor from the table.
        3. Present the result clearly with both units.
        """;

    // Stage 3 – Read Resources: supplementary data fetched via read_skill_resource.
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

    // Stage 4 – Run Scripts: executable logic invoked via run_skill_script.
    [AgentSkillScript("convert")]
    [Description("Multiplies a value by a conversion factor and returns the result as JSON.")]
    private static string ConvertUnits(double value, double factor)
    {
        double result = Math.Round(value * factor, 4);
        return JsonSerializer.Serialize(new { value, factor, result });
    }
}
```

### FictionSkill

A skill that calculates a fictional "fiction" value between two given entities, using entity-specific rules.

| Component | Name | Description |
|-----------|------|-------------|
| **Resource** | `entity-rules` | Rules that define entity factors (human = 100, planet = 1000, animal = 10000, default = 42) |
| **Script** | `calculate-fiction-script` | Calculates fiction between two entities using the computed fictionFactor |

**How it works:** The agent reads the `entity-rules` resource to determine the entity factor for each entity, sums the factors with a random float (0–1), then invokes the `calculate-fiction-script` with both entity names and the computed `fictionFactor`. The script returns `entity1.Length * entity2.Length * fictionFactor` as JSON.

```csharp
internal sealed class FictionSkill : AgentClassSkill<FictionSkill>
{
    // Stage 1 – Advertise: name and description injected into the system prompt.
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "fiction-calculator",
        "Calculates the fiction between two given entities.");

    // Stage 2 – Load Instructions: detailed guidance loaded on demand via load_skill.
    protected override string Instructions => """
        Use this skill when the user asks to calculate the fiction between entities.

        1. Review the 'entity-rules' resource and remember all rules.
        2. Use the 'calculate-fiction-script', passing names of entity1 and entity2.
        3. If user specifies more than two entities, ask the user politely to provide two entities only.
        4. If user specifies less than two entities, ask the user politely to provide two entities.
        5. Present the result clearly with both units.
        """;

    // Stage 3 – Read Resources: supplementary data fetched via read_skill_resource.
    [AgentSkillResource("entity-rules")]
    [Description("Contains rules related to entities.")]
    public string EntityRules => """
        # Entity Rules

        The default entityFactor = 42
        The entityFactor is not the default in the following cases:
        If the entity is a human, the entityFactor is 100.
        If the entity is a planet, the entityFactor is 1000.
        If the entity is an animal, the entityFactor is 10000.

        Calculate the fictionFactor as the sum of entityFactors for both entities,
        plus a random float value between 0..1.
        """;

    // Stage 4 – Run Scripts: executable logic invoked via run_skill_script.
    [AgentSkillScript("calculate-fiction-script")]
    [Description("Calculates the fiction between entity1 and entity2 by using the fictionFactor "
               + "and returns the result as JSON.")]
    public static string CalculateFiction(string entity1, string entity2, float fictionFactor)
    {
        double result = entity1.Length * entity2.Length * fictionFactor;
        return JsonSerializer.Serialize(new { entity1, entity2, fictionFactor, result });
    }
}
```

## Registering Skills with an Agent

Skills are provided to an agent via `AgentSkillsProvider` and passed as context providers:

```csharp
var skillsProvider = new AgentSkillsProvider(new UnitConverterSkill(), new FictionSkill());

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new() { Instructions = "You are a helpful assistant." },
        AIContextProviders = [skillsProvider],
    },
    model: deploymentName);
```

## Prerequisites

- .NET 10
- An Azure OpenAI resource with a deployed model
- Environment variables:
  - `AZURE_OPENAI_ENDPOINT` — your Azure OpenAI endpoint
  - `AZURE_OPENAI_DEPLOYMENT_NAME` — deployment name (defaults to `gpt-5.4-mini`)

## Running

```bash
dotnet run
```

## References

- [Agent Skills documentation](https://learn.microsoft.com/en-us/agent-framework/agents/skills?pivots=programming-language-csharp)
- [Agent Skills specification](https://agentskills.io/)
