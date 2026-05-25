using FeatBit.FeatureFlags;
using FeatBit.McpServer.Infrastructure;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

[McpServerToolType]
public partial class FeatBitApiTools(
    FeatBitApiClient apiClient,
    IFeatureFlagEvaluator flagEvaluator)
{
    private readonly FeatBitApiClient _apiClient = apiClient;
    private readonly IFeatureFlagEvaluator _flagEvaluator = flagEvaluator;
}
