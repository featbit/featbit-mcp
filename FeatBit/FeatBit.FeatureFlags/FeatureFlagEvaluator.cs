using System.Diagnostics;
using FeatBit.Contracts;
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;
using Microsoft.Extensions.Logging;

namespace FeatBit.FeatureFlags;

/// <summary>
/// Implements feature flag evaluation with OpenTelemetry tracing.
/// Each evaluation creates a span with relevant attributes for observability.
/// </summary>
public class FeatureFlagEvaluator : IFeatureFlagEvaluator
{
    private readonly IFbClient _fbClient;
    private readonly ILogger<FeatureFlagEvaluator> _logger;
    private readonly ISessionContext _sessionContext;
    private static readonly ActivitySource ActivitySource = new("FeatBit.FeatureFlags.Evaluator");

    public FeatureFlagEvaluator(
        IFbClient fbClient, 
        ILogger<FeatureFlagEvaluator> logger,
        ISessionContext sessionContext)
    {
        _fbClient = fbClient;
        _logger = logger;
        _sessionContext = sessionContext;
    }

    /// <inheritdoc />
    public bool ReleaseEnabled(FeatureFlag flag)
    {
        using var activity = ActivitySource.StartActivity(
            $"FeatureFlag.Evaluate: {flag.Key}",
            ActivityKind.Server);
        
        // Create user from session context
        var user = FbUser.Builder(_sessionContext.SessionId).Build();
        
        // Add color hint for visualization tools
        // activity?.SetTag("display.color", "purple");
        // activity?.SetTag("ui.color", "#9C27B0");
        // activity?.SetTag("span.category", "feature_flag");
        
        activity?.SetTag("feature_flag.key", flag.Key);
        activity?.SetTag("feature_flag.default_value", flag.DefaultValue);
        activity?.SetTag("feature_flag.user.key", user.Key);
        
        var result = _fbClient.BoolVariation(flag.Key, user, flag.DefaultValue);
        
        activity?.SetTag("feature_flag.result", result);
        activity?.SetTag("feature_flag.description", flag.Description);
        
        return result;
    }
}
