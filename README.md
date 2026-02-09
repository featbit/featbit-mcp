# FeatBit MCP Server

A Model Context Protocol (MCP) server that connects AI coding agents directly to FeatBit's REST API for programmatic feature flag management. Built with .NET 10, ASP.NET Core, and Aspire for modern cloud-native architecture.

## 🔌 Installation & Getting Started

The FeatBit MCP Server supports quick installation across multiple development environments. Choose your preferred client below:

Standard config works in most clients:

```json
{
  "servers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp"
    }
  }
}
```

#### VS Code

Install in VS Code or search "@mcp featbit" in Extensions, or manually add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp"
    }
  }
}
```

[VS Code MCP Guide](https://code.visualstudio.com/docs/copilot/customization/mcp-servers#_add-an-mcp-server)

#### Cursor

Add to Cursor MCP settings (`~/.cursor/mcp.json` on macOS/Linux or `%APPDATA%\.cursor\mcp.json` on Windows):

```json
{
  "servers": {
    "featbit": {
      "url": "https://mcp.featbit.co/mcp"
    }
  }
}
```

[Cursor MCP Guide](https://cursor.com/docs/context/mcp#using-mcpjson)

#### Claude Code

Run the following command:

```bash
claude mcp add --transport http featbit https://mcp.featbit.co/mcp
```

[Claude Code MCP Guide](https://code.claude.com/docs/en/mcp#option-1%3A-add-a-remote-http-server)

#### Codex

Run the following command:

```bash
codex mcp add "featbit" --url "https://mcp.featbit.co/mcp"
```

[Codex MCP documentation](https://developers.openai.com/codex/mcp/#connect-codex-to-an-mcp-server)

### ▶️ Getting Started

1. Install the FeatBit MCP Server using one of the methods above
2. Configure your FeatBit API credentials (see Configuration section)
3. You should see the FeatBit MCP Server in the list of available tools
4. Try a prompt like:
   - "Create a new FeatBit project called 'Mobile App' with key 'mobile-app'"
   - "List all my FeatBit projects"
   - "Create a feature flag called 'new-ui' in environment [envId]"
   - "Toggle the 'dark-mode' feature flag to enabled"
   - "Show me the details of the 'checkout-flow' feature flag"

That's it! Your AI assistant can now manage FeatBit feature flags directly through the REST API.

---

### What Problems Does This Solve?

This MCP server enables AI coding agents (like GitHub Copilot) to manage FeatBit feature flags programmatically through natural language interactions. The server provides direct integration with FeatBit's REST API through 9 specialized MCP tools:

#### Core Tools (8 Tools)

1. **CreateProject** - Create new FeatBit projects as top-level containers for feature flags
   - Auto-generates two default environments (Prod and Dev) with credentials
   - Ideal for setting up new project structures

2. **GetProjects** - List all projects in the organization
   - Returns project details including environments and their credentials
   - Useful for project discovery and inventory management

3. **GetProject** - Get detailed information about a specific project
   - Retrieves complete project configuration including all environments
   - Helps understand existing project setup

4. **CreateEnvironment** - Create new environments within projects
   - Support for custom environments (Staging, QA, UAT, etc.)
   - Auto-generates Server Key and Client Key for each environment

5. **CreateFeatureFlag** - Create feature flags with custom variations
   - Supports boolean, string, number, and JSON variation types
   - Configure targeting rules and default behaviors
   - Ideal for new feature rollout setup

6. **GetFeatureFlag** - Retrieve feature flag details
   - Get current configuration, variations, and targeting rules
   - Useful for inspecting existing flag configurations

7. **UpdateFeatureFlag** - Update feature flag properties
   - Modify name, description, variations, or tags
   - Maintains feature flag lifecycle management

8. **ToggleFeatureFlag** - Enable or disable feature flags quickly
   - Quick on/off switching for feature control
   - Supports optional comments for audit trails

#### Advanced Tool (1 Tool)

9. **CallAdvancedApi** - Access any FeatBit REST API endpoint
   - Covers edge cases and operations not handled by core tools
   - Supports GET, POST, PUT, PATCH, and DELETE methods
   - Enables future-proof API access without server updates

### Design Philosophy

The server uses a **hybrid approach** balancing token efficiency with AI usability:
- **Core tools** provide clear semantics and type safety for common operations
- **Advanced tool** handles less common API operations dynamically
- **Total: 9 tools** (~2-3K tokens in context) - minimal context overhead for AI agents

### 📚 Additional Resources

**For FeatBit Knowledge, Best Practices, and Development Skills:**

Use the [featbit/featbit-skills](https://github.com/featbit/featbit-skills) repository as Agent Skills for coding agents. This repository provides:
- **Best practices** for feature flag management and implementation patterns
- **Integration guides** for various SDKs and frameworks
- **Deployment strategies** for different cloud platforms
- **Troubleshooting guides** and common solutions
- **Code examples** and real-world use cases

These skills complement the MCP server by providing deeper domain knowledge and coding guidance that AI agents can leverage when helping with FeatBit implementations.

## How to Run Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2025, VS Code, or JetBrains Rider
- FeatBit account and OpenAPI key - get one at [featbit.co](https://featbit.co)
  - Sign up for a free account
  - Navigate to Profile → Access tokens → Create token
  - Copy your OpenAPI key for configuration

### Configuration

1. **Clone the repository**
   ```bash
   git clone https://github.com/featbit/featbit-mcp.git
   cd featbit-mcp
   ```

2. **Configure FeatBit API** (Required)
   
   Edit `FeatBit/FeatBit.McpServer/appsettings.json` or set environment variables:
   ```json
   {
     "FeatBitApi": {
       "BaseUrl": "https://app.featbit.co",
       "ApiKey": "your-openapi-key-here",
       "JwtToken": ""
     }
   }
   ```
   
   **Authentication Methods** (Choose one):
   - **OpenAPI Key** (Recommended for MCP servers)
     - Best for automation and machine-to-machine communication
     - Set `FeatBitApi:ApiKey` in configuration
     - No expiration unless revoked
     - Get your key from FeatBit console: Profile → Access tokens → Create token
   
   - **JWT Bearer Token**
     - For user-scoped operations
     - Set `FeatBitApi:JwtToken` in configuration
     - Session-based expiration

3. **Configure AI Provider** (Optional - for advanced features)
   
   Edit the same file to add your Azure OpenAI configuration:
   ```json
   {
     "AI": {
       "Provider": "AzureOpenAI",
       "AzureOpenAI": {
         "Endpoint": "https://your-resource.openai.azure.com/",
         "ApiKey": "your-azure-openai-api-key",
         "Deployment": "gpt-4"
       }
     }
   }
   ```
   
   > **Note**: AI provider is optional and used only for advanced document search features. The core REST API tools work without AI configuration.

4. **Configure FeatBit SDK** (Optional - for feature flagging the MCP server itself)
   
   The MCP server can use FeatBit's own feature flags to control its behavior (self-dogfooding):
   ```json
   {
     "FeatBit": {
       "EnvSecret": "your-environment-secret",
       "StreamingUri": "wss://app.featbit.co",
       "EventUri": "https://app.featbit.co"
     }
   }
   ```
   
   > **Note**: Without this configuration, the server uses default values from [FeatureFlag.cs](FeatBit/FeatBit.FeatureFlags/FeatureFlag.cs). This is optional and only needed if you want to dynamically control the MCP server's behavior via feature flags.

### Running Options

#### Option 1: Using .NET Aspire AppHost (Recommended)

```bash
cd FeatBit
dotnet run --project FeatBit.AppHost
```

This starts the Aspire Dashboard where you can monitor the MCP server with full observability.

#### Option 2: Run MCP Server Directly

```bash
cd FeatBit
dotnet run --project FeatBit.McpServer
```

The server will start on `http://localhost:5000` (or the port specified in launchSettings.json).

