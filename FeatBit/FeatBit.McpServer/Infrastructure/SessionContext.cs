using System.Diagnostics;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides session context information for the current HTTP request.
/// Scoped service that extracts session ID from MCP headers or OpenTelemetry trace.
/// </summary>
public class SessionContext : ISessionContext
{
    private readonly string _sessionId;

    public SessionContext(IHttpContextAccessor httpContextAccessor)
    {
        _sessionId = httpContextAccessor.HttpContext?.Request.Headers["Mcp-Session-Id"].FirstOrDefault() 
                     ?? Activity.Current?.Id 
                     ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public string SessionId => _sessionId;
}
