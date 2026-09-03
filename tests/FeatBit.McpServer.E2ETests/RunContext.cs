using System.Globalization;
using System.Security.Cryptography;

namespace FeatBit.McpServer.E2ETests;

internal sealed class RunContext
{
    private RunContext(string runId)
    {
        RunId = runId;
        ProjectName = $"MCP E2E {runId}";
        ProjectKey = $"mcp-e2e-{runId}";
        EnvironmentName = "MCP E2E";
        EnvironmentKey = "e2e";
        MainFlag = new TrackedFeatureFlag(
            $"MCP E2E Main {runId}",
            $"mcp-e2e-main-{runId}",
            "Main MCP tool lifecycle");
        FeatureFlags.Add(MainFlag);
    }

    public string RunId { get; }

    public string ProjectName { get; }

    public string ProjectKey { get; }

    public string EnvironmentName { get; }

    public string EnvironmentKey { get; }

    public string ProjectState { get; set; } = "not_attempted";

    public string EnvironmentState { get; set; } = "not_attempted";

    public TrackedFeatureFlag MainFlag { get; }

    public List<TrackedFeatureFlag> FeatureFlags { get; } = [];

    public bool ArchiveApproved { get; set; }

    public string ArchiveDecision { get; set; } = "not_requested";

    public static RunContext Create()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var suffix = RandomNumberGenerator.GetHexString(4).ToLowerInvariant();
        return new RunContext($"{timestamp}-{suffix}");
    }

    public TrackedFeatureFlag AddFeatureFlag(string name, string key, string purpose)
    {
        var featureFlag = new TrackedFeatureFlag(name, key, purpose);
        FeatureFlags.Add(featureFlag);
        return featureFlag;
    }
}

internal sealed class TrackedFeatureFlag(string name, string key, string purpose)
{
    public string Name { get; } = name;

    public string Key { get; } = key;

    public string Purpose { get; } = purpose;

    public string CreationState { get; set; } = "not_attempted";

    public bool? IsEnabled { get; set; }

    public bool? IsArchived { get; set; }

    public DateOnly? DeleteAfter { get; set; }

    public string? DeletionStatus { get; set; }
}
