namespace FeatBit.McpServer.Tools.Deployments;

public static class FeatBitArchitecture
{
    public static string GetArchitectureTutorial(string? version = null)
    {
        return version?.ToLower() switch
        {
            _ => GetOverviewArchitecture()
        };
    }

    private static string GetOverviewArchitecture()
    {
        return """
        NOT IMPLEMENTED.
        """;
    }
}
