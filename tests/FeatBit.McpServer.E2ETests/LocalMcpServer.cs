using System.Collections.Concurrent;
using System.Diagnostics;

namespace FeatBit.McpServer.E2ETests;

internal sealed class LocalMcpServer : IAsyncDisposable
{
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output;

    private LocalMcpServer(Process process, ConcurrentQueue<string> output)
    {
        _process = process;
        _output = output;
    }

    public static async Task BuildAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var projectPath = GetServerProjectPath(repositoryRoot);
        var result = await RunProcessAsync(
            repositoryRoot,
            ["build", projectPath, "--nologo", "--verbosity", "minimal"],
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"FeatBit.McpServer build failed.{Environment.NewLine}{Tail(result.Output, 30)}");
        }
    }

    public static async Task<LocalMcpServer> StartAsync(
        string repositoryRoot,
        Uri mcpUrl,
        Uri apiBaseUrl,
        CancellationToken cancellationToken)
    {
        if (await IsHealthyAsync(mcpUrl, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A server is already listening at {GetOrigin(mcpUrl)}. " +
                "Stop it or rerun with --use-existing-server.");
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(GetServerProjectPath(repositoryRoot));
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = GetOrigin(mcpUrl).AbsoluteUri.TrimEnd('/');
        startInfo.Environment["FeatBitApi__BaseUrl"] = apiBaseUrl.AbsoluteUri.TrimEnd('/');
        startInfo.Environment["FeatBit__EventUri"] = "https://app-eval.featbit.co";
        startInfo.Environment.Remove("FeatBit__EnvSecret");
        startInfo.Environment.Remove("APPLICATIONINSIGHTS_CONNECTION_STRING");
        startInfo.Environment.Remove("OTEL_EXPORTER_OTLP_ENDPOINT");

        var output = new ConcurrentQueue<string>();
        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, eventArgs) => Capture(output, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => Capture(output, eventArgs.Data);

        if (!process.Start())
            throw new InvalidOperationException("Failed to start FeatBit.McpServer.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var server = new LocalMcpServer(process, output);

        try
        {
            await server.WaitUntilHealthyAsync(mcpUrl, cancellationToken);
            return server;
        }
        catch
        {
            await server.DisposeAsync();
            throw;
        }
    }

    public static async Task WaitForExistingAsync(Uri mcpUrl, CancellationToken cancellationToken)
    {
        if (!await IsHealthyAsync(mcpUrl, cancellationToken))
        {
            throw new InvalidOperationException(
                $"No healthy MCP server was found at {GetOrigin(mcpUrl)}.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) when (_process.HasExited)
                {
                }
            }

            await _process.WaitForExitAsync();
        }
        finally
        {
            _process.Dispose();
        }
    }

    private async Task WaitUntilHealthyAsync(Uri mcpUrl, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"FeatBit.McpServer exited before becoming healthy.{Environment.NewLine}{Tail(_output, 30)}");
            }

            if (await IsHealthyAsync(mcpUrl, cancellationToken))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }

        throw new TimeoutException(
            $"FeatBit.McpServer did not become healthy within 45 seconds.{Environment.NewLine}{Tail(_output, 30)}");
    }

    private static async Task<bool> IsHealthyAsync(Uri mcpUrl, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        try
        {
            using var response = await client.GetAsync(new Uri(GetOrigin(mcpUrl), "/health"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException("Failed to start dotnet build.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(
            process.ExitCode,
            (await standardOutput) + Environment.NewLine + (await standardError));
    }

    private static Uri GetOrigin(Uri mcpUrl)
        => new(mcpUrl.GetLeftPart(UriPartial.Authority));

    private static string GetServerProjectPath(string repositoryRoot)
        => Path.Combine(repositoryRoot, "FeatBit", "FeatBit.McpServer", "FeatBit.McpServer.csproj");

    private static void Capture(ConcurrentQueue<string> output, string? line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        output.Enqueue(line);
        while (output.Count > 100 && output.TryDequeue(out _))
        {
        }
    }

    private static string Tail(string output, int lineCount)
        => string.Join(
            Environment.NewLine,
            output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).TakeLast(lineCount));

    private static string Tail(ConcurrentQueue<string> output, int lineCount)
        => string.Join(Environment.NewLine, output.TakeLast(lineCount));

    private sealed record ProcessResult(int ExitCode, string Output);
}
