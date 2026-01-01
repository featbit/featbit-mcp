using System.Reflection;
using System.Text.Json;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Default implementation of IDocumentLoader that loads documents from embedded resources or file system.
/// This is the standard implementation for loading markdown documentation bundled with the application.
/// </summary>
public class ResourcesDocumentLoader : IDocumentLoader
{
    /// <summary>
    /// Loads available documents and extracts descriptions from markdown files.
    /// </summary>
    public IDocumentLoader.DocumentOption[] LoadAvailableDocuments(string[] documentFiles, string resourceSubPath)
    {
        var documents = new List<IDocumentLoader.DocumentOption>();

        foreach (var fileName in documentFiles)
        {
            var description = ExtractDescriptionFromMarkdown(fileName, resourceSubPath);
            documents.Add(new IDocumentLoader.DocumentOption(fileName, description));
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

    /// <summary>
    /// Discovers all available document filenames in the specified resource path.
    /// </summary>
    public string[] DiscoverDocuments(string resourceSubPath, string filePattern = "*")
    {
        var fileNames = new List<string>();
        var extension = filePattern.StartsWith("*.") ? filePattern.Substring(1) : "";

        // First, try to discover from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith($"FeatBit.McpServer.Resources.{resourceSubPath}.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Filter by extension if specified
        if (!string.IsNullOrEmpty(extension))
        {
            resourceNames = resourceNames.Where(r => r.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (resourceNames.Count > 0)
        {
            // Extract filenames from resource names
            foreach (var resourceName in resourceNames)
            {
                // Example: FeatBit.McpServer.Resources.Deployments.README.md -> README.md
                var parts = resourceName.Split('.');
                if (parts.Length >= 2)
                {
                    var fileName = string.Join(".", parts.Skip(parts.Length - 2));
                    if (!string.IsNullOrEmpty(extension) && fileName.EndsWith(extension))
                    {
                        fileNames.Add(fileName);
                    }
                    else if (string.IsNullOrEmpty(extension))
                    {
                        fileNames.Add(fileName);
                    }
                }
            }

            return [.. fileNames];
        }

        // Fallback: discover from file system
        var executablePath = AppContext.BaseDirectory;
        var pathParts = resourceSubPath.Split('.');
        var resourcePath = Path.Combine(executablePath, "Resources", Path.Combine(pathParts));
        
        if (Directory.Exists(resourcePath))
        {
            var searchPattern = string.IsNullOrEmpty(filePattern) || filePattern == "*" ? "*.*" : filePattern;
            var files = Directory.GetFiles(resourcePath, searchPattern, SearchOption.TopDirectoryOnly);
            fileNames.AddRange(files.Select(Path.GetFileName).Where(f => f != null).Cast<string>());
        }

        return [.. fileNames];
    }

    /// <summary>
    /// Loads and deserializes a JSON configuration file from embedded resources or file system.
    /// </summary>
    public T? LoadJsonConfiguration<T>(string fileName, string resourceSubPath) where T : class
    {
        try
        {
            var content = LoadDocumentContent(fileName, resourceSubPath);
            
            // Check if content is an error message
            if (content.StartsWith("❌"))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
