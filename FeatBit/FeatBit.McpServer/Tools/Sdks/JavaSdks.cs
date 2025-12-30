namespace FeatBit.McpServer.Tools.Sdks;

public class JavaSdks
{
    private readonly DocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.JavaSdks";

    /// <summary>
    /// Creates a new instance of JavaSdks with dependency injection.
    /// </summary>
    public JavaSdks(DocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-java-sdk.md", ResourceSubPath);
    }
}
