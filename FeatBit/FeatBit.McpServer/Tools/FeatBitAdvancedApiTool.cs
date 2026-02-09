using System.ComponentModel;
using System.Text.Json;
using FeatBit.McpServer.Infrastructure;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

/// <summary>
/// Advanced FeatBit API Tool for custom and less common operations
/// This provides a fallback for API endpoints not covered by the core tools
/// </summary>
[McpServerToolType]
public class FeatBitAdvancedApiTool
{
    private readonly FeatBitApiClient _apiClient;
    private readonly ILogger<FeatBitAdvancedApiTool> _logger;

    public FeatBitAdvancedApiTool(
        FeatBitApiClient apiClient,
        ILogger<FeatBitAdvancedApiTool> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Call any FeatBit REST API endpoint for advanced scenarios not covered by the core tools. Use this for custom API operations, bulk operations, or accessing newer API endpoints. Supports GET, POST, PUT, PATCH, and DELETE methods.")]
    public async Task<string> CallAdvancedApi(
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE")]
        string method,
        [Description("API endpoint path starting with /api/v1/ (e.g., '/api/v1/projects', '/api/v1/envs/{envId}/feature-flags')")]
        string endpoint,
        [Description("Request body as JSON string (required for POST, PUT, PATCH; optional for GET and DELETE)")]
        string? bodyJson = null,
        [Description("Optional FeatBit API key for authentication. If not provided, uses configured API key.")]
        string? apiKey = null)
    {
        _logger.LogInformation("Calling advanced API: {Method} {Endpoint}", method, endpoint);

        // Validate method
        var upperMethod = method.ToUpperInvariant();
        if (upperMethod is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE"))
        {
            return JsonSerializer.Serialize(new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError 
                { 
                    Message = $"Invalid HTTP method: {method}. Must be GET, POST, PUT, PATCH, or DELETE" 
                } }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        // Validate endpoint starts with /api/
        if (!endpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError 
                { 
                    Message = "Endpoint must start with '/api/v1/'" 
                } }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        try
        {
            FeatBitApiResponse<object> response;

            switch (upperMethod)
            {
                case "GET":
                    response = await _apiClient.GetAsync<object>(endpoint, apiKey);
                    break;

                case "POST":
                    if (string.IsNullOrEmpty(bodyJson))
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "POST request requires a body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    
                    var postBody = JsonSerializer.Deserialize<object>(bodyJson);
                    if (postBody == null)
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "Failed to deserialize POST body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    response = await _apiClient.PostAsync<object, object>(endpoint, postBody, apiKey);
                    break;

                case "PUT":
                    if (string.IsNullOrEmpty(bodyJson))
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "PUT request requires a body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    
                    var putBody = JsonSerializer.Deserialize<object>(bodyJson);
                    if (putBody == null)
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "Failed to deserialize PUT body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    response = await _apiClient.PutAsync<object, object>(endpoint, putBody, apiKey);
                    break;

                case "PATCH":
                    if (string.IsNullOrEmpty(bodyJson))
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "PATCH request requires a body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    
                    var patchBody = JsonSerializer.Deserialize<object>(bodyJson);
                    if (patchBody == null)
                    {
                        return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                        {
                            Success = false,
                            Errors = new[] { new FeatBitApiError 
                            { 
                                Message = "Failed to deserialize PATCH body" 
                            } }
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    response = await _apiClient.PatchAsync<object, object>(endpoint, patchBody, apiKey);
                    break;

                case "DELETE":
                    response = await _apiClient.DeleteAsync(endpoint, apiKey);
                    break;

                default:
                    return JsonSerializer.Serialize(new FeatBitApiResponse<object>
                    {
                        Success = false,
                        Errors = new[] { new FeatBitApiError 
                        { 
                            Message = $"Unsupported method: {method}" 
                        } }
                    }, new JsonSerializerOptions { WriteIndented = true });
            }

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError 
                { 
                    Message = $"Invalid JSON in request body: {ex.Message}" 
                } }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling advanced API: {Method} {Endpoint}", method, endpoint);
            return JsonSerializer.Serialize(new FeatBitApiResponse<object>
            {
                Success = false,
                Errors = new[] { new FeatBitApiError 
                { 
                    Message = $"Error calling API: {ex.Message}" 
                } }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
