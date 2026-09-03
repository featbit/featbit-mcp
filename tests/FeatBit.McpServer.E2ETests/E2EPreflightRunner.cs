using System.Text.Json;

namespace FeatBit.McpServer.E2ETests;

internal sealed class E2EPreflightRunner(E2EOptions options)
{
    public async Task<int> RunAsync()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        LocalMcpServer? localServer = null;
        McpConnection? mcp = null;
        var exitCode = 0;

        try
        {
            var repositoryRoot = RepositoryLocator.FindRoot();
            var configuration = FeatBitConfiguration.CreatePreflight();
            Console.WriteLine("Running non-destructive MCP transport preflight. No SaaS credentials will be loaded.");

            await LocalMcpServer.BuildAsync(repositoryRoot, cancellation.Token);
            Console.WriteLine("[PASSED] Build");

            if (options.UseExistingServer)
            {
                await LocalMcpServer.WaitForExistingAsync(options.McpUrl, cancellation.Token);
            }
            else
            {
                localServer = await LocalMcpServer.StartAsync(
                    repositoryRoot,
                    options.McpUrl,
                    configuration.ApiBaseUrl,
                    cancellation.Token);
            }

            Console.WriteLine("[PASSED] Local MCP server health check");
            mcp = await McpConnection.ConnectAsync(options.McpUrl, configuration, cancellation.Token);
            if (mcp.SawMcpSessionId)
                throw new InvalidOperationException("MCP initialize unexpectedly returned Mcp-Session-Id.");

            Console.WriteLine($"[PASSED] MCP initialize with {mcp.ServerName} {mcp.ServerVersion}");

            var tools = await mcp.ListToolNamesAsync(cancellation.Token);
            var missing = McpToolInventory.Required.Except(tools, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"tools/list is missing required tools: {string.Join(", ", missing)}.");
            }

            Console.WriteLine(
                $"[PASSED] tools/list returned {tools.Count} tools; all {McpToolInventory.Required.Length} required tools are present.");

            var invalidRollout = JsonSerializer.Serialize(new[]
            {
                new { variationId = "preflight-a", percentage = 60 },
                new { variationId = "preflight-b", percentage = 20 }
            });
            var validation = await mcp.CallJsonAsync(
                "update_feature_flag_rollout",
                new Dictionary<string, object?>
                {
                    ["envId"] = "preflight-no-request",
                    ["key"] = "preflight-no-request",
                    ["rolloutAssignments"] = invalidRollout
                },
                cancellation.Token);
            var error = ApiJson.RequireString(validation, "error", "preflight rollout validation");
            if (!error.Contains("sum to 100", StringComparison.OrdinalIgnoreCase) ||
                !error.Contains("80", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The preflight tools/call did not return the expected local rollout validation error.");
            }

            Console.WriteLine("[PASSED] tools/call returned the expected local 60/20 rollout validation error");
            Console.WriteLine("Preflight passed. No FeatBit REST API request was made.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Preflight failed: {ex.Message}");
            exitCode = 1;
        }
        finally
        {
            if (mcp is not null)
            {
                try
                {
                    await mcp.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to close MCP client cleanly: {ex.Message}");
                    exitCode = 1;
                }
            }

            if (localServer is not null)
            {
                try
                {
                    await localServer.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to stop local MCP server: {ex.Message}");
                    exitCode = 1;
                }
            }
        }

        return exitCode;
    }
}
