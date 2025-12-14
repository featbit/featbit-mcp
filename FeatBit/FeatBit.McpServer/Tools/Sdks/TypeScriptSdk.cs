namespace FeatBit.McpServer.Tools.Sdks;

public static class TypeScriptSdk
{
    public static SdkInfo Info => JavaScriptSdk.Info; // Same SDK, different usage

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // 1. Install NPM Package
        // npm install featbit-js-client-sdk
        
        import { initialize, IOption, IUser, FbClient } from 'featbit-js-client-sdk';
        
        // 2. Define Configuration and User
        const option: IOption = {
          streamingUri: '{{{streamingUrl}}}',
          eventsUri: '{{{eventUrl}}}'
        };
        
        const user: IUser = {
          keyId: 'user-unique-id',
          name: 'User Name',
          customizedProperties: [
            { name: 'email', value: 'user@example.com' }
          ]
        };
        
        // 3. Initialize Client
        const client: FbClient = initialize('{{{envSecret}}}', user, option);
        
        // 4. Wait for Initialization
        await client.waitUntilReady();
        console.log('FeatBit SDK is ready');
        
        // 5. Get Feature Flag Value (Strongly Typed)
        const flagValue: boolean = client.variation('feature-flag-key', false);
        
        if (flagValue) {
          console.log('Feature is enabled');
        }
        
        // 6. Get String Feature Flag
        const stringFlag: string = client.variation('string-flag-key', 'default');
        
        // 7. Get Number Feature Flag
        const numberFlag: number = client.variation('number-flag-key', 0);
        """;
    }
}
