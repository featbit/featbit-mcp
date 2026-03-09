using FeatBit.Contracts;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides session context for the MCP server by using the current HTTP request's
/// trace identifier as the session ID for feature flag evaluation.
/// </summary>
public class McpSessionContext(IHttpContextAccessor httpContextAccessor) : ISessionContext
{
    public string SessionId =>
        httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
}
