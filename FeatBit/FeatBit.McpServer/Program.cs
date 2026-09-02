using FeatBit.Contracts;
using FeatBit.FeatureFlags;
using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Middleware;
using FeatBit.Sdk.Server.DependencyInjection;
using ModelContextProtocol.AspNetCore;

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
// Initialize FeatBit SDK
// ========================================
builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = builder.Configuration["FeatBit:EnvSecret"];
    options.StreamingUri = new Uri(builder.Configuration["FeatBit:StreamingUri"] ?? "wss://app-eval.featbit.co");
    options.EventUri = new Uri(builder.Configuration["FeatBit:EventUri"] ?? "https://app-eval.featbit.co");
    options.StartWaitTime = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("FeatBit:StartWaitTimeSeconds", 3));
    options.DisableEvents = false;
});

builder.Services.AddScoped<ISessionContext, McpSessionContext>();
builder.Services.AddScoped<IFeatureFlagEvaluator, FeatureFlagEvaluator>();

// ========================================
// Register MCP Server
// ========================================
// Add the MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
        options.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly()
    .WithRequestFilters(r => r.AddMcpToolFlagGateFilter(typeof(Program).Assembly));

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
