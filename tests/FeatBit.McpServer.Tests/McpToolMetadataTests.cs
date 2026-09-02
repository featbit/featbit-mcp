using System.ComponentModel;
using System.Reflection;
using FeatBit.McpServer.Tools;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Xunit;

namespace FeatBit.McpServer.Tests;

public class McpToolMetadataTests
{
    private static readonly string[] ExpectedToolMethods =
    [
        "AddFlagTargetUser",
        "ArchiveFeatureFlag",
        "CreateFeatureFlag",
        "EvaluateFeatureFlags",
        "GetAuditLogs",
        "GetFeatureFlag",
        "GetFeatureFlagAuditLogs",
        "GetFeatureFlags",
        "GetProject",
        "GetProjectFeatureFlags",
        "GetProjects",
        "ToggleFeatureFlag",
        "UpdateFeatureFlagRollout"
    ];

    [Fact]
    public void ToolType_ExposesExpectedToolInventoryAndDescriptions()
    {
        var type = typeof(FeatBitApiTools);
        Assert.NotNull(type.GetCustomAttribute<McpServerToolTypeAttribute>());

        var methods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .OrderBy(method => method.Name)
            .ToArray();

        Assert.Equal(ExpectedToolMethods, methods.Select(method => method.Name).ToArray());
        Assert.All(methods, method =>
        {
            Assert.Equal(typeof(Task<string>), method.ReturnType);
            Assert.False(
                string.IsNullOrWhiteSpace(method.GetCustomAttribute<DescriptionAttribute>()?.Description),
                $"{method.Name} must have a description.");
            Assert.All(method.GetParameters(), parameter =>
                Assert.False(
                    string.IsNullOrWhiteSpace(parameter.GetCustomAttribute<DescriptionAttribute>()?.Description),
                    $"{method.Name}.{parameter.Name} must have a description."));
        });
    }

    [Fact]
    public void McpAspNetCoreAssembly_IsVersionTwoPointTwo()
    {
        var assemblyVersion = typeof(HttpServerTransportOptions).Assembly.GetName().Version;

        Assert.NotNull(assemblyVersion);
        Assert.Equal(2, assemblyVersion.Major);
        Assert.Equal(2, assemblyVersion.Minor);
        Assert.True(Enum.IsDefined(HttpServerSessionMode.StatefulForInitializeClients));
    }
}
