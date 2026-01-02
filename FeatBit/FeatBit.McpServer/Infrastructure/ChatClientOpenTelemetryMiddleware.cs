using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FeatBit.McpServer.Infrastructure;

/// <summary>
/// Custom OpenTelemetry middleware that manually records AI chat interactions with full details.
/// Records: system prompts, user prompts, responses, token usage, and function calls.
/// </summary>
public class ChatClientOpenTelemetryMiddleware : DelegatingChatClient
{
    private static readonly ActivitySource ActivitySource = new("Microsoft.Extensions.AI");
    private readonly ILogger _logger;

    public ChatClientOpenTelemetryMiddleware(IChatClient innerClient, ILogger logger) 
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("chat", ActivityKind.Client);
        
        if (activity != null)
        {
            try
            {
                // Record model and provider
                var modelId = options?.ModelId ?? "unknown";
                activity.SetTag("gen_ai.request.model", modelId);
                activity.SetTag("gen_ai.system", DetermineProvider(modelId));

                // Record chat options
                if (options != null)
                {
                    if (options.Temperature.HasValue)
                        activity.SetTag("gen_ai.request.temperature", options.Temperature.Value);
                    
                    if (options.MaxOutputTokens.HasValue)
                        activity.SetTag("gen_ai.request.max_tokens", options.MaxOutputTokens.Value);
                    
                    if (options.TopP.HasValue)
                        activity.SetTag("gen_ai.request.top_p", options.TopP.Value);
                    
                    if (options.FrequencyPenalty.HasValue)
                        activity.SetTag("gen_ai.request.frequency_penalty", options.FrequencyPenalty.Value);
                    
                    if (options.PresencePenalty.HasValue)
                        activity.SetTag("gen_ai.request.presence_penalty", options.PresencePenalty.Value);
                    
                    if (options.ResponseFormat != null)
                        activity.SetTag("gen_ai.request.response_format", options.ResponseFormat.ToString());
                }

                // Record all messages (prompts)
                var messagesList = chatMessages.ToList();
                for (int i = 0; i < messagesList.Count; i++)
                {
                    var message = messagesList[i];
                    activity.SetTag($"gen_ai.prompt.{i}.role", message.Role.Value);
                    
                    // Record the actual content
                    var contentText = GetMessageContent(message);
                    if (!string.IsNullOrEmpty(contentText))
                    {
                        activity.SetTag($"gen_ai.prompt.{i}.content", contentText);
                    }

                    // Record function calls if present
                    if (message.Contents != null)
                    {
                        var functionCalls = message.Contents
                            .OfType<FunctionCallContent>()
                            .ToList();

                        for (int j = 0; j < functionCalls.Count; j++)
                        {
                            var call = functionCalls[j];
                            activity.SetTag($"gen_ai.prompt.{i}.function_call.{j}.name", call.Name);
                            
                            if (call.Arguments != null)
                            {
                                var argsJson = JsonSerializer.Serialize(call.Arguments);
                                activity.SetTag($"gen_ai.prompt.{i}.function_call.{j}.arguments", argsJson);
                            }
                        }
                    }
                }

                activity.SetTag("gen_ai.prompt.count", messagesList.Count);

                // Call the inner client
                var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);

                // Record response
                if (response != null)
                {
                    // Extract text content from response
                    var responseText = response.ToString();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        activity.SetTag("gen_ai.response.0.role", "assistant");
                        activity.SetTag("gen_ai.response.0.content", responseText);
                    }

                    // Try to get additional metadata using reflection (since ChatResponse doesn't expose these publicly)
                    var responseType = response.GetType();
                    
                    // Try to get Usage details
                    var usageProp = responseType.GetProperty("Usage");
                    if (usageProp != null)
                    {
                        var usage = usageProp.GetValue(response) as UsageDetails;
                        if (usage != null)
                        {
                            if (usage.InputTokenCount.HasValue)
                                activity.SetTag("gen_ai.usage.input_tokens", usage.InputTokenCount.Value);
                            
                            if (usage.OutputTokenCount.HasValue)
                                activity.SetTag("gen_ai.usage.output_tokens", usage.OutputTokenCount.Value);
                            
                            if (usage.TotalTokenCount.HasValue)
                                activity.SetTag("gen_ai.usage.total_tokens", usage.TotalTokenCount.Value);
                        }
                    }

                    // Try to get FinishReason
                    var finishReasonProp = responseType.GetProperty("FinishReason");
                    if (finishReasonProp != null)
                    {
                        var finishReason = finishReasonProp.GetValue(response);
                        if (finishReason != null)
                        {
                            activity.SetTag("gen_ai.response.finish_reason", finishReason.ToString());
                        }
                    }
                }

                activity.SetStatus(ActivityStatusCode.Ok);
                return response!;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                
                // Manually record exception details
                if (activity != null)
                {
                    activity.SetTag("exception.type", ex.GetType().FullName);
                    activity.SetTag("exception.message", ex.Message);
                    activity.SetTag("exception.stacktrace", ex.StackTrace);
                }
                
                throw;
            }
        }
        else
        {
            // Fallback if activity creation fails
            return await base.GetResponseAsync(chatMessages, options, cancellationToken);
        }
    }

    private static string GetMessageContent(ChatMessage message)
    {
        if (message.Text != null)
        {
            return message.Text;
        }

        if (message.Contents != null)
        {
            var textContents = message.Contents
                .OfType<TextContent>()
                .Select(c => c.Text)
                .Where(t => !string.IsNullOrEmpty(t));
            
            return string.Join("\n", textContents);
        }

        return string.Empty;
    }

    private static string DetermineProvider(string modelId)
    {
        if (modelId.Contains("gpt", StringComparison.OrdinalIgnoreCase))
            return "openai";
        
        if (modelId.Contains("claude", StringComparison.OrdinalIgnoreCase))
            return "anthropic";
        
        if (modelId.Contains("gemini", StringComparison.OrdinalIgnoreCase))
            return "google";
        
        return "unknown";
    }
}
