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
        // 1. Install Go Package
        // go get github.com/featbit/featbit-go-sdk
        
        package main
        
        import (
            "fmt"
            "time"
            
            fb "github.com/featbit/featbit-go-sdk"
        )
        
        func main() {
            // 2. Initialize FeatBit Client
            config := fb.ConfigBuilder().
                StreamingURL("{{{streamingUrl}}}").
                EventsURL("{{{eventUrl}}}").
                Build()
            
            client, err := fb.NewClient("{{{envSecret}}}", config)
            if err != nil {
                fmt.Printf("FeatBit SDK initialization failed: %v\n", err)
                return
            }
            defer client.Close()
            
            // Wait for initialization
            client.WaitForInitialization(10 * time.Second)
            
            // 3. Define User
            user := fb.NewUserBuilder("user-unique-id").
                Name("User Name").
                Custom("email", "user@example.com").
                Build()
            
            // 4. Get Feature Flag Value
            flagValue := client.BoolVariation("feature-flag-key", user, false)
            
            if flagValue {
                fmt.Println("Feature is enabled")
                // Execute new feature code
            } else {
                fmt.Println("Feature is disabled")
                // Execute old code
            }
            
            // 5. Get Other Types of Feature Flags
            stringFlag := client.StringVariation("string-flag-key", user, "default")
            numberFlag := client.NumberVariation("number-flag-key", user, 0)
            jsonFlag := client.JsonVariation("json-flag-key", user, "{}")
            
            fmt.Printf("String: %s, Number: %f, JSON: %s\n", stringFlag, numberFlag, jsonFlag)
        }
        """;
    }
}