#### Option 3: Using VS Code Tasks

The project includes pre-configured VS Code tasks:
- `build` - Build the entire solution
- `build-apphost` - Build only the AppHost
- `build-mcpserver` - Build only the MCP Server

Press `Ctrl+Shift+P` → "Tasks: Run Task" → Select a task

### Testing the MCP Server

#### Connect to Hosted Server (Recommended)

Use the standard configuration to connect to the hosted FeatBit MCP server:

```json
{
  "servers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp"
    }
  }
}
```

#### Connect to Local Server

If you're running the server locally, use:

```json
{
  "servers": {
    "featbit-local": {
      "type": "http",
      "url": "http://localhost:5180/mcp"
    }
  }
}
```

#### Using HTTP API

The server also exposes HTTP endpoints for testing:

```bash
# Test with curl
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"method":"tools/list"}'
```

Or use the included [Postman collection](FeatBit/Postman/FeatBit-MCP-Server.postman_collection.json).

---


## Architecture & Design Patterns

### Technology Stack

- **.NET 10**: Latest .NET runtime
- **ASP.NET Core**: Web framework for HTTP-based MCP server
- **Aspire**: Cloud-native orchestration and observability
- **Model Context Protocol (MCP)**: Microsoft's MCP framework for AI agent integration
- **Microsoft Extensions AI**: Unified AI client abstraction
- **FeatBit Server SDK**: Feature flag evaluation
- **OpenTelemetry**: Distributed tracing, metrics, and logging

