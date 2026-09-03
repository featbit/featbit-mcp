using System.Net.Http.Headers;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FeatBit.McpServer.E2ETests;

internal sealed class McpConnection : IAsyncDisposable
{
    private readonly DynamicMcpHeadersHandler _headersHandler;
    private readonly HttpClient _httpClient;
    private readonly McpClient _client;

    private McpConnection(
        DynamicMcpHeadersHandler headersHandler,
        HttpClient httpClient,
        McpClient client)
    {
        _headersHandler = headersHandler;
        _httpClient = httpClient;
        _client = client;
    }

    public string ServerName => _client.ServerInfo.Name;

    public string ServerVersion => _client.ServerInfo.Version;

    public bool SawMcpSessionId => _headersHandler.SawMcpSessionId;

    public static async Task<McpConnection> ConnectAsync(
        Uri mcpUrl,
        FeatBitConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var headersHandler = new DynamicMcpHeadersHandler(configuration)
        {
            InnerHandler = new SocketsHttpHandler { AllowAutoRedirect = false }
        };
        var httpClient = new HttpClient(headersHandler)
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = mcpUrl,
                Name = "featbit-mcp-e2e",
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = TimeSpan.FromSeconds(30)
            },
            httpClient);

        try
        {
            var client = await McpClient.CreateAsync(
                transport,
                new McpClientOptions
                {
                    ClientInfo = new Implementation
                    {
                        Name = "featbit-mcp-e2e-tests",
                        Version = "1.0.0"
                    },
                    InitializationTimeout = TimeSpan.FromSeconds(30)
                },
                cancellationToken: cancellationToken);

            return new McpConnection(headersHandler, httpClient, client);
        }
        catch
        {
            await transport.DisposeAsync();
            httpClient.Dispose();
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListToolNamesAsync(CancellationToken cancellationToken)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(tool => tool.Name).Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<JsonElement> CallJsonAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            toolName,
            arguments,
            cancellationToken: cancellationToken);

        if (result.IsError == true)
            throw new InvalidOperationException($"MCP tool '{toolName}' returned an MCP error result.");

        foreach (var text in result.Content.OfType<TextContentBlock>().Select(block => block.Text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
            }
        }

        if (result.StructuredContent is { } structuredContent)
            return structuredContent.Clone();

        throw new InvalidOperationException(
            $"MCP tool '{toolName}' did not return a JSON text or structured result.");
    }

    public async Task<JsonElement> CallEvaluationJsonAsync(
        string environmentSecret,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        _headersHandler.EnvironmentSecret = environmentSecret;
        try
        {
            return await CallJsonAsync("evaluate_feature_flags", arguments, cancellationToken);
        }
        finally
        {
            _headersHandler.EnvironmentSecret = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _client.DisposeAsync();
        }
        finally
        {
            _httpClient.Dispose();
        }
    }

    private sealed class DynamicMcpHeadersHandler(FeatBitConfiguration configuration)
        : DelegatingHandler
    {
        private int _sawMcpSessionId;

        public string? EnvironmentSecret { get; set; }

        public bool SawMcpSessionId => Volatile.Read(ref _sawMcpSessionId) == 1;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Remove("Authorization");
            request.Headers.Remove("Organization");
            request.Headers.Remove("Workspace");
            request.Headers.Remove("X-FeatBit-Env-Secret");
            request.Headers.TryAddWithoutValidation("Authorization", configuration.Authorization);
            request.Headers.TryAddWithoutValidation("Organization", configuration.Organization);
            if (!string.IsNullOrWhiteSpace(configuration.Workspace))
                request.Headers.TryAddWithoutValidation("Workspace", configuration.Workspace);
            if (!string.IsNullOrWhiteSpace(EnvironmentSecret))
                request.Headers.TryAddWithoutValidation("X-FeatBit-Env-Secret", EnvironmentSecret);

            if (!request.Headers.Accept.Any(value => value.MediaType == "application/json"))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!request.Headers.Accept.Any(value => value.MediaType == "text/event-stream"))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await base.SendAsync(request, cancellationToken);
            if (response.Headers.Contains("Mcp-Session-Id"))
                Interlocked.Exchange(ref _sawMcpSessionId, 1);

            return response;
        }
    }
}
