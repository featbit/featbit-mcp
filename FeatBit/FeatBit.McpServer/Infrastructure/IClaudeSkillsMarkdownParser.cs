namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Parser for Claude Skills markdown documents.
/// Claude Skills markdown follows a standard format with YAML frontmatter containing metadata.
/// This parser is used throughout the project for all markdown documentation.
/// </summary>
public interface IClaudeSkillsMarkdownParser
{
    /// <summary>
    /// Represents a parsed Claude Skills markdown document.
    /// </summary>
    /// <param name="FileName">The filename of the document</param>
    /// <param name="Name">The name from frontmatter, or filename without extension if not present</param>
    /// <param name="Description">The description from frontmatter, or default message if not present</param>
    /// <param name="Metadata">All frontmatter key-value pairs</param>
    /// <param name="Content">The full markdown content including frontmatter</param>
    record ParsedDocument(
        string FileName,
        string Name,
        string Description,
        IReadOnlyDictionary<string, string> Metadata,
        string Content
    );

    /// <summary>
    /// Parses a Claude Skills markdown document and extracts its metadata.
    /// </summary>
    /// <param name="fileName">The filename of the document</param>
    /// <param name="content">The markdown content</param>
    /// <returns>Parsed document with extracted metadata</returns>
    ParsedDocument ParseDocument(string fileName, string content);

    /// <summary>
    /// Extracts YAML frontmatter from a markdown document.
    /// </summary>
    /// <param name="content">The markdown content</param>
    /// <returns>A dictionary of frontmatter key-value pairs, or empty if no frontmatter found</returns>
    Dictionary<string, string> ExtractFrontmatter(string content);

    /// <summary>
    /// Removes the frontmatter from markdown content.
    /// </summary>
    /// <param name="content">The markdown content</param>
    /// <returns>Content without frontmatter</returns>
    string RemoveFrontmatter(string content);
}
