namespace FeatBit.McpServer.Tools.Sdks;

public static class NodeJsSdk
{
    public static SdkInfo Info => new(
        Platform: "Node.js",
        Type: "Server",
        PackageName: "featbit-node-server-sdk",
        InstallCommand: "npm install featbit-node-server-sdk",
        Repository: "https://github.com/featbit/featbit-node-server-sdk",
        Documentation: "https://docs.featbit.co/sdk/server-side-sdks/node-js"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // 1. Install NPM Package
        // npm install featbit-node-server-sdk
        
        const { FbClientBuilder } = require('featbit-node-server-sdk');
        
        // 2. Initialize FeatBit Client
        const client = new FbClientBuilder()
          .sdkKey('{{{envSecret}}}')
          .streamingUri('{{{streamingUrl}}}')
          .eventsUri('{{{eventUrl}}}')
          .build();
        
        // 3. Wait for Initialization
        await client.waitForInitialization();
        console.log('FeatBit SDK is ready');
        
        // 4. Define User
        const user = {
          keyId: 'user-unique-id',
          name: 'User Name',
          email: 'user@example.com'
        };
        
        // 5. Get Feature Flag Value
        const flagValue = await client.boolVariation('feature-flag-key', user, false);
        
        if (flagValue) {
          console.log('Feature is enabled');
          // Execute new feature code
        } else {
          console.log('Feature is disabled');
          // Execute old code
        }
        
        // 6. Get Other Types of Feature Flags
        const stringFlag = await client.stringVariation('string-flag-key', user, 'default');
        const numberFlag = await client.numberVariation('number-flag-key', user, 0);
        const jsonFlag = await client.jsonVariation('json-flag-key', user, {});
        
        // 7. Close Client When Application Shuts Down
        await client.close();
        """;
    }
}
