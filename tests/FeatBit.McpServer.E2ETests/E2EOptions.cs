namespace FeatBit.McpServer.E2ETests;

internal sealed record E2EOptions(
    bool Execute,
    bool Preflight,
    bool ShowHelp,
    bool UseExistingServer,
    Uri McpUrl,
    string ConfigPath,
    string? TokenEnvironmentVariable,
    string? ReportDirectory)
{
    private const string DefaultMcpUrl = "http://localhost:5180/mcp";

    public static E2EOptions Parse(string[] args)
    {
        var execute = false;
        var preflight = false;
        var showHelp = false;
        var useExistingServer = false;
        var mcpUrl = new Uri(DefaultMcpUrl);
        var configPath = GetDefaultConfigPath();
        string? tokenEnvironmentVariable = null;
        string? reportDirectory = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--execute":
                    execute = true;
                    break;
                case "--preflight":
                    preflight = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--use-existing-server":
                    useExistingServer = true;
                    break;
                case "--mcp-url":
                    mcpUrl = ParseMcpUrl(ReadValue(args, ref index, "--mcp-url"));
                    break;
                case "--config":
                    configPath = Path.GetFullPath(ReadValue(args, ref index, "--config"));
                    break;
                case "--token-env":
                    tokenEnvironmentVariable = ReadEnvironmentVariableName(
                        ReadValue(args, ref index, "--token-env"));
                    break;
                case "--report-directory":
                    reportDirectory = Path.GetFullPath(ReadValue(args, ref index, "--report-directory"));
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {args[index]}");
            }
        }

        if (execute && preflight)
            throw new ArgumentException("--execute and --preflight cannot be used together.");

        return new E2EOptions(
            execute,
            preflight,
            showHelp,
            useExistingServer,
            mcpUrl,
            configPath,
            tokenEnvironmentVariable,
            reportDirectory);
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("FeatBit MCP Server live SaaS E2E runner");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine(
            "  dotnet run --project tests/FeatBit.McpServer.E2ETests -- --execute [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --execute                 Required acknowledgement for SaaS mutations.");
        writer.WriteLine("  --preflight               Build, initialize MCP, and list tools without SaaS credentials.");
        writer.WriteLine("  --use-existing-server     Connect to an already running local MCP server.");
        writer.WriteLine($"  --mcp-url <url>           Local MCP endpoint. Default: {DefaultMcpUrl}");
        writer.WriteLine("  --config <path>           FeatBit credential file path.");
        writer.WriteLine("  --token-env <name>        Read Authorization from the named environment variable.");
        writer.WriteLine("  --report-directory <dir>  Report output directory.");
        writer.WriteLine("  -h, --help                Show this help.");
        writer.WriteLine();
        writer.WriteLine($"Default config: {GetDefaultConfigPath()}");
        writer.WriteLine();
        writer.WriteLine("The runner never deletes projects/environments and never archives flags as cleanup.");
        writer.WriteLine("It pauses for explicit approval before the single archive_feature_flag test call.");
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        index++;
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException($"{option} requires a value.");

        return args[index];
    }

    private static Uri ParseMcpUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("--mcp-url must be an absolute HTTP or HTTPS URL.");
        }

        if (!uri.IsLoopback)
        {
            throw new ArgumentException(
                "--mcp-url must resolve to loopback. The runner will not send FeatBit credentials to a remote MCP server.");
        }

        if (!string.Equals(uri.AbsolutePath.TrimEnd('/'), "/mcp", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("--mcp-url must use the /mcp path without query, fragment, or user information.");
        }

        return uri;
    }

    private static string ReadEnvironmentVariableName(string value)
    {
        if (value.Any(char.IsWhiteSpace) || value.Contains('=', StringComparison.Ordinal))
            throw new ArgumentException("--token-env must be a valid environment variable name.");

        return value;
    }

    private static string GetDefaultConfigPath()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(applicationData, "featbit", "config.json");
    }
}
