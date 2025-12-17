namespace FeatBit.McpServer.Tools.Sdks;

public static class GoSdk
{
    public static SdkInfo Info => new(
        Platform: "Go",
        Type: "Server",
        PackageName: "featbit-go-server-sdk",
        InstallCommand: "go get github.com/featbit/featbit-go-sdk",
        Repository: "https://github.com/featbit/featbit-go-sdk",
        Documentation: "https://docs.featbit.co/sdk/server-side-sdks/go"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        NOT IMPLEMENTED.
        """;
    }
}