### Project Structure

```
FeatBit.sln
├── FeatBit.AppHost              # Aspire orchestration host
├── FeatBit.McpServer            # Main MCP server application
│   ├── Controllers/             # (Future: REST controllers)
│   ├── Infrastructure/          # Cross-cutting concerns
│   │   ├── FeatBitApiClient.cs # REST API client wrapper
│   │   └── Models/             # API request/response models
│   ├── Middleware/             # Request pipeline middleware
│   │   ├── GlobalExceptionHandlerMiddleware.cs
│   │   └── McpToolTracingMiddleware.cs
│   └── Tools/                  # MCP tool implementations
│       ├── FeatBitApiTools.cs      # Core 8 REST API tools
│       └── FeatBitAdvancedApiTool.cs # Advanced API tool
├── FeatBit.ServiceDefaults      # Aspire service defaults
├── FeatBit.FeatureFlags         # Feature flag evaluation (for self-dogfooding)
└── FeatBit.Contracts            # Shared interfaces
```

### Design Patterns

#### 1. **Hybrid Tool Strategy** (Core + Advanced)

The MCP server uses a hybrid approach with 8 focused core tools and 1 fallback advanced tool:

```csharp
// Core tools for common operations
[McpServerTool]
[Description("Create a new project in FeatBit...")]
public async Task<string> CreateProject(string name, string key, string? apiKey = null)
{
    var response = await _apiClient.PostAsync<CreateProjectRequest, ProjectResponse>(...);
    return JsonSerializer.Serialize(response);
}

// Advanced tool for edge cases
[McpServerTool]
[Description("Call any FeatBit REST API endpoint...")]
public async Task<string> CallAdvancedApi(string method, string endpoint, string? bodyJson = null)
{
    // Handles any API operation not covered by core tools
}
```

**Benefits:**
- **Token efficiency**: Only 9 tools (~2-3K tokens) in AI agent context
- **Clear semantics**: Core tools provide type safety and validation
- **Future-proof**: Advanced tool handles new API operations without server updates
- **Progressive disclosure**: Common operations are easy, advanced operations are possible

#### 2. **API Client Wrapper Pattern**

`FeatBitApiClient` wraps HTTP operations with consistent error handling and authentication:

```csharp
public class FeatBitApiClient
{
    public async Task<FeatBitApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, string? apiKey = null);
    public async Task<FeatBitApiResponse<TResponse>> PostAsync<TRequest, TResponse>(...);
    public async Task<FeatBitApiResponse<TResponse>> PutAsync<TRequest, TResponse>(...);
    public async Task<FeatBitApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(...);
}
```

**Benefits:**
- Centralized authentication (OpenAPI key or JWT token)
- Consistent error handling and response wrapping
- Simplified retry logic and timeout configuration
- Easy to mock for unit testing

#### 3. **Middleware Pipeline Pattern**

Custom middleware for cross-cutting concerns:

```csharp
app.UseMiddleware<McpToolTracingMiddleware>();      // OpenTelemetry tracing
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Error handling
```

**Benefits:**
- Separation of concerns (tracing, error handling, logging)
- Request/response interception for observability
- Consistent error responses across all tools

#### 4. **Dependency Injection Pattern**

Heavy use of .NET's built-in DI container:

```csharp
// Singleton for shared resources
builder.Services.AddSingleton<FeatBitApiClient>();

// Scoped per request (for feature flag evaluation)
builder.Services.AddScoped<ISessionContext, SessionContext>();
builder.Services.AddScoped<IFeatureFlagEvaluator, FeatureFlagEvaluator>();

// Transient for tool instances
builder.Services.AddTransient<FeatBitApiTools>();
builder.Services.AddTransient<FeatBitAdvancedApiTool>();
```

**Benefits:**
- Testability through interface abstractions
- Lifecycle management (singleton, scoped, transient)
- Easy to swap implementations (e.g., mock API client for testing)

#### 5. **Feature Toggle Pattern** (Self-Dogfooding)

The server can use FeatBit's own SDK to control its behavior (optional):

```csharp
// Feature flags control server behavior
var enableAdvancedLogging = await featureFlagEvaluator.BoolVariationAsync(
    "enable-advanced-logging", 
    defaultValue: false
);
```

This enables:
- Gradual rollout of new MCP server features
- A/B testing different API integration strategies
- Safe experimentation in production
- Dynamic configuration without redeployment

**Note**: Feature flag evaluation is optional. Without FeatBit SDK configuration, the server uses default values from [FeatureFlag.cs](FeatBit/FeatBit.FeatureFlags/FeatureFlag.cs).

