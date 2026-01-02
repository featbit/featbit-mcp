using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeatBit.McpServer.Services;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// FeatBit Documentation Tools
/// Provides intelligent documentation URL routing to help users find relevant FeatBit documentation
/// based on their questions or topics.
/// </summary>
[McpServerToolType]
public class FeatBitDocTools(
    DocService docService)
{
    private static readonly ActivitySource ActivitySource = new("FeatBit.McpTools");
    [McpServerTool]
    [Description("Search for relevant FeatBit documentation. Given a topic or question, this tool returns the most relevant documentation URL. The coding agent can then fetch the content from the URL to answer user questions.")]
    public async Task<string> SearchDocumentation(
        [Description("User's question or topic about FeatBit (e.g., 'What are targeting rules?', 'How to activate the Enterprise License?', 'Can we preset a time to release a feature?')")]
        string topic)
    {
        using var activity = ActivitySource.StartActivity("McpTool.SearchDocumentation");
        activity?.SetTag("mcp.tool.name", "SearchDocumentation");
        activity?.SetTag("mcp.tool.parameter.topic", topic);
        
        // Delegate to doc service for AI-powered URL routing
        var documentationUrls = await docService.GetDocumentationUrlsAsync(topic);

        activity?.SetTag("mcp.tool.result.count", documentationUrls?.Count ?? 0);
        
        // Return the URLs in JSON format
        return JsonSerializer.Serialize(documentationUrls);
    }
}
