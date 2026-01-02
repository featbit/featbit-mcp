using FeatBit.McpServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

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
        // Use the deployment service to get appropriate documentation
        var documentation = await deploymentService.GetDeploymentDocumentationAsync(
            method, 
            whereToDeploy, 
            topic);
        
        return documentation ?? string.Empty;
    }
}
