using System.Net;
using System.Text;
using FeatBit.FeatureFlags;
using FeatBit.McpServer.Infrastructure;
using FeatBit.McpServer.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeatBit.McpServer.Tests;

internal static class ToolTestFactory
{
    public static FeatBitApiTools CreateTools(
        RecordingHttpMessageHandler handler,
        string flagListVariation = "full",
        bool releaseEnabled = false,
        Action<DefaultHttpContext>? configureContext = null,
        string apiBaseUrl = "https://api.featbit.test",
        string evaluationBaseUrl = "https://eval.featbit.test")
    {
        var apiClient = CreateApiClient(
            handler,
            configureContext,
            apiBaseUrl,
            evaluationBaseUrl);

        return new FeatBitApiTools(
            apiClient,
            new StubFeatureFlagEvaluator(flagListVariation, releaseEnabled));
    }

    public static FeatBitApiClient CreateApiClient(
        HttpMessageHandler handler,
        Action<DefaultHttpContext>? configureContext = null,
        string apiBaseUrl = "https://api.featbit.test",
        string evaluationBaseUrl = "https://eval.featbit.test")
    {
        var context = new DefaultHttpContext();
        configureContext?.Invoke(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatBitApi:BaseUrl"] = apiBaseUrl,
                ["FeatBit:EventUri"] = evaluationBaseUrl
            })
            .Build();

        return new FeatBitApiClient(
            new HttpClient(handler),
            new HttpContextAccessor { HttpContext = context },
            configuration,
            NullLogger<FeatBitApiClient>.Instance);
    }
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _responses = [];

    public List<RecordedRequest> Requests { get; } = [];

    public RecordingHttpMessageHandler(params string[] responseBodies)
    {
        foreach (var body in responseBodies)
            EnqueueJson(body);
    }

    public void EnqueueJson(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        => _responses.Enqueue(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    public void EnqueueException(Exception exception)
        => _responses.Enqueue(_ => Task.FromException<HttpResponseMessage>(exception));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers
            .Concat(request.Content?.Headers
                ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri ?? throw new InvalidOperationException("Request URI is required."),
            body,
            headers));

        if (_responses.Count > 0)
            return await _responses.Dequeue()(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
        };
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body,
    IReadOnlyDictionary<string, string[]> Headers)
{
    public string? GetHeader(string name)
        => Headers.TryGetValue(name, out var values) ? string.Join(",", values) : null;
}

internal sealed class StubFeatureFlagEvaluator(
    string flagListVariation = "full",
    bool releaseEnabled = false) : IFeatureFlagEvaluator
{
    public bool ReleaseEnabled(FeatureFlag flag) => releaseEnabled;

    public void ReleaseEnabledThen(FeatureFlag flag, Action action)
    {
        if (ReleaseEnabled(flag))
            action();
    }

    public T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func, T defaultValue)
        => ReleaseEnabled(flag) ? func() : defaultValue;

    public T ReleaseEnabledThen<T>(FeatureFlag flag, Func<T> func)
        => ReleaseEnabled(flag) ? func() : default!;

    public async Task ReleaseEnabledThenAsync(FeatureFlag flag, Func<Task> asyncAction)
    {
        if (ReleaseEnabled(flag))
            await asyncAction();
    }

    public async Task<T> ReleaseEnabledThenAsync<T>(
        FeatureFlag flag,
        Func<Task<T>> asyncFunc,
        T defaultValue)
        => ReleaseEnabled(flag) ? await asyncFunc() : defaultValue;

    public async Task<T> ReleaseEnabledThenAsync<T>(FeatureFlag flag, Func<Task<T>> asyncFunc)
        => ReleaseEnabled(flag) ? await asyncFunc() : default!;

    public string StringVariation(FeatureFlag flag)
        => flag == FeatureFlag.FlagList ? flagListVariation : flag.DefaultStringValue;
}
