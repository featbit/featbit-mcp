using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// OpenTelemetry middleware for IChatClient that records GenAI semantic conventions.
/// This ensures complete telemetry including assistant responses in Aspire Dashboard.
/// Uses the OpenTelemetry GenAI semantic conventions format expected by Aspire.
/// </summary>
public class ChatClientOpenTelemetryMiddleware(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private static readonly ActivitySource ActivitySource = new("Microsoft.Extensions.AI");

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("chat", ActivityKind.Client);
        
        if (activity != null)
        {
            var messagesList = chatMessages.ToList();
            
            // Set GenAI semantic convention attributes
            activity.SetTag("gen_ai.system", "azure_openai");
            activity.SetTag("gen_ai.operation.name", "chat");
            
            // Capture input messages in OpenTelemetry GenAI format
            if (ShouldCaptureContent())
            {
                var inputMessages = messagesList.Select(m => new
                {
                    role = m.Role.Value,
                    parts = new[]
                    {
                        new
                        {
                            type = "text",
                            content = m.Text ?? string.Empty
                        }
                    }
                }).ToArray();
                
                var inputJson = JsonSerializer.Serialize(inputMessages);
                activity.SetTag("gen_ai.input.messages", inputJson);
            }

            try
            {
                var response = await base.GetResponseAsync(messagesList, options, cancellationToken);
                
                // Set model ID from response (more reliable than options)
                if (!string.IsNullOrEmpty(response.ModelId))
                {
                    activity.SetTag("gen_ai.request.model", response.ModelId);
                    activity.SetTag("gen_ai.response.model", response.ModelId);
                }
                
                // Capture response in OpenTelemetry GenAI format
                if (ShouldCaptureContent() && !string.IsNullOrEmpty(response.Text))
                {
                    var outputMessages = new[]
                    {
                        new
                        {
                            role = "assistant",
                            parts = new[]
                            {
                                new
                                {
                                    type = "text",
                                    content = response.Text
                                }
                            },
                            finish_reason = response.FinishReason.ToString()
                        }
                    };
                    
                    var outputJson = JsonSerializer.Serialize(outputMessages);
                    activity.SetTag("gen_ai.output.messages", outputJson);
                }
                
                // Capture usage information
                if (response?.Usage != null)
                {
                    activity.SetTag("gen_ai.usage.input_tokens", response.Usage.InputTokenCount);
                    activity.SetTag("gen_ai.usage.output_tokens", response.Usage.OutputTokenCount);
                    activity.SetTag("gen_ai.usage.total_tokens", response.Usage.TotalTokenCount);
                }
                
                activity.SetStatus(ActivityStatusCode.Ok);
                return response;
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        return await base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    private static bool ShouldCaptureContent()
    {
        // Check environment variable for content capture
        var envVar = Environment.GetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT");
        return string.Equals(envVar, "true", StringComparison.OrdinalIgnoreCase);
    }
}
