using System.Text.Json.Serialization;

namespace FeatBit.McpServer.Infrastructure.Models;

// ============================================
// Project Models
// ============================================

public class CreateProjectRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }
}

public class ProjectResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("environments")]
    public List<EnvironmentResponse>? Environments { get; set; }
}

// ============================================
// Environment Models
// ============================================

public class CreateEnvironmentRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class EnvironmentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("secrets")]
    public List<SecretResponse>? Secrets { get; set; }

    [JsonPropertyName("settings")]
    public object? Settings { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class SecretResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

// ============================================
// Feature Flag Models
// ============================================

public class CreateFeatureFlagRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("variationType")]
    public required string VariationType { get; set; }

    [JsonPropertyName("variations")]
    public required List<Variation> Variations { get; set; }

    [JsonPropertyName("enabledVariationId")]
    public required string EnabledVariationId { get; set; }

    [JsonPropertyName("disabledVariationId")]
    public required string DisabledVariationId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class UpdateFeatureFlagRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("variations")]
    public List<Variation>? Variations { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class ToggleFeatureFlagRequest
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class Variation
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}

public class FeatureFlagResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("envId")]
    public string? EnvId { get; set; }

    [JsonPropertyName("revision")]
    public string? Revision { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("variationType")]
    public string? VariationType { get; set; }

    [JsonPropertyName("variations")]
    public List<Variation>? Variations { get; set; }

    [JsonPropertyName("targetUsers")]
    public List<object>? TargetUsers { get; set; }

    [JsonPropertyName("rules")]
    public List<object>? Rules { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("disabledVariationId")]
    public string? DisabledVariationId { get; set; }

    [JsonPropertyName("fallthrough")]
    public object? Fallthrough { get; set; }

    [JsonPropertyName("exptIncludeAllTargets")]
    public bool ExptIncludeAllTargets { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}
