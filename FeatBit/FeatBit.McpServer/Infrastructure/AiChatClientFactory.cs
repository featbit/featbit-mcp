using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Factory for creating AI chat client instances based on configuration.
/// Supports OpenAI, Azure OpenAI, and other AI providers.
/// </summary>
public class AiChatClientFactory
{
    /// <summary>
    /// Creates an IChatClient instance based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration containing AI provider settings</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <param name="loggerFactory">Logger factory for middleware</param>
    /// <returns>An IChatClient instance, or NullChatClient if no provider is configured</returns>
    public static IChatClient CreateChatClient(IConfiguration configuration, ILogger logger, ILoggerFactory loggerFactory)
    {
        var chatClient = TryCreateChatClient(configuration, logger);
        
        if (chatClient == null)
        {
            logger.LogWarning("No AI provider configured. AI-powered features will be disabled. " +
                              "Configure AI:Provider in appsettings.json.");
            return new NullChatClient();
        }

        var provider = configuration["AI:Provider"] ?? "auto-detected";
        logger.LogInformation("AI chat client initialized successfully using provider: {Provider}", provider);
        
        // Wrap the chat client with custom OpenTelemetry middleware and logging
        // Custom middleware manually records all prompts, responses, tokens, and function calls
        chatClient = chatClient
            .AsBuilder()
            .Use(innerClient => new ChatClientOpenTelemetryMiddleware(innerClient, logger))
            .UseLogging(loggerFactory)
            .Build();
        
        return chatClient;
    }

    private static IChatClient? TryCreateChatClient(IConfiguration configuration, ILogger logger)
    {
        var provider = configuration["AI:Provider"];

        return provider?.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAIChatClient(configuration, logger),
            "azure" or "azureopenai" => CreateAzureOpenAIChatClient(configuration, logger),
            "anthropic" or "claude" => CreateAnthropicChatClient(configuration, logger),
            _ => TryAutoDetectProvider(configuration, logger)
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
        var deployment = configuration["AI:AzureOpenAI:Deployment"] ?? "gpt-5.1";

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            logger.LogDebug("Azure OpenAI endpoint or API key not found in configuration");
            return null;
        }

        logger.LogInformation("Initializing Azure OpenAI chat client - endpoint: {Endpoint}, deployment: {Deployment}", 
            endpoint, deployment);
        
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        return azureClient.GetChatClient(deployment).AsIChatClient();
    }

    private static IChatClient? CreateAnthropicChatClient(IConfiguration configuration, ILogger logger)
    {
        // Note: Anthropic support requires a separate NuGet package
        // Install: dotnet add package Anthropic.SDK
        logger.LogWarning("Anthropic provider requested but not yet implemented. Please use OpenAI or Azure OpenAI.");
        return null;
    }

    private static IChatClient? TryAutoDetectProvider(IConfiguration configuration, ILogger logger)
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
    /// A null object implementation of IChatClient that throws when used.
    /// Allows the application to run without AI configuration.
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
