using System.ComponentModel;
using ModelContextProtocol.Server;
using FeatBit.McpServer.Tools.Sdks;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// FeatBit SDK Management Tools
/// Provides SDK querying and integration code generation to help developers quickly integrate FeatBit
/// </summary>
[McpServerToolType]
public class FeatBitSdkTools(ILogger<FeatBitSdkTools> logger, NetServerSdk netServerSdk)
{
    [McpServerTool]
    [Description("Get FeatBit SDK integration code examples for the specified sdk and topic. Helps developers quickly integrate FeatBit into their applications.")]
    public async Task<string> GenerateIntegrationCode(
        [Description("Which SDK would you like to use? Options: 'dotnet-server-sdk', 'dotnet-console-sdk', 'dotnet-client-sdk', 'javascript-client-sdk', 'react-sdk', 'node-sdk', 'typescript-client-sdk', 'java-sdk', 'python-sdk', 'go-sdk', 'node-sdk'")]
        string sdk,
        [Description("Describe what you're trying to achieve with the FeatBit SDK (e.g., integrate featbit .net sdk in asp.net core project.)")]
        string topic)
    {
        logger.LogInformation("MCP Tool Called: GenerateIntegrationCode for language={Language}, topic={Topic}", sdk, topic);

        var code = sdk.ToLower() switch
        {
            ".net server sdk" => await netServerSdk.GenerateSdkGuideAsync(topic),
            _ => "Please enter a valid SDK option. Supported options are: '.net server sdk', '.net client sdk', 'javascript client sdk', 'react sdk', 'nodejs sdk', 'typescript client sdk', 'java sdk', 'python sdk', 'go sdk', 'node sdk'."
        };

        logger.LogInformation("MCP Tool Result: GenerateIntegrationCode completed for {Language}, topic={Topic}", sdk, topic);
        return code;
    }
}
