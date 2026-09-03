namespace FeatBit.McpServer.E2ETests;

internal static class RepositoryLocator
{
    public static string FindRoot()
    {
        foreach (var candidate in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(candidate);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "FeatBit", "FeatBit.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException(
            "Could not locate the repository root containing FeatBit/FeatBit.sln.");
    }
}
