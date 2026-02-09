using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults for observability (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// ========================================
// Register Core Services
// ========================================
// Add HttpContextAccessor to access HTTP request information
builder.Services.AddHttpContextAccessor();

// Register HttpClient for FeatBitApiClient
builder.Services.AddHttpClient<FeatBitApiClient>();

// ========================================
// Register MCP Server
// ========================================
// Add the MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Add MCP tool tracing middleware (must be before exception handler to trace all requests)
app.UseMiddleware<McpToolTracingMiddleware>();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map MCP endpoint to /mcp path
app.MapMcp("/mcp");

app.Run();
