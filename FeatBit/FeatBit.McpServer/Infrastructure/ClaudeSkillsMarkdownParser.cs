using System.Text.RegularExpressions;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Default implementation of Claude Skills markdown parser.
/// </summary>
public partial class ClaudeSkillsMarkdownParser : IClaudeSkillsMarkdownParser
{
    private static readonly Regex FrontmatterRegex = GenerateFrontmatterRegex();

    [GeneratedRegex(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex GenerateFrontmatterRegex();

    /// <summary>
    /// Parses a Claude Skills markdown document and extracts its metadata.
    /// </summary>
    /// <param name="fileName">The filename of the document</param>
    /// <param name="content">The markdown content</param>
    /// <returns>Parsed document with extracted metadata</returns>
    public IClaudeSkillsMarkdownParser.ParsedDocument ParseDocument(string fileName, string content)
    {
        var frontmatter = ExtractFrontmatter(content);
        
        var name = frontmatter.TryGetValue("name", out var n) ? n : Path.GetFileNameWithoutExtension(fileName);
        var description = frontmatter.TryGetValue("description", out var d) ? d : "No description available";

        return new IClaudeSkillsMarkdownParser.ParsedDocument(
            FileName: fileName,
            Name: name,
            Description: description,
            Metadata: frontmatter,
            Content: content
        );
    }

    /// <summary>
    /// Extracts YAML frontmatter from a markdown document.
    /// </summary>
    /// <param name="content">The markdown content</param>
    /// <returns>A dictionary of frontmatter key-value pairs, or empty if no frontmatter found</returns>
    public Dictionary<string, string> ExtractFrontmatter(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        var match = FrontmatterRegex.Match(content);
        if (!match.Success)
        {
            return result;
        }

        var frontmatter = match.Groups[1].Value;
        var lines = frontmatter.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Removes the frontmatter from markdown content.
    /// </summary>
    /// <param name="content">The markdown content</param>
    /// <returns>Content without frontmatter</returns>
    public string RemoveFrontmatter(string content)
    {
        return FrontmatterRegex.Replace(content, string.Empty);
    }
}
