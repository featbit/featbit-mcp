using System.Text.Json;
using FeatBit.McpServer.E2ETests;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class E2ERunnerSafetyTests
{
    [Fact]
    public void Parse_RequiresExplicitExecuteAcknowledgement()
    {
        var options = E2EOptions.Parse([]);

        Assert.False(options.Execute);
        Assert.False(options.Preflight);
        Assert.False(options.UseExistingServer);
        Assert.True(options.McpUrl.IsLoopback);
        Assert.Equal("/mcp", options.McpUrl.AbsolutePath);
        Assert.Null(options.TokenEnvironmentVariable);
    }

    [Fact]
    public void Parse_AcceptsTokenEnvironmentVariableNameWithoutItsValue()
    {
        var options = E2EOptions.Parse(["--execute", "--token-env", "FEATBIT_TEST_SERVICE_TOKEN"]);

        Assert.Equal("FEATBIT_TEST_SERVICE_TOKEN", options.TokenEnvironmentVariable);
    }

    [Theory]
    [InlineData("BAD NAME")]
    [InlineData("BAD=NAME")]
    public void Parse_RejectsInvalidTokenEnvironmentVariableName(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            E2EOptions.Parse(["--execute", "--token-env", name]));

        Assert.Contains("--token-env", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DoesNotAllowPreflightAndExecuteTogether()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            E2EOptions.Parse(["--preflight", "--execute"]));

        Assert.Contains("cannot be used together", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://mcp.featbit.co/mcp")]
    [InlineData("https://example.com/mcp")]
    [InlineData("http://localhost:5180/not-mcp")]
    [InlineData("http://localhost:5180/mcp?redirect=true")]
    public void Parse_RejectsUnsafeMcpEndpoints(string endpoint)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            E2EOptions.Parse(["--execute", "--mcp-url", endpoint]));

        Assert.Contains("--mcp-url", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveValueRedactor_RemovesCredentialsAndRuntimeIds()
    {
        var redactor = new SensitiveValueRedactor();
        redactor.Add("secret-token");
        redactor.Add("7de8eb43-5af9-4ce1-9d45-67442f340f6d");

        var result = redactor.Redact(
            "token=secret-token id=7de8eb43-5af9-4ce1-9d45-67442f340f6d");

        Assert.Equal("token=<redacted> id=<redacted>", result);
    }

    [Fact]
    public void RequireServerSecret_SelectsServerCredentialWithoutExposingOthers()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "secrets": [
                { "name": "Client Key", "type": "client", "value": "client-value" },
                { "name": "Server Key", "type": "server", "value": "server-value" }
              ]
            }
            """);

        var result = ApiJson.RequireServerSecret(document.RootElement, "test");

        Assert.Equal("server-value", result);
    }

    [Fact]
    public async Task Report_RedactsSensitiveValuesButKeepsCleanupKeys()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"featbit-e2e-report-{Guid.NewGuid():N}");
        try
        {
            var context = RunContext.Create();
            context.ProjectState = "creation_attempted_outcome_unknown";
            var deletionCandidate = context.AddFeatureFlag(
                "Deletion candidate",
                $"deletion-candidate-{context.RunId}",
                "Composite fixture");
            deletionCandidate.CreationState = "confirmed_created";
            deletionCandidate.DeleteAfter = new DateOnly(2026, 9, 2);
            deletionCandidate.DeletionStatus = "due";
            var redactor = new SensitiveValueRedactor();
            redactor.Add("secret-token");
            redactor.Add("7de8eb43-5af9-4ce1-9d45-67442f340f6d");
            var report = new E2EReport(
                context,
                new Uri("http://localhost:5180/mcp"),
                new Uri("https://app-api.featbit.co"),
                redactor);
            report.Passed("Step 1: Build", "build passed");
            report.Passed("Step 10: Toggle", "toggle passed");
            report.Failed(
                "test",
                "secret-token 7de8eb43-5af9-4ce1-9d45-67442f340f6d");

            var path = await report.WriteAsync(directory, directory, CancellationToken.None);
            var content = await File.ReadAllTextAsync(path);

            Assert.DoesNotContain("secret-token", content, StringComparison.Ordinal);
            Assert.DoesNotContain("7de8eb43-5af9-4ce1-9d45-67442f340f6d", content, StringComparison.Ordinal);
            Assert.Contains(context.ProjectName, content, StringComparison.Ordinal);
            Assert.Contains(context.ProjectKey, content, StringComparison.Ordinal);
            Assert.Contains("## Composite Agent Scenario", content, StringComparison.Ordinal);
            Assert.Contains(deletionCandidate.Key, content, StringComparison.Ordinal);
            Assert.Contains("Row count: `1`; due: `1`", content, StringComparison.Ordinal);

            var transportStart = content.IndexOf("## MCP Transport Setup", StringComparison.Ordinal);
            var discoveryStart = content.IndexOf("## Tool Discovery", StringComparison.Ordinal);
            var flowStart = content.IndexOf("## Flow Results", StringComparison.Ordinal);
            var lifecycleStart = content.IndexOf(
                "## Run-Scoped Project and Environment Lifecycle",
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Step 10: Toggle",
                content[transportStart..discoveryStart],
                StringComparison.Ordinal);
            Assert.Contains(
                "Step 10: Toggle",
                content[flowStart..lifecycleStart],
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
