using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    // === Project Management ===

    [McpServerTool]
    [Description("Get the list of all projects within the current organization.")]
    public Task<string> GetProjects()
        => _apiClient.GetAsync("/api/v1/projects");

    [McpServerTool]
    [Description("Create a project with the given display name and immutable key.")]
    public Task<string> CreateProject(
        [Description("The display name of the project")]
        string name,
        [Description("The unique, immutable key of the project")]
        string key)
        => _apiClient.PostAsync(
            "/api/v1/projects",
            JsonSerializer.Serialize(new { name, key }));

    [McpServerTool]
    [Description("Get a single project by ID with its environments and credentials (Server Key, Client Key).")]
    public Task<string> GetProject(
        [Description("The unique identifier (UUID) of the project")]
        string projectId)
        => _apiClient.GetAsync($"/api/v1/projects/{Uri.EscapeDataString(projectId)}");

    [McpServerTool]
    [Description(
        "List feature flags across every environment in a project. " +
        "Use this when the user asks about flags in the current project rather than one known environment. " +
        "Supports filtering by name/key and tags, and can fetch all pages for each environment.")]
    public async Task<string> GetProjectFeatureFlags(
        [Description("The project ID (UUID)")]
        string projectId,
        [Description("Filter by flag name or key (partial match)")]
        string? name = null,
        [Description("Comma-separated tag names to filter by, e.g. 'checkout,payments'")]
        string? tags = null,
        [Description("Page index per environment, 0-based (default: 0)")]
        int? pageIndex = null,
        [Description("Page size per environment (default: 10, or 100 when fetchAll is true and pageSize is omitted)")]
        int? pageSize = null,
        [Description("true to fetch every page for each environment; false to return one page per environment")]
        bool fetchAll = false)
    {
        var projectJson = await _apiClient.GetAsync($"/api/v1/projects/{Uri.EscapeDataString(projectId)}");

        using var projectDoc = JsonDocument.Parse(projectJson);
        if (!TryGetDataObject(projectDoc.RootElement, out var project))
            return projectJson;

        if (!project.TryGetProperty("environments", out var envs) || envs.ValueKind != JsonValueKind.Array)
            return JsonSerializer.Serialize(new { error = "Project response did not include an environments array." });

        var environmentResults = new List<object>();
        foreach (var env in envs.EnumerateArray())
        {
            if (!env.TryGetProperty("id", out var envIdElement))
                continue;

            var envId = envIdElement.GetString();
            if (string.IsNullOrWhiteSpace(envId))
                continue;

            var flags = fetchAll
                ? await FetchAllFeatureFlagItemsAsync(envId, name, tags, pageIndex, pageSize)
                : await FetchFeatureFlagPageAsync(envId, name, tags, null, null, null, pageIndex, pageSize);

            if (!flags.Success)
                return flags.RawJson;

            environmentResults.Add(new
            {
                envId,
                envName = TryGetString(env, "name"),
                envKey = TryGetString(env, "key"),
                totalCount = flags.TotalCount,
                items = flags.Items
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            data = new
            {
                projectId = TryGetString(project, "id"),
                projectName = TryGetString(project, "name"),
                projectKey = TryGetString(project, "key"),
                environments = environmentResults
            }
        });
    }
}
