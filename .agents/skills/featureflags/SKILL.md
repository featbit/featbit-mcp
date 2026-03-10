---
name: featureflags
description: Provides step-by-step guidance for integrating feature flags into FeatBit MCP Server tools and code paths. Use when user asks about "add feature flag", "gate a tool", "control tool release", "McpToolFlagGate", "IFeatureFlagEvaluator", "ReleaseEnabled", "hide tool", "disable tool", or wants to control whether a feature or MCP tool is available at runtime. Do not use for general FeatBit REST API questions or questions about managing flags in the FeatBit dashboard.
license: MIT
metadata:
  author: FeatBit
  version: 1.0.0
  category: sdk-integration
---

# Feature Flags Integration

Feature flags are a cross-cutting concern in this project (like logging). The entire feature flag system lives in `FeatBit.FeatureFlags/`. All infrastructure — SDK initialization, `IFeatureFlagEvaluator` registration, and the `tools/list` filter — is already wired in `Program.cs`. No extra setup is needed beyond Steps 1–2 of each approach below.

## When to Use

- **Gate an entire MCP tool** (hide it from `tools/list` when the flag is off) → Approach 1: `[McpToolFlagGate]`
- **Branch logic inside a tool or service** (switch endpoint, enable extra step, skip an action) → Approach 2: `IFeatureFlagEvaluator`

## Why Use Feature Flags

- Safe, gradual rollout of new tools and behaviour without redeployment.
- Per-session user targeting via `ISessionContext.SessionId`.
- Every evaluation is automatically traced in OpenTelemetry — no extra instrumentation needed.
- `DefaultValue: false` keeps new code dark until explicitly enabled in the FeatBit dashboard.

---

## Approach 1: Gate an MCP Tool with `[McpToolFlagGate]`

Use this to make a tool method invisible to MCP clients — as if the tool does not exist — when the flag is disabled.

### Step 1 — Declare the flag in `FeatureFlag.cs`

Add a `static readonly` field to `FeatBit.FeatureFlags/FeatureFlag.cs`:

```csharp
public static readonly FeatureFlag MyNewTool = new(
    Key: "my-new-tool",       // must match the key in the FeatBit dashboard exactly
    DefaultValue: false,      // tool is hidden when SDK is unavailable
    Description: "Controls whether MyNewTool is exposed to MCP clients"
);
```

### Step 2 — Decorate the tool method

In your tool class inside `FeatBit.McpServer/Tools/`, add the attribute:

```csharp
[McpServerTool]
[Description("Does something new.")]
[McpToolFlagGate(nameof(FeatureFlag.MyNewTool))]   // nameof keeps it refactoring-safe
public Task<string> MyNewTool() => apiClient.GetAsync("/api/v1/new-endpoint");
```

No further changes are needed. `AddMcpToolFlagGateFilter` is already registered in `Program.cs` and scans the assembly at startup.

---

## Approach 2: Control a Code Branch with `IFeatureFlagEvaluator`

Use this for conditional logic inside a method — e.g., choosing between two API endpoints, enabling an extra processing step, or conditionally triggering a side-effect.

### Step 1 — Declare the flag in `FeatureFlag.cs`

Same as Approach 1, Step 1.

### Step 2 — Inject `IFeatureFlagEvaluator`

Use primary constructor syntax in your tool class:

```csharp
[McpServerToolType]
public class MyTools(FeatBitApiClient apiClient, IFeatureFlagEvaluator flagEvaluator)
```

> `IFeatureFlagEvaluator` is registered as **scoped**. Never inject it into a singleton.

### Step 3 — Evaluate the flag

Pick the pattern that fits the situation:

**Async guard with fallback** — preferred for tool methods (`Task<string>` return):

```csharp
return await flagEvaluator.ReleaseEnabledThenAsync(
    FeatureFlag.MyNewFeature,
    async () => await apiClient.GetAsync("/api/v1/new-endpoint"),
    await apiClient.GetAsync("/api/v1/legacy-endpoint")   // returned when flag is off
);
```

**Simple boolean check** — use when the two branches differ significantly:

```csharp
if (flagEvaluator.ReleaseEnabled(FeatureFlag.MyNewFeature))
    return await apiClient.GetAsync("/api/v1/new-endpoint");

return await apiClient.GetAsync("/api/v1/legacy-endpoint");
```

**Void side-effect guard** — run an action only when the flag is on:

```csharp
flagEvaluator.ReleaseEnabledThen(FeatureFlag.MyNewFeature, () => DoSomething());
```

---

## Choosing the Right Approach

| Goal | Approach |
|------|----------|
| Hide a tool completely from `tools/list` | `[McpToolFlagGate]` (Approach 1) |
| Switch between two async implementations | `ReleaseEnabledThenAsync` (Approach 2) |
| Simple if/else conditional logic | `ReleaseEnabled` (Approach 2) |
| Conditionally execute a void side-effect | `ReleaseEnabledThen` (Approach 2) |

---

## Rules

- Always use `nameof(FeatureFlag.SomeField)` in `[McpToolFlagGate]`— never a hardcoded string.
- Never call `IFeatureFlagEvaluator` from a singleton; it is scoped per request.
- Never add `try/catch` inside tool methods around flag evaluation — errors are handled by middleware.
- The flag `Key` must exactly match what is configured in the FeatBit dashboard.
- `DefaultValue: false` is the standard — features stay dark when the SDK is unavailable.

