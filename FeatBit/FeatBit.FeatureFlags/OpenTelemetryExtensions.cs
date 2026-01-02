using OpenTelemetry.Trace;

namespace FeatBit.FeatureFlags;

/// <summary>
/// OpenTelemetry configuration extensions for FeatBit.FeatureFlags library.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds FeatBit.FeatureFlags tracing instrumentation to the OpenTelemetry TracerProviderBuilder.
    /// This configures the tracer to capture feature flag evaluation spans.
    /// </summary>
    /// <param name="builder">The TracerProviderBuilder to configure</param>
    /// <returns>The configured TracerProviderBuilder for method chaining</returns>
    public static TracerProviderBuilder AddFeatBitFeatureFlagsInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource("FeatBit.FeatureFlags.Evaluator");
    }
}
