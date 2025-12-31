namespace FeatBit.McpServer.Tools.Deployments;

public static class HelmDeployment
{
    public static DeploymentInfo Info => new(
        Name: "Helm Chart Deployment",
        Platform: "Kubernetes",
        Type: "Helm",
        Description: "Deploy FeatBit on Kubernetes using Helm charts for orchestration and management",
        Prerequisites: ["Kubernetes cluster (v1.19+)", "Helm 3.0+", "kubectl configured"],
        Documentation: "https://docs.featbit.co/deployment/using-helm",
        Difficulty: "Medium"
    );

    public static string GetTutorial(string? version = null)
    {
        var helmVersion = version ?? "latest";
        
        return $$$"""
        NOT IMPLEMENTED.
        """;
    }
}
