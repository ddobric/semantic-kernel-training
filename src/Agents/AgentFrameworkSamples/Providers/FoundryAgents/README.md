# Foundry Agents

> **⚠️ These samples cannot run inside this solution.**

The Foundry Agent samples have been moved to a separate solution due to package conflicts that arose during SDK reorganization.

## Why?

The Foundry samples require the `Microsoft.Agents.AI.Foundry` package, which collides with other packages used in this solution. Running them here results in assembly conflicts and build errors.

## Where to find them

The Foundry Agent samples now live in their own solution:

```
.\src\Agents\FoundryAgent\FoundryAgent.sln
```

Please open that solution to build and run the Foundry-specific samples.

