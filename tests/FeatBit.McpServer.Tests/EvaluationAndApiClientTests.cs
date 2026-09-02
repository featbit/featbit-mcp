using System.Net;
using System.Text.Json;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class EvaluationAndApiClientTests
{
    [Fact]
    public async Task EvaluateFeatureFlags_ForwardsSecretAndBuildsUserAndFilterPayload()
    {
        var response = """[{"key":"checkout","variation":{"value":"true"}}]""";
        var handler = new RecordingHttpMessageHandler(response);
        var tools = ToolTestFactory.CreateTools(
            handler,
            configureContext: context =>
            {
                context.Request.Headers["X-FeatBit-Env-Secret"] = "env-secret";
                context.Request.Headers.Authorization = "Bearer management-token";
            },
            evaluationBaseUrl: "https://eval.featbit.test/");

        var result = await tools.EvaluateFeatureFlags(
            "user-1",
            "Ada",
            """[{"name":"country","value":"US"},{"name":"age","value":30}]""",
            "checkout, search",
            "frontend, mobile",
            "or");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "https://eval.featbit.test/api/public/featureflag/evaluate",
            request.Uri.AbsoluteUri);
        Assert.Equal("env-secret", request.GetHeader("Authorization"));
        Assert.Equal("application/json; charset=utf-8", request.GetHeader("Content-Type"));
        Assert.Equal(response, result);

        using var doc = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        var user = doc.RootElement.GetProperty("user");
        Assert.Equal("user-1", user.GetProperty("keyId").GetString());
        Assert.Equal("Ada", user.GetProperty("name").GetString());
        Assert.Equal(2, user.GetProperty("customizedProperties").GetArrayLength());

        var filter = doc.RootElement.GetProperty("filter");
        Assert.Equal(
            ["checkout", "search"],
            filter.GetProperty("keys").EnumerateArray().Select(value => value.GetString()!).ToArray());
        Assert.Equal(
            ["frontend", "mobile"],
            filter.GetProperty("tags").EnumerateArray().Select(value => value.GetString()!).ToArray());
        Assert.Equal("or", filter.GetProperty("tagFilterMode").GetString());
    }

    [Fact]
    public async Task ManagementApi_ForwardsAuthenticationAndContextHeaders()
    {
        var handler = new RecordingHttpMessageHandler("""{"success":true}""");
        var tools = ToolTestFactory.CreateTools(
            handler,
            configureContext: context =>
            {
                context.Request.Headers.Authorization = "Bearer token";
                context.Request.Headers["Organization"] = "organization-id";
                context.Request.Headers["Workspace"] = "workspace-id";
            });

        await tools.GetProjects();

        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer token", request.GetHeader("Authorization"));
        Assert.Equal("organization-id", request.GetHeader("Organization"));
        Assert.Equal("workspace-id", request.GetHeader("Workspace"));
        Assert.Equal("application/json", request.GetHeader("Accept"));
    }

    [Fact]
    public async Task ManagementApi_PassesThroughHttpErrorBody()
    {
        const string response = """{"errors":["Feature flag key already exists."]}""";
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueJson(response, HttpStatusCode.BadRequest);
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetProjects();

        Assert.Equal(response, result);
    }

    [Fact]
    public async Task ManagementApi_SerializesTransportException()
    {
        var handler = new RecordingHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("network unavailable"));
        var tools = ToolTestFactory.CreateTools(handler);

        var result = await tools.GetProjects();

        using var doc = JsonDocument.Parse(result);
        Assert.Equal("network unavailable", doc.RootElement.GetProperty("error").GetString());
    }
}
