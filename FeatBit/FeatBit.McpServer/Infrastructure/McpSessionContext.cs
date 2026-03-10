using FeatBit.Contracts;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Provides session context for the MCP server.
/// Resolves the session ID in order of preference:
///   1. MCP protocol session ID from the "mcp-session-id" request header (stable across requests in the same session)
///   2. HTTP trace identifier as fallback (unique per request)
/// </summary>
public class McpSessionContext(IHttpContextAccessor httpContextAccessor) : ISessionContext
{
    public string SessionId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null) return Guid.NewGuid().ToString();

            return context.Request.Headers.TryGetValue("mcp-session-id", out var sessionId) && !string.IsNullOrEmpty(sessionId)
                ? sessionId.ToString()
                : context.TraceIdentifier;
        }
    }
}
