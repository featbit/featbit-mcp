using FeatBit.McpServer.Domain.Sdks;
using FeatBit.McpServer.Extensions;
using FeatBit.McpServer.FeatureFlags;
using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Middleware;
using FeatBit.McpServer.Services;
using FeatBit.Sdk.Server.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults for observability (OpenTelemetry, health checks, etc.)
// This already includes OpenTelemetry logging, tracing, and metrics
builder.AddServiceDefaults();

// Online Mode - use AddFeatBit extension
builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = builder.Configuration["FeatBit:EnvSecret"];
    options.StreamingUri = new Uri(builder.Configuration["FeatBit:StreamingUri"] ?? "wss://app-eval.featbit.co");
    options.EventUri = new Uri(builder.Configuration["FeatBit:EventUri"] ?? "https://app-eval.featbit.co");
    options.StartWaitTime = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("FeatBit:StartWaitTimeSeconds", 3));
    options.DisableEvents = true;
});

// ========================================
// Register Core Services
// ========================================
// Add AI chat client (OpenAI, Azure OpenAI, etc.)
builder.Services.AddAiChatClient(builder.Configuration);

// Add HttpContextAccessor to access HTTP request information
builder.Services.AddHttpContextAccessor();

// Session context - provides session ID from MCP headers or OpenTelemetry trace
builder.Services.AddScoped<ISessionContext, SessionContext>();

// Document loader service - abstraction for loading documents from various sources
// Currently using embedded resources, but can be replaced with S3, HTTP, Database, etc.
builder.Services.AddSingleton<IDocumentLoader, ResourcesDocumentLoader>();

// Claude Skills markdown parser for parsing all markdown documentation
builder.Services.AddSingleton<IClaudeSkillsMarkdownParser, ClaudeSkillsMarkdownParser>();

// Feature flag evaluator with OpenTelemetry tracing support
builder.Services.AddSingleton<IFeatureFlagEvaluator, FeatureFlagEvaluator>();

// Deployment service for routing and selecting deployment documentation
builder.Services.AddSingleton<DeploymentService>();

// SDK service for routing and selecting SDK documentation
builder.Services.AddSingleton<SdkService>();

// Documentation service for routing questions to documentation URLs
builder.Services.AddSingleton<DocService>();

// ========================================
// Register SDK Services
// ========================================
// Each SDK service decides whether to use AI for document selection
builder.Services.AddTransient<NetServerSdk>();
builder.Services.AddTransient<JavascriptSdks>();
builder.Services.AddTransient<JavaSdks>();
builder.Services.AddTransient<PythonSdks>();
builder.Services.AddTransient<GoSdks>();

// ========================================
// Register MCP Server
// ========================================
// Register tool classes explicitly for DI
//builder.Services.AddSingleton<FeatBit.McpServer.Tools.FeatBitSdkTools>();

// Add the MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map MCP endpoint
app.MapMcp();

app.Run();
