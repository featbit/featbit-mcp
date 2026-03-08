using System.ComponentModel;
using FeatBit.McpServer.Infrastructure;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

[McpServerToolType]
public class FeatBitApiTools(FeatBitApiClient apiClient)
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
    public Task<string> GetFeatureFlags(
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

        return apiClient.GetAsync(path);
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
}
