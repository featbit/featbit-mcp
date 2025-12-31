namespace FeatBit.McpServer.Tools.Deployments;

public static class DockerComposeDeployment
{
    public static DeploymentInfo Info => new(
        Name: "Docker Compose Deployment",
        Platform: "Docker",
        Type: "Docker Compose",
        Description: "Deploy FeatBit locally or on any server using Docker Compose",
        Prerequisites: ["Docker Engine 20.10+", "Docker Compose 2.0+"],
        Documentation: "https://docs.featbit.co/deployment/docker-compose",
        Difficulty: "Easy"
    );

    public static string GetTutorial()
    {
        return """
        NOT IMPLEMENTED.
        """;
    }
}
