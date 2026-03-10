namespace FeatBit.FeatureFlags;

/// <summary>
/// Represents a feature flag with its key and default value.
/// 
/// Architecture Design - Cross-Cutting Concern:
/// - Feature flags are application-level feature toggles, not domain-specific business concepts
/// - Located in FeatureFlags/ at project root, separate from Domain/ business layer
/// - Can control functionality across any module (Sdks, Deployments, Services, Tools)
/// - Similar to logging, monitoring, and other infrastructure-level concerns
/// 
/// Design Benefits:
/// - Immutable record type for type safety
/// - Binds Key, DefaultValue, and Description together, ensuring consistency
/// - Easy reference tracking via IDE's "Find References"
/// </summary>
/// <param name="Key">The feature flag key used in FeatBit</param>
/// <param name="DefaultValue">The default value when FeatBit is unavailable or in offline mode</param>
/// <param name="Description">Human-readable description of what this flag controls</param>
public sealed record FeatureFlag(string Key, bool DefaultValue, string Description, string DefaultStringValue = "")
{
    public static readonly FeatureFlag DocNotFound = new(
        Key: "doc-not-found",
        DefaultValue: false,
        Description: "Controls whether to return a suggestion message when no documentation is found"
    );

    public static readonly FeatureFlag AddFeatureFlagTargetUser = new(
        Key: "add-feature-flag-target-user",
        DefaultValue: false,
        Description: "Controls whether the AddFlagTargetUser tool is available"
    );

    public static readonly FeatureFlag FlagList = new(
        Key: "flag-list",
        DefaultValue: false,
        Description: "Controls the verbosity of GetFeatureFlags: 'full' = full objects, 'short' = keys only, 'key-ct-ut' = key + createdAt + updatedAt",
        DefaultStringValue: "full"
    );
}
