using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    // === Feature Flag Evaluation ===

    [McpServerTool]
    [Description(
        "Evaluate feature flags for a given end user and return the variation served to that user. " +
        "Set the X-FeatBit-Env-Secret request header to the environment secret key before calling this tool. " +
        "Returns an array of flag evaluations, each with the key, variation value, match reason, and experiment tracking info.")]
    public async Task<string> EvaluateFeatureFlags(
        [Description("Unique identifier for the end user")]
        string userKeyId,
        [Description("Display name for the end user (optional)")]
        string? userName = null,
        [Description(
            "JSON array of custom targeting properties for the user, " +
            "e.g. [{\"name\":\"country\",\"value\":\"US\"}] (optional)")]
        string? customProperties = null,
        [Description(
            "Comma-separated feature flag keys to evaluate; " +
            "omit to evaluate all flags in the environment (optional)")]
        string? flagKeys = null,
        [Description(
            "Comma-separated tags to filter flags by, e.g. 'frontend,mobile'; " +
            "omit to skip tag filtering (optional)")]
        string? tags = null,
        [Description(
            "How to combine multiple tags: 'and' (flag must have all tags) or 'or' (flag must have any tag). " +
            "Defaults to 'and' when tags are provided (optional)")]
        string? tagFilterMode = null)
    {
        JsonElement[]? customProps = null;
        if (!string.IsNullOrWhiteSpace(customProperties))
        {
            try
            {
                customProps = JsonSerializer.Deserialize<JsonElement[]>(customProperties);
            }
            catch
            {
                // Invalid JSON is ignored so evaluation can proceed with the base user.
            }
        }

        var keys = SplitCommaSeparated(flagKeys);
        var tagList = SplitCommaSeparated(tags);

        var user = new Dictionary<string, object?> { ["keyId"] = userKeyId };
        if (!string.IsNullOrWhiteSpace(userName))
            user["name"] = userName;
        if (customProps is { Length: > 0 })
            user["customizedProperties"] = customProps;

        var payload = new Dictionary<string, object?> { ["user"] = user };
        if (keys.Length > 0 || tagList.Length > 0)
        {
            var filter = new Dictionary<string, object?>();
            if (keys.Length > 0)
                filter["keys"] = keys;

            if (tagList.Length > 0)
            {
                filter["tags"] = tagList;
                filter["tagFilterMode"] = string.IsNullOrWhiteSpace(tagFilterMode) ? "and" : tagFilterMode;
            }

            payload["filter"] = filter;
        }

        return await _apiClient.EvaluateAsync(JsonSerializer.Serialize(payload));
    }
}
