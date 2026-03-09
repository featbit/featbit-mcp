using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// HTTP client for interacting with FeatBit REST API.
/// Credentials (Authorization, Organization, Workspace) are forwarded from the
/// incoming MCP HTTP request headers — no per-tool apiKey parameter needed.
/// </summary>
public class FeatBitApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FeatBitApiClient> _logger;

    public FeatBitApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<FeatBitApiClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        var baseUrl = configuration["FeatBitApi:BaseUrl"] ?? "https://app-api.featbit.co";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("GET {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    public async Task<string> PutAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("PUT {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Put, endpoint);
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    public async Task<string> PostAsync(string endpoint, string jsonBody)
    {
        try
        {
            _logger.LogInformation("POST {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Post, endpoint);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);

        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return request;

        // Forward auth and context headers from the incoming MCP request
        foreach (var header in new[] { "Authorization", "Organization", "Workspace" })
        {
            if (ctx.Request.Headers.TryGetValue(header, out var value) && !StringValues.IsNullOrEmpty(value))
                request.Headers.TryAddWithoutValidation(header, (string?)value);
        }

        return request;
    }
}
