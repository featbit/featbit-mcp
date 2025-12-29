using System.ComponentModel;
using FeatBit.McpServer.Tools.Deployments;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

[McpServerToolType]
public static class FeatBitDeploymentTools
{
    [McpServerTool]
    [Description("Only call when user explicitly asks about deployment of FeatBit, provide step-by-step deployment tutorials for various platforms and scenarios. This tool will help users deploy FeatBit efficiently. User must specify what method they use, where they want to deploy, and what topic they want to learn about.")]
    public static string HowToDeploy(
        [Description("The deployment method. Options: helm-charts, terraform, docker-compose, aspire, others")]
        string method,
        [Description("Where to deploy.  Options: azure, aws, gcp, on-premises, docker-compose, kubernetes, all")]
        string whereToDeploy,
        [Description("Describe the specific deployment topic you want to learn.")]
        string topic)
    {
        return "NOT IMPLEMENTED YET";
    }
}
