using System.Text;

namespace FeatBit.McpServer.E2ETests;

internal sealed class E2EReport(
    RunContext context,
    Uri mcpUrl,
    Uri apiBaseUrl,
    SensitiveValueRedactor redactor)
{
    private readonly List<StepResult> _steps = [];
    private IReadOnlyList<string> _discoveredTools = [];

    public bool HasFailure => _steps.Any(step => step.Status == StepStatus.Failed);

    public bool HasSkippedStep => _steps.Any(step => step.Status == StepStatus.Skipped);

    public void SetDiscoveredTools(IReadOnlyList<string> tools) => _discoveredTools = tools;

    public void Passed(string step, string evidence)
        => _steps.Add(new StepResult(step, StepStatus.Passed, evidence));

    public void Failed(string step, string evidence)
        => _steps.Add(new StepResult(step, StepStatus.Failed, evidence));

    public void Skipped(string step, string evidence)
        => _steps.Add(new StepResult(step, StepStatus.Skipped, evidence));

    public async Task<string> WriteAsync(
        string repositoryRoot,
        string? configuredDirectory,
        CancellationToken cancellationToken)
    {
        var reportDirectory = configuredDirectory ?? Path.Combine(repositoryRoot, "tests", "reports");
        Directory.CreateDirectory(reportDirectory);
        var reportPath = Path.Combine(reportDirectory, $"{context.RunId}-featbit-mcp-e2e.md");
        var markdown = redactor.Redact(BuildMarkdown());
        await File.WriteAllTextAsync(
            reportPath,
            markdown,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
        return reportPath;
    }

    private string BuildMarkdown()
    {
        var status = HasFailure
            ? "failed"
            : HasSkippedStep
                ? "completed with skipped steps"
                : "passed";
        var builder = new StringBuilder();
        builder.AppendLine("# FeatBit MCP Server SaaS E2E Report");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Status: {status}");
        builder.AppendLine($"- Run ID: `{Escape(context.RunId)}`");
        builder.AppendLine($"- Completed at: `{DateTimeOffset.UtcNow:O}`");
        builder.AppendLine(
            $"- Cleanup: `{(context.ProjectState == "not_attempted" ? "not_required" : "awaiting_manual_cleanup")}`");
        builder.AppendLine();
        builder.AppendLine("## Environment and Sanitized Configuration");
        builder.AppendLine();
        builder.AppendLine("| Setting | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| MCP endpoint | `{Escape(mcpUrl.ToString())}` |");
        builder.AppendLine($"| FeatBit API host | `{Escape(apiBaseUrl.ToString())}` |");
        builder.AppendLine("| Authorization | `<redacted>` |");
        builder.AppendLine("| Organization | `<redacted>` |");
        builder.AppendLine("| Environment secret | `<redacted>` |");
        builder.AppendLine();
        builder.AppendLine("## MCP Transport Setup");
        builder.AppendLine();
        AppendSteps(builder, IsTransportStep);
        builder.AppendLine();
        builder.AppendLine("## Tool Discovery");
        builder.AppendLine();
        if (_discoveredTools.Count == 0)
        {
            builder.AppendLine("Tool discovery did not complete.");
        }
        else
        {
            foreach (var tool in _discoveredTools)
                builder.AppendLine($"- `{Escape(tool)}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Flow Results");
        builder.AppendLine();
        AppendSteps(builder, step => !IsTransportStep(step));
        builder.AppendLine();
        builder.AppendLine("## Run-Scoped Project and Environment Lifecycle");
        builder.AppendLine();
        builder.AppendLine("| Resource | Name | Key | State |");
        builder.AppendLine("| --- | --- | --- | --- |");
        builder.AppendLine(
            $"| Project | `{Escape(context.ProjectName)}` | `{Escape(context.ProjectKey)}` | `{Escape(context.ProjectState)}` |");
        builder.AppendLine(
            $"| Environment | `{Escape(context.EnvironmentName)}` | `{Escape(context.EnvironmentKey)}` | `{Escape(context.EnvironmentState)}` |");
        builder.AppendLine();
        builder.AppendLine("## Disposable Feature Flag Lifecycle");
        builder.AppendLine();
        builder.AppendLine("| Purpose | Name | Key | Creation | Enabled | Archived |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var featureFlag in context.FeatureFlags)
        {
            builder.AppendLine(
                $"| {Escape(featureFlag.Purpose)} | `{Escape(featureFlag.Name)}` | `{Escape(featureFlag.Key)}` | " +
                $"`{Escape(featureFlag.CreationState)}` | `{Format(featureFlag.IsEnabled)}` | `{Format(featureFlag.IsArchived)}` |");
        }

        builder.AppendLine();
        builder.AppendLine($"Archive approval decision: `{Escape(context.ArchiveDecision)}`.");
        builder.AppendLine();
        builder.AppendLine("## Composite Agent Scenario");
        builder.AppendLine();
        var deletionCandidates = context.FeatureFlags
            .Where(flag => flag.DeleteAfter.HasValue && !string.IsNullOrWhiteSpace(flag.DeletionStatus))
            .ToArray();
        if (deletionCandidates.Length == 0)
        {
            builder.AppendLine("The tagged deletion-date scenario did not produce results.");
        }
        else
        {
            builder.AppendLine("| env_key | flag_key | delete_after | deletion_status |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var featureFlag in deletionCandidates)
            {
                builder.AppendLine(
                    $"| `{Escape(context.EnvironmentKey)}` | `{Escape(featureFlag.Key)}` | " +
                    $"`{featureFlag.DeleteAfter:yyyy-MM-dd}` | `{Escape(featureFlag.DeletionStatus!)}` |");
            }

            var dueCount = deletionCandidates.Count(flag => flag.DeletionStatus == "due");
            var notDueCount = deletionCandidates.Count(flag => flag.DeletionStatus == "not_due");
            var unknownCount = deletionCandidates.Length - dueCount - notDueCount;
            builder.AppendLine();
            builder.AppendLine(
                $"Row count: `{deletionCandidates.Length}`; due: `{dueCount}`; not due: `{notDueCount}`; unknown: `{unknownCount}`.");
        }

        builder.AppendLine();
        builder.AppendLine("## Manual Cleanup Handoff");
        builder.AppendLine();
        if (context.ProjectState == "not_attempted")
        {
            builder.AppendLine("No Project creation was attempted.");
        }
        else
        {
            builder.AppendLine("Open the FeatBit SaaS Projects page and inspect this exact test Project:");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine("Manual cleanup required");
            builder.AppendLine($"Project: {context.ProjectName}");
            builder.AppendLine($"Project key: {context.ProjectKey}");
            builder.AppendLine($"Environment: {context.EnvironmentName}");
            builder.AppendLine($"Environment key: {context.EnvironmentKey}");
            builder.AppendLine("Feature Flags:");
            foreach (var featureFlag in context.FeatureFlags.Where(flag => flag.CreationState != "not_attempted"))
            {
                builder.AppendLine(
                    $"- {featureFlag.Name} (key: {featureFlag.Key}, enabled: {Format(featureFlag.IsEnabled)}, " +
                    $"archived: {Format(featureFlag.IsArchived)})");
            }

            builder.AppendLine("```");
            builder.AppendLine();
            builder.AppendLine(
                "The runner did not delete the Project or Environment and did not archive any Feature Flag as cleanup.");
        }

        builder.AppendLine();
        builder.AppendLine("## Findings");
        builder.AppendLine();
        var failedSteps = _steps.Where(step => step.Status == StepStatus.Failed).ToArray();
        if (failedSteps.Length == 0)
        {
            builder.AppendLine("- No blocking failure was recorded.");
        }
        else
        {
            foreach (var step in failedSteps)
                builder.AppendLine($"- `{Escape(step.Name)}`: {Escape(step.Evidence)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Evidence Snippets");
        builder.AppendLine();
        foreach (var step in _steps)
        {
            builder.AppendLine(
                $"- `{Escape(step.Name)}` — `{step.Status.ToString().ToLowerInvariant()}`: {Escape(step.Evidence)}");
        }

        builder.AppendLine();
        builder.AppendLine(
            "This report intentionally excludes access tokens, Organization/Workspace values, resource IDs, variation IDs, and environment secrets.");
        return builder.ToString();
    }

    private void AppendSteps(StringBuilder builder, Func<StepResult, bool> predicate)
    {
        builder.AppendLine("| Step | Status | Evidence |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var step in _steps.Where(predicate))
        {
            builder.AppendLine(
                $"| {Escape(step.Name)} | `{step.Status.ToString().ToLowerInvariant()}` | {Escape(step.Evidence)} |");
        }
    }

    private static bool IsTransportStep(StepResult step)
        => step.Name.StartsWith("Step 1:", StringComparison.Ordinal) ||
           step.Name.StartsWith("Step 2:", StringComparison.Ordinal) ||
           step.Name.StartsWith("Step 3:", StringComparison.Ordinal);

    private static string Escape(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string Format(bool? value)
        => value.HasValue ? value.Value.ToString().ToLowerInvariant() : "unknown";

    private sealed record StepResult(string Name, StepStatus Status, string Evidence);

    private enum StepStatus
    {
        Passed,
        Failed,
        Skipped
    }
}
