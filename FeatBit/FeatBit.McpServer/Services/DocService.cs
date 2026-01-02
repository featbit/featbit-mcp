using System.Text.Json;
using Microsoft.Extensions.AI;
using FeatBit.FeatureFlags;
using FeatBit.McpServer.Infrastructure;

namespace FeatBit.McpServer.Services;

/// <summary>
/// Service for routing documentation questions to relevant FeatBit documentation URLs.
/// Uses AI with a two-phase approach: first selects the most relevant section, 
/// then selects up to 3 most relevant URLs within that section.
/// </summary>
public class DocService
{
    private record SectionSelection(string Title, string Path, string Summary, string Reason);
    private record UrlSelection(List<string> Urls, string Reason);
    private record DocTreeFile(string Path, string Url, string FullUrl, string Summary);
    private record DocTreeSubsection(string Title, string Path, string Summary, List<DocTreeFile> Files);
    private record DocTreeSection(string Title, string Path, string Summary, List<DocTreeFile> Files, List<DocTreeSubsection>? Subsections);
    private record DocTree(string Version, string GeneratedAt, string Description, string BaseUrl, List<DocTreeSection> Sections);

    private readonly IChatClient _chatClient;
    private readonly ILogger<DocService> _logger;
    private readonly IDocumentLoader _documentLoader;
    private readonly IFeatureFlagEvaluator _featureFlag;
    private DocTree? _docTree;

    public DocService(
        IChatClient chatClient,
        ILogger<DocService> logger,
        IDocumentLoader documentLoader,
        IFeatureFlagEvaluator featureFlagEvaluator)
    {
        _chatClient = chatClient;
        _logger = logger;
        _documentLoader = documentLoader;
        _featureFlag = featureFlagEvaluator;
    }

    /// <summary>
    /// Gets the most relevant documentation URLs (up to 3) based on the user's topic/question.
    /// Uses a two-phase AI approach: first selects the best section, then selects the best URLs within that section.
    /// </summary>
    /// <param name="topic">User's question or topic about FeatBit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of relevant documentation URLs (up to 3), or empty list if no match found</returns>
    public async Task<List<string>> GetDocumentationUrlsAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        // Load doc tree if not already loaded
        if (_docTree == null)
        {
            await LoadDocTreeAsync(cancellationToken);
        }

        // Phase 1: Select the most relevant section
        var selectedSection = await SelectBestSectionAsync(topic, cancellationToken);
        if (selectedSection == null)
        {
            _logger.LogWarning("No matching section found for topic: {Topic}", topic);
            return new List<string>();
        }

        // Phase 2: Select up to 3 most relevant URLs within the section
        var section = _docTree?.Sections.FirstOrDefault(s => s.Title == selectedSection.Title);
        if (section == null)
        {
            _logger.LogWarning("Section not found in doc tree: {Section}", selectedSection.Title);
            return new List<string>();
        }
        var urls = await SelectBestUrlsAsync(topic, section, cancellationToken);

        // Check feature flag when no URLs are found
        if (_featureFlag.ReleaseEnabled(FeatureFlag.DocNotFound))
        {
            if (!urls.Any())
            {
                _logger.LogTrace("Do something when no documentation is found");
            }
        }

