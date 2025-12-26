# FeatBit .NET Server SDK - Console Application

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md

This document provides comprehensive guidance for integrating the FeatBit .NET Server SDK into Console applications, Worker Services, and other non-ASP.NET Core applications.

--------------------------------

### NuGet Installation

Install the FeatBit Server SDK via NuGet package manager.

```bash
dotnet add package FeatBit.ServerSdk
```

--------------------------------

### SDK Integration and Initialization

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#quick-start

Initialize the FeatBit SDK in a console application.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using FeatBit.Sdk.Server.Options;

// Setup SDK options
var options = new FbOptionsBuilder("<replace-with-your-env-secret>")
    .Event(new Uri("https://app-eval.featbit.co"))
    .Streaming(new Uri("wss://app-eval.featbit.co"))
    .StartWaitTime(TimeSpan.FromSeconds(5))
    .DisableEvents(true)  // Disable evluation insights and custom event tracking by default
    .Build();

// Creates a new client instance
var fbClient = new FbClient(options);

if (!fbClient.Initialized)
{
    Console.WriteLine("FbClient failed to initialize. All Variation calls will use fallback value.");
}
else
{
    Console.WriteLine("FbClient successfully initialized!");
}

// Close the client when done
await fbClient.CloseAsync();
```

--------------------------------

### Evaluate Feature Flag with Specific User After Initialization

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#fbuser

Create users with custom properties to evaluate feature flags with targeting rules.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;

var user = FbUser.Builder("user-123")
    .Name("Bob Smith")
    .Custom("email", "bob@example.com")
    .Custom("plan", "premium")
    .Build();

// Evaluate a boolean flag
var showNewFeature = fbClient.BoolVariation("new-feature", user, defaultValue: false);
if(showNewFeature)
{
    Console.WriteLine($"New feature enabled: {showNewFeature}");
}
else
{
    Console.WriteLine($"New feature disabled: {showNewFeature}");
}
```

--------------------------------

### Evaluate Feature Flag with Anonymous User

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#quick-start

Evaluate flags for anonymous users or batch processing scenarios.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;

// Create an anonymous user
var anonymousUser = FbUser.Builder("anonymous").Build();

// Or generate unique anonymous user
var randomUser = FbUser.Builder($"session-{Guid.NewGuid()}").Build();

// Evaluate flag
var isEnabled = fbClient.BoolVariation("beta-feature", anonymousUser, defaultValue: false);
Console.WriteLine($"Beta feature enabled: {isEnabled}");

await fbClient.CloseAsync();
```

--------------------------------

### Dependency Injection Setup, Worker Service Integration

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#dependency-injection

For long-running console applications or Windows Services, use the Worker Service pattern with dependency injection.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using FeatBit.Sdk.Server.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add FeatBit service
builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = "<replace-with-your-env-secret>";
    options.StreamingUri = new Uri("wss://app-eval.featbit.co");
    options.EventUri = new Uri("https://app-eval.featbit.co");
    options.StartWaitTime = TimeSpan.FromSeconds(3);
});

builder.Services.AddHostedService<FeatureFlagWorker>();

var host = builder.Build();
await host.RunAsync();

public class FeatureFlagWorker : BackgroundService
{
    private readonly IFbClient _fbClient;
    private readonly ILogger<FeatureFlagWorker> _logger;

    public FeatureFlagWorker(IFbClient fbClient, ILogger<FeatureFlagWorker> logger)
    {
        _fbClient = fbClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var user = FbUser.Builder($"worker-{Guid.NewGuid()}").Build();
            var isEnabled = _fbClient.BoolVariation("scheduled-task", user, defaultValue: false);

            if (isEnabled)
            {
                _logger.LogInformation("Scheduled task is enabled. Executing...");
                // Execute your task
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

--------------------------------

### Get All Variation Types in Console Application

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#evaluating-flags

The SDK provides multiple variation methods for different data types. Each has a standard method and a detail method that returns evaluation metadata.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using System.Text.Json;

var user = FbUser.Builder("user-123")
            .Name("Alice")
            .Custom("country", "US")
            .Build();

// Boolean variation
var enableNewUI = client.BoolVariation("enable-new-ui", user, defaultValue: false);
Console.WriteLine($"Enable New UI: {enableNewUI}");

// String variation
var theme = client.StringVariation("theme", user, defaultValue: "light");
Console.WriteLine($"Theme: {theme}");

// Integer variation
var maxConnections = client.IntVariation("max-connections", user, defaultValue: 10);
Console.WriteLine($"Max Connections: {maxConnections}");

// Double variation
var discountRate = client.DoubleVariation("discount-rate", user, defaultValue: 0.0);
Console.WriteLine($"Discount Rate: {discountRate:P}");

// Float variation
var taxRate = client.FloatVariation("tax-rate", user, defaultValue: 0.0f);
Console.WriteLine($"Tax Rate: {taxRate:P}");

// JSON variation (stored as string)
var configJson = client.StringVariation("app-config", user, defaultValue: "{}");
var config = JsonDocument.Parse(configJson);
Console.WriteLine($"Config: {configJson}");

// Using detail methods for debugging
var featureDetail = client.BoolVariationDetail("debug-feature", user, defaultValue: false);
Console.WriteLine($"Feature: {featureDetail.Value}, Reason: {featureDetail.Reason}, Kind: {featureDetail.Kind}");

```

