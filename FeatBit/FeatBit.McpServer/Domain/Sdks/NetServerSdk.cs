using FeatBit.McpServer.Infrastructure;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Domain.Sdks;

public class NetServerSdk
{
    private readonly IDocumentLoader _documentLoader;
    private readonly IChatClient _chatClient;
    private const string ResourceSubPath = "Sdks.DotNETSdks";

    /// <summary>
    /// Creates a new instance of NetServerSdk with dependency injection.
    /// </summary>
    public NetServerSdk(IDocumentLoader documentLoader, IChatClient chatClient)
    {
        _documentLoader = documentLoader;
        _chatClient = chatClient;
    }

    /// <summary>
    /// Lazy-loaded list of available documents with descriptions extracted from markdown files.
    /// </summary>
    private IDocumentLoader.DocumentOption[]? _availableDocuments;
    
    private IDocumentLoader.DocumentOption[] AvailableDocuments
    {
        get
        {
            if (_availableDocuments == null)
            {
                var documentFiles = new[] { "NetServerSdkAspNetCore.md", "NetServerSdkConsole.md" };
                _availableDocuments = _documentLoader.LoadAvailableDocuments(documentFiles, ResourceSubPath);
            }
            return _availableDocuments;
        }
    }

    public async Task<string> GenerateSdkGuideAsync(string? topic = null, CancellationToken cancellationToken = default)
    {
        // Use AI to select the most appropriate documentation file
        var selectedFileName = await SelectDocumentWithAIAsync(topic, cancellationToken);
        
        // Load the selected document
        return _documentLoader.LoadDocumentContent(selectedFileName, ResourceSubPath);
    }

    /// <summary>
    /// Selects the most appropriate SDK documentation file based on the user's topic using AI.
    /// Falls back to the first document if AI is not configured or fails.
    /// </summary>
    private async Task<string> SelectDocumentWithAIAsync(string? topic, CancellationToken cancellationToken = default)
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
            // Log error and fall back to default
            Console.Error.WriteLine($"AI selection failed: {ex.Message}. Using default document.");
            return AvailableDocuments[0].FileName;
        }
    }

    /// <summary>
    /// Uses an LLM to intelligently select the best documentation file.
    /// </summary>
    private async Task<string> SelectWithAIAsync(string topic, CancellationToken cancellationToken)
    {
        // Build dynamic prompt with available documents and their descriptions
        var documentList = string.Join("\n", AvailableDocuments.Select((doc, index) => 
            $"{index + 1}. {doc.FileName} - {doc.Description}"));

        var systemPrompt = $$"""
            You are an expert .NET developer assistant helping to select the most appropriate FeatBit SDK documentation.
            
            Available documentation files:
            {{documentList}}
            
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

        // If AI returns invalid response, fall back to first document
        Console.Error.WriteLine($"AI returned unexpected filename: {selectedFile}. Falling back to default.");
        return AvailableDocuments[0].FileName;
    }
}
