namespace FeatBit.Contracts;

/// <summary>
/// Provides access to the current session context information.
/// This is a shared contract that allows infrastructure implementations to provide
/// session-specific data to other components without tight coupling.
/// </summary>
public interface ISessionContext
{
    /// <summary>
    /// Gets the unique identifier for the current session.
    /// Used for contextual operations like feature flag evaluation.
    /// </summary>
    string SessionId { get; }
}
