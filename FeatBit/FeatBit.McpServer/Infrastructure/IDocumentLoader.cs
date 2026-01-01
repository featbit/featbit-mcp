namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Interface for loading documentation files from various sources.
/// Implementations can load from embedded resources, file system, S3, HTTP, database, etc.
/// </summary>
public interface IDocumentLoader
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
    /// <param name="documentFiles">Array of document file names to load</param>
    /// <param name="resourceSubPath">Resource sub-path or category identifier</param>
    /// <returns>Array of document options with metadata</returns>
    DocumentOption[] LoadAvailableDocuments(string[] documentFiles, string resourceSubPath);

    /// <summary>
    /// Extracts the description from the markdown file's YAML front matter.
    /// Format:
    /// ---
    /// description: "description text"
    /// ---
    /// </summary>
    /// <param name="fileName">The markdown file name</param>
    /// <param name="resourceSubPath">Resource sub-path or category identifier</param>
    /// <returns>Description text or file name if not found</returns>
    string ExtractDescriptionFromMarkdown(string fileName, string resourceSubPath);

    /// <summary>
    /// Loads document content from the underlying storage.
    /// </summary>
    /// <param name="fileName">The document file name</param>
    /// <param name="resourceSubPath">Resource sub-path or category identifier</param>
    /// <returns>The document content as string</returns>
    string LoadDocumentContent(string fileName, string resourceSubPath);

    /// <summary>
    /// Discovers all available document filenames in the specified resource path.
    /// </summary>
    /// <param name="resourceSubPath">Resource sub-path or category identifier</param>
    /// <param name="filePattern">File pattern to match (e.g., "*.md"). Defaults to all files.</param>
    /// <returns>Array of discovered document filenames</returns>
    string[] DiscoverDocuments(string resourceSubPath, string filePattern = "*");

    /// <summary>
    /// Loads and deserializes a JSON configuration file from the specified resource path.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON content into</typeparam>
    /// <param name="fileName">The JSON file name</param>
    /// <param name="resourceSubPath">Resource sub-path or category identifier</param>
    /// <returns>Deserialized object of type T, or null if file not found or deserialization fails</returns>
    T? LoadJsonConfiguration<T>(string fileName, string resourceSubPath) where T : class;
}
