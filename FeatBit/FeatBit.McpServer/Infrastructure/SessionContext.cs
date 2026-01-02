using System.Diagnostics;
using FeatBit.Contracts;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides session context information for the current HTTP request.
/// Uses OpenTelemetry Activity ID or generates a unique ID per request for feature flag evaluation.
/// Note: MCP HTTP transport manages its own sessions internally - this is for FeatBit user context only.
/// </summary>
public class SessionContext : ISessionContext
{
    private readonly string _sessionId;

    public SessionContext(IHttpContextAccessor httpContextAccessor)
    {
        // Use OpenTelemetry trace ID as session identifier for feature flags
        // This provides a unique but traceable identifier per request
        _sessionId = Activity.Current?.Id 
                     ?? httpContextAccessor.HttpContext?.TraceIdentifier 
                     ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public string SessionId => _sessionId;
}