--------------------------------

### Disable Events Collection

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#disable-events-collection

By default, the SDK sends events to the FeatBit server. You can disable event collection while keeping the SDK in online mode to reduce network traffic.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;

var options = new FbOptionsBuilder("<replace-with-your-env-secret>")
    .Event(new Uri("https://app-eval.featbit.co"))
    .Streaming(new Uri("wss://app-eval.featbit.co"))
    .DisableEvents(true)  // Disable event collection
    .Build();

using var client = new FbClient(options);
```

--------------------------------

### Offline Mode with Bootstrap

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#offline-mode

Use offline mode to stop remote calls and populate feature flags from a JSON file. Perfect for testing or environments without internet access.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;

class Program
{
    static async Task Main(string[] args)
    {
        // Option 1: Simple offline mode (uses fallback values only)
        var offlineOptions = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        using var offlineClient = new FbClient(offlineOptions);

        // Option 2: Offline mode with JSON bootstrap
        var bootstrapJson = File.ReadAllText("featbit-bootstrap.json");

        var bootstrapOptions = new FbOptionsBuilder()
            .Offline(true)
            .UseJsonBootstrapProvider(bootstrapJson)
            .Build();

        using var bootstrapClient = new FbClient(bootstrapOptions);

        var user = FbUser.Builder("test-user").Build();
        var result = bootstrapClient.BoolVariation("test-feature", user, defaultValue: false);
        Console.WriteLine($"Feature result: {result}");

        await bootstrapClient.CloseAsync();
    }
}
```

To generate the bootstrap JSON file, use curl:

```bash
curl -H "Authorization: <your-env-secret>" \
  https://app-eval.featbit.co/api/public/sdk/server/latest-all > featbit-bootstrap.json
```

--------------------------------

### Event Tracking and Custom Metrics

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#experiments-abn-testing

Track custom events with metrics for A/B testing and experiments. Events must be tracked AFTER evaluating the related feature flag.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;

class Program
{
    static async Task Main(string[] args)
    {
        using var client = new FbClient("<replace-with-your-env-secret>");

        var user = FbUser.Builder("user-456")
            .Name("Jane Doe")
            .Custom("email", "jane@example.com")
            .Build();

        // IMPORTANT: Evaluate feature flag first
        var newCheckoutEnabled = client.BoolVariation("new-checkout-flow", user, defaultValue: false);
        Console.WriteLine($"New checkout flow: {newCheckoutEnabled}");

        if (newCheckoutEnabled)
        {
            // Simulate purchase
            Console.WriteLine("Processing purchase with new checkout...");

            // Track purchase event with monetary value (numericValue parameter)
            client.Track(user, "checkout-completed", numericValue: 99.99);
            Console.WriteLine("Tracked purchase event");

            // Track events without numeric value (defaults to 1.0)
            client.Track(user, "button-clicked");
            client.Track(user, "add-to-cart");
        }

        // Ensure events are sent before exit
        await client.CloseAsync();
        Console.WriteLine("All events sent successfully");
    }
}
```

Note: The `numericValue` parameter defaults to 1.0 and can represent revenue, count, or any metric for your experiment.

--------------------------------

## Configuration Options

### Custom Options with Logging

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#fbclient-using-custom-options

Configure the FeatBit client with custom options including URLs, timeouts, and logging for console applications.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Options;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a console logger factory
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure SDK with custom options
        var options = new FbOptionsBuilder("<replace-with-your-env-secret>")
            .Streaming(new Uri("wss://app-eval.featbit.co"))
            .Event(new Uri("https://app-eval.featbit.co"))
            .StartWaitTime(TimeSpan.FromSeconds(5))
            .LoggerFactory(loggerFactory)
            .Build();

        using var client = new FbClient(options);

        var user = FbUser.Builder("user-123").Build();
        var result = client.BoolVariation("test-feature", user, defaultValue: false);
        Console.WriteLine($"Feature result: {result}");

        await client.CloseAsync();
    }
}
```

--------------------------------