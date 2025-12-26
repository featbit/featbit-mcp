# FeatBit .NET Server SDK - ASP.NET Core Integration

This document provides comprehensive guidance for integrating the FeatBit .NET Server SDK into ASP.NET Core applications using dependency injection.

--------------------------------

### NuGet Installation

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#installation

Install the FeatBit Server SDK via NuGet package manager.

```bash
dotnet add package FeatBit.ServerSdk
```

--------------------------------

### ASP.NET Core - Basic Setup with Dependency Injection

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#dependency-injection

Register FeatBit services in your ASP.NET Core application. The SDK will automatically use the ILoggerFactory provided by the host.

```csharp
using FeatBit.Sdk.Server.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Add FeatBit service
builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = "<replace-with-your-env-secret>";
    options.StreamingUri = new Uri("wss://app-eval.featbit.co");
    options.EventUri = new Uri("https://app-eval.featbit.co");
    options.StartWaitTime = TimeSpan.FromSeconds(3);
    options.DisableEvents = true; // Disable event collection
});

var app = builder.Build();
app.MapControllers();
app.Run();
```

--------------------------------

### Use Feature Flag with Authenticated User in Controller

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#dependency-injection

Inject IFbClient into controllers and evaluate flags for authenticated users with custom properties.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using Microsoft.AspNetCore.Mvc;

public class HomeController : ControllerBase
{
    private readonly IFbClient _fbClient;

    public HomeController(IFbClient fbClient)
    {
        _fbClient = fbClient;
    }

    [HttpGet("check-feature")]
    public IActionResult CheckFeature()
    {
        // Create user from authenticated context
        var user = FbUser.Builder(User.Identity.Name ?? "anonymous")
            .Name(User.Identity.Name)
            .Custom("role", "admin")
            .Custom("subscription", "premium")
            .Custom("country", "US")
            .Build();

        var isFeatureEnabled = _fbClient.BoolVariation("new-feature", user, defaultValue: false);
        
        return Ok(new { featureEnabled = isFeatureEnabled });
    }
}
```

--------------------------------

### Use Feature Flag with Anonymous User in Controller

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#dependency-injection

Evaluate flags for anonymous or unauthenticated users, useful for public features.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using Microsoft.AspNetCore.Mvc;

public class ApiController : ControllerBase
{
    private readonly IFbClient _fbClient;

    public ApiController(IFbClient fbClient)
    {
        _fbClient = fbClient;
    }

    [HttpGet("public-feature")]
    public IActionResult CheckPublicFeature()
    {
        // Use session ID or generate unique anonymous ID
        var sessionId = HttpContext.Session?.Id ?? Guid.NewGuid().ToString();
        var anonymousUser = FbUser.Builder($"anonymous-{sessionId}").Build();

        var flagValue = _fbClient.BoolVariation("public-beta-feature", anonymousUser, defaultValue: false);
        
        return Ok(new { betaAccess = flagValue });
    }
}
```

--------------------------------

### Complete Web API Example with All Variation Types

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#evaluating-flags

A comprehensive controller example showing all flag variation types and best practices.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController : ControllerBase
{
    private readonly IFbClient _fbClient;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(IFbClient fbClient, ILogger<FeaturesController> logger)
    {
        _fbClient = fbClient;
        _logger = logger;
    }

    [HttpGet("user-config")]
    public IActionResult GetUserConfiguration()
    {
        // Create user context
        var user = FbUser.Builder(User.Identity?.Name ?? "guest")
            .Name(User.Identity?.Name)
            .Custom("userId", User.Identity?.Name)
            .Custom("plan", "premium")
            .Build();

        // Boolean flag
        var newUIEnabled = _fbClient.BoolVariation("new-ui-enabled", user, defaultValue: false);
        
        // String flag
        var theme = _fbClient.StringVariation("user-theme", user, defaultValue: "light");
        
        // Integer flag
        var maxItems = _fbClient.IntVariation("max-items-per-page", user, defaultValue: 10);
        
        // Double flag
        var discountRate = _fbClient.DoubleVariation("discount-rate", user, defaultValue: 0.0);

        // JSON configuration (as string)
        var configJson = _fbClient.StringVariation("app-config", user, defaultValue: "{}");

        _logger.LogInformation("Configuration loaded for user {User}", user.Key);

        return Ok(new
        {
            newUIEnabled,
            theme,
            maxItems,
            discountRate,
            config = configJson
        });
    }

    [HttpGet("feature-detail/{flagKey}")]
    public IActionResult GetFeatureDetail(string flagKey)
    {
        var user = FbUser.Builder(User.Identity?.Name ?? "guest").Build();

        // Get variation with evaluation details
        var detail = _fbClient.BoolVariationDetail(flagKey, user, defaultValue: false);

        return Ok(new
        {
            flagKey,
            value = detail.Value,
            reason = detail.Reason,
            kind = detail.Kind
        });
    }
}
```

--------------------------------

### Event Tracking in ASP.NET Core

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#experiments-abn-testing

Track custom events for A/B testing and experiments in your controllers.

```csharp
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly IFbClient _fbClient;

    public CheckoutController(IFbClient fbClient)
    {
        _fbClient = fbClient;
    }

    [HttpPost("complete")]
    public IActionResult CompleteCheckout([FromBody] CheckoutRequest request)
    {
        var user = FbUser.Builder(User.Identity?.Name ?? "guest")
            .Name(User.Identity?.Name)
            .Custom("email", request.Email)
            .Build();

        // Evaluate feature flag first
        var useNewCheckout = _fbClient.BoolVariation("new-checkout-flow", user, defaultValue: false);

        if (useNewCheckout)
        {
            // Process with new checkout flow
            var success = ProcessNewCheckout(request);
            
            if (success)
            {
                // Track conversion event with revenue
                _fbClient.Track(user, "checkout-completed", numericValue: request.TotalAmount);
                
                // Track specific action
                _fbClient.Track(user, "used-new-checkout");
            }
            
            return Ok(new { success, checkoutVersion = "new" });
        }
        else
        {
            // Process with old checkout flow
            var success = ProcessOldCheckout(request);
            
            if (success)
            {
                _fbClient.Track(user, "checkout-completed", numericValue: request.TotalAmount);
            }
            
            return Ok(new { success, checkoutVersion = "old" });
        }
    }

    private bool ProcessNewCheckout(CheckoutRequest request) => true; // Implementation
    private bool ProcessOldCheckout(CheckoutRequest request) => true; // Implementation
}

public record CheckoutRequest(string Email, decimal TotalAmount);
```

--------------------------------

### ASP.NET Core Configuration from appsettings.json

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#dependency-injection

Configure FeatBit using appsettings.json for easier environment management.

**Program.cs:**
```csharp
using FeatBit.Sdk.Server.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = builder.Configuration["FeatBit:EnvSecret"];
    options.StreamingUri = new Uri(builder.Configuration["FeatBit:StreamingUri"]);
    options.EventUri = new Uri(builder.Configuration["FeatBit:EventUri"]);
    options.StartWaitTime = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("FeatBit:StartWaitTimeSeconds")
    );
});

var app = builder.Build();
app.MapControllers();
app.Run();
```

**appsettings.json:**
```json
{
  "FeatBit": {
    "EnvSecret": "your-env-secret-here",
    "StreamingUri": "wss://app-eval.featbit.co",
    "EventUri": "https://app-eval.featbit.co",
    "StartWaitTimeSeconds": 3
  }
}
```

**appsettings.Development.json:**
```json
{
  "FeatBit": {
    "EnvSecret": "dev-env-secret",
    "StreamingUri": "ws://localhost:5100",
    "EventUri": "http://localhost:5100"
  }
}
```

--------------------------------

### Enable/Disable Events Collection in ASP.NET Core

Source: https://github.com/featbit/featbit-dotnet-sdk/blob/main/README.md#disable-events-collection

Disable automatic event collection while keeping the SDK online for flag updates.

```csharp
using FeatBit.Sdk.Server.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatBit(options =>
{
    options.EnvSecret = builder.Configuration["FeatBit:EnvSecret"];
    options.StreamingUri = new Uri(builder.Configuration["FeatBit:StreamingUri"]);
    options.EventUri = new Uri(builder.Configuration["FeatBit:EventUri"]);
    options.DisableEvents = true; // Enable event collection
});
```

--------------------------------