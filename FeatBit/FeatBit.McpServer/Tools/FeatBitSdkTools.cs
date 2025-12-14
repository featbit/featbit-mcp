using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using FeatBit.McpServer.Tools.Sdks;

/// <summary>
/// FeatBit SDK Management Tools
/// Provides SDK querying and integration code generation to help developers quickly integrate FeatBit
/// </summary>
[McpServerToolType]
public class FeatBitSdkTools(ILogger<FeatBitSdkTools> logger)
{
    private static readonly List<SdkInfo> _availableSDKs = new()
    {
        // Client-side SDKs
        JavaScriptSdk.Info,
        ReactSdk.Info,
        
        // Server-side SDKs
        CSharpSdk.Info,
        NodeJsSdk.Info,
        PythonSdk.Info,
        JavaSdk.Info,
        GoSdk.Info
    };

    [McpServerTool]
    [Description("Get a list of all FeatBit supported SDKs. Can be filtered by programming language or SDK type. Returns complete information including installation commands and documentation links.")]
    public List<SdkInfo> GetSDKs(
        [Description("Optional: Filter by programming language, such as 'javascript', 'python', 'csharp', 'java', 'go', etc.")] 
        string? language = null,
        [Description("Optional: Filter by SDK type. 'Client' for client-side SDKs, 'Server' for server-side SDKs")]
        string? type = null)
    {
        logger.LogInformation("MCP Tool Called: GetSDKs with language={Language}, type={Type}", language, type);
        
        var result = _availableSDKs.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(language))
        {
            result = result.Where(s => s.Platform.Contains(language, StringComparison.OrdinalIgnoreCase) ||
                                      s.PackageName.Contains(language, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrWhiteSpace(type))
        {
            result = result.Where(s => s.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
        
        var sdks = result.ToList();
        logger.LogInformation("MCP Tool Result: GetSDKs returned {Count} SDKs", sdks.Count);
        
        return sdks;
    }

    [McpServerTool]
    [Description("Generate FeatBit SDK integration code examples for the specified programming language. Can return specific topic examples or comprehensive guide. Topics: basic, advanced-config, evaluation, evaluation-detail, events, user-management, experimentation, lifecycle, practical-examples, all (default).")]
    public string GenerateIntegrationCode(
        [Description("Programming language: 'csharp', 'javascript', 'typescript', 'node', 'python', 'java', 'go', 'react'")] 
        string language,
        [Description("FeatBit Environment Secret, obtained from FeatBit management portal")] 
        string envSecret,
        [Description("Optional: FeatBit streaming server URL. Defaults to cloud service URL")] 
        string? streamingUrl = null,
        [Description("Optional: FeatBit events server URL. Defaults to cloud service URL")]
        string? eventUrl = null,
        [Description("Optional: Specific topic to generate code for. Options: basic, advanced-config, evaluation, evaluation-detail, events, user-management, experimentation, lifecycle, practical-examples, all (default). Currently only supported for JavaScript SDK.")]
        string? topic = null)
    {
        logger.LogInformation("MCP Tool Called: GenerateIntegrationCode for language={Language}, topic={Topic}", language, topic);
        
        var streaming = streamingUrl ?? "wss://app.featbit.co";
        var events = eventUrl ?? "https://app.featbit.co";
        var selectedTopic = topic ?? "all";
        
        var code = language.ToLower() switch
        {
            "csharp" or "dotnet" or "c#" => CSharpSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "javascript" or "js" => JavaScriptSdk.GenerateIntegrationCode(envSecret, streaming, events, selectedTopic),
            "typescript" or "ts" => TypeScriptSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "node" or "nodejs" or "node.js" => NodeJsSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "python" or "py" => PythonSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "java" => JavaSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "go" or "golang" => GoSdk.GenerateIntegrationCode(envSecret, streaming, events),
            "react" => ReactSdk.GenerateIntegrationCode(envSecret, streaming, events),
            _ => $"Language '{language}' is not currently supported. Supported languages: C#, JavaScript, TypeScript, Node.js, Python, Java, Go, React"
        };
        
        logger.LogInformation("MCP Tool Result: GenerateIntegrationCode completed for {Language}, topic={Topic}", language, selectedTopic);
        return code;
    }
}
