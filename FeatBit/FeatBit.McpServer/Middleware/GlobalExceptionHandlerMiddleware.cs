using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace FeatBit.McpServer.Middleware;

/// <summary>
/// Global exception handling middleware that captures all unhandled exceptions
/// and returns a standardized error response.
/// Note: OpenTelemetry tracing and logging are automatically handled by ASP.NET Core instrumentation
/// configured in ServiceDefaults (via builder.AddServiceDefaults()).
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception - OpenTelemetry will automatically capture this
        // ASP.NET Core instrumentation will also record the exception in the current Activity
        _logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            context.Request.Path,
            context.Request.Method);

        // Determine status code based on exception type
        var statusCode = exception switch
        {
            ArgumentException or ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException or FileNotFoundException => HttpStatusCode.NotFound,
            NotImplementedException => HttpStatusCode.NotImplemented,
            TimeoutException => HttpStatusCode.RequestTimeout,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        // Create error response
        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = exception.Message,
            Type = exception.GetType().Name,
            TraceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        // Return JSON error response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    /// <summary>
    /// Standard error response format
    /// </summary>
    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}