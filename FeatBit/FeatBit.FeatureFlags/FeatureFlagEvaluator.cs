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

    /// <summary>
    /// Creates and configures an Activity for feature flag evaluation with all standard tags.
    /// </summary>
    private Activity? CreateFeatureFlagActivity(FeatureFlag flag, string operationType, out bool result)
    {
        var activity = ActivitySource.StartActivity(
            $"FeatureFlag.{operationType}: {flag.Key}",
            ActivityKind.Client);
        
        // FeatBit is a remote service - set peer.service and server.address
        activity?.SetTag("peer.service", "featbit");
        activity?.SetTag("server.address", "app-eval.featbit.co");
        
        var user = FbUser.Builder(_sessionContext.SessionId).Build();
        
        activity?.SetTag("feature_flag.key", flag.Key);
        activity?.SetTag("feature_flag.default_value", flag.DefaultValue);
        activity?.SetTag("feature_flag.user.key", user.Key);
        
        result = _fbClient.BoolVariation(flag.Key, user, flag.DefaultValue);
        
        activity?.SetTag("feature_flag.result", result);
        activity?.SetTag("feature_flag.description", flag.Description);
        
        return activity;
    }

    /// <inheritdoc />
    public bool ReleaseEnabled(FeatureFlag flag)
    {
        using var activity = CreateFeatureFlagActivity(flag, "Evaluate", out var result);
        return result;
    }

    /// <inheritdoc />
    public void ReleaseEnabledThen(FeatureFlag flag, Action action)
    {
        using var activity = CreateFeatureFlagActivity(flag, "Guard", out var result);
        
        if (result)
        {
            activity?.AddEvent(new ActivityEvent("Flag enabled, executing action"));
            action();
        }
        else
        {
            activity?.AddEvent(new ActivityEvent("Flag disabled, skipping action"));
            _logger.LogDebug("Feature flag {FlagKey} is disabled, skipping action", flag.Key);
        }
    }

    /// <inheritdoc />
    public T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func, T defaultValue)
    {
        using var activity = CreateFeatureFlagActivity(flag, "Guard", out var result);
        
        if (result)
        {
            activity?.AddEvent(new ActivityEvent("Flag enabled, executing function"));
            var funcResult = func();
            activity?.SetTag("feature_flag.execution.completed", true);
            return funcResult;
        }
        else
        {
            activity?.AddEvent(new ActivityEvent("Flag disabled, returning default value"));
            activity?.SetTag("feature_flag.execution.completed", false);
            _logger.LogDebug("Feature flag {FlagKey} is disabled, returning default value", flag.Key);
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func)
    {
        return ReleaseEnabledThen(flag, func, default(T)!);
    }

    /// <inheritdoc />
    public async Task ReleaseEnabledThenAsync(FeatureFlag flag, Func<Task> asyncAction)
    {
        using var activity = CreateFeatureFlagActivity(flag, "Guard", out var result);
        
        if (result)
        {
            activity?.AddEvent(new ActivityEvent("Flag enabled, executing async action"));
            await asyncAction();
            activity?.SetTag("feature_flag.execution.completed", true);
        }
        else
        {
            activity?.AddEvent(new ActivityEvent("Flag disabled, skipping async action"));
            activity?.SetTag("feature_flag.execution.completed", false);
            _logger.LogDebug("Feature flag {FlagKey} is disabled, skipping async action", flag.Key);
        }
    }

    /// <inheritdoc />
    public async Task<T> ReleaseEnabledThenAsync<T>(FeatureFlag flag, Func<Task<T>> asyncFunc, T defaultValue)
    {
        using var activity = CreateFeatureFlagActivity(flag, "Guard", out var result);
        
        if (result)
        {
            activity?.AddEvent(new ActivityEvent("Flag enabled, executing async function"));
            var funcResult = await asyncFunc();
            activity?.SetTag("feature_flag.execution.completed", true);
            return funcResult;
        }
        else
        {
            activity?.AddEvent(new ActivityEvent("Flag disabled, returning default value"));
            activity?.SetTag("feature_flag.execution.completed", false);
            _logger.LogDebug("Feature flag {FlagKey} is disabled, returning default value", flag.Key);
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public Task<T> ReleaseEnabledThenAsync<T>(FeatureFlag flag, Func<Task<T>> asyncFunc)
    {
        return ReleaseEnabledThenAsync(flag, asyncFunc, default(T)!);
    }
}
