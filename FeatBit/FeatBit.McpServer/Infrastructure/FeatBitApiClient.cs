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
    private readonly string _evalUrl;

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
        _evalUrl = (configuration["FeatBit:EventUri"] ?? "https://app-eval.featbit.co").TrimEnd('/');
    }

    public async Task<string> GetAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("GET {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            return await SafeReadResponseAsync(response, "GET", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            return InternalErrorJson;
        }
    }

    public async Task<string> PutAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("PUT {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Put, endpoint);
            var response = await _httpClient.SendAsync(request);
            return await SafeReadResponseAsync(response, "PUT", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            return InternalErrorJson;
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
            return await SafeReadResponseAsync(response, "POST", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return InternalErrorJson;
        }
    }

    public async Task<string> PatchAsync(string endpoint, string jsonBody)
    {
        try
        {
            _logger.LogInformation("PATCH {Endpoint}", endpoint);
            using var request = CreateRequest(HttpMethod.Patch, endpoint);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            return await SafeReadResponseAsync(response, "PATCH", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PATCH {Endpoint}", endpoint);
            return InternalErrorJson;
        }
    }

    public async Task<string> EvaluateAsync(string jsonBody)
    {
        const string path = "/api/public/featureflag/evaluate";
        var fullUrl = _evalUrl + path;
        try
        {
            _logger.LogInformation("POST (eval) {Endpoint}", path);
            using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

            // Eval API authenticates with the environment secret key (plain, no "Bearer" prefix)
            // forwarded from the X-FeatBit-Env-Secret header of the incoming MCP request
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx?.Request.Headers.TryGetValue("X-FeatBit-Env-Secret", out var secret) == true
                && !StringValues.IsNullOrEmpty(secret))
                request.Headers.TryAddWithoutValidation("Authorization", (string?)secret);

            var response = await _httpClient.SendAsync(request);
            return await SafeReadResponseAsync(response, "POST (eval)", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST (eval) {Endpoint}", path);
            return InternalErrorJson;
        }
    }

    // Sanitize HTTP responses: successful responses pass through; error responses are
    // logged in full (for server-side diagnostics) but only a generic message is returned
    // to the caller so that internal details never reach the MCP client.
    private static readonly string InternalErrorJson =
        JsonSerializer.Serialize(new { error = "An internal server error occurred." });

    private async Task<string> SafeReadResponseAsync(HttpResponseMessage response, string method, string endpoint)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
            return body;

        _logger.LogWarning(
            "{Method} {Endpoint} returned {StatusCode}: {Body}",
            method, endpoint, (int)response.StatusCode, body);
        return InternalErrorJson;
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
