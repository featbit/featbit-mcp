using System.Reflection;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Tools.Sdks;

public class NetServerSdk
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Creates a new instance of NetServerSdk with dependency injection.
    /// </summary>
    public NetServerSdk(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> GenerateSdkGuideAsync(string? topic = null, CancellationToken cancellationToken = default)
    {
        // Use AI to select the most appropriate documentation file
        var selectedFileName = await SelectDocumentAsync(topic, cancellationToken);
        
        // Load the selected document
        return LoadDocumentContent(selectedFileName);
    }

    /// <summary>
    /// Describes the available SDK documentation files.
    /// </summary>
    private record SdkDocumentOption(
        string FileName,
        string Description,
        string[] Keywords
    );

    private static readonly SdkDocumentOption[] AvailableDocuments =
    [
        new SdkDocumentOption(
            "NetServerSdkAspNetCore.md",
            "ASP.NET Core web applications using dependency injection, middleware, and hosted services",
            ["aspnet", "asp.net", "web", "api", "mvc", "razor", "blazor", "di", "dependency injection", "middleware", "webapi"]
        ),
        new SdkDocumentOption(
            "NetServerSdkConsole.md",
            "Console applications, worker services, background services, and non-web applications",
            ["console", "worker", "background", "service", "daemon", "batch", "cli", "command-line", "standalone"]
        )
    ];

    /// <summary>
    /// Selects the most appropriate SDK documentation file based on the user's topic.
    /// Falls back to keyword matching if AI is not configured or fails.
    /// </summary>
    private async Task<string> SelectDocumentAsync(string? topic, CancellationToken cancellationToken = default)
    {
        // If no topic, default to ASP.NET Core (most common scenario)
        if (string.IsNullOrWhiteSpace(topic))
        {
            return AvailableDocuments[0].FileName;
        }

        // Try AI-based selection
        try
        {
            return await SelectWithAIAsync(topic, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error and fall back to keyword matching
            Console.Error.WriteLine($"AI selection failed: {ex.Message}. Falling back to keyword matching.");
        }

        // Fallback: keyword-based selection
        return SelectWithKeywords(topic);
    }

    /// <summary>
    /// Uses an LLM to intelligently select the best documentation file.
    /// </summary>
    private async Task<string> SelectWithAIAsync(string topic, CancellationToken cancellationToken)
    {
        var systemPrompt = """
            You are an expert .NET developer assistant helping to select the most appropriate FeatBit SDK documentation.
            
            Available documentation files:
            1. NetServerSdkAspNetCore.md - For ASP.NET Core applications (web apps, APIs, MVC, Razor, Blazor) using DI
            2. NetServerSdkConsole.md - For console apps, worker services, background services, CLI tools
            
            Based on the user's topic/question, respond with ONLY the filename (e.g., "NetServerSdkAspNetCore.md" or "NetServerSdkConsole.md").
            Do not include any explanation or additional text.
            
            Guidelines:
            - Choose ASP.NET Core for: web applications, REST APIs, GraphQL, hosted web services, middleware scenarios
            - Choose Console for: background workers, scheduled jobs, CLI tools, non-web services, batch processing
            - If ambiguous, prefer ASP.NET Core as it's more commonly used
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"User topic/question: {topic}")
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = 50,
            Temperature = 0.3f // Lower temperature for more deterministic results
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        var selectedFile = response.ToString()?.Trim() ?? string.Empty;

        // Validate response
        if (AvailableDocuments.Any(d => d.FileName.Equals(selectedFile, StringComparison.OrdinalIgnoreCase)))
        {
            return selectedFile;
        }

        // If AI returns invalid response, fall back to keyword matching
        Console.Error.WriteLine($"AI returned unexpected filename: {selectedFile}. Falling back to keyword matching.");
        return SelectWithKeywords(topic);
    }

    /// <summary>
    /// Fallback: Simple keyword-based selection.
    /// </summary>
    private string SelectWithKeywords(string topic)
    {
        var normalizedTopic = topic.ToLowerInvariant();

        foreach (var doc in AvailableDocuments)
        {
            if (doc.Keywords.Any(keyword => normalizedTopic.Contains(keyword)))
            {
                return doc.FileName;
            }
        }

        // Default to ASP.NET Core if no match
        return AvailableDocuments[0].FileName;
    }

    private string LoadDocumentContent(string fileName)
    {
        var resourcePath = $"FeatBit.McpServer.Resources.Sdks.{fileName}";
        
        // Try to read from embedded resource first (production)
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);

        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: try to read from file system (development)
        var executablePath = AppContext.BaseDirectory;
        var markdownPath = Path.Combine(executablePath, "Resources", "Sdks", fileName);

        if (File.Exists(markdownPath))
        {
            return File.ReadAllText(markdownPath);
        }

        // Final fallback
        return $"❌ SDK documentation '{fileName}' not found. Please ensure the resource file is properly embedded.";
    }

    // Keep the synchronous version for backward compatibility
    public string GenerateSdkGuide(string? topic = null)
    {
        return GenerateSdkGuideAsync(topic).GetAwaiter().GetResult();
    }
}
