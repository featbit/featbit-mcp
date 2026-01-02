namespace FeatBit.FeatureFlags;

/// <summary>
/// Provides feature flag evaluation with OpenTelemetry tracing support.
/// Wraps the FeatBit SDK client to add observability for feature flag evaluations.
/// </summary>
public interface IFeatureFlagEvaluator
{
    /// <summary>
    /// Evaluates a boolean feature flag with tracing support.
    /// Creates a span in OpenTelemetry that includes flag key, result, and user information.
    /// User context is automatically created from the current session.
    /// </summary>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <returns>The evaluated boolean value</returns>
    bool ReleaseEnabled(FeatureFlag flag);
}
