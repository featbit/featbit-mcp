using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    // === Audit Logs ===

    [McpServerTool]
    [Description(
        "List audit logs in an environment. " +
        "Supports keyword, creator, reference, time range, cross-environment, and pagination filters.")]
    public async Task<string> GetAuditLogs(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("Filter by keyword or comment fragment")]
        string? query = null,
        [Description("Filter by audit log creator ID (UUID)")]
        string? creatorId = null,
        [Description("Filter by referenced resource ID, e.g. a feature flag ID")]
        string? refId = null,
        [Description("Filter by referenced resource type, e.g. FeatureFlag")]
        string? refType = null,
        [Description("Start created-at time as Unix milliseconds")]
        long? from = null,
        [Description("End created-at time as Unix milliseconds")]
        long? to = null,
        [Description("true to query across environments")]
        bool crossEnvironment = false,
        [Description("Page index, 0-based (default: 0)")]
        int? pageIndex = null,
        [Description("Page size (default: 10, or 100 when fetchAll is true and pageSize is omitted)")]
        int? pageSize = null,
        [Description("true to fetch all pages; false to return a single page")]
        bool fetchAll = false)
    {
        if (fetchAll)
            return await GetAllAuditLogsJsonAsync(envId, query, creatorId, refId, refType, from, to, crossEnvironment, pageIndex, pageSize);

        return await _apiClient.GetAsync(BuildAuditLogsPath(envId, query, creatorId, refId, refType, from, to, crossEnvironment, pageIndex, pageSize));
    }

    [McpServerTool]
    [Description(
        "List audit logs for a feature flag. " +
        "Provide either flagId or flagKey; when flagKey is provided, the tool resolves the feature flag ID first.")]
    public async Task<string> GetFeatureFlagAuditLogs(
        [Description("The environment ID (UUID)")]
        string envId,
        [Description("The feature flag ID (UUID). Optional when flagKey is provided.")]
        string? flagId = null,
        [Description("The feature flag key. Used to resolve the flag ID when flagId is omitted.")]
        string? flagKey = null,
        [Description("Filter by keyword or comment fragment")]
        string? query = null,
        [Description("Filter by audit log creator ID (UUID)")]
        string? creatorId = null,
        [Description("Start created-at time as Unix milliseconds")]
        long? from = null,
        [Description("End created-at time as Unix milliseconds")]
        long? to = null,
        [Description("true to query across environments")]
        bool crossEnvironment = false,
        [Description("Page index, 0-based (default: 0)")]
        int? pageIndex = null,
        [Description("Page size (default: 10, or 100 when fetchAll is true and pageSize is omitted)")]
        int? pageSize = null,
        [Description("true to fetch all pages; false to return a single page")]
        bool fetchAll = false)
    {
        var resolvedFlagId = flagId;
        if (string.IsNullOrWhiteSpace(resolvedFlagId) && !string.IsNullOrWhiteSpace(flagKey))
        {
            var flagJson = await _apiClient.GetAsync(
                $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags/{Uri.EscapeDataString(flagKey)}");

            using var flagDoc = JsonDocument.Parse(flagJson);
            if (!TryGetDataObject(flagDoc.RootElement, out var flag))
                return flagJson;

            resolvedFlagId = TryGetString(flag, "id");
        }

        if (string.IsNullOrWhiteSpace(resolvedFlagId))
            return JsonSerializer.Serialize(new { error = "Either flagId or flagKey is required." });

        return await GetAuditLogs(
            envId,
            query,
            creatorId,
            resolvedFlagId,
            "FeatureFlag",
            from,
            to,
            crossEnvironment,
            pageIndex,
            pageSize,
            fetchAll);
    }
}
