using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace FeatBit.FeatureFlags;

/// <summary>
/// Extension methods for wiring up the <see cref="McpToolFlagGateAttribute"/> filter.
/// </summary>
public static class McpToolFlagGateFilterExtensions
{
    // Converts PascalCase method name to snake_case to match MCP SDK tool name convention.
    // e.g. "AddFlagTargetUser" → "add_flag_target_user"
    private static string ToSnakeCase(string name) =>
        Regex.Replace(
            Regex.Replace(name, "([A-Z]+)([A-Z][a-z])", "$1_$2"),
            "([a-z\\d])([A-Z])", "$1_$2"
        ).ToLower();

    /// <summary>
    /// Registers a <c>tools/list</c> filter that hides any MCP tool decorated with
    /// <see cref="McpToolFlagGateAttribute"/> when its corresponding feature flag is disabled.
    /// </summary>
    /// <param name="builder">The MCP request filter builder.</param>
    /// <param name="toolsAssembly">The assembly that contains the MCP tool classes.</param>
    public static IMcpRequestFilterBuilder AddMcpToolFlagGateFilter(
        this IMcpRequestFilterBuilder builder,
        Assembly toolsAssembly)
    {
        // Build FeatureFlag lookup by field name once at startup
        var flagsByFieldName = typeof(FeatureFlag)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(FeatureFlag))
            .ToDictionary(f => f.Name, f => (FeatureFlag)f.GetValue(null)!);

        // Build gated tools lookup: MCP tool name (snake_case) → FeatureFlag
        // The MCP SDK converts PascalCase method names to snake_case in the tools/list response.
        var gatedTools = toolsAssembly
            .GetTypes()
            .SelectMany(t => t.GetMethods())
            .Select(m => (Name: ToSnakeCase(m.Name), Gate: m.GetCustomAttribute<McpToolFlagGateAttribute>()))
            .Where(x => x.Gate is not null && flagsByFieldName.ContainsKey(x.Gate.FlagFieldName))
            .ToDictionary(x => x.Name, x => flagsByFieldName[x.Gate!.FlagFieldName]);

        return builder.AddListToolsFilter(next => async (context, ct) =>
        {
            var result = await next(context, ct);
            var flagEvaluator = context.Services?.GetService<IFeatureFlagEvaluator>();
            if (flagEvaluator is not null && result.Tools is { } tools)
            {
                for (var i = tools.Count - 1; i >= 0; i--)
                    if (gatedTools.TryGetValue(tools[i].Name, out var flag) && !flagEvaluator.ReleaseEnabled(flag))
                        tools.RemoveAt(i);
            }

            return result;
        });
    }
}
