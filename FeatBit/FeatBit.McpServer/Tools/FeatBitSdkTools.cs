using System.ComponentModel;
using FeatBit.FeatureFlags;
using FeatBit.McpServer.Services;
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// FeatBit SDK Management Tools
/// Provides SDK querying and integration code generation to help developers quickly integrate FeatBit
/// across various programming languages and platforms.
/// </summary>
[McpServerToolType]
public class FeatBitSdkTools(
    SdkService sdkService,
    ILogger<FeatBitSdkTools> logger)
{
    [McpServerTool]
    [Description("Only call when user explicitly asks for FeatBit SDK cases. Get FeatBit SDK integration code examples for the specified sdk and topic. Helps developers quickly integrate FeatBit into their applications, with best practice.")]
    public async Task<string> GenerateIntegrationCode(
        [Description("Which SDK would you like to use? Options: 'dotnet-server-sdk', 'dotnet-console-sdk', 'dotnet-client-sdk', 'javascript-client-sdk', 'react-webapp-sdk', 'react-native-sdk', 'node-sdk', 'openfeature-node-sdk', 'typescript-client-sdk', 'openfeature-js-client-sdk', 'java-sdk', 'python-sdk', 'go-sdk'")]
        string sdk,
        [Description("Describe what you're trying to achieve with the FeatBit SDK (e.g., integrate featbit .net sdk in asp.net core project.)")]
        string topic)
    {
        // Delegate to SDK service for routing and documentation retrieval
        var documentation = await sdkService.GetSdkDocumentationAsync(sdk, topic);
        
        return documentation ?? string.Empty;
    }
}
