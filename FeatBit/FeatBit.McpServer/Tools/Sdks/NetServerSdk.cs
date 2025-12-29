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
        string Description
    );

    /// <summary>
    /// Lazy-loaded list of available documents with descriptions extracted from markdown files.
    /// </summary>
    private SdkDocumentOption[]? _availableDocuments;
    
    private SdkDocumentOption[] AvailableDocuments
    {
        get
        {
            if (_availableDocuments == null)
            {
                _availableDocuments = LoadAvailableDocuments();
            }
            return _availableDocuments;
        }
    }

    /// <summary>
    /// Loads available documents and extracts descriptions from markdown files.
    /// </summary>
    private SdkDocumentOption[] LoadAvailableDocuments()
    {
        var documentFiles = new[] { "NetServerSdkAspNetCore.md", "NetServerSdkConsole.md" };
        var documents = new List<SdkDocumentOption>();

        foreach (var fileName in documentFiles)
        {
            var description = ExtractDescriptionFromMarkdown(fileName);
            documents.Add(new SdkDocumentOption(fileName, description));
        }

        return [.. documents];
    }

    /// <summary>
    /// Extracts the description from the markdown file's YAML front matter.
    /// Format:
    /// ---
    /// description: "description text"
    /// ---
    /// </summary>
    private string ExtractDescriptionFromMarkdown(string fileName)
    {
        try
        {
            var content = LoadDocumentContent(fileName);
            
            // Check if file starts with YAML front matter (---)
            if (!content.TrimStart().StartsWith("---"))
            {
                return GetFallbackDescription(fileName);
            }

            var lines = content.Split('\n');
            var inFrontMatter = false;
            var frontMatterStarted = false;

            foreach (var line in lines.Take(20)) // Check first 20 lines
            {
                var trimmed = line.Trim();
                
                // Start of front matter
                if (trimmed == "---" && !frontMatterStarted)
                {
                    inFrontMatter = true;
                    frontMatterStarted = true;
                    continue;
                }
                
                // End of front matter
                if (trimmed == "---" && frontMatterStarted)
                {
                    break;
                }
                
                // Parse description field
                if (inFrontMatter && trimmed.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
                {
                    var description = trimmed.Substring("description:".Length).Trim();
                    // Remove quotes if present
                    description = description.Trim('"', '\'');
                    return description;
                }
            }
        }
        catch
        {
            // Ignore errors during description extraction
        }

        return GetFallbackDescription(fileName);
    }

    private static string GetFallbackDescription(string fileName)
    {
        return fileName.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase) 
            ? "ASP.NET Core applications" 
            : "Console and worker applications";
    }

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

        // If AI returns invalid response, fall back to simple selection
        Console.Error.WriteLine($"AI returned unexpected filename: {selectedFile}. Falling back to default.");
        return AvailableDocuments[0].FileName;
    }

    private string LoadDocumentContent(string fileName)
    {
        var resourcePath = $"FeatBit.McpServer.Resources.Sdks.DotNETSdk.{fileName}";
        
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
        var markdownPath = Path.Combine(executablePath, "Resources", "Sdks", "DotNETSdk", fileName);

        if (File.Exists(markdownPath))
        {
            return File.ReadAllText(markdownPath);
        }

        // Final fallback
        return $"❌ SDK documentation '{fileName}' not found. Please ensure the resource file is properly embedded.";
    }
}
