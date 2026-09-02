using System.Text.Json;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class FeatureFlagToolTests
{
    private const string EmptyPage = """
        {"success":true,"data":{"totalCount":0,"items":[]}}
        """;

    [Fact]
    public async Task GetFeatureFlags_EncodesEveryFilter()
    {
        var handler = new RecordingHttpMessageHandler(EmptyPage);
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetFeatureFlags(
            "env/id",
            "checkout / beta",
            "blue team,api/v2",
            true,
            false,
            "updated at",
            2,
            25);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/feature-flags" +
            "?Name=checkout%20%2F%20beta" +
            "&Tags=blue%20team&Tags=api%2Fv2" +
            "&IsEnabled=true&IsArchived=false" +
            "&SortBy=updated%20at&PageIndex=2&PageSize=25",
            request.Uri.PathAndQuery);
        Assert.Equal(EmptyPage, result);
    }

    [Fact]
    public async Task GetFeatureFlags_FetchAll_AggregatesPages()
    {
        var handler = new RecordingHttpMessageHandler(
            """{"success":true,"data":{"totalCount":3,"items":[{"key":"a"},{"key":"b"}]}}""",
            """{"success":true,"data":{"totalCount":3,"items":[{"key":"c"}]}}""");
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetFeatureFlags("env", fetchAll: true);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            "/api/v1/envs/env/feature-flags?PageIndex=0&PageSize=100",
            handler.Requests[0].Uri.PathAndQuery);
        Assert.Equal(
            "/api/v1/envs/env/feature-flags?PageIndex=1&PageSize=100",
            handler.Requests[1].Uri.PathAndQuery);

        using var doc = JsonDocument.Parse(result);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(3, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            ["a", "b", "c"],
            data.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("key").GetString()!)
                .ToArray());
    }

    [Theory]
    [InlineData("short")]
    [InlineData("key-ct-ut")]
    public async Task GetFeatureFlags_ShapesListAccordingToFeatureFlag(string variation)
    {
        var handler = new RecordingHttpMessageHandler(
            """
            {"success":true,"data":{"totalCount":2,"items":[
              {"key":"a","createdAt":"2026-01-01","updatedAt":"2026-01-02","name":"A"},
              {"key":"b","createdAt":"2026-02-01","updatedAt":"2026-02-02","name":"B"}
            ]}}
            """);
        var tools = ToolTestFactory.CreateTools(handler, flagListVariation: variation);

        var result = await tools.GetFeatureFlags("env");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        if (variation == "short")
        {
            Assert.Equal(
                ["a", "b"],
                doc.RootElement.EnumerateArray().Select(item => item.GetString()!).ToArray());
            return;
        }

        var first = doc.RootElement[0];
        Assert.Equal("a", first.GetProperty("key").GetString());
        Assert.Equal("2026-01-01", first.GetProperty("createdAt").GetString());
        Assert.Equal("2026-01-02", first.GetProperty("updatedAt").GetString());
        Assert.False(first.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task ReadToggleAndArchiveTools_UseExpectedRoutes()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        await tools.GetFeatureFlag("env/id", "flag/key");
        await tools.ToggleFeatureFlag("env/id", "flag/key", true);
        await tools.ArchiveFeatureFlag("env/id", "flag/key");

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal(
                    "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey",
                    request.Uri.PathAndQuery);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal(
                    "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey/toggle/true",
                    request.Uri.PathAndQuery);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal(
                    "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey/archive",
                    request.Uri.PathAndQuery);
            });
    }

    [Fact]
    public async Task UpdateFeatureFlagRollout_BuildsFallthroughPatch()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        await tools.UpdateFeatureFlagRollout(
            "env/id",
            "flag/key",
            """[{"variationId":"v1","percentage":70},{"variationId":"v2","percentage":30}]""",
            "email");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey",
            request.Uri.PathAndQuery);

        using var doc = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        var operation = Assert.Single(doc.RootElement.EnumerateArray().ToArray());
        Assert.Equal("replace", operation.GetProperty("op").GetString());
        Assert.Equal("/fallthrough", operation.GetProperty("path").GetString());

        var fallthrough = operation.GetProperty("value");
        Assert.Equal("email", fallthrough.GetProperty("dispatchKey").GetString());
        Assert.False(fallthrough.GetProperty("includedInExpt").GetBoolean());
        var variations = fallthrough.GetProperty("variations").EnumerateArray().ToArray();
        Assert.Equal(2, variations.Length);
        Assert.Equal("v1", variations[0].GetProperty("id").GetString());
        Assert.Equal([0d, 0.7d], ReadRollout(variations[0]));
        Assert.Equal("v2", variations[1].GetProperty("id").GetString());
        Assert.Equal([0.7d, 1d], ReadRollout(variations[1]));
    }

    [Fact]
    public async Task UpdateFeatureFlagRollout_RejectsInvalidPercentageTotalWithoutApiCall()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.UpdateFeatureFlagRollout(
            "env",
            "flag",
            """[{"variationId":"v1","percentage":60},{"variationId":"v2","percentage":20}]""");

        Assert.Empty(handler.Requests);
        using var doc = JsonDocument.Parse(result);
        Assert.Contains("sum to 100", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AddFlagTargetUser_SendsExpectedPayload()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler, releaseEnabled: true);

        await tools.AddFlagTargetUser("env/id", "flag/key", "user/1", "user@example.com");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey/target-users",
            request.Uri.PathAndQuery);
        using var doc = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        Assert.Equal("user/1", doc.RootElement.GetProperty("keyId").GetString());
        Assert.Equal("user@example.com", doc.RootElement.GetProperty("name").GetString());
    }

    private static double[] ReadRollout(JsonElement variation)
        => variation.GetProperty("rollout").EnumerateArray().Select(value => value.GetDouble()).ToArray();
}
