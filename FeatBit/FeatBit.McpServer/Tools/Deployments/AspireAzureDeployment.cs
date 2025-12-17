using System.Reflection;

namespace FeatBit.McpServer.Tools.Deployments;

public static class AspireAzureDeployment
{
  private const string TutorialResourcePath = "FeatBit.McpServer.Resources.Deployments.AspireAzureDeployment.md";

  public static DeploymentInfo Info => new(
      Name: ".NET Aspire Azure Deployment",
      Platform: "Azure",
      Type: "Aspire",
      Description: "Deploy FeatBit to Azure using .NET Aspire for cloud-native applications",
      Prerequisites: ["Azure subscription", "Azure Developer CLI (azd)", ".NET 10", "PostgreSQL or MongoDB database", "Redis instance"],
      Documentation: "https://github.com/featbit/featbit-aspire",
      Difficulty: "Easy"
  );

  public static string GetTutorial()
  {
    // Try to read from embedded resource first
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(TutorialResourcePath);

    if (stream != null)
    {
      using var reader = new StreamReader(stream);
      return reader.ReadToEnd();
    }

    // Fallback: try to read from file system (for development/runtime updates)
    var executablePath = AppContext.BaseDirectory;
    var markdownPath = Path.Combine(executablePath, "Resources", "Deployments", "AspireAzureDeployment.md");

    if (File.Exists(markdownPath))
    {
      return File.ReadAllText(markdownPath);
    }

    // Final fallback: return inline content
    return "NOT IMPLEMENTED";
  }
}
