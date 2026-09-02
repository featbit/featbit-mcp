using System.Text.Json;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class CreateFeatureFlagCompatibilityTests
{
    // These FeatBit releases share the same CreateFeatureFlag request model and validator.
    // Each row documents a supported server version against the common payload contract below.
    public static TheoryData<string> SupportedFeatBitVersions =>
    [
        "5.4.4",
        "5.4.5",
        "5.4.6",
        "5.4.7",
        "5.4.8"
    ];

    [Theory]
    [MemberData(nameof(SupportedFeatBitVersions))]
    public async Task CreateFeatureFlag_SendsPayloadRequiredBySupportedFeatBitVersions(string featBitVersion)
    {
        var handler = new RecordingHttpMessageHandler();
        var tools = ToolTestFactory.CreateTools(handler);
        var envId = Guid.NewGuid().ToString();

        await tools.CreateFeatureFlag(
            envId,
            "Compatibility flag",
            "compatibility-flag",
            "FeatBit 5.4 compatibility contract",
            "compatibility,mcp");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"/api/v1/envs/{envId}/feature-flags", request.Uri.PathAndQuery);
        Assert.InRange(Version.Parse(featBitVersion), new Version(5, 4, 4), new Version(5, 4, 8));

        using var payload = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        var root = payload.RootElement;

        Assert.Equal(envId, root.GetProperty("envId").GetString());
        Assert.Equal("Compatibility flag", root.GetProperty("name").GetString());
        Assert.Equal("compatibility-flag", root.GetProperty("key").GetString());
        Assert.False(root.GetProperty("isEnabled").GetBoolean());
        Assert.Equal("boolean", root.GetProperty("variationType").GetString());

        var variations = root.GetProperty("variations").EnumerateArray().ToArray();
        Assert.Equal(2, variations.Length);

        var enabledVariationId = root.GetProperty("enabledVariationId").GetString();
        var disabledVariationId = root.GetProperty("disabledVariationId").GetString();
        Assert.NotNull(enabledVariationId);
        Assert.NotNull(disabledVariationId);
        Assert.NotEqual(enabledVariationId, disabledVariationId);
        Assert.True(Guid.TryParse(enabledVariationId, out _));
        Assert.True(Guid.TryParse(disabledVariationId, out _));

        Assert.Contains(variations, variation =>
            variation.GetProperty("id").GetString() == enabledVariationId &&
            variation.GetProperty("name").GetString() == "True" &&
            variation.GetProperty("value").GetString() == "true");
        Assert.Contains(variations, variation =>
            variation.GetProperty("id").GetString() == disabledVariationId &&
            variation.GetProperty("name").GetString() == "False" &&
            variation.GetProperty("value").GetString() == "false");

        var tags = root.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()!).ToArray();
        Assert.Equal(["compatibility", "mcp"], tags);
    }
}
