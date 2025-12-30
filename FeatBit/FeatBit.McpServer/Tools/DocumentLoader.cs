using System.Reflection;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// Provides functionality for loading documentation files.
/// This service handles loading documents from embedded resources or file system,
/// and extracting metadata from markdown files.
/// </summary>
public class DocumentLoader
{
    /// <summary>
    /// Describes a documentation file with metadata.
    /// </summary>
    public record DocumentOption(
        string FileName,
        string Description
    );

    /// <summary>
    /// Loads available documents and extracts descriptions from markdown files.
    /// </summary>
    public DocumentOption[] LoadAvailableDocuments(string[] documentFiles, string resourceSubPath)
    {
        var documents = new List<DocumentOption>();

        foreach (var fileName in documentFiles)
        {
            var description = ExtractDescriptionFromMarkdown(fileName, resourceSubPath);
            documents.Add(new DocumentOption(fileName, description));
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
    public string ExtractDescriptionFromMarkdown(string fileName, string resourceSubPath)
    {
        try
        {
            var content = LoadDocumentContent(fileName, resourceSubPath);
            
            // Check if file starts with YAML front matter (---)
            if (!content.TrimStart().StartsWith("---"))
            {
                return fileName;
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

        return fileName;
    }

    /// <summary>
    /// Loads document content from embedded resources or file system.
    /// </summary>
    /// <param name="fileName">The markdown file name</param>
    /// <param name="resourceSubPath">The resource sub-path (e.g., "Sdks.DotNETSdk" or "Sdks.JavascriptSdk")</param>
    public string LoadDocumentContent(string fileName, string resourceSubPath)
    {
        var resourcePath = $"FeatBit.McpServer.Resources.{resourceSubPath}.{fileName}";
        
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
        var pathParts = resourceSubPath.Split('.');
        var markdownPath = Path.Combine(executablePath, "Resources", Path.Combine(pathParts), fileName);

        if (File.Exists(markdownPath))
        {
            return File.ReadAllText(markdownPath);
        }

        // Final fallback
        return $"❌ SDK documentation '{fileName}' not found. Please ensure the resource file is properly embedded.";
    }
}
