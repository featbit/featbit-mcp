using System.Diagnostics;
using System.Text.Json;

namespace FeatBit.McpServer.Middleware;

/// <summary>
/// Middleware that automatically traces all MCP tool invocations.
/// Captures tool names, input parameters, and execution results in OpenTelemetry traces.
/// </summary>
public class McpToolTracingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ActivitySource ActivitySource = new("FeatBit.McpTools");

    public McpToolTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only trace MCP tool calls (requests to /mcp endpoint with tool calls)
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await _next(context);
            return;
        }

        // Enable buffering to allow reading the request body multiple times
        context.Request.EnableBuffering();

        // Read and parse the request body
        string requestBody;
        using (var reader = new StreamReader(
            context.Request.Body,
            leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for next middleware
        }

        // Try to parse MCP request to extract tool information
        string? toolName = null;
        JsonDocument? requestJson = null;
        Dictionary<string, object>? parameters = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                requestJson = JsonDocument.Parse(requestBody);
                
                // MCP protocol structure: { "method": "tools/call", "params": { "name": "ToolName", "arguments": {...} } }
                if (requestJson.RootElement.TryGetProperty("method", out var method) &&
                    method.GetString() == "tools/call")
                {
                    if (requestJson.RootElement.TryGetProperty("params", out var paramsElement))
                    {
                        if (paramsElement.TryGetProperty("name", out var nameElement))
                        {
                            toolName = nameElement.GetString();
                        }

                        if (paramsElement.TryGetProperty("arguments", out var argsElement))
                        {
                            parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText());
                        }
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, continue without tracing
        }

        // Create activity for tool invocation
        if (!string.IsNullOrEmpty(toolName))
        {
            using var activity = ActivitySource.StartActivity($"McpTool.{toolName}");
            
            if (activity != null)
            {
                activity.SetTag("mcp.tool.name", toolName);

                // Add all parameters as tags
                if (parameters != null)
                {
                    foreach (var (key, value) in parameters)
                    {
                        activity.SetTag($"mcp.tool.parameter.{key}", value?.ToString());
                    }
                }

                // Capture response
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);

                    // Record response details
                    activity.SetTag("http.status_code", context.Response.StatusCode);
                    activity.SetTag("mcp.tool.result.length", responseBody.Length);

                    // Copy response back to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
            else
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }

        requestJson?.Dispose();
    }
}
