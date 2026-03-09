Project Introduction:

- .NET 10
- ASP.NET Core
- Aspire

---

## Project Structure

```
FeatBit/
  FeatBit.sln
  FeatBit.AppHost/          # Aspire orchestration host
  FeatBit.Contracts/        # Shared interfaces (e.g. ISessionContext)
  FeatBit.FeatureFlags/     # Feature flag evaluation library
  FeatBit.McpServer/        # MCP server (HTTP transport, tools, middleware)
    Infrastructure/         # FeatBitApiClient (HTTP client for FeatBit REST API)
    Middleware/             # ASP.NET Core middleware (tracing, exception handling)
    Tools/                  # MCP tool classes ([McpServerToolType])
  FeatBit.ServiceDefaults/  # Shared Aspire service defaults (OTel, health checks)
```

---

## Code Rules

### Language & Framework

- Target `net10.0` for all projects.
- Enable `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in every `.csproj`.
- Use top-level statements in `Program.cs`; avoid `Startup` classes.
- Use `async`/`await` throughout; never use `.Result` or `.Wait()`.

### Dependency Injection

- Register all services in `Program.cs` via the `builder.Services` extension methods.
- Prefer constructor injection; declare dependencies as constructor parameters (primary constructor syntax is preferred for simple classes).
- Register `HttpClient` through `AddHttpClient<T>()` for typed clients (e.g. `FeatBitApiClient`).

### MCP Tools

- Place all tool classes in `FeatBit.McpServer/Tools/`.
- Decorate the class with `[McpServerToolType]` and each method with `[McpServerTool]`.
- Add a `[Description("...")]` attribute to every tool method and every parameter — descriptions are surfaced directly to AI clients.
- Use URI encoding for all path and query parameters: `Uri.EscapeDataString(value)`.
- Tool methods must return `Task<string>` and delegate to `FeatBitApiClient`.
- Group related tools with `// === Section Name ===` comments inside the class.
- Never include an `apiKey` or auth parameter in tool signatures — credentials are forwarded automatically from request headers.

### API Client (`FeatBitApiClient`)

- Lives in `FeatBit.McpServer/Infrastructure/`.
- Base URL is read from configuration key `FeatBitApi:BaseUrl`; default to the cloud URL as fallback.
- Forward `Authorization`, `Organization`, and `Workspace` headers from the incoming `IHttpContextAccessor` context to every outgoing request — do this in a shared `CreateRequest` helper.
- Wrap every HTTP call in a `try/catch`; on error log with `_logger.LogError(ex, ...)` and return `JsonSerializer.Serialize(new { error = ex.Message })`.
- Log every request at `Information` level before sending: `_logger.LogInformation("GET {Endpoint}", endpoint)`.

### Middleware

- Place all middleware in `FeatBit.McpServer/Middleware/`.
- Register middleware in `Program.cs` before `app.MapMcp()`.
- `McpToolTracingMiddleware` must be registered before `GlobalExceptionHandlerMiddleware` so all requests — including errors — are traced.
- Tracing uses `System.Diagnostics.ActivitySource` with source name `"FeatBit.McpTools"`. Set tags as `mcp.tool.name` and `mcp.tool.parameter.<name>`.
- Exception handling middleware returns a JSON body with a `message` field and sets the status code to `500`.

### Observability

- Always call `builder.AddServiceDefaults()` in every service's `Program.cs`.
- OpenTelemetry (traces + metrics) is configured centrally in `FeatBit.ServiceDefaults`.
- Use `ActivitySource` for custom spans; never use `Console.WriteLine` for diagnostic output.
- Structured logging with `ILogger<T>`; use message templates (e.g. `"GET {Endpoint}"`) — never string interpolation in log calls.

### Configuration

- Configuration keys follow the pattern `Section:Key` (e.g. `FeatBitApi:BaseUrl`).
- Provide sensible defaults as fallback (`?? "default"`) so the app starts without configuration.
- Secrets (API keys, tokens) must never be committed; use `appsettings.Development.json` or environment variables locally.

### Error Handling

- API client methods must never throw; they catch and return serialized error JSON.
- Middleware catches all unhandled exceptions from the pipeline and returns a structured response.
- Do not add redundant `try/catch` blocks inside MCP tool methods — errors are handled at the middleware and client layers.

### Naming Conventions

- Classes: `PascalCase`. Files match class name exactly.
- Methods / properties: `PascalCase`.
- Local variables / parameters: `camelCase`.
- Private fields: `_camelCase`.
- Constants: `PascalCase` (or `UPPER_SNAKE_CASE` for true compile-time constants).
- Namespaces mirror the folder structure: `FeatBit.McpServer.Tools`, `FeatBit.McpServer.Infrastructure`, etc.

### Feature Flags

Feature flags are treated as a **cross-cutting concern** (like logging), not a domain concept. All flag definitions live in `FeatBit.FeatureFlags/FeatureFlag.cs` and are available to any project via a project reference.

#### Declaring a Feature Flag

Add new flags as `static readonly` fields in `FeatBit.FeatureFlags/FeatureFlag.cs`:

```csharp
public sealed record FeatureFlag(string Key, bool DefaultValue, string Description)
{
    public static readonly FeatureFlag MyNewFeature = new(
        Key: "my-new-feature",          // must match the key in FeatBit dashboard
        DefaultValue: false,            // fallback when SDK is unavailable
        Description: "Controls whether MyNewFeature is enabled"
    );
}
```

The `Key` must exactly match the feature flag key configured in the FeatBit dashboard.

#### Using Feature Flags via Dependency Injection

Inject `IFeatureFlagEvaluator` as a constructor parameter (primary constructor syntax preferred):

```csharp
[McpServerToolType]
public class MyTools(FeatBitApiClient apiClient, IFeatureFlagEvaluator flagEvaluator)
{
    [McpServerTool, Description("...")]
    public async Task<string> MyTool()
    {
        // Simple boolean check
        if (flagEvaluator.ReleaseEnabled(FeatureFlag.MyNewFeature))
        {
            return await apiClient.GetAsync("/new-endpoint");
        }

        // Guard clause — execute action only when enabled
        flagEvaluator.ReleaseEnabledThen(FeatureFlag.MyNewFeature, () => DoSomething());

        // Guard clause — return new or fallback value
        return await flagEvaluator.ReleaseEnabledThenAsync(
            FeatureFlag.MyNewFeature,
            async () => await apiClient.GetAsync("/new-endpoint"),
            await apiClient.GetAsync("/legacy-endpoint")   // default if disabled
        );
    }
}
```

`IFeatureFlagEvaluator` is registered as **scoped** because it depends on `ISessionContext` (which is scoped per HTTP request). Do not capture it in a singleton.

---

### Build & Tasks

Run the following tasks (defined in `.vscode/tasks.json`) to build:

| Task | Command |
|------|---------|
| `build` | Builds the entire solution |
| `build-apphost` | Builds `FeatBit.AppHost` only |
| `build-mcpserver` | Builds `FeatBit.McpServer` only |
| `kill-apphost` | Stops any running AppHost / McpServer processes |