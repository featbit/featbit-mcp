using FeatBit.McpServer.Infrastructure;

namespace FeatBit.McpServer.Domain.Sdks;

public class JavaSdks
{
    private readonly IDocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.JavaSdks";

    /// <summary>
    /// Creates a new instance of JavaSdks with dependency injection.
    /// </summary>
    public JavaSdks(IDocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-java-sdk.md", ResourceSubPath);
    }
}
