namespace FeatBit.McpServer.Tools.Sdks;

/// <summary>
/// FeatBit SDK Information
/// </summary>
/// <param name="Platform">Platform/Programming Language</param>
/// <param name="Type">SDK Type: Client (client-side) or Server (server-side)</param>
/// <param name="PackageName">Package Name</param>
/// <param name="InstallCommand">Installation Command</param>
/// <param name="Repository">GitHub Repository URL</param>
/// <param name="Documentation">Documentation URL</param>
public record SdkInfo(
    string Platform,
    string Type,
    string PackageName,
    string InstallCommand,
    string Repository,
    string Documentation
);
