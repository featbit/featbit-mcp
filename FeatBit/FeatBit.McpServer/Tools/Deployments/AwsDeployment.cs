namespace FeatBit.McpServer.Tools.Deployments;

public static class AwsDeployment
{
    public static DeploymentInfo Info => new(
        Name: "AWS Deployment",
        Platform: "AWS",
        Type: "Multiple (ECS, EKS, EC2)",
        Description: "Deploy FeatBit on AWS using various services like ECS, EKS, or EC2",
        Prerequisites: ["AWS account", "AWS CLI", "Appropriate IAM permissions"],
        Documentation: "https://docs.featbit.co/deployment/aws",
        Difficulty: "Medium"
    );

    public static string GetTutorial(string? method = null)
    {
        return method?.ToLower() switch
        {
            "ecs" => GetEcsTutorial(),
            "eks" => GetEksTutorial(),
            "ec2" => GetEc2Tutorial(),
            _ => GetOverviewTutorial()
        };
    }

    private static string GetOverviewTutorial()
    {
        return """
        # ============================================================================
        # FeatBit AWS Deployment Overview
        # ============================================================================
        
        NOT IMPLEMENTED.
        """;
    }

    private static string GetEcsTutorial()
    {
        return """
        # ============================================================================
        # FeatBit AWS ECS Deployment Tutorial
        # ============================================================================
        
        
        NOT IMPLEMENTED.
        """;
    }

    private static string GetEksTutorial()
    {
        return """
        # ============================================================================
        # FeatBit AWS EKS Deployment Tutorial
        # ============================================================================
        
        
        NOT IMPLEMENTED.
        """;
    }
}