        return urls;
    }

    private Task LoadDocTreeAsync(CancellationToken cancellationToken)
    {
        _docTree = _documentLoader.LoadJsonConfiguration<DocTree>("docs-tree.json", "Docs");

        if (_docTree == null)
        {
            _logger.LogError("Failed to load docs-tree.json");
        }

        return Task.CompletedTask;
    }

    private async Task<SectionSelection?> SelectBestSectionAsync(
        string topic,
        CancellationToken cancellationToken)
    {
        var sectionsJson = JsonSerializer.Serialize(
            _docTree!.Sections.Select(s => new
            {
                title = s.Title,
                path = s.Path,
                summary = s.Summary
            }),
            new JsonSerializerOptions { WriteIndented = true });

        var systemPrompt = """
            You are a FeatBit documentation expert. Your task is to analyze the user's question or topic 
            and select EXACTLY ONE most relevant documentation section from the available sections.
            
            SELECTION GUIDELINES:
            
            1. **Understand the Intent**: Carefully analyze what the user is trying to learn or accomplish
            2. **Semantic Matching**: Use the section's summary to understand its scope and match it to the user's needs
            3. **Consider Context**: Think about the user's level (beginner vs advanced) and their goal (learning vs troubleshooting)
            4. **Be Decisive**: You MUST select exactly one section, even if the topic is broad
            
            RESPONSE FORMAT:
            Return a JSON object with these properties:
            - "Title": The exact title of the selected section
            - "Path": The exact path of the selected section
            - "Summary": The exact summary of the selected section
            - "Reason": A brief explanation (1-2 sentences) of why this section was selected
            
            Example:
            ```json
            {
              "Title": "Feature Flags",
              "Path": "/feature-flags",
              "Summary": "Learn how to create and manage feature flags...",
              "Reason": "The user is asking about targeting rules, which is a core feature flag management topic."
            }
            ```
            """;

        var userMessage = $$"""
            Available FeatBit documentation sections:
            ```json
            {{sectionsJson}}
            ```
            
            User's Question/Topic: {{topic}}
            
            Select EXACTLY ONE most relevant section from the available sections above.
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


        var response = await _chatClient.GetResponseAsync<SectionSelection>(
            messages,
            options,
            cancellationToken: cancellationToken);

        return response.Result;
    }

    private async Task<List<string>> SelectBestUrlsAsync(
        string topic,
        DocTreeSection section,
        CancellationToken cancellationToken)
    {
        // Collect all files from the section and subsections
        var allFiles = new List<DocTreeFile>(section.Files);

        if (section.Subsections != null)
        {
            foreach (var subsection in section.Subsections)
            {
                allFiles.AddRange(subsection.Files);
            }
        }

        if (allFiles.Count == 0)
        {
            _logger.LogWarning("No files found in section: {Section}", section.Title);
            return new List<string>();
        }

        var filesJson = JsonSerializer.Serialize(
            allFiles.Select(f => new
            {
                url = f.FullUrl,
                summary = f.Summary
            }),
            new JsonSerializerOptions { WriteIndented = true });

        var systemPrompt = """
            You are an full-stack software engineer. Your task is to select the most relevant documentation URLs (maximum 3) that best answer the user's question or topic.
            
            SELECTION GUIDELINES:
            
            1. **Prioritize Relevance**: Choose URLs that directly address the user's question
            2. **Maximum 3 URLs**: Return 1-3 URLs, not more. If only 1 is highly relevant, return just 1
            3. **Order by Relevance**: List URLs from most to least relevant
            4. **Consider Specificity**: Prefer specific guides over general overviews when the question is specific
            5. **Quality over Quantity**: It's better to return 1 perfect match than 3 mediocre matches
            
            RESPONSE FORMAT:
            Return a JSON object with two properties:
            - "Urls": An array of 1-3 full documentation URLs
            - "Reason": A brief explanation (1-2 sentences) of why these URLs were selected
            
            If no URLs are relevant, return an empty array for "Urls" and explain why in "Reason".
            
            Example:
            ```json
            {
              "Urls": [
                "https://docs.featbit.co/feature-flags/targeting-rules",
                "https://docs.featbit.co/feature-flags/segments"
              ],
              "Reason": "The user is asking about targeting specific users, so targeting rules is most relevant, and segments provide additional context for user grouping."
            }
            ```
            """;

        var userMessage = $$"""
            Available documentation pages in the "{{section.Title}}" section:
            ```json
            {{filesJson}}
            ```
            
            User's Question/Topic: {{topic}}
            
            Select up to 3 most relevant documentation URLs from the available pages above.
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

        var response = await _chatClient.GetResponseAsync<UrlSelection>(
            messages,
            options,
            cancellationToken: cancellationToken);

        if (response.Result?.Urls != null && response.Result.Urls.Count > 0)
        {
            _logger.LogInformation(
                "Selected URLs: {Urls}, Reason: {Reason}",
                string.Join(", ", response.Result.Urls),
                response.Result.Reason);

            // Ensure we don't return more than 3 URLs
            return response.Result.Urls.Take(3).ToList();
        }

        _logger.LogWarning("No URLs selected by AI");

        return new List<string>();
    }
}
