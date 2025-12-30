namespace FeatBit.McpServer.Tools.Sdks;

public class PythonSdks
{
    private readonly DocumentLoader _documentLoader;
    private const string ResourceSubPath = "Sdks.PythonSdks";

    /// <summary>
    /// Creates a new instance of JavaSdks with dependency injection.
    /// </summary>
    public PythonSdks(DocumentLoader documentLoader)
    {
        _documentLoader = documentLoader;
    }

    public string GenerateSdkGuide(string sdk)
    {
        return _documentLoader.LoadDocumentContent("featbit-java-sdk.md", ResourceSubPath);
    }
}
