using System.ComponentModel;
using System.Text.Json;
using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Infrastructure.Models;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// FeatBit REST API Tools
/// Provides direct integration with FeatBit's REST API for managing projects, environments, and feature flags.
/// This replaces the previous documentation-based approach with actual API interactions.
/// </summary>
[McpServerToolType]
public class FeatBitApiTools
{
    private readonly FeatBitApiClient _apiClient;
    private readonly ILogger<FeatBitApiTools> _logger;

    public FeatBitApiTools(
        FeatBitApiClient apiClient,
        ILogger<FeatBitApiTools> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ========================================
    // Project Management Tools
    // ========================================

    [McpServerTool]
    [Description("Create a new project in FeatBit. Projects are top-level containers for organizing feature flags. Auto-generates two default environments: Prod and Dev, each with Server Key and Client Key.")]
    public async Task<string> CreateProject(
        [Description("Display name of the project (e.g., 'E-Commerce Platform')")]
        string name,
        [Description("Unique identifier using alphanumeric chars, dots, underscores, or hyphens (e.g., 'ecommerce')")]
        string key,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Creating project: {Name} ({Key})", name, key);

        var request = new CreateProjectRequest
        {
            Name = name,
            Key = key
        };

        var response = await _apiClient.PostAsync<CreateProjectRequest, ProjectResponse>(
            "/api/v1/projects", 
            request,
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [McpServerTool]
    [Description("List all projects in the organization. Returns project details including environments and their credentials.")]
    public async Task<string> GetProjects(
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Fetching all projects");

        var response = await _apiClient.GetAsync<List<ProjectResponse>>(
            "/api/v1/projects",
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [McpServerTool]
    [Description("Get detailed information about a specific project, including all its environments and their credentials (Server Key and Client Key).")]
    public async Task<string> GetProject(
        [Description("The unique identifier (GUID) of the project")]
        string projectId,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Fetching project: {ProjectId}", projectId);

        var response = await _apiClient.GetAsync<ProjectResponse>(
            $"/api/v1/projects/{projectId}",
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ========================================
    // Environment Management Tools
    // ========================================

    [McpServerTool]
    [Description("Create a new environment within a project (e.g., Staging, QA, UAT). Auto-generates Server Key and Client Key for the environment.")]
    public async Task<string> CreateEnvironment(
        [Description("The project ID where the environment will be created")]
        string projectId,
        [Description("Display name of the environment (e.g., 'Staging')")]
        string name,
        [Description("Unique identifier within the project (e.g., 'staging')")]
        string key,
        [Description("Optional description of the environment's purpose")]
        string? description = null,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Creating environment: {Name} in project {ProjectId}", name, projectId);

        var request = new CreateEnvironmentRequest
        {
            Name = name,
            Key = key,
            Description = description
        };

        var response = await _apiClient.PostAsync<CreateEnvironmentRequest, EnvironmentResponse>(
            $"/api/v1/projects/{projectId}/envs",
            request,
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ========================================
    // Feature Flag Management Tools
    // ========================================

    [McpServerTool]
    [Description("Create a new feature flag in an environment. Supports boolean, string, number, and JSON variation types. Use this for creating flags with specific variation configurations.")]
    public async Task<string> CreateFeatureFlag(
        [Description("The environment ID where the feature flag will be created")]
        string envId,
        [Description("Display name of the feature flag (e.g., 'New Checkout Flow')")]
        string name,
        [Description("Unique identifier matching pattern ^[a-zA-Z0-9._-]+$ (e.g., 'new-checkout-flow')")]
        string key,
        [Description("Initial enabled/disabled state (true or false)")]
        bool isEnabled,
        [Description("Variation type: 'boolean', 'string', 'number', or 'json'")]
        string variationType,
        [Description("JSON array of variations, e.g., [{\"id\":\"v1\",\"name\":\"On\",\"value\":\"true\"},{\"id\":\"v2\",\"name\":\"Off\",\"value\":\"false\"}]")]
        string variationsJson,
        [Description("The variation ID to serve when flag is enabled")]
        string enabledVariationId,
        [Description("The variation ID to serve when flag is disabled")]
        string disabledVariationId,
        [Description("Optional description of the feature flag")]
        string? description = null,
        [Description("Optional comma-separated tags for categorization (e.g., 'checkout,ui-redesign')")]
        string? tags = null,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Creating feature flag: {Name} in environment {EnvId}", name, envId);

        // Parse variations JSON
        List<Variation> variations;
        try
        {
            variations = JsonSerializer.Deserialize<List<Variation>>(variationsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Variation>();
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = $"Invalid variations JSON: {ex.Message}" } }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        var request = new CreateFeatureFlagRequest
        {
            Name = name,
            Key = key,
            IsEnabled = isEnabled,
            Description = description,
            VariationType = variationType,
            Variations = variations,
            EnabledVariationId = enabledVariationId,
            DisabledVariationId = disabledVariationId,
            Tags = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
        };

        var response = await _apiClient.PostAsync<CreateFeatureFlagRequest, FeatureFlagResponse>(
            $"/api/v1/envs/{envId}/feature-flags",
            request,
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [McpServerTool]
    [Description("Get detailed information about a specific feature flag, including its variations, targeting rules, and current state.")]
    public async Task<string> GetFeatureFlag(
        [Description("The environment ID containing the feature flag")]
        string envId,
        [Description("The unique key of the feature flag")]
        string flagKey,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Fetching feature flag: {FlagKey} in environment {EnvId}", flagKey, envId);

        var response = await _apiClient.GetAsync<FeatureFlagResponse>(
            $"/api/v1/envs/{envId}/feature-flags/{flagKey}",
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [McpServerTool]
    [Description("Update an existing feature flag's properties such as name, description, variations, or tags. Does not change the enabled/disabled state.")]
    public async Task<string> UpdateFeatureFlag(
        [Description("The environment ID containing the feature flag")]
        string envId,
        [Description("The unique key of the feature flag to update")]
        string flagKey,
        [Description("New display name (optional)")]
        string? name = null,
        [Description("New description (optional)")]
        string? description = null,
        [Description("Updated variations JSON (optional), same format as CreateFeatureFlag")]
        string? variationsJson = null,
        [Description("Updated comma-separated tags (optional)")]
        string? tags = null,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Updating feature flag: {FlagKey} in environment {EnvId}", flagKey, envId);

        List<Variation>? variations = null;
        if (!string.IsNullOrEmpty(variationsJson))
        {
            try
            {
                variations = JsonSerializer.Deserialize<List<Variation>>(variationsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                {
                    Success = false,
                    Errors = new[] { new FeatBitApiError { Message = $"Invalid variations JSON: {ex.Message}" } }
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        var request = new UpdateFeatureFlagRequest
        {
            Name = name,
            Description = description,
            Variations = variations,
            Tags = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
        };

        var response = await _apiClient.PutAsync<UpdateFeatureFlagRequest, FeatureFlagResponse>(
            $"/api/v1/envs/{envId}/feature-flags/{flagKey}",
            request,
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [McpServerTool]
    [Description("Toggle a feature flag's enabled/disabled state. This is commonly used to turn features on or off quickly.")]
    public async Task<string> ToggleFeatureFlag(
        [Description("The environment ID containing the feature flag")]
        string envId,
        [Description("The unique key of the feature flag to toggle")]
        string flagKey,
        [Description("Set to true to enable the flag, false to disable it")]
        bool isEnabled,
        [Description("Optional comment explaining the reason for toggling")]
        string? comment = null,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Toggling feature flag: {FlagKey} to {State} in environment {EnvId}", 
            flagKey, isEnabled ? "enabled" : "disabled", envId);

        var request = new ToggleFeatureFlagRequest
        {
            IsEnabled = isEnabled,
            Comment = comment
        };

        var response = await _apiClient.PatchAsync<ToggleFeatureFlagRequest, FeatureFlagResponse>(
            $"/api/v1/envs/{envId}/feature-flags/{flagKey}/toggle",
            request,
            apiKey);

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
