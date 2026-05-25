using System.Text.Json;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    private async Task<string> GetAllFeatureFlagsJsonAsync(
        string envId,
        string? name,
        string? tags,
        bool? isEnabled,
        bool? isArchived,
        string? sortBy,
        int? pageIndex,
        int? pageSize)
    {
        var result = await FetchAllFeatureFlagItemsAsync(envId, name, tags, pageIndex, pageSize, isEnabled, isArchived, sortBy);
        if (!result.Success)
            return result.RawJson;

        return JsonSerializer.Serialize(new
        {
            success = true,
            data = new
            {
                totalCount = result.TotalCount,
                items = result.Items
            }
        });
    }

    private async Task<PagedItemsResult> FetchFeatureFlagPageAsync(
        string envId,
        string? name,
        string? tags,
        bool? isEnabled,
        bool? isArchived,
        string? sortBy,
        int? pageIndex,
        int? pageSize)
    {
        var json = await _apiClient.GetAsync(BuildFeatureFlagsPath(envId, name, tags, isEnabled, isArchived, sortBy, pageIndex, pageSize));
        return ParsePagedItems(json);
    }

    private async Task<PagedItemsResult> FetchAllFeatureFlagItemsAsync(
        string envId,
        string? name,
        string? tags,
        int? pageIndex,
        int? pageSize,
        bool? isEnabled = null,
        bool? isArchived = null,
        string? sortBy = null)
    {
        var cursor = pageIndex.GetValueOrDefault(0);
        var effectivePageSize = pageSize.GetValueOrDefault(100);
        var items = new List<JsonElement>();
        long totalCount = 0;

        while (true)
        {
            var page = await FetchFeatureFlagPageAsync(envId, name, tags, isEnabled, isArchived, sortBy, cursor, effectivePageSize);
            if (!page.Success)
                return page;

            totalCount = page.TotalCount;
            items.AddRange(page.Items);

            if (page.Items.Count == 0)
                break;

            if (totalCount > 0 && items.Count >= totalCount)
                break;

            cursor++;
        }

        return new PagedItemsResult(true, string.Empty, totalCount, items);
    }

    private async Task<string> GetAllAuditLogsJsonAsync(
        string envId,
        string? query,
        string? creatorId,
        string? refId,
        string? refType,
        long? from,
        long? to,
        bool crossEnvironment,
        int? pageIndex,
        int? pageSize)
    {
        var cursor = pageIndex.GetValueOrDefault(0);
        var effectivePageSize = pageSize.GetValueOrDefault(100);
        var items = new List<JsonElement>();
        long totalCount = 0;

        while (true)
        {
            var json = await _apiClient.GetAsync(BuildAuditLogsPath(
                envId,
                query,
                creatorId,
                refId,
                refType,
                from,
                to,
                crossEnvironment,
                cursor,
                effectivePageSize));

            var page = ParsePagedItems(json);
            if (!page.Success)
                return page.RawJson;

            totalCount = page.TotalCount;
            items.AddRange(page.Items);

            if (page.Items.Count == 0)
                break;

            if (totalCount > 0 && items.Count >= totalCount)
                break;

            cursor++;
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            data = new
            {
                totalCount,
                items
            }
        });
    }

    private static string BuildFeatureFlagsPath(
        string envId,
        string? name,
        string? tags,
        bool? isEnabled,
        bool? isArchived,
        string? sortBy,
        int? pageIndex,
        int? pageSize)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
            query.Add($"Name={Uri.EscapeDataString(name)}");

        foreach (var tag in SplitCommaSeparated(tags))
            query.Add($"Tags={Uri.EscapeDataString(tag)}");

        if (isEnabled.HasValue)
            query.Add($"IsEnabled={isEnabled.Value.ToString().ToLowerInvariant()}");
        if (isArchived.HasValue)
            query.Add($"IsArchived={isArchived.Value.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(sortBy))
            query.Add($"SortBy={Uri.EscapeDataString(sortBy)}");
        if (pageIndex.HasValue)
            query.Add($"PageIndex={pageIndex.Value}");
        if (pageSize.HasValue)
            query.Add($"PageSize={pageSize.Value}");

        var path = $"/api/v1/envs/{Uri.EscapeDataString(envId)}/feature-flags";
        return query.Count == 0 ? path : $"{path}?{string.Join("&", query)}";
    }

    private static string BuildAuditLogsPath(
        string envId,
        string? query,
        string? creatorId,
        string? refId,
        string? refType,
        long? from,
        long? to,
        bool crossEnvironment,
        int? pageIndex,
        int? pageSize)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
            queryParts.Add($"Query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(creatorId))
            queryParts.Add($"CreatorId={Uri.EscapeDataString(creatorId)}");
        if (!string.IsNullOrWhiteSpace(refId))
            queryParts.Add($"RefId={Uri.EscapeDataString(refId)}");
        if (!string.IsNullOrWhiteSpace(refType))
            queryParts.Add($"RefType={Uri.EscapeDataString(refType)}");
        if (from.HasValue)
            queryParts.Add($"From={from.Value}");
        if (to.HasValue)
            queryParts.Add($"To={to.Value}");
        if (crossEnvironment)
            queryParts.Add("CrossEnvironment=true");
        if (pageIndex.HasValue)
            queryParts.Add($"PageIndex={pageIndex.Value}");
        if (pageSize.HasValue)
            queryParts.Add($"PageSize={pageSize.Value}");

        var path = $"/api/v1/envs/{Uri.EscapeDataString(envId)}/audit-logs";
        return queryParts.Count == 0 ? path : $"{path}?{string.Join("&", queryParts)}";
    }

    private static PagedItemsResult ParsePagedItems(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!TryGetDataObject(doc.RootElement, out var data))
            return new PagedItemsResult(false, json, 0, []);

        if (!data.TryGetProperty("items", out var itemElement) || itemElement.ValueKind != JsonValueKind.Array)
            return new PagedItemsResult(false, json, 0, []);

        var totalCount = data.TryGetProperty("totalCount", out var totalElement) && totalElement.TryGetInt64(out var total)
            ? total
            : 0;

        var items = itemElement.EnumerateArray()
            .Select(item => item.Clone())
            .ToList();

        return new PagedItemsResult(true, json, totalCount, items);
    }

    private static bool TryGetDataObject(JsonElement root, out JsonElement data)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out data) &&
            data.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        data = default;
        return false;
    }

    private static string TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static string[] SplitCommaSeparated(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed record PagedItemsResult(bool Success, string RawJson, long TotalCount, List<JsonElement> Items);
}
