using FeatBit.McpServer.Extensions;
using FeatBit.McpServer.Tools;
using FeatBit.McpServer.Tools.Sdks;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults for observability (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add AI chat client (OpenAI, Azure OpenAI, etc.)
builder.Services.AddAiChatClient(builder.Configuration);

// Register document loader service (no AI dependency - pure document loading)
builder.Services.AddSingleton<DocumentLoader>();

// Register SDK services (each decides whether to use AI for document selection)
builder.Services.AddSingleton<NetServerSdk>();
builder.Services.AddSingleton<JavascriptSdks>();
builder.Services.AddSingleton<JavaSdks>();
builder.Services.AddSingleton<PythonSdks>();

// Register tool classes explicitly for DI
//builder.Services.AddSingleton<FeatBit.McpServer.Tools.FeatBitSdkTools>();

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
