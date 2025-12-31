using FeatBit.McpServer.Infrastructure;

namespace FeatBit.McpServer.Domain.Sdks;

public class GoSdks
{
    private readonly IDocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.GoSdks";

    /// <summary>
    /// Creates a new instance of GoSdks with dependency injection.
    /// </summary>
    public GoSdks(IDocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-go-sdk.md", ResourceSubPath);
    }
}
