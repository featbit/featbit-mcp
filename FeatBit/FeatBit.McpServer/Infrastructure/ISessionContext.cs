namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides access to the current session context information.
/// </summary>
public interface ISessionContext
{
    /// <summary>
    /// Gets the current session ID from OpenTelemetry trace or generates a unique ID per request.
    /// Used for feature flag user context, not MCP protocol session management.
    /// </summary>
    string SessionId { get; }
}
