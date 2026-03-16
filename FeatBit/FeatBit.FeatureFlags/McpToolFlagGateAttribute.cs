namespace FeatBit.FeatureFlags;

/// <summary>
/// Hides the MCP tool from <c>tools/list</c> when the referenced feature flag is disabled.
/// Use <c>nameof(FeatureFlag.SomeFlag)</c> as the argument so the reference is refactoring-safe.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpToolFlagGateAttribute(string flagFieldName) : Attribute
{
    public string FlagFieldName { get; } = flagFieldName;
}
