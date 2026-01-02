namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides access to the current session context information.
/// </summary>
public interface ISessionContext
{
    /// <summary>
    /// Gets the current session ID from MCP headers, OpenTelemetry trace, or generates a fallback.
    /// </summary>
    string SessionId { get; }
}
