using System.Text.Json;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class ProjectAndAuditLogToolTests
{
    [Fact]
    public async Task ProjectReadTools_UseExpectedRoutes()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        await tools.GetProjects();
        await tools.GetProject("project/id");

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/projects", request.Uri.PathAndQuery);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/projects/project%2Fid", request.Uri.PathAndQuery);
            });
    }

    [Fact]
    public async Task GetProjectFeatureFlags_AggregatesEveryEnvironment()
    {
        var handler = new RecordingHttpMessageHandler(
            """
            {"success":true,"data":{
              "id":"project-1","name":"Payments","key":"payments",
              "environments":[
                {"id":"env-1","name":"Development","key":"dev"},
                {"id":"env-2","name":"Production","key":"prod"}
              ]
            }}
            """,
            """{"success":true,"data":{"totalCount":1,"items":[{"key":"flag-dev"}]}}""",
            """{"success":true,"data":{"totalCount":2,"items":[{"key":"flag-prod-a"},{"key":"flag-prod-b"}]}}""");
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetProjectFeatureFlags(
            "project-1",
            name: "checkout beta",
            tags: "payments,api/v2",
            pageIndex: 1,
            pageSize: 20);

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal("/api/v1/projects/project-1", handler.Requests[0].Uri.PathAndQuery);
        Assert.Equal(
            "/api/v1/envs/env-1/feature-flags" +
            "?Name=checkout%20beta&Tags=payments&Tags=api%2Fv2&PageIndex=1&PageSize=20",
            handler.Requests[1].Uri.PathAndQuery);
        Assert.Equal(
            "/api/v1/envs/env-2/feature-flags" +
            "?Name=checkout%20beta&Tags=payments&Tags=api%2Fv2&PageIndex=1&PageSize=20",
            handler.Requests[2].Uri.PathAndQuery);

        using var doc = JsonDocument.Parse(result);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("project-1", data.GetProperty("projectId").GetString());
        Assert.Equal("Payments", data.GetProperty("projectName").GetString());
        Assert.Equal("payments", data.GetProperty("projectKey").GetString());

        var environments = data.GetProperty("environments").EnumerateArray().ToArray();
        Assert.Equal(2, environments.Length);
        Assert.Equal("env-1", environments[0].GetProperty("envId").GetString());
        Assert.Equal(1, environments[0].GetProperty("totalCount").GetInt32());
        Assert.Equal("flag-dev", environments[0].GetProperty("items")[0].GetProperty("key").GetString());
        Assert.Equal("env-2", environments[1].GetProperty("envId").GetString());
        Assert.Equal(2, environments[1].GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetAuditLogs_EncodesEveryFilter()
    {
        var response = """{"success":true,"data":{"totalCount":0,"items":[]}}""";
        var handler = new RecordingHttpMessageHandler(response);
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetAuditLogs(
            "env/id",
            "changed flag/key",
            "creator/id",
            "ref/id",
            "Feature Flag",
            1000,
            2000,
            true,
            3,
            25);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/audit-logs" +
            "?Query=changed%20flag%2Fkey" +
            "&CreatorId=creator%2Fid&RefId=ref%2Fid&RefType=Feature%20Flag" +
            "&From=1000&To=2000&CrossEnvironment=true&PageIndex=3&PageSize=25",
            request.Uri.PathAndQuery);
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task GetAuditLogs_FetchAll_AggregatesPages()
    {
        var handler = new RecordingHttpMessageHandler(
            """{"success":true,"data":{"totalCount":2,"items":[{"id":"log-1"}]}}""",
            """{"success":true,"data":{"totalCount":2,"items":[{"id":"log-2"}]}}""");
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetAuditLogs("env", fetchAll: true);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/api/v1/envs/env/audit-logs?PageIndex=0&PageSize=100", handler.Requests[0].Uri.PathAndQuery);
        Assert.Equal("/api/v1/envs/env/audit-logs?PageIndex=1&PageSize=100", handler.Requests[1].Uri.PathAndQuery);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(
            ["log-1", "log-2"],
            doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetString()!)
                .ToArray());
    }

    [Fact]
    public async Task GetFeatureFlagAuditLogs_ResolvesFlagKeyBeforeQueryingLogs()
    {
        var handler = new RecordingHttpMessageHandler(
            """{"success":true,"data":{"id":"flag-id"}}""",
            """{"success":true,"data":{"totalCount":1,"items":[{"id":"log-id"}]}}""");
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetFeatureFlagAuditLogs(
            "env/id",
            flagKey: "flag/key",
            query: "rollout changed",
            pageIndex: 0,
            pageSize: 10);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/feature-flags/flag%2Fkey",
            handler.Requests[0].Uri.PathAndQuery);
        Assert.Equal(
            "/api/v1/envs/env%2Fid/audit-logs" +
            "?Query=rollout%20changed&RefId=flag-id&RefType=FeatureFlag&PageIndex=0&PageSize=10",
            handler.Requests[1].Uri.PathAndQuery);
        Assert.Contains("log-id", result);
    }

    [Fact]
    public async Task GetFeatureFlagAuditLogs_RequiresFlagIdOrKey()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetFeatureFlagAuditLogs("env");

        Assert.Empty(handler.Requests);
        using var doc = JsonDocument.Parse(result);
        Assert.Contains("flagId or flagKey", doc.RootElement.GetProperty("error").GetString());
    }
}
