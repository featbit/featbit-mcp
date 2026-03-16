using System.ComponentModel;
using System.Text.Json;
using FeatBit.FeatureFlags;
using FeatBit.McpServer.Infrastructure;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

[McpServerToolType]
public class FeatBitApiTools(FeatBitApiClient apiClient, IFeatureFlagEvaluator flagEvaluator)
{
    // ========================================
    // Project Management
    // ========================================

    [McpServerTool]
    [Description("Get the list of all projects within the current organization.")]
    public Task<string> GetProjects()
        => apiClient.GetAsync("/api/v1/projects");

    [McpServerTool]
    [Description("Get a single project by ID with its environments and credentials (Server Key, Client Key).")]
    public Task<string> GetProject(
        [Description("The unique identifier (UUID) of the project")]
        string projectId)
        => apiClient.GetAsync($"/api/v1/projects/{Uri.EscapeDataString(projectId)}");

    // ========================================
    // Feature Flag Management
    // ========================================

    [McpServerTool]
    [Description(
        "Get the list of feature flags in an environment. " +
        "Supports filtering by name/key, tags, enabled/disabled status, and archived status, with pagination.")]
    public async Task<string> GetFeatureFlags(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("Filter by flag name or key (partial match)")]
        string? name = null,
        [Description("Comma-separated tag names to filter by, e.g. 'checkout,payments'")]
        string? tags = null,
        [Description("true = enabled flags only, false = disabled flags only, omit = both")]
        bool? isEnabled = null,
        [Description("true = archived flags only; omit or false = active flags only")]
        bool? isArchived = null,
        [Description("Field to sort by (default: createdAt)")]
        string? sortBy = null,
        [Description("Page index, 0-based (default: 0)")]
        int? pageIndex = null,
        [Description("Page size (default: 10)")]
        int? pageSize = null)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(name))
            query.Add($"Name={Uri.EscapeDataString(name)}");

        if (!string.IsNullOrEmpty(tags))
            foreach (var tag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                query.Add($"Tags={Uri.EscapeDataString(tag)}");

        if (isEnabled.HasValue)  query.Add($"IsEnabled={isEnabled.Value.ToString().ToLower()}");
        if (isArchived.HasValue) query.Add($"IsArchived={isArchived.Value.ToString().ToLower()}");
        if (!string.IsNullOrEmpty(sortBy)) query.Add($"SortBy={Uri.EscapeDataString(sortBy)}");
        if (pageIndex.HasValue)  query.Add($"PageIndex={pageIndex.Value}");
        if (pageSize.HasValue)   query.Add($"PageSize={pageSize.Value}");

        var path = $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags";
        if (query.Count > 0) path += "?" + string.Join("&", query);

        var json = await apiClient.GetAsync(path);

        // [FeatureFlag: flag-list] shape the response based on variation: full | short | key-ct-ut
        var variation = flagEvaluator.StringVariation(FeatureFlag.FlagList);
        if (variation == "full")
        {
            return json;
        }
        else if (variation == "short")
        {
            using var doc = JsonDocument.Parse(json);
            var keys = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("key").GetString())
                .ToList();
            return JsonSerializer.Serialize(keys);
        }
        else if (variation == "key-ct-ut")
        {
            using var doc = JsonDocument.Parse(json);
            var slim = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(item => new
                {
                    key       = item.GetProperty("key").GetString(),
                    createdAt = item.GetProperty("createdAt").GetString(),
                    updatedAt = item.GetProperty("updatedAt").GetString()
                })
                .ToList();
            return JsonSerializer.Serialize(slim);
        }

        return json;
    }

    [McpServerTool]
    [Description("Enable or disable a feature flag.")]
    public Task<string> ToggleFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key,
        [Description("true to enable the flag, false to disable it")]
        bool status)
        => apiClient.PutAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}/toggle/{status.ToString().ToLower()}");

    [McpServerTool]
    [Description("Get a single feature flag by key.")]
    public Task<string> GetFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key)
        => apiClient.GetAsync($"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}");

    [McpServerTool]
    [Description(
        "Archive a feature flag with the specified key. " +
        "Archived flags are hidden from the main list by default but can be restored later.")]
    public Task<string> ArchiveFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key)
        => apiClient.PutAsync($"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}/archive");

    [McpServerTool]
    [Description(
        "Create a feature flag with the given name, key, and description. " +
        "The flag is created in a disabled state; use ToggleFeatureFlag to enable it.")]
    public Task<string> CreateFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The display name of the feature flag")]
        string name,
        [Description("The unique key of the feature flag (must be unique within the environment)")]
        string key,
        [Description("Optional description for the feature flag")]
        string? description = null)
        => apiClient.PostAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags",
            JsonSerializer.Serialize(new { envId, name, key, isEnabled = false, description }));

    [McpServerTool]
    [Description(
        "Update the default rollout (fallthrough) of a feature flag using the JSON patch method. " +
        "Only the fallthrough configuration is modified — other flag settings are left unchanged. " +
        "Provide the rollout as a JSON array where each element specifies a variation ID and the percentage of " +
        "traffic to route to it. Percentages must sum to 100. " +
        "Example: [{\"variationId\":\"abc-uuid\",\"percentage\":70},{\"variationId\":\"def-uuid\",\"percentage\":30}]")]
    public async Task<string> UpdateFeatureFlagRollout(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key,
        [Description(
            "JSON array of rollout assignments. Each element: {\"variationId\": \"<uuid>\", \"percentage\": <number>}. " +
            "Percentages must sum to 100.")]
        string rolloutAssignments,
        [Description(
            "The user attribute used for consistent bucketing (e.g. 'email', 'country'). " +
            "Omit or set to null for random/percentage-based rollout.")]
        string? dispatchKey = null)
    {
        using var doc = JsonDocument.Parse(rolloutAssignments);
        var assignments = doc.RootElement.EnumerateArray()
            .Select(el => new
            {
                variationId = el.GetProperty("variationId").GetString()!,
                percentage  = el.GetProperty("percentage").GetDouble()
            })
            .ToArray();

        var total = assignments.Sum(a => a.percentage);
        if (Math.Abs(total - 100.0) > 0.01)
            return JsonSerializer.Serialize(new { error = $"Percentages must sum to 100, but got {total}." });

        double cursor = 0.0;
        var variations = assignments.Select(a =>
        {
            var start = Math.Round(cursor, 4);
            cursor += a.percentage / 100.0;
            var end = Math.Round(cursor, 4);
            return new { id = a.variationId, rollout = new[] { start, end }, exptRollout = 1.0 };
        }).ToArray();

        var fallthrough = new
        {
            dispatchKey      = dispatchKey,
            includedInExpt   = false,
            variations
        };

        var patch = JsonSerializer.Serialize(
            new[] { new { op = "replace", path = "/fallthrough", value = (object)fallthrough } });

        return await apiClient.PatchAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}",
            patch);
    }

    // ========================================
    // Feature Flag Evaluation
    // ========================================

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
            try { customProps = JsonSerializer.Deserialize<JsonElement[]>(customProperties); }
            catch { /* invalid JSON — customizedProperties omitted */ }
        }

        string[]? keys = null;
        if (!string.IsNullOrWhiteSpace(flagKeys))
            keys = flagKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string[]? tagList = null;
        if (!string.IsNullOrWhiteSpace(tags))
            tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var user = new Dictionary<string, object?> { ["keyId"] = userKeyId };
        if (!string.IsNullOrWhiteSpace(userName))
            user["name"] = userName;
        if (customProps is { Length: > 0 })
            user["customizedProperties"] = customProps;

        var payload = new Dictionary<string, object?> { ["user"] = user };

        var hasKeys = keys is { Length: > 0 };
        var hasTags = tagList is { Length: > 0 };
        if (hasKeys || hasTags)
        {
            var filter = new Dictionary<string, object?>();
            if (hasKeys)  filter["keys"] = keys;
            if (hasTags)
            {
                filter["tags"] = tagList;
                filter["tagFilterMode"] = string.IsNullOrWhiteSpace(tagFilterMode) ? "and" : tagFilterMode;
            }
            payload["filter"] = filter;
        }

        return await apiClient.EvaluateAsync(JsonSerializer.Serialize(payload));
    }

    [McpServerTool]
    [McpToolFlagGate(nameof(FeatureFlag.AddFeatureFlagTargetUser))]
    [Description("Add an individual user to the targeting list of a feature flag.")]
    public Task<string> AddFlagTargetUser(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string flagKey,
        [Description("The unique key that identifies the user")]
        string userKey,
        [Description("The user's email address, used as the display name")]
        string userEmail)
        => apiClient.PostAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(flagKey)}/target-users",
            JsonSerializer.Serialize(new { keyId = userKey, name = userEmail }));
}
