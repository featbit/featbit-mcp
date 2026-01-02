# FeatBit MCP Server

A Model Context Protocol (MCP) server that enables AI coding agents to interact with FeatBit feature flag management. Built with .NET 10, ASP.NET Core, and Aspire for modern cloud-native architecture.

## Table of Contents

- [What's This Project For](#whats-this-project-for)
- [How to Run Locally](#how-to-run-locally)
- [How to Build and Release](#how-to-build-and-release)
- [Architecture & Design Patterns](#architecture--design-patterns)

---

## What's This Project For

This MCP server enables AI coding agents (like GitHub Copilot) to help developers with FeatBit feature flag management through natural language interactions. The server provides:

### Core Capabilities

1. **Feature Flag Deployment Assistance**
   - Step-by-step deployment guides for various platforms (Kubernetes, Azure, AWS, GCP, on-premise)
   - Support for different deployment methods (Helm, Docker Compose, Terraform, etc.)

2. **SDK Integration Code Generation**
   - Generate integration code for multiple SDKs:
     - .NET (Server, Console, Client)
     - JavaScript/TypeScript (Client, React, React Native, Node.js)
     - Java SDK
     - Python SDK
     - Go SDK
     - OpenFeature integrations
   - Provides best practices and working examples tailored to specific use cases

3. **Documentation Search & Troubleshooting**
   - Intelligent document routing using AI and RAG (Retrieval-Augmented Generation)
   - Quick access to relevant FeatBit documentation
   - Troubleshooting guidance for common issues

4. **Bootstrap Mode Support**
   - Offline operation with local JSON file fallback
   - Useful when FeatBit server is unavailable or for testing

### Key Features

- **AI-Powered**: Uses Microsoft Extensions AI with Azure OpenAI integration
- **Feature Flag Controlled**: Uses FeatBit's own SDK to control server behavior (dogfooding)
- **Observable**: Full OpenTelemetry integration for logging, tracing, and metrics
- **Cloud-Native**: Built with .NET Aspire for modern distributed application development

---

## How to Run Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2025, VS Code, or JetBrains Rider
- (Optional) FeatBit account for online mode - get one at [featbit.co](https://featbit.co)

### Configuration

1. **Clone the repository**
   ```bash
   git clone https://github.com/featbit/featbit-mcp.git
   cd featbit-mcp
   ```

2. **Configure FeatBit Connection** (Optional - for online mode)
   
   Edit [FeatBit/FeatBit.McpServer/appsettings.Development.json](FeatBit/FeatBit.McpServer/appsettings.Development.json):
   ```json
   {
     "FeatBit": {
       "EnvSecret": "your-environment-secret",
       "StreamingUri": "wss://app-eval.featbit.co",
       "EventUri": "https://app-eval.featbit.co",
       "StartWaitTimeSeconds": 3
     }
   }
   ```

3. **Configure AI Provider** (Azure OpenAI Only)
   
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
   
   > **Note**: Currently, only Azure OpenAI is supported as the AI provider.

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

#### From VS Code or GitHub Copilot

Configure your MCP client (e.g., VS Code settings) to connect to the server:

```json
{
  "servers": {
    "FeatBitMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "c:/Code/featbit/featbit-mcp/FeatBit/FeatBit.McpServer"
      ]
    }
  }
}
```

Then ask questions like:
- "How do I integrate FeatBit .NET SDK in my ASP.NET Core project?"
- "Show me how to deploy FeatBit to Kubernetes using Helm"
- "What's the best way to use feature flags in React?"

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

## How to Build and Release

### Building for Release

#### Build as NuGet Package

```bash
cd FeatBit/FeatBit.McpServer
dotnet pack -c Release
```

The package will be created in `bin/Release/FeatBit.McpServer.{version}.nupkg`

#### Build for Specific Platforms

The project is configured to build self-contained executables for multiple platforms:
- `win-x64`, `win-arm64`
- `osx-arm64`
- `linux-x64`, `linux-arm64`, `linux-musl-x64`

To build for a specific platform:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

The output will be in `bin/Release/net10.0/win-x64/publish/`

### Release Process

#### 1. Update Version

Edit [FeatBit.McpServer.csproj](FeatBit/FeatBit.McpServer/FeatBit.McpServer.csproj):

```xml
<PackageVersion>0.2.0-beta</PackageVersion>
```

#### 2. Update Package Metadata

Ensure all metadata is correct in the `.csproj` file:
- `<PackageId>`
- `<Description>`
- `<PackageTags>`
- `<PackageReadmeFile>`

#### 3. Build and Test

```bash
dotnet build -c Release
dotnet test -c Release
```

#### 4. Create NuGet Package

```bash
dotnet pack -c Release
```

#### 5. Publish to NuGet.org

```bash
dotnet nuget push bin/Release/*.nupkg \
  --api-key <your-api-key> \
  --source https://api.nuget.org/v3/index.json
```

#### 6. Create GitHub Release

```bash
git tag v0.2.0
git push origin v0.2.0
```

Create a release on GitHub with release notes and attach the platform-specific binaries.

### Using the Published MCP Server

Once published to NuGet.org, users can reference it in their MCP configuration:

```json
{
  "servers": {
    "FeatBitMcpServer": {
      "type": "nuget",
      "package": "FeatBit.McpServer",
      "version": "0.2.0-beta"
    }
  }
}
```

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
│   ├── Domain/                  # Domain models
│   │   ├── Deployments/        # Deployment-related models
│   │   ├── Migrations/         # Data migrations
│   │   └── Sdks/               # SDK-specific models
│   ├── Extensions/             # Service registration extensions
│   ├── Infrastructure/         # Cross-cutting concerns
│   ├── Middleware/             # Request pipeline middleware
│   ├── Resources/              # Embedded documentation
│   │   ├── Deployments/       # Deployment guides
│   │   └── Sdks/              # SDK integration guides
│   ├── Services/               # Business logic services
│   └── Tools/                  # MCP tool implementations
├── FeatBit.ServiceDefaults      # Aspire service defaults
├── FeatBit.FeatureFlags         # Feature flag evaluation
└── FeatBit.Contracts            # Shared interfaces
```

### Design Patterns

#### 1. **Gateway Pattern** (Tool Routing)

The MCP server uses a gateway pattern to limit and consolidate tools exposed to AI agents:

```csharp
// Single tool handles multiple SDKs and topics
[McpServerTool]
public async Task<string> GenerateIntegrationCode(string sdk, string topic)
{
    // Gateway validates and routes to appropriate internal handler
    return await sdkService.GetSdkDocumentationAsync(sdk, topic);
}
```

**Benefits:**
- Simplified AI agent interaction (fewer tools to choose from)
- Centralized validation and routing logic
- Easier to add new SDKs without exposing new tools

#### 2. **Strategy Pattern** (SDK Selection)

Different SDK implementations registered as services:

```csharp
builder.Services.AddTransient<NetServerSdk>();
builder.Services.AddTransient<JavascriptSdks>();
builder.Services.AddTransient<JavaSdks>();
```

The `SdkService` acts as a context that selects the appropriate strategy based on the `sdk` parameter.

#### 3. **Middleware Pipeline Pattern**

Custom middleware for cross-cutting concerns:

```csharp
app.UseMiddleware<McpToolTracingMiddleware>();      // OpenTelemetry tracing
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Error handling
```

#### 4. **Dependency Injection Pattern**

Heavy use of .NET's built-in DI container:

```csharp
// Scoped per request
builder.Services.AddScoped<ISessionContext, SessionContext>();
builder.Services.AddScoped<IFeatureFlagEvaluator, FeatureFlagEvaluator>();

// Singleton for shared resources
builder.Services.AddSingleton<IDocumentLoader, ResourcesDocumentLoader>();
builder.Services.AddSingleton<IClaudeSkillsMarkdownParser, ClaudeSkillsMarkdownParser>();

// Transient for lightweight services
builder.Services.AddTransient<NetServerSdk>();
```

#### 5. **Abstract Factory Pattern** (AI Client Factory)

`AiChatClientFactory` creates different AI chat clients based on configuration:

```csharp
public IChatClient CreateChatClient(string? provider = null)
{
    // Factory selects OpenAI, Azure OpenAI, or other providers
}
```

#### 6. **Repository Pattern** (Document Loading)

`IDocumentLoader` abstracts document retrieval:

```csharp
public interface IDocumentLoader
{
    Task<string> LoadDocumentAsync(string path);
}
```

Implementations:
- `ResourcesDocumentLoader`: Loads from embedded resources
- `S3DocumentLoader`: Could load from AWS S3 (future)
- Enables easy switching between storage backends

#### 7. **Feature Toggle Pattern** (Self-Dogfooding)

The server uses FeatBit's own SDK to control its behavior:

```csharp
// Feature flags control SDK selection strategy
var useAiForSelection = await featureFlagEvaluator.BoolVariationAsync(
    "use-ai-sdk-selection", 
    defaultValue: false
);
```

This enables:
- Gradual rollout of new features
- A/B testing different AI selection strategies
- Safe experimentation in production

#### 8. **Chain of Responsibility Pattern** (Document Selection)

Document selection can use multiple strategies in sequence:
1. Try exact match by filename
2. If not found, use AI for semantic search
3. If still not found, return default documentation

#### 9. **Adapter Pattern** (MCP Tool Tracing)

`McpToolTracingMiddleware` adapts the MCP request/response into OpenTelemetry spans:

```csharp
public class McpToolTracingMiddleware
{
    // Converts MCP tool invocations to OpenTelemetry traces
}
```

### Key Architectural Decisions

#### Session Context vs MCP Session Management

```csharp
// Separate from MCP's internal session management
builder.Services.AddScoped<ISessionContext, SessionContext>();
```

The server maintains its own session context for feature flag user identification, independent of MCP's session handling.

#### Embedded Resources Strategy

Documentation is embedded in the assembly:

```xml
<EmbeddedResource Include="Resources\Deployments\*.md" />
<EmbeddedResource Include="Resources\Sdks\**\*.md" />
```

**Benefits:**
- Self-contained distribution
- No external dependencies at runtime
- Versioning aligned with code

**Trade-offs:**
- Larger assembly size
- Documentation updates require recompilation

#### Bootstrap Mode for Offline Operation

The server supports loading feature flags from a local JSON file:

```json
// featbit-bootstrap.json
{
  "featureFlags": [...],
  "segments": [...]
}
```

This enables:
- Development without internet connection
- Testing with specific flag configurations
- Fallback when FeatBit server is unavailable

### Observability Architecture

Full OpenTelemetry integration via Aspire:

```csharp
builder.AddServiceDefaults(); // Adds OpenTelemetry
```

**Traces:**
- HTTP request tracing
- MCP tool invocation tracing (via `McpToolTracingMiddleware`)
- AI chat client tracing (via `ChatClientOpenTelemetryMiddleware`)
- Feature flag evaluation tracing

**Metrics:**
- Request counts and durations
- Tool usage statistics
- Feature flag evaluation counts

**Logs:**
- Structured logging via `ILogger<T>`
- Correlated with traces via Activity IDs

### Scalability Considerations

1. **Stateless Design**: Each request is independent, enabling horizontal scaling
2. **Scoped Services**: Session context is scoped per request, no shared state
3. **Feature Flag Caching**: FeatBit SDK caches flags locally, reducing network calls
4. **Resource Embedding**: No external I/O for documentation retrieval

### Security Considerations

1. **API Key Management**: AI provider keys stored in configuration (use Key Vault in production)
2. **FeatBit Secret**: Environment secret for feature flag evaluation
3. **Input Validation**: Tool parameters validated before processing
4. **Error Handling**: Global exception handler prevents information leakage

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
- Issues: [GitHub Issues](https://github.com/featbit/featbit-mcp/issues)
- Community: [FeatBit Slack](https://join.slack.com/t/featbit/shared_invite/...)