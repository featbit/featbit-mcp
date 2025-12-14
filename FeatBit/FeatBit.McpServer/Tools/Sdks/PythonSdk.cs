namespace FeatBit.McpServer.Tools.Sdks;

public static class PythonSdk
{
    public static SdkInfo Info => new(
        Platform: "Python",
        Type: "Server",
        PackageName: "featbit-python-sdk",
        InstallCommand: "pip install featbit-python-sdk",
        Repository: "https://github.com/featbit/featbit-python-sdk",
        Documentation: "https://docs.featbit.co/sdk/server-side-sdks/python"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        # 1. Install Python Package
        # pip install featbit-python-sdk
        
        from featbit import FbClient, FbOptionsBuilder, FbUser
        
        # 2. Initialize FeatBit Client
        options = (FbOptionsBuilder('{{{envSecret}}}')
                  .streaming_uri('{{{streamingUrl}}}')
                  .events_uri('{{{eventUrl}}}')
                  .build())
        
        client = FbClient(options)
        
        # Wait for initialization
        if not client.initialize():
            print('FeatBit SDK initialization failed')
        
        # 3. Define User
        user = (FbUser.builder('user-unique-id')
                .name('User Name')
                .custom('email', 'user@example.com')
                .build())
        
        # 4. Get Feature Flag Value
        flag_value = client.bool_variation('feature-flag-key', user, False)
        
        if flag_value:
            print('Feature is enabled')
            # Execute new feature code
        else:
            print('Feature is disabled')
            # Execute old code
        
        # 5. Get Other Types of Feature Flags
        string_flag = client.string_variation('string-flag-key', user, 'default')
        number_flag = client.number_variation('number-flag-key', user, 0)
        json_flag = client.json_variation('json-flag-key', user, {})
        
        # 6. Close Client When Application Shuts Down
        client.close()
        """;
    }
}
