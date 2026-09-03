using System.Text.Json;

namespace FeatBit.McpServer.E2ETests;

internal sealed record FeatBitConfiguration(
    Uri ApiBaseUrl,
    string Authorization,
    string Organization,
    string? Workspace)
{
    public static FeatBitConfiguration CreatePreflight()
        => new(
            new Uri("https://app-api.featbit.co"),
            "preflight-not-a-credential",
            "00000000-0000-0000-0000-000000000000",
            null);

    public static async Task<FeatBitConfiguration> LoadAsync(
        string path,
        string? tokenEnvironmentVariable,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"FeatBit configuration was not found at {path}.");

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<ConfigurationDocument>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        if (document is null)
            throw new InvalidOperationException("FeatBit configuration is empty or invalid JSON.");

        if (!Uri.TryCreate(document.Host, UriKind.Absolute, out var apiBaseUrl) ||
            apiBaseUrl.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(apiBaseUrl.Host, "app-api.featbit.co", StringComparison.OrdinalIgnoreCase) ||
            !apiBaseUrl.IsDefaultPort ||
            apiBaseUrl.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(apiBaseUrl.Query) ||
            !string.IsNullOrEmpty(apiBaseUrl.Fragment) ||
            !string.IsNullOrEmpty(apiBaseUrl.UserInfo))
        {
            throw new InvalidOperationException(
                "The live E2E configuration host must be exactly https://app-api.featbit.co.");
        }

        var token = document.Token;
        if (!string.IsNullOrWhiteSpace(tokenEnvironmentVariable))
        {
            token = Environment.GetEnvironmentVariable(tokenEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{tokenEnvironmentVariable}' is missing or empty.");
            }
        }

        ValidateHeaderValue(token, "token");
        ValidateHeaderValue(document.Organization, "organization");
        if (!string.IsNullOrEmpty(document.Workspace))
            ValidateHeaderValue(document.Workspace, "workspace");

        return new FeatBitConfiguration(
            apiBaseUrl,
            token!,
            document.Organization!,
            document.Workspace);
    }

    private static void ValidateHeaderValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value != value.Trim() ||
            value.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"FeatBit configuration property '{name}' is missing or invalid.");
        }
    }

    private sealed record ConfigurationDocument(
        string? Host,
        string? Token,
        string? Organization,
        string? Workspace);
}
