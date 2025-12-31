using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Tools.Deployments;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Services;

/// <summary>
/// Service for handling FeatBit deployment documentation routing and selection.
/// This service uses AI to intelligently select the most appropriate deployment documentation
/// based on user's deployment method, target platform, and specific topic.
/// </summary>
public class DeploymentService
{
    private readonly IDocumentLoader _documentLoader;
    private readonly IChatClient _chatClient;
    private readonly ILogger<DeploymentService> _logger;
    private const string ResourceSubPath = "Deployments";

    /// <summary>
    /// Available deployment documentation files with their metadata
    /// </summary>
    private record DeploymentDocument(
        string FileName,
        string Method,
        string[] Platforms,
        string Description
    );

    public DeploymentService(
        IDocumentLoader documentLoader,
        IChatClient chatClient,
        ILogger<DeploymentService> logger)
    {
        _documentLoader = documentLoader;
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Available deployment documents with their metadata
    /// </summary>
    private DeploymentDocument[]? _availableDocuments;
    
    private DeploymentDocument[] AvailableDocuments
    {
        get
        {
            if (_availableDocuments == null)
            {
                _availableDocuments = new[]
                {
                    new DeploymentDocument(
                        FileName: "AspireAzureDeployment.md",
                        Method: "aspire",
                        Platforms: new[] { "azure" },
                        Description: "Deploy FeatBit to Azure using .NET Aspire with Azure Container Apps, including infrastructure provisioning"
                    ),
                    new DeploymentDocument(
                        FileName: "HelmDeployment.md",
                        Method: "helm-charts",
                        Platforms: new[] { "kubernetes", "azure", "aws", "gcp", "on-premises" },
                        Description: "Deploy FeatBit on Kubernetes clusters using Helm charts for orchestration"
                    ),
                    new DeploymentDocument(
                        FileName: "TerraformAzureDeployment.md",
                        Method: "terraform",
                        Platforms: new[] { "azure" },
                        Description: "Infrastructure as Code deployment to Azure using Terraform"
                    ),
                    new DeploymentDocument(
                        FileName: "DockerComposeDeployment.md",
                        Method: "docker-compose",
                        Platforms: new[] { "docker-compose", "on-premises" },
                        Description: "Local or on-premises deployment using Docker Compose"
                    ),
                    new DeploymentDocument(
                        FileName: "README.md",
                        Method: "all",
                        Platforms: new[] { "all" },
                        Description: "Overview of all deployment methods and general deployment guidance"
                    )
                };
            }
            return _availableDocuments;
        }
    }

    /// <summary>
    /// Gets deployment documentation based on method, platform, and topic.
    /// Uses AI to select the most appropriate documentation when available.
    /// </summary>
    /// <param name="method">Deployment method (helm-charts, terraform, docker-compose, aspire, others)</param>
    /// <param name="platform">Target platform (azure, aws, gcp, on-premises, kubernetes, docker-compose, all)</param>
    /// <param name="topic">Specific topic the user wants to learn about</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deployment documentation content</returns>
    public async Task<string> GetDeploymentDocumentationAsync(
        string method,
        string platform,
        string topic,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting deployment documentation for method={Method}, platform={Platform}, topic={Topic}",
            method, platform, topic);

        // First, try to select document using AI
        var selectedFileName = await SelectDocumentWithAIAsync(method, platform, topic, cancellationToken);
        
        // Load and return the selected document
        var content = _documentLoader.LoadDocumentContent(selectedFileName, ResourceSubPath);
        
        _logger.LogInformation("Successfully loaded deployment document: {FileName}", selectedFileName);
        return content;
    }

