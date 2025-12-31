using FeatBit.McpServer.Infrastructure;

namespace FeatBit.McpServer.Domain.Sdks;

public class PythonSdks
{
    private readonly IDocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.PythonSdks";

    /// <summary>
    /// Creates a new instance of PythonSdks with dependency injection.
    /// </summary>
    public PythonSdks(IDocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-java-sdk.md", ResourceSubPath);
    }
}
