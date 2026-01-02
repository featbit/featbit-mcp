using FeatBit.McpServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// FeatBit Deployment Management Tools
/// Provides deployment documentation and guidance to help DevOps teams deploy FeatBit
/// across various platforms and infrastructure configurations.
/// </summary>
[McpServerToolType]
public class FeatBitDeploymentTools(
    DeploymentService deploymentService,
    ILogger<FeatBitDeploymentTools> logger)
{
    private static readonly ActivitySource ActivitySource = new("FeatBit.McpTools");
    [McpServerTool]
    [Description("Only call when user explicitly asks about deployment of FeatBit, provide step-by-step deployment tutorials for various platforms and scenarios. This tool will help users deploy FeatBit efficiently. User must specify what method they use, where they want to deploy, and what topic they want to learn about.")]
    public async Task<string> HowToDeploy(
        [Description("The deployment method, e.g., helm, docker-compose, azure-portal, aws-cloudformation, terraform, others")]
        string method,
        [Description("Where to deploy, e.g., kubernetes, azure, aws, gcp, on-premise, others")]
        string whereToDeploy,
        [Description("Describe the specific deployment topic you want to learn.")]
        string topic)
    {
        using var activity = ActivitySource.StartActivity("McpTool.HowToDeploy");
        activity?.SetTag("mcp.tool.name", "HowToDeploy");
        activity?.SetTag("mcp.tool.parameter.method", method);
        activity?.SetTag("mcp.tool.parameter.whereToDeploy", whereToDeploy);
        activity?.SetTag("mcp.tool.parameter.topic", topic);
        
        logger.LogInformation("MCP Tool Called: HowToDeploy for method={Method}, platform={Platform}, topic={Topic}", 
            method, whereToDeploy, topic);

        // Use the deployment service to get appropriate documentation
        var documentation = await deploymentService.GetDeploymentDocumentationAsync(
            method, 
            whereToDeploy, 
            topic);
        
        activity?.SetTag("mcp.tool.result.length", documentation?.Length ?? 0);
        logger.LogInformation("MCP Tool Result: HowToDeploy completed successfully");
        return documentation ?? string.Empty;
    }
}
