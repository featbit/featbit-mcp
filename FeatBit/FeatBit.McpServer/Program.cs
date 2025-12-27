using FeatBit.McpServer.Extensions;
using FeatBit.McpServer.Tools.Sdks;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults for observability (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add AI chat client (OpenAI, Azure OpenAI, etc.)
builder.Services.AddAiChatClient(builder.Configuration);

// Register SDK services
builder.Services.AddSingleton<NetServerSdk>();

// Register tool classes explicitly for DI
builder.Services.AddSingleton<FeatBit.McpServer.Tools.FeatBitSdkTools>();
builder.Services.AddSingleton<FeatBit.McpServer.Tools.RandomNumberTools>();

// Add the MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map MCP endpoint
app.MapMcp();

app.Run();
