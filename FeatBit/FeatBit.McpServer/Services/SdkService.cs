using FeatBit.McpServer.Domain.Sdks;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Services;

/// <summary>
/// Service for handling FeatBit SDK documentation routing and selection.
/// This service provides a unified interface for retrieving SDK integration documentation
/// across different programming languages and platforms.
/// </summary>
public class SdkService
{
    private readonly NetServerSdk _netServerSdk;
    private readonly JavascriptSdks _javascriptSdks;
    private readonly JavaSdks _javaSdks;
    private readonly PythonSdks _pythonSdks;
    private readonly GoSdks _goSdks;
    private readonly ILogger<SdkService> _logger;

    /// <summary>
    /// SDK categories for routing
    /// </summary>
    private enum SdkCategory
    {
        DotNet,
        JavaScript,
        Java,
        Python,
        Go,
        Unknown
    }

    public SdkService(
        NetServerSdk netServerSdk,
        JavascriptSdks javascriptSdks,
        JavaSdks javaSdks,
        PythonSdks pythonSdks,
        GoSdks goSdks,
        ILogger<SdkService> logger)
    {
        _netServerSdk = netServerSdk;
        _javascriptSdks = javascriptSdks;
        _javaSdks = javaSdks;
        _pythonSdks = pythonSdks;
        _goSdks = goSdks;
        _logger = logger;
    }

    /// <summary>
    /// Gets SDK integration documentation based on the SDK identifier and topic.
    /// Routes to the appropriate SDK-specific service.
    /// </summary>
    /// <param name="sdkIdentifier">SDK identifier (e.g., 'dotnet-server-sdk', 'javascript-client-sdk')</param>
    /// <param name="topic">Specific integration topic or scenario</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SDK integration documentation content</returns>
    public async Task<string> GetSdkDocumentationAsync(
        string sdkIdentifier,
        string topic,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting SDK documentation for sdk={Sdk}, topic={Topic}",
            sdkIdentifier, topic);

        var category = GetSdkCategory(sdkIdentifier);
        
        var result = category switch
        {
            SdkCategory.DotNet => await _netServerSdk.GenerateSdkGuideAsync(topic, cancellationToken),
            SdkCategory.JavaScript => _javascriptSdks.GenerateSdkGuide(sdkIdentifier),
            SdkCategory.Java => _javaSdks.GenerateSdkGuide(sdkIdentifier),
            SdkCategory.Python => _pythonSdks.GenerateSdkGuide(sdkIdentifier),
            SdkCategory.Go => _goSdks.GenerateSdkGuide(sdkIdentifier),
            _ => GetUnknownSdkMessage(sdkIdentifier)
        };

        _logger.LogInformation("Successfully retrieved SDK documentation for {Sdk}", sdkIdentifier);
        return result;
    }

    /// <summary>
    /// Determines the SDK category based on the identifier
    /// </summary>
    private static SdkCategory GetSdkCategory(string sdkIdentifier)
    {
        var normalized = sdkIdentifier.ToLowerInvariant();

        return normalized switch
        {
            // .NET SDKs
            "dotnet-server-sdk" or "dotnet-console-sdk" or "dotnet-client-sdk" 
                => SdkCategory.DotNet,

            // JavaScript/TypeScript SDKs
            "javascript-client-sdk" or "typescript-client-sdk" or 
            "react-webapp-sdk" or "react-native-sdk" or 
            "node-sdk" or "openfeature-node-sdk" or "openfeature-js-client-sdk"
                => SdkCategory.JavaScript,

            // Java SDKs
            "java-sdk" or "java-server-sdk"
                => SdkCategory.Java,

            // Python SDKs
            "python-sdk" or "python-server-sdk"
                => SdkCategory.Python,

            // Go SDKs
            "go-sdk" or "go-server-sdk"
                => SdkCategory.Go,

            _ => SdkCategory.Unknown
        };
    }

    /// <summary>
    /// Returns a helpful message for unknown SDKs
    /// </summary>
    private static string GetUnknownSdkMessage(string sdkIdentifier)
    {
        return $$$"""
            ❌ Unknown SDK identifier: '{{{sdkIdentifier}}}'
            
            ## Supported SDKs
            
            ### .NET
            - dotnet-server-sdk
            - dotnet-console-sdk
            - dotnet-client-sdk
            
            ### JavaScript/TypeScript
            - javascript-client-sdk
            - typescript-client-sdk
            - react-webapp-sdk
            - react-native-sdk
            - node-sdk
            - openfeature-node-sdk
            - openfeature-js-client-sdk
            
            ### Java
            - java-sdk
            - java-server-sdk
            
            ### Python
            - python-sdk
            - python-server-sdk
            
            ### Go
            - go-sdk
            - go-server-sdk
            
            Please choose one of the supported SDK identifiers and try again.
            """;
    }

    /// <summary>
    /// Gets a list of all supported SDK identifiers with descriptions
    /// </summary>
    public IEnumerable<object> GetSupportedSdks()
    {
        return new[]
        {
            new { Sdk = "dotnet-server-sdk", Language = ".NET", Type = "Server", Description = "ASP.NET Core, Web APIs, Background Services" },
            new { Sdk = "dotnet-console-sdk", Language = ".NET", Type = "Server", Description = "Console Applications, CLI Tools" },
            new { Sdk = "dotnet-client-sdk", Language = ".NET", Type = "Client", Description = "Desktop, Mobile (MAUI, Xamarin)" },
            
            new { Sdk = "javascript-client-sdk", Language = "JavaScript", Type = "Client", Description = "Vanilla JavaScript for Web" },
            new { Sdk = "typescript-client-sdk", Language = "TypeScript", Type = "Client", Description = "TypeScript for Web" },
            new { Sdk = "react-webapp-sdk", Language = "React", Type = "Client", Description = "React Web Applications" },
            new { Sdk = "react-native-sdk", Language = "React Native", Type = "Client", Description = "React Native Mobile Apps" },
            new { Sdk = "node-sdk", Language = "Node.js", Type = "Server", Description = "Node.js Backend Services" },
            new { Sdk = "openfeature-node-sdk", Language = "Node.js", Type = "Server", Description = "OpenFeature Node.js Provider" },
            new { Sdk = "openfeature-js-client-sdk", Language = "JavaScript", Type = "Client", Description = "OpenFeature JavaScript Provider" },
            
            new { Sdk = "java-sdk", Language = "Java", Type = "Server", Description = "Java Backend Services" },
            new { Sdk = "python-sdk", Language = "Python", Type = "Server", Description = "Python Backend Services" },
            new { Sdk = "go-sdk", Language = "Go", Type = "Server", Description = "Go Backend Services" }
        };
    }
}
