namespace FeatBit.McpServer.Tools.Sdks;

public static class CSharpSdk
{
    public static SdkInfo Info => new(
        Platform: "C#/.NET",
        Type: "Server",
        PackageName: "FeatBit.ServerSdk",
        InstallCommand: "dotnet add package FeatBit.ServerSdk",
        Repository: "https://github.com/featbit/featbit-dotnet-sdk",
        Documentation: "https://docs.featbit.co/sdk/server-side-sdks/dotnet"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // 1. Install NuGet Package
        // dotnet add package FeatBit.ServerSdk
        
        using FeatBit.Sdk.Server;
        using FeatBit.Sdk.Server.Model;
        using FeatBit.Sdk.Server.Options;
        
        // 2. Initialize FeatBit Client
        var options = new FbOptionsBuilder("{{{envSecret}}}")
            .StreamingUri(new Uri("{{{streamingUrl}}}"))
            .EventUri(new Uri("{{{eventUrl}}}"))
            .Build();
        
        var client = new FbClient(options);
        
        // Wait for initialization
        if (!await client.InitializeAsync())
        {
            Console.WriteLine("FeatBit SDK initialization failed");
        }
        
        // 3. Define User
        var user = FbUser.Builder("user-unique-id")
            .Name("User Name")
            .Custom("email", "user@example.com")
            .Build();
        
        // 4. Get Feature Flag Value
        var flagValue = client.BoolVariation("feature-flag-key", user, defaultValue: false);
        
        if (flagValue)
        {
            Console.WriteLine("Feature is enabled");
            // Execute new feature code
        }
        else
        {
            Console.WriteLine("Feature is disabled");
            // Execute old code
        }
        
        // 5. Get String Feature Flag
        var stringFlag = client.StringVariation("string-flag-key", user, defaultValue: "default");
        
        // 6. Get Number Feature Flag
        var numberFlag = client.NumberVariation("number-flag-key", user, defaultValue: 0);
        
        // 7. Get JSON Feature Flag
        var jsonFlag = client.JsonVariation("json-flag-key", user, defaultValue: "{}");
        
        // 8. Close Client When Application Shuts Down
        await client.CloseAsync();
        """;
    }
}
