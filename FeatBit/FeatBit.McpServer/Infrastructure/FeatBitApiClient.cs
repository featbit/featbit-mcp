using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// HTTP client for interacting with FeatBit REST API
/// Handles authentication, request/response serialization, and error handling
/// </summary>
public class FeatBitApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeatBitApiClient> _logger;
    private readonly string? _apiKey;
    private readonly string? _jwtToken;

    public FeatBitApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FeatBitApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Load configuration
        var baseUrl = configuration["FeatBitApi:BaseUrl"] ?? "https://app.featbit.co";
        _apiKey = configuration["FeatBitApi:ApiKey"];
        _jwtToken = configuration["FeatBitApi:JwtToken"];

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Note: Authentication headers are now set per-request to support dynamic API keys
    }

    /// <summary>
    /// Send GET request to FeatBit API
    /// </summary>
    public async Task<FeatBitApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Sending GET request to {Endpoint}", endpoint);
            
            using var request = CreateRequest(HttpMethod.Get, endpoint, apiKey);
            var response = await _httpClient.SendAsync(request);
            return await ProcessResponseAsync<TResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending GET request to {Endpoint}", endpoint);
            return new FeatBitApiResponse<TResponse>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Send POST request to FeatBit API
    /// </summary>
    public async Task<FeatBitApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Sending POST request to {Endpoint}", endpoint);
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Post, endpoint, apiKey);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            
            return await ProcessResponseAsync<TResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending POST request to {Endpoint}", endpoint);
            return new FeatBitApiResponse<TResponse>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Send PUT request to FeatBit API
    /// </summary>
    public async Task<FeatBitApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Sending PUT request to {Endpoint}", endpoint);
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Put, endpoint, apiKey);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            
            return await ProcessResponseAsync<TResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PUT request to {Endpoint}", endpoint);
            return new FeatBitApiResponse<TResponse>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Send PATCH request to FeatBit API
    /// </summary>
    public async Task<FeatBitApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Sending PATCH request to {Endpoint}", endpoint);
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Patch, endpoint, apiKey);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            
            return await ProcessResponseAsync<TResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PATCH request to {Endpoint}", endpoint);
            return new FeatBitApiResponse<TResponse>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Send DELETE request to FeatBit API
    /// </summary>
    public async Task<FeatBitApiResponse<object>> DeleteAsync(string endpoint, string? apiKey = null)
    {
        try
        {
            _logger.LogInformation("Sending DELETE request to {Endpoint}", endpoint);
            
            using var request = CreateRequest(HttpMethod.Delete, endpoint, apiKey);
            var response = await _httpClient.SendAsync(request);
            return await ProcessResponseAsync<object>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending DELETE request to {Endpoint}", endpoint);
            return new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError { Message = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Create HTTP request with authentication header
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, string? apiKey = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        // Use per-request API key if provided, otherwise fall back to configured credentials
        var effectiveApiKey = apiKey ?? _apiKey;
        
        if (!string.IsNullOrEmpty(effectiveApiKey))
        {
            // OpenAPI Key authentication - send directly without scheme
            request.Headers.TryAddWithoutValidation("Authorization", effectiveApiKey);
        }
        else if (!string.IsNullOrEmpty(_jwtToken))
        {
            // JWT Bearer Token authentication
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        }
        
        return request;
    }

    /// <summary>
    /// Process HTTP response and deserialize to FeatBit API response format
    /// </summary>
    private async Task<FeatBitApiResponse<TResponse>> ProcessResponseAsync<TResponse>(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        
        _logger.LogDebug("Response Status: {StatusCode}, Body: {Body}", 
            response.StatusCode, responseBody);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var apiResponse = JsonSerializer.Deserialize<FeatBitApiResponse<TResponse>>(responseBody, 
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                
                return apiResponse ?? new FeatBitApiResponse<TResponse>
                {
                    Success = false,
                    Errors = new[] { new FeatBitApiError { Message = "Failed to deserialize response" } }
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing response");
                return new FeatBitApiResponse<TResponse>
                {
                    Success = false,
                    Errors = new[] { new FeatBitApiError { Message = $"Deserialization error: {ex.Message}" } }
                };
            }
        }
        else
        {
            // Try to parse error response
            try
            {
                var errorResponse = JsonSerializer.Deserialize<FeatBitApiResponse<TResponse>>(responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                
                return errorResponse ?? new FeatBitApiResponse<TResponse>
                {
                    Success = false,
                    Errors = new[] { new FeatBitApiError 
                    { 
                        Message = $"HTTP {response.StatusCode}: {responseBody}" 
                    } }
                };
            }
            catch
            {
                return new FeatBitApiResponse<TResponse>
                {
                    Success = false,
                    Errors = new[] { new FeatBitApiError 
                    { 
                        Message = $"HTTP {response.StatusCode}: {responseBody}" 
                    } }
                };
            }
        }
    }
}

/// <summary>
/// Standard FeatBit API response wrapper
/// </summary>
public class FeatBitApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public FeatBitApiError[]? Errors { get; set; }
}

/// <summary>
/// FeatBit API error details
/// </summary>
public class FeatBitApiError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
