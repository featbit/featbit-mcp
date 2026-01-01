namespace FeatBit.McpServer.Domain.Deployments;

/// <summary>
/// Represents metadata extracted from a deployment documentation markdown file.
/// </summary>
public record DeploymentDocumentMetadata
{
    /// <summary>
    /// The filename of the markdown document
    /// </summary>
    public required string FileName { get; init; }
    
    /// <summary>
    /// The name from the YAML frontmatter
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The description from the YAML frontmatter explaining when to use this document
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// The full content of the markdown file
    /// </summary>
    public required string Content { get; init; }
}
