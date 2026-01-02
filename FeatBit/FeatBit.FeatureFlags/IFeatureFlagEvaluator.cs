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

    /// <summary>
    /// Evaluates a feature flag and executes an action if enabled.
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="action">The action to execute if the flag is enabled</param>
    void ReleaseEnabledThen(FeatureFlag flag, Action action);

    /// <summary>
    /// Evaluates a feature flag and executes a function if enabled, returning its result or a default value.
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="func">The function to execute if the flag is enabled</param>
    /// <param name="defaultValue">The default value to return if the flag is disabled</param>
    /// <returns>The result of the function if enabled, otherwise the default value</returns>
    T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func, T defaultValue);

    /// <summary>
    /// Evaluates a feature flag and executes a function if enabled, returning its result or default(T).
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="func">The function to execute if the flag is enabled</param>
    /// <returns>The result of the function if enabled, otherwise default(T)</returns>
    T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func);

    /// <summary>
    /// Evaluates a feature flag and executes an async action if enabled.
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="asyncAction">The async action to execute if the flag is enabled</param>
    Task ReleaseEnabledThenAsync(FeatureFlag flag, Func<Task> asyncAction);

    /// <summary>
    /// Evaluates a feature flag and executes an async function if enabled, returning its result or a default value.
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="asyncFunc">The async function to execute if the flag is enabled</param>
    /// <param name="defaultValue">The default value to return if the flag is disabled</param>
    /// <returns>The result of the async function if enabled, otherwise the default value</returns>
    Task<T> ReleaseEnabledThenAsync<T>(FeatureFlag flag, Func<Task<T>> asyncFunc, T defaultValue);

    /// <summary>
    /// Evaluates a feature flag and executes an async function if enabled, returning its result or default(T).
    /// Creates nested spans in OpenTelemetry showing the feature flag check and the wrapped execution.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="flag">The feature flag to evaluate</param>
    /// <param name="asyncFunc">The async function to execute if the flag is enabled</param>
    /// <returns>The result of the async function if enabled, otherwise default(T)</returns>
    Task<T> ReleaseEnabledThenAsync<T>(FeatureFlag flag, Func<Task<T>> asyncFunc);
}
