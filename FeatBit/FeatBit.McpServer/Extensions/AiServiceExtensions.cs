using FeatBit.McpServer.Infrastructure;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Extensions;

/// <summary>
/// Extension methods for registering AI services in dependency injection.
/// </summary>
public static class AiServiceExtensions
{
    /// <summary>
    /// Adds an AI chat client to the service collection.
    /// Uses AiChatClientFactory to create the appropriate client based on configuration.
    /// </summary>
    public static IServiceCollection AddAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<IChatClient>>();

            return AiChatClientFactory.CreateChatClient(config, logger);
        });

        return services;
    }
}
