using System.Text.Json;
using FeatBit.McpServer.Domain.Deployments;
using FeatBit.McpServer.Infrastructure;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Services;

public class DeploymentService
{
    private record DocumentSelection(string[] Names);
    private readonly IDocumentLoader _documentLoader;
    private readonly IClaudeSkillsMarkdownParser _markdownParser;
    private readonly IChatClient _chatClient;
    private readonly ILogger<DeploymentService> _logger;
    private const string ResourceSubPath = "Deployments";

    private List<DeploymentDocumentMetadata>? _availableDocuments;

    public DeploymentService(
        IDocumentLoader documentLoader,
        IClaudeSkillsMarkdownParser markdownParser,
        IChatClient chatClient,
        ILogger<DeploymentService> logger)
    {
        _documentLoader = documentLoader;
        _markdownParser = markdownParser;
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Lazily loads and caches all available deployment documents with their metadata.
    /// </summary>
    private List<DeploymentDocumentMetadata> AvailableDocuments
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

    private List<DeploymentDocumentMetadata> LoadAvailableDocuments()
    {
        return _documentLoader.DiscoverDocuments(ResourceSubPath, "*.md")
            .Select(fileName =>
            {
                try
                {
                    var content = _documentLoader.LoadDocumentContent(fileName, ResourceSubPath);
                    var parsed = _markdownParser.ParseDocument(fileName, content);
                    return new DeploymentDocumentMetadata
                    {
                        FileName = parsed.FileName,
                        Name = parsed.Name,
                        Description = parsed.Description,
                        Content = parsed.Content
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load document: {FileName}", fileName);
                    return null;
                }
            })
            .Where(doc => doc != null)
            .ToList()!;
    }

    public async Task<string> GetDeploymentDocumentationAsync(
        string method,
        string platform,
        string topic,
        CancellationToken cancellationToken = default)
    {
        var selectedDocuments = await SelectDocumentsWithAIAsync(method, platform, topic, cancellationToken);
        if (selectedDocuments.Count == 0)
        {
            _logger.LogWarning("No documents selected, returning empty content");
            return "";
        }
        return string.Join("\n\n", selectedDocuments.Select(doc => doc.Content));
    }

    private async Task<List<DeploymentDocumentMetadata>> SelectDocumentsWithAIAsync(
        string method,
        string platform,
        string topic,
        CancellationToken cancellationToken)
    {
        if (AvailableDocuments.Count == 0)
        {
            _logger.LogWarning("No deployment documents available");
            return new List<DeploymentDocumentMetadata>();
        }

        // Build document list as JSON for better AI comprehension
        var documentListJson = JsonSerializer.Serialize(
            AvailableDocuments.Select(doc => new
            {
                name = doc.Name,
                description = doc.Description
            }),
            new JsonSerializerOptions { WriteIndented = true });

        var systemPrompt = """
            You are an expert DevOps engineer. Your task is to select the most relevant FeatBit deployment documentation based on the user's deployment method, target platform, and specific question.
            
            SELECTION GUIDELINES:
            
            1. **Prioritize Exact Matches**: Choose documents whose descriptions closely match the user's requirements.
            
            2. **Return Multiple Documents** when:
               - They provide complementary information for the user's needs
               - The user's query is broad and covers multiple deployment approaches
               - The topic spans across different deployment methods
            
            3. **Return a Single Document** when:
               - There is a clear and specific match
               - The user's query focuses on one particular deployment method or platform
            
            IMPORTANT:
            - Use exact `name` from the available documents list
            - Return at most 2 documents unless absolutely necessary
            - Prefer specific, targeted documentation over broad overviews
            
            RESPONSE FORMAT:
            Return a JSON object with a single property "Names" containing an array of selected document names, like this:
            ```json
            {
              "Names": ["document-name-001", "document-name-002"]
            }
            ```
            """;

        var userMessage = $$"""
            Available deployment documentation files:
            ```json
            {{documentListJson}}
            ```
            
            User Request:
            - Deployment Method: {{method}}
            - Target Platform: {{platform}}
            - Topic/Question: {{topic}}
            
            Select the best deployment documentation file(s) from the available files above.
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json
        };

        var selectedDocuments = new List<DeploymentDocumentMetadata>();
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _chatClient.GetResponseAsync<DocumentSelection>(messages, options, cancellationToken: cancellationToken);
                var selectedNames = response.Result?.Names ?? Array.Empty<string>();

                selectedDocuments = selectedNames
                    .Select(name =>
                    {
                        var doc = AvailableDocuments.FirstOrDefault(d =>
                            d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (doc == null)
                            _logger.LogWarning("AI selected invalid document name: {Name}", name);
                        return doc;
                    })
                    .Where(doc => doc != null)
                    .ToList()!;

                if (selectedDocuments.Count > 0)
                {
                    _logger.LogInformation("Successfully selected {Count} documents on attempt {Attempt}",
                        selectedDocuments.Count, attempt);
                    return selectedDocuments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during AI document selection on attempt {Attempt}/{MaxRetries}",
                    attempt, maxRetries);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
        }

        _logger.LogError("All retry attempts failed to select valid documents, returning empty list");
        return selectedDocuments;
    }
}
