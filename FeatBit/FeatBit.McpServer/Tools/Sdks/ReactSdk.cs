namespace FeatBit.McpServer.Tools.Sdks;

public static class ReactSdk
{
    public static SdkInfo Info => new(
        Platform: "React",
        Type: "Client",
        PackageName: "featbit-react-client-sdk",
        InstallCommand: "npm install featbit-react-client-sdk",
        Repository: "https://github.com/featbit/featbit-react-client-sdk",
        Documentation: "https://docs.featbit.co/sdk/client-side-sdks/react"
    );

    public static string GenerateIntegrationCode(string envSecret, string streamingUrl, string eventUrl)
    {
        return $$$"""
        // 1. Install NPM Package
        // npm install featbit-react-client-sdk
        
        import React from 'react';
        import { withFbProvider, useFbClient, useFbFlag } from 'featbit-react-client-sdk';
        
        // 2. Configure Provider (in root component)
        const App = () => {
          return (
            <div>
              <FeatureComponent />
            </div>
          );
        };
        
        export default withFbProvider({
          envSecret: '{{{envSecret}}}',
          user: {
            keyId: 'user-unique-id',
            name: 'User Name',
            customizedProperties: [
              { name: 'email', value: 'user@example.com' }
            ]
          },
          options: {
            streamingUri: '{{{streamingUrl}}}',
            eventsUri: '{{{eventUrl}}}'
          }
        })(App);
        
        // 3. Use Feature Flags in Components (with Hooks)
        function FeatureComponent() {
          const flagValue = useFbFlag('feature-flag-key', false);
          
          return (
            <div>
              {flagValue ? (
                <div>✅ Feature is enabled</div>
              ) : (
                <div>❌ Feature is disabled</div>
              )}
            </div>
          );
        }
        
        // 4. Use Client Instance
        function AdvancedComponent() {
          const client = useFbClient();
          
          const checkFeature = () => {
            const value = client.variation('feature-flag-key', false);
            console.log('Feature flag value:', value);
          };
          
          return <button onClick={checkFeature}>Check Feature</button>;
        }
        """;
    }
}