    /// <summary>
    /// Uses AI to intelligently select the most appropriate deployment document
    /// based on method, platform, and user's specific topic.
    /// </summary>
    private async Task<string> SelectDocumentWithAIAsync(
        string method,
        string platform,
        string topic,
        CancellationToken cancellationToken)
    {
        // Build document list for AI prompt
        var documentList = string.Join("\n", AvailableDocuments.Select((doc, index) =>
            $"{index + 1}. {doc.FileName}\n" +
            $"   Method: {doc.Method}\n" +
            $"   Platforms: {string.Join(", ", doc.Platforms)}\n" +
            $"   Description: {doc.Description}"));

        var systemPrompt = $$"""
            You are an expert DevOps engineer helping to select the most appropriate FeatBit deployment documentation.
            
            Available deployment documentation files:
            {{documentList}}
            
            Based on the user's deployment method, target platform, and specific topic, respond with ONLY the filename 
            (e.g., "AspireAzureDeployment.md" or "HelmDeployment.md").
            Do not include any explanation or additional text - just the filename.
            
            Selection Guidelines:
            - Match the deployment method first (aspire, helm-charts, terraform, docker-compose)
            - Consider the target platform (azure, aws, gcp, kubernetes, on-premises)
            - Consider the user's specific topic or question
            - If multiple documents match, choose the most specific one
            - If uncertain or if method/platform is "all" or "others", choose README.md for overview
            
            Priority Rules:
            1. Exact method + platform match (highest priority)
            2. Method match with platform compatibility
            3. Platform-specific documentation
            4. General overview (README.md) as fallback
            """;

        var userMessage = $"""
            Deployment Method: {method}
            Target Platform: {platform}
            User's Topic/Question: {topic}
            
            Select the best deployment documentation file.
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = 50,
            Temperature = 0.3f // Lower temperature for more deterministic results
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        var selectedFile = response.ToString()?.Trim() ?? "";

        _logger.LogInformation("AI selected deployment document: {FileName}", selectedFile);

        // Validate the selection
        if (AvailableDocuments.Any(d => d.FileName.Equals(selectedFile, StringComparison.OrdinalIgnoreCase)))
        {
            return selectedFile;
        }

        // If AI returns invalid filename, fall back to rule-based
        _logger.LogWarning("AI returned invalid filename: {FileName}, using rule-based fallback", selectedFile);
        return SelectDocumentByRules(method, platform);
    }

    /// <summary>
    /// Rule-based fallback for document selection when AI is not available or fails.
    /// </summary>
    private string SelectDocumentByRules(string method, string platform)
    {
        var normalizedMethod = method.ToLowerInvariant();
        var normalizedPlatform = platform.ToLowerInvariant();

        // Try exact match first
        var exactMatch = AvailableDocuments.FirstOrDefault(d =>
            d.Method.Equals(normalizedMethod, StringComparison.OrdinalIgnoreCase) &&
            d.Platforms.Any(p => p.Equals(normalizedPlatform, StringComparison.OrdinalIgnoreCase)));

        if (exactMatch != null)
        {
            return exactMatch.FileName;
        }

        // Try method match with platform compatibility
        var methodMatch = AvailableDocuments.FirstOrDefault(d =>
            d.Method.Equals(normalizedMethod, StringComparison.OrdinalIgnoreCase) ||
            d.Platforms.Contains(normalizedMethod));

        if (methodMatch != null)
        {
            return methodMatch.FileName;
        }

        // Platform-only match
        var platformMatch = AvailableDocuments.FirstOrDefault(d =>
            d.Platforms.Any(p => p.Equals(normalizedPlatform, StringComparison.OrdinalIgnoreCase)));

        if (platformMatch != null)
        {
            return platformMatch.FileName;
        }

        // Default fallback to README
        _logger.LogInformation("No specific match found, returning README.md");
        return "README.md";
    }

    /// <summary>
    /// Gets a list of available deployment methods with their descriptions.
    /// Useful for helping users understand their deployment options.
    /// </summary>
    public IEnumerable<object> GetAvailableDeploymentMethods()
    {
        return AvailableDocuments.Select(d => new
        {
            d.FileName,
            d.Method,
            Platforms = string.Join(", ", d.Platforms),
            d.Description
        });
    }
}
