using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace FeatBit.McpServer.Extensions;

/// <summary>
/// Extension methods for configuring AI services (OpenAI, Azure OpenAI, etc.).
/// </summary>
public static class AiServiceExtensions
{
    /// <summary>
    /// Adds an AI chat client based on the configuration.
    /// Supports OpenAI, Azure OpenAI, and other providers via Microsoft.Extensions.AI.
    /// </summary>
    public static IServiceCollection AddAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<IChatClient>>();

            var chatClient = CreateChatClient(config, logger);
            
            if (chatClient == null)
            {
                logger.LogWarning("No AI provider configured. AI-powered features will be disabled. " +
                                  "Configure AI:Provider in appsettings.json.");
            }
            else
            {
                var provider = config["AI:Provider"] ?? "auto-detected";
                logger.LogInformation("AI chat client initialized successfully using provider: {Provider}", provider);
            }

            return chatClient ?? CreateNullChatClient();
        });

        return services;
    }

    private static IChatClient? CreateChatClient(IConfiguration configuration, ILogger logger)
    {
        var provider = configuration["AI:Provider"];

        return provider?.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAIChatClient(configuration, logger),
            "azure" or "azureopenai" => CreateAzureOpenAIChatClient(configuration, logger),
            "anthropic" or "claude" => CreateAnthropicChatClient(configuration, logger),
            _ => TryCreateDefaultChatClient(configuration, logger)
        };
    }

    private static IChatClient? CreateOpenAIChatClient(IConfiguration configuration, ILogger logger)
    {
        var apiKey = configuration["AI:OpenAI:ApiKey"];
        var model = configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogDebug("OpenAI API key not found in configuration");
            return null;
        }

        logger.LogInformation("Initializing OpenAI chat client with model: {Model}", model);
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    private static IChatClient? CreateAzureOpenAIChatClient(IConfiguration configuration, ILogger logger)
    {
        var endpoint = configuration["AI:AzureOpenAI:Endpoint"];
        var apiKey = configuration["AI:AzureOpenAI:ApiKey"];
        var deployment = configuration["AI:AzureOpenAI:Deployment"] ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            logger.LogDebug("Azure OpenAI endpoint or API key not found in configuration");
            return null;
        }

        logger.LogInformation("Initializing Azure OpenAI chat client with endpoint: {Endpoint}, deployment: {Deployment}", endpoint, deployment);
        return new AzureOpenAIClient(
            new Uri(endpoint), 
            new AzureKeyCredential(apiKey))
            .GetChatClient(deployment)
            .AsIChatClient();
    }

    private static IChatClient? CreateAnthropicChatClient(IConfiguration configuration, ILogger logger)
    {
        // Note: Anthropic support requires a separate NuGet package
        // Install: dotnet add package Anthropic.SDK
        logger.LogWarning("Anthropic provider requested but not yet implemented. Please use OpenAI or Azure OpenAI.");
        return null;
    }

    private static IChatClient? TryCreateDefaultChatClient(IConfiguration configuration, ILogger logger)
    {
        logger.LogDebug("No specific provider configured, trying auto-detection...");
        
        // Try OpenAI first
        var client = CreateOpenAIChatClient(configuration, logger);
        if (client != null) return client;

        // Try Azure OpenAI
        client = CreateAzureOpenAIChatClient(configuration, logger);
        if (client != null) return client;

        // Try Anthropic
        return CreateAnthropicChatClient(configuration, logger);
    }

    /// <summary>
    /// Creates a null chat client that always returns an error.
    /// This allows the application to run even without AI configuration.
    /// </summary>
    private static IChatClient CreateNullChatClient()
    {
        return new NullChatClient();
    }

    /// <summary>
    /// A simple IChatClient implementation that throws an exception when used.
    /// </summary>
    private class NullChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("null", null, null);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "AI chat client is not configured. Please configure AI:Provider in appsettings.json or set environment variables.");
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "AI chat client is not configured. Please configure AI:Provider in appsettings.json or set environment variables.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
