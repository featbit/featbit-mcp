using System.ComponentModel;
using System.Text.Json;
using FeatBit.FeatureFlags;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    // === Feature Flag Management ===

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
        int? pageSize = null,
        [Description("true to fetch all pages; false to return a single page")]
        bool fetchAll = false)
    {
        var json = fetchAll
            ? await GetAllFeatureFlagsJsonAsync(envId, name, tags, isEnabled, isArchived, sortBy, pageIndex, pageSize)
            : await _apiClient.GetAsync(BuildFeatureFlagsPath(envId, name, tags, isEnabled, isArchived, sortBy, pageIndex, pageSize));

        return ShapeFeatureFlagList(json);
    }

    [McpServerTool]
    [Description("Get a single feature flag by key.")]
    public Task<string> GetFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key)
        => _apiClient.GetAsync($"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}");

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
        string? description = null,
        [Description("Optional comma-separated tag names to attach to the feature flag")]
        string? tags = null)
    {
        var trueVariationId = Guid.NewGuid().ToString();
        var falseVariationId = Guid.NewGuid().ToString();
        var payload = new Dictionary<string, object?>
        {
            ["envId"] = envId,
            ["name"] = name,
            ["key"] = key,
            ["isEnabled"] = false,
            ["variationType"] = "boolean",
            ["variations"] = new[]
            {
                new { id = trueVariationId, value = "true", name = "True" },
                new { id = falseVariationId, value = "false", name = "False" }
            },
            ["enabledVariationId"] = trueVariationId,
            ["disabledVariationId"] = falseVariationId
        };

        if (!string.IsNullOrWhiteSpace(description))
            payload["description"] = description;

        var tagList = SplitCommaSeparated(tags);
        if (tagList.Length > 0)
            payload["tags"] = tagList;

        return _apiClient.PostAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags",
            JsonSerializer.Serialize(payload));
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
        => _apiClient.PutAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}/toggle/{status.ToString().ToLowerInvariant()}");

    [McpServerTool]
    [Description(
        "Archive a feature flag with the specified key. " +
        "Archived flags are hidden from the main list by default but can be restored later.")]
    public Task<string> ArchiveFeatureFlag(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag key")]
        string key)
        => _apiClient.PutAsync($"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}/archive");

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
                percentage = el.GetProperty("percentage").GetDouble()
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
            dispatchKey,
            includedInExpt = false,
            variations
        };

        var patch = JsonSerializer.Serialize(
            new[] { new { op = "replace", path = "/fallthrough", value = (object)fallthrough } });

        return await _apiClient.PatchAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(key)}",
            patch);
    }

    // === Targeting ===

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
        => _apiClient.PostAsync(
            $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(flagKey)}/target-users",
            JsonSerializer.Serialize(new { keyId = userKey, name = userEmail }));

    private string ShapeFeatureFlagList(string json)
    {
        var variation = _flagEvaluator.StringVariation(FeatureFlag.FlagList);
        if (variation == "full")
            return json;

        using var doc = JsonDocument.Parse(json);
        if (!TryGetDataObject(doc.RootElement, out var data) ||
            !data.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return json;
        }

        if (variation == "short")
        {
            var keys = items.EnumerateArray()
                .Select(item => item.GetProperty("key").GetString())
                .ToList();

            return JsonSerializer.Serialize(keys);
        }

        if (variation == "key-ct-ut")
        {
            var slim = items.EnumerateArray()
                .Select(item => new
                {
                    key = item.GetProperty("key").GetString(),
                    createdAt = item.GetProperty("createdAt").GetString(),
                    updatedAt = item.GetProperty("updatedAt").GetString()
                })
                .ToList();

            return JsonSerializer.Serialize(slim);
        }

        return json;
    }
}
