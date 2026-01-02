using System.Diagnostics;
using FeatBit.McpServer.Infrastructure;
using FeatBit.Sdk.Server;
using FeatBit.Sdk.Server.Model;

namespace FeatBit.McpServer.FeatureFlags;

/// <summary>
/// Implements feature flag evaluation with OpenTelemetry tracing.
/// Each evaluation creates a span with relevant attributes for observability.
/// </summary>
public class FeatureFlagEvaluator : IFeatureFlagEvaluator
{
    private readonly IFbClient _fbClient;
    private readonly ILogger<FeatureFlagEvaluator> _logger;
    private readonly ISessionContext _sessionContext;
    private static readonly ActivitySource ActivitySource = new("FeatBit.FeatureFlags");

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
        using var activity = ActivitySource.StartActivity($"FeatureFlag.Evaluate: {flag.Key}");
        
        // Create user from session context
        var user = FbUser.Builder(_sessionContext.SessionId).Build();
        
        activity?.SetTag("feature_flag.key", flag.Key);
        activity?.SetTag("feature_flag.default_value", flag.DefaultValue);
        activity?.SetTag("feature_flag.user.key", user.Key);
        
        var result = _fbClient.BoolVariation(flag.Key, user, flag.DefaultValue);
        
        activity?.SetTag("feature_flag.result", result);
        activity?.SetTag("feature_flag.description", flag.Description);
        
        _logger.LogDebug(
            "Feature flag evaluated: {FlagKey} = {Result} for user {UserKey}",
            flag.Key,
            result,
            user.Key);
        
        return result;
    }
}
