namespace FeatBit.McpServer.Tools.Sdks;

public class GoSdks
{
    private readonly DocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.GoSdks";

    /// <summary>
    /// Creates a new instance of JavaSdks with dependency injection.
    /// </summary>
    public GoSdks(DocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-go-sdk.md", ResourceSubPath);
    }
}
