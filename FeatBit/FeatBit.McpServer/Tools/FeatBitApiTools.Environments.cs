using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

public partial class FeatBitApiTools
{
    // === Environment Management ===

    [McpServerTool]
    [Description("Create an environment under a project with the given name, immutable key, and optional description.")]
    public Task<string> CreateEnvironment(
        [Description("The unique identifier (UUID) of the parent project")]
        string projectId,
        [Description("The display name of the environment")]
        string name,
        [Description("The unique, immutable key of the environment within the project")]
        string key,
        [Description("Optional description of the environment; defaults to an empty string")]
        string? description = null)
        => _apiClient.PostAsync(
            $"/api/v1/projects/{Uri.EscapeDataString(projectId)}/envs",
            JsonSerializer.Serialize(new
            {
                name,
                key,
                description = description ?? string.Empty
            }));
}