#### 6. **Adapter Pattern** (MCP Tool Tracing)

`McpToolTracingMiddleware` adapts the MCP request/response into OpenTelemetry spans:

```csharp
public class McpToolTracingMiddleware
{
    // Converts MCP tool invocations to OpenTelemetry traces
}
```

### Key Architectural Decisions

#### Hybrid Tool Architecture (8+1 Pattern)

The server exposes 9 tools total: 8 core tools for common operations + 1 advanced tool for edge cases.

**Rationale:**
- **Token efficiency**: Minimize AI agent context window usage (~2-3K tokens)
- **Semantic clarity**: Core tools provide clear, type-safe interfaces
- **Extensibility**: Advanced tool handles new API operations without server updates
- **Progressive complexity**: Simple cases are easy, complex cases are possible

#### REST API Direct Integration

The server directly integrates with FeatBit's REST API rather than wrapping the SDK:

**Benefits:**
- Full CRUD operations (Create, Read, Update, Delete/Toggle)
- Access to administrative operations (projects, environments, flags)
- Real-time updates without SDK cache delays
- Programmatic control over feature flag lifecycle

**Trade-offs:**
- Requires FeatBit API authentication
- Network latency for each operation
- No offline evaluation capabilities

#### Session Context for Feature Flag Evaluation

```csharp
// Separate from MCP's internal session management
builder.Services.AddScoped<ISessionContext, SessionContext>();
```

The server maintains its own session context for feature flag user identification (when using FeatBit SDK for self-dogfooding), independent of MCP's session handling.

#### Default Values and Graceful Degradation

The server uses feature flags defined in [FeatureFlag.cs](FeatBit/FeatBit.FeatureFlags/FeatureFlag.cs) with default values as fallback:

```csharp
public sealed record FeatureFlag(string Key, bool DefaultValue, string Description)
{
    public static readonly FeatureFlag DocNotFound = new(
        Key: "doc-not-found",
        DefaultValue: false,
        Description: "Controls whether to return a suggestion message when no documentation is found"
    );
}
```

This enables:
- Development without connecting to FeatBit server
- Automatic fallback when FeatBit server is unavailable
- Type-safe feature flag definitions with default values

**Advanced Offline Mode:**
For more advanced offline scenarios and bootstrap capabilities, please refer to the [FeatBit .NET Server SDK documentation](https://github.com/featbit/featbit-dotnet-sdk) or contact FeatBit official support.

### Observability Architecture

Full OpenTelemetry integration via Aspire:

```csharp
builder.AddServiceDefaults(); // Adds OpenTelemetry
```

**Traces:**
- HTTP request tracing (incoming MCP requests)
- HTTP client tracing (outgoing FeatBit API calls)
- MCP tool invocation tracing (via `McpToolTracingMiddleware`)
- Feature flag evaluation tracing (when SDK is configured)

**Metrics:**
- Request counts and durations
- Tool usage statistics (which tools are called most frequently)
- FeatBit API call success/failure rates
- Feature flag evaluation counts (when SDK is configured)

**Logs:**
- Structured logging via `ILogger<T>`
- Correlated with traces via Activity IDs
- API request/response logging for debugging

### Scalability Considerations

1. **Stateless Design**: Each MCP request is independent, enabling horizontal scaling
2. **Scoped Services**: Session context is scoped per request, no shared state between requests
3. **HTTP Client Pooling**: `FeatBitApiClient` uses HttpClient pooling for efficient connection reuse
4. **Feature Flag Caching**: FeatBit SDK (when configured) caches flags locally, reducing network calls

### Security Considerations

1. **API Key Management**: 
   - FeatBit OpenAPI keys stored in configuration (use Azure Key Vault or similar in production)
   - Support for both OpenAPI keys (recommended) and JWT tokens
   - Keys passed per request or configured globally
   
2. **Authentication Flexibility**:
   - Users can provide their own API key per tool invocation
   - Falls back to configured default API key
   - Enables multi-tenant scenarios

3. **Input Validation**: 
   - Tool parameters validated before API calls
   - JSON deserialization with error handling
   - Endpoint validation in advanced tool

4. **Error Handling**: 
   - Global exception handler prevents information leakage
   - Structured error responses
   - API errors wrapped in consistent format

---

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

See [LICENSE](LICENSE) for details.

## Support

- Documentation: [FeatBit Documentation](https://docs.featbit.co)
- Issues: [GitHub Issues](https://github.com/featbit/featbit/issues)
- Community: [FeatBit Slack](https://join.slack.com/t/featbit/shared_invite/zt-1ew5e2vbb-x6Apan1xZOaYMnFzqZkGNQ)