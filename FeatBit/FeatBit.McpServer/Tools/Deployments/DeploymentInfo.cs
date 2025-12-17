namespace FeatBit.McpServer.Tools.Deployments;

/// <summary>
/// Information about a FeatBit deployment method
/// </summary>
/// <param name="Name">Deployment method name</param>
/// <param name="Platform">Target platform (e.g., Azure, AWS, Kubernetes, Docker)</param>
/// <param name="Type">Deployment type (e.g., Helm, Terraform, Docker Compose, Aspire)</param>
/// <param name="Description">Brief description of the deployment method</param>
/// <param name="Prerequisites">List of prerequisites required for this deployment</param>
/// <param name="Documentation">Link to official documentation</param>
/// <param name="Difficulty">Difficulty level: Easy, Medium, Hard</param>
public record DeploymentInfo(
    string Name,
    string Platform,
    string Type,
    string Description,
    string[] Prerequisites,
    string Documentation,
    string Difficulty
);
