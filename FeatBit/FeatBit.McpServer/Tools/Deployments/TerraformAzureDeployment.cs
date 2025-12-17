namespace FeatBit.McpServer.Tools.Deployments;

public static class TerraformAzureDeployment
{
    public static DeploymentInfo Info => new(
        Name: "Terraform Azure Deployment",
        Platform: "Azure",
        Type: "Terraform",
        Description: "Infrastructure as Code deployment of FeatBit on Azure using Terraform",
        Prerequisites: ["Azure subscription", "Terraform 1.0+", "Azure CLI"],
        Documentation: "https://docs.featbit.co/deployment/terraform-azure",
        Difficulty: "Medium"
    );

    public static string GetTutorial()
    {
        return """
        NOT IMPLEMENTED.
        """;
    }
}
