using FeatBit.McpServer.Infrastructure;

namespace FeatBit.McpServer.Domain.Sdks;

public class JavascriptSdks
{
    private readonly IDocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.JavascriptSdks";

    /// <summary>
    /// Creates a new instance of JavascriptSdks with dependency injection.
    /// </summary>
    public JavascriptSdks(IDocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    /// <summary>
    /// Maps SDK identifiers to their corresponding markdown files.
    /// </summary>
    private static readonly Dictionary<string, string[]> SdkFileMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "javascript-client-sdk", ["featbit-js-client-sdk.md"] },
        { "typescript-client-sdk", ["featbit-js-client-sdk.md"] },
        { "react-webapp-sdk", ["featbit-react-client-sdk.md"] },
        { "react-native-sdk", ["featbit-react-native-client-sdk.md"] },
        { "node-sdk", ["featbit-node-server-sdk.md"] },
        { "openfeature-js-client-sdk", ["featbit-openfeature-provider-js-client.md",
        "featbit-js-client-sdk.md"] },
        { "openfeature-node-sdk", ["featbit-openfeature-provider-node-server.md", "featbit-node-server-sdk.md"] }
    };

    public string GenerateSdkGuide(string sdk)
    {
        // Get the document file(s) for the specified SDK
        if (!SdkFileMapping.TryGetValue(sdk, out var documentFiles))
        {
            return $"❌ Unknown JavaScript SDK: '{sdk}'. Available SDKs: {string.Join(", ", SdkFileMapping.Keys)}";
        }

        // If SDK has multiple documents, concatenate them
        if (documentFiles.Length > 1)
        {
            var contents = documentFiles
                .Select(file => _documentLoader.LoadDocumentContent(file, ResourceSubPath))
                .ToArray();

            return string.Join("\n\n---\n\n", contents);
        }

        // Single document - load directly
        return _documentLoader.LoadDocumentContent(documentFiles[0], ResourceSubPath);
    }
}
