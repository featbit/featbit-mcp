namespace FeatBit.McpServer.Tools.Sdks;

public static class JavaSdk
{
    public static SdkInfo Info => new(
        Platform: "Java",
        Type: "Server",
        PackageName: "featbit-java-server-sdk",
        InstallCommand: "implementation 'co.featbit:server-sdk-java:latest'",
        Repository: "https://github.com/featbit/featbit-java-server-sdk",
        Documentation: "https://docs.featbit.co/sdk/server-side-sdks/java"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // 1. Add Gradle Dependency
        // implementation 'co.featbit:server-sdk-java:latest'
        
        import co.featbit.server.*;
        import co.featbit.server.exterior.*;
        
        // 2. Initialize FeatBit Client
        FBConfig config = new FBConfig.Builder()
            .streamingURL("{{{streamingUrl}}}")
            .eventsURL("{{{eventUrl}}}")
            .build();
        
        FBClient client = new FBClient("{{{envSecret}}}", config);
        
        // Wait for initialization
        if (!client.isInitialized()) {
            System.out.println("FeatBit SDK initialization failed");
        }
        
        // 3. Define User
        FBUser user = new FBUser.Builder("user-unique-id")
            .userName("User Name")
            .custom("email", "user@example.com")
            .build();
        
        // 4. Get Feature Flag Value
        boolean flagValue = client.boolVariation("feature-flag-key", user, false);
        
        if (flagValue) {
            System.out.println("Feature is enabled");
            // Execute new feature code
        } else {
            System.out.println("Feature is disabled");
            // Execute old code
        }
        
        // 5. Get Other Types of Feature Flags
        String stringFlag = client.stringVariation("string-flag-key", user, "default");
        Double numberFlag = client.numberVariation("number-flag-key", user, 0.0);
        JsonElement jsonFlag = client.jsonVariation("json-flag-key", user, new JsonObject());
        
        // 6. Close Client When Application Shuts Down
        client.close();
        """;
    }
}
