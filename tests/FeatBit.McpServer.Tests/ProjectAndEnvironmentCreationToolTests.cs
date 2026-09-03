using System.Text.Json;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class ProjectAndEnvironmentCreationToolTests
{
    [Fact]
    public async Task CreateProject_SendsProviderCompatibleRequest()
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        await tools.CreateProject("Checkout service", "checkout-service");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/projects", request.Uri.PathAndQuery);

        using var payload = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        Assert.Equal(2, payload.RootElement.EnumerateObject().Count());
        Assert.Equal("Checkout service", payload.RootElement.GetProperty("name").GetString());
        Assert.Equal("checkout-service", payload.RootElement.GetProperty("key").GetString());
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("Pre-production validation", "Pre-production validation")]
    public async Task CreateEnvironment_SendsProviderCompatibleRequest(
        string? description,
        string expectedDescription)
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);

        await tools.CreateEnvironment(
            "project/id",
            "Staging",
            "staging",
            description);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/projects/project%2Fid/envs", request.Uri.PathAndQuery);

        using var payload = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        Assert.Equal(3, payload.RootElement.EnumerateObject().Count());
        Assert.Equal("Staging", payload.RootElement.GetProperty("name").GetString());
        Assert.Equal("staging", payload.RootElement.GetProperty("key").GetString());
        Assert.Equal(expectedDescription, payload.RootElement.GetProperty("description").GetString());
    }
}
