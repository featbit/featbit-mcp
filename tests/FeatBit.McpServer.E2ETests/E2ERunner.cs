using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace FeatBit.McpServer.E2ETests;

internal sealed class E2ERunner(E2EOptions options)
{
    private readonly RunContext _context = RunContext.Create();
    private readonly SensitiveValueRedactor _redactor = new();
    private E2EReport? _report;
    private string? _currentStep;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        string? repositoryRoot = null;
        LocalMcpServer? localServer = null;
        McpConnection? mcp = null;
        var exitCode = 0;

        try
        {
            repositoryRoot = RepositoryLocator.FindRoot();
            _report = new E2EReport(
                _context,
                options.McpUrl,
                new Uri("https://app-api.featbit.co"),
                _redactor);
            var configuration = await FeatBitConfiguration.LoadAsync(
                options.ConfigPath,
                options.TokenEnvironmentVariable,
                cancellationToken);
            RegisterConfigurationSecrets(configuration);

            PrintRunBanner();

            await ExecuteStepAsync("Step 1: Build", async () =>
            {
                await LocalMcpServer.BuildAsync(repositoryRoot, cancellationToken);
                return "FeatBit.McpServer built successfully.";
            });

            if (options.UseExistingServer)
            {
                await ExecuteStepAsync("Step 2: Start MCP Server", async () =>
                {
                    await LocalMcpServer.WaitForExistingAsync(options.McpUrl, cancellationToken);
                    return "Connected to an explicitly selected existing local server.";
                });
            }
            else
            {
                localServer = await ExecuteStepAsync("Step 2: Start MCP Server", async () =>
                {
                    var server = await LocalMcpServer.StartAsync(
                        repositoryRoot,
                        options.McpUrl,
                        configuration.ApiBaseUrl,
                        cancellationToken);
                    return (server, $"Started local server at {options.McpUrl}.");
                });
            }

            mcp = await ExecuteStepAsync("Step 3: MCP Initialize", async () =>
            {
                var connection = await McpConnection.ConnectAsync(
                    options.McpUrl,
                    configuration,
                    cancellationToken);
                if (connection.SawMcpSessionId)
                {
                    await connection.DisposeAsync();
                    throw new InvalidOperationException("MCP initialize unexpectedly returned Mcp-Session-Id.");
                }

                return (
                    connection,
                    $"Initialized with {connection.ServerName} {connection.ServerVersion} over stateless Streamable HTTP.");
            });

            await ExecuteScenarioAsync(mcp, cancellationToken);
            exitCode = _report.HasSkippedStep ? 2 : 0;
        }
        catch (OperationCanceledException)
        {
            RecordFailure("Run cancelled by the operator.");
            Console.Error.WriteLine("E2E run cancelled. Generated resources were left untouched.");
            exitCode = 1;
        }
        catch (Exception ex)
        {
            var message = _redactor.Redact(ex.Message);
            RecordFailure(message);
            Console.Error.WriteLine($"E2E run failed: {message}");
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
                    Console.Error.WriteLine($"Failed to close MCP client cleanly: {_redactor.Redact(ex.Message)}");
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
                    Console.Error.WriteLine($"Failed to stop local MCP server: {_redactor.Redact(ex.Message)}");
                }
            }

            if (_report is not null && repositoryRoot is not null)
            {
                try
                {
                    var reportPath = await _report.WriteAsync(
                        repositoryRoot,
                        options.ReportDirectory,
                        CancellationToken.None);
                    PrintManualCleanupHandoff(reportPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to write the E2E report: {_redactor.Redact(ex.Message)}");
                    exitCode = 1;
                }
            }
        }

        return exitCode;
    }

    private async Task ExecuteScenarioAsync(McpConnection mcp, CancellationToken cancellationToken)
    {
        var runtime = new RuntimeState();

        var tools = await ExecuteStepAsync("Step 4: Tool Discovery", async () =>
        {
            var names = await mcp.ListToolNamesAsync(cancellationToken);
            var missing = McpToolInventory.Required.Except(names, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"tools/list is missing required tools: {string.Join(", ", missing)}.");
            }

            _report!.SetDiscoveredTools(names);
            return (
                names,
                $"Discovered {names.Count} tools; all {McpToolInventory.Required.Length} required tools are present.");
        });
        _ = tools;

        await ExecuteStepAsync("Step 5: Create Run-Scoped Project", async () =>
        {
            _context.ProjectState = "creation_attempted_outcome_unknown";
            var response = await mcp.CallJsonAsync(
                "create_project",
                new Dictionary<string, object?>
                {
                    ["name"] = _context.ProjectName,
                    ["key"] = _context.ProjectKey
                },
                cancellationToken);
            var project = ApiJson.RequireDataObject(response, "create_project");
            runtime.ProjectId = ApiJson.RequireUuid(project, "id", "create_project");
            _redactor.Add(runtime.ProjectId);
            ApiJson.RequireEqual(
                _context.ProjectName,
                ApiJson.RequireString(project, "name", "create_project"),
                "create_project",
                "name");
            ApiJson.RequireEqual(
                _context.ProjectKey,
                ApiJson.RequireString(project, "key", "create_project"),
                "create_project",
                "key");
            _context.ProjectState = "confirmed_created";
            return $"Created Project '{_context.ProjectName}' with key '{_context.ProjectKey}'.";
        });

        await ExecuteStepAsync("Step 6: Create Run-Scoped Environment", async () =>
        {
            _context.EnvironmentState = "creation_attempted_outcome_unknown";
            var description = $"Created by FeatBit MCP live integration test {_context.RunId}";
            var response = await mcp.CallJsonAsync(
                "create_environment",
                new Dictionary<string, object?>
                {
                    ["projectId"] = runtime.ProjectId,
                    ["name"] = _context.EnvironmentName,
                    ["key"] = _context.EnvironmentKey,
                    ["description"] = description
                },
                cancellationToken);
            var environment = ApiJson.RequireDataObject(response, "create_environment");
            runtime.EnvironmentId = ApiJson.RequireUuid(environment, "id", "create_environment");
            _redactor.Add(runtime.EnvironmentId);
            ApiJson.RequireEqual(
                _context.EnvironmentName,
                ApiJson.RequireString(environment, "name", "create_environment"),
                "create_environment",
                "name");
            ApiJson.RequireEqual(
                description,
                ApiJson.RequireString(environment, "description", "create_environment"),
                "create_environment",
                "description");
            var returnedKey = ApiJson.TryGetString(environment, "key");
            if (!string.IsNullOrEmpty(returnedKey))
            {
                ApiJson.RequireEqual(
                    _context.EnvironmentKey,
                    returnedKey,
                    "create_environment",
                    "key");
            }

            runtime.EnvironmentSecret = ApiJson.TryGetServerSecret(environment);
            _redactor.Add(runtime.EnvironmentSecret);
            _context.EnvironmentState = "confirmed_created";
            return $"Created Environment '{_context.EnvironmentName}' with key '{_context.EnvironmentKey}'.";
        });

        await ExecuteStepAsync("Step 7: Confirm Project And Environment", async () =>
        {
            var projectsResponse = await mcp.CallJsonAsync("get_projects", null, cancellationToken);
            var projects = ApiJson.RequireDataArray(projectsResponse, "get_projects");
            var projectFromList = ApiJson.RequireSingleByString(
                projects,
                "key",
                _context.ProjectKey,
                "get_projects");
            ApiJson.RequireEqualUuid(
                runtime.ProjectId,
                ApiJson.RequireUuid(projectFromList, "id", "get_projects"),
                "get_projects");

            var projectResponse = await mcp.CallJsonAsync(
                "get_project",
                new Dictionary<string, object?> { ["projectId"] = runtime.ProjectId },
                cancellationToken);
            var project = ApiJson.RequireDataObject(projectResponse, "get_project");
            ApiJson.RequireEqual(
                _context.ProjectKey,
                ApiJson.RequireString(project, "key", "get_project"),
                "get_project",
                "key");
            var environments = ApiJson.RequireArrayProperty(project, "environments", "get_project");
            var environment = ApiJson.RequireSingleByString(
                environments,
                "key",
                _context.EnvironmentKey,
                "get_project");
            ApiJson.RequireEqualUuid(
                runtime.EnvironmentId,
                ApiJson.RequireUuid(environment, "id", "get_project"),
                "get_project");
            runtime.EnvironmentSecret ??= ApiJson.RequireServerSecret(environment, "get_project");
            _redactor.Add(runtime.EnvironmentSecret);
            return "Confirmed the generated Project and e2e Environment through canonical reads.";
        });

        var mainFlag = await ExecuteStepAsync("Step 8: Create Disposable Feature Flag", async () =>
        {
            var created = await CreateFeatureFlagAsync(
                mcp,
                runtime.EnvironmentId,
                _context.MainFlag,
                $"Created by FeatBit MCP live integration test {_context.RunId}",
                "mcp,e2e",
                cancellationToken);
            return (created, $"Created Feature Flag '{_context.MainFlag.Key}' with mcp and e2e tags.");
        });

        await ExecuteStepAsync("Step 9: Read Created Flag", async () =>
        {
            var environmentList = await mcp.CallJsonAsync(
                "get_feature_flags",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["name"] = _context.MainFlag.Key,
                    ["fetchAll"] = true
                },
                cancellationToken);
            if (!ApiJson.FeatureFlagListContains(environmentList, _context.MainFlag.Key, "get_feature_flags"))
                throw new InvalidOperationException("get_feature_flags did not return the generated Feature Flag.");

            var projectList = await mcp.CallJsonAsync(
                "get_project_feature_flags",
                new Dictionary<string, object?>
                {
                    ["projectId"] = runtime.ProjectId,
                    ["name"] = _context.MainFlag.Key,
                    ["fetchAll"] = true
                },
                cancellationToken);
            var projectData = ApiJson.RequireDataObject(projectList, "get_project_feature_flags");
            var environments = ApiJson.RequireArrayProperty(
                projectData,
                "environments",
                "get_project_feature_flags");
            var environment = ApiJson.RequireSingleByString(
                environments,
                "envKey",
                _context.EnvironmentKey,
                "get_project_feature_flags");
            var items = ApiJson.RequireArrayProperty(environment, "items", "get_project_feature_flags");
            _ = ApiJson.RequireSingleByString(
                items,
                "key",
                _context.MainFlag.Key,
                "get_project_feature_flags");

            var singleResponse = await mcp.CallJsonAsync(
                "get_feature_flag",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key
                },
                cancellationToken);
            var single = ApiJson.RequireDataObject(singleResponse, "get_feature_flag");
            ApiJson.RequireEqualUuid(
                mainFlag.Id,
                ApiJson.RequireUuid(single, "id", "get_feature_flag"),
                "get_feature_flag");
            return "Environment, Project-wide, and single-Flag reads all returned the generated Flag.";
        });

        await ExecuteStepAsync("Step 10: Toggle, Rollout, And Re-enable", async () =>
        {
            await RequireMutationAsync(
                mcp,
                "toggle_feature_flag",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key,
                    ["status"] = true
                },
                cancellationToken);
            _context.MainFlag.IsEnabled = true;

            await RequireMutationAsync(
                mcp,
                "toggle_feature_flag",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key,
                    ["status"] = false
                },
                cancellationToken);
            _context.MainFlag.IsEnabled = false;

            var rollout = JsonSerializer.Serialize(new[]
            {
                new { variationId = mainFlag.TrueVariationId, percentage = 70 },
                new { variationId = mainFlag.FalseVariationId, percentage = 30 }
            });
            await RequireMutationAsync(
                mcp,
                "update_feature_flag_rollout",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key,
                    ["rolloutAssignments"] = rollout
                },
                cancellationToken);

            await RequireMutationAsync(
                mcp,
                "toggle_feature_flag",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key,
                    ["status"] = true
                },
                cancellationToken);
            _context.MainFlag.IsEnabled = true;
            return "Enable, disable, 70/30 rollout update, and re-enable all succeeded.";
        });

        await ExecuteStepAsync("Step 11: Evaluate", async () =>
        {
            var variation = await WaitForEvaluationAsync(mcp, runtime, cancellationToken);
            return $"Evaluation returned the generated Flag with variation '{variation}'.";
        });

        var archiveApproved = await ExecuteStepAsync("Step 12: Manual Inspection And Archive Approval", async () =>
        {
            var approved = await RequestArchiveApprovalAsync(cancellationToken);
            return (
                approved,
                approved
                    ? "Operator explicitly approved the archive-tool test."
                    : "Operator did not approve archive; the target Flag was preserved.");
        });

        if (archiveApproved)
        {
            await ExecuteStepAsync("Step 13: Test Archive Feature Flag", async () =>
            {
                await RequireMutationAsync(
                    mcp,
                    "archive_feature_flag",
                    new Dictionary<string, object?>
                    {
                        ["envId"] = runtime.EnvironmentId,
                        ["key"] = _context.MainFlag.Key
                    },
                    cancellationToken);
                _context.MainFlag.IsArchived = true;

                var activeList = await mcp.CallJsonAsync(
                    "get_feature_flags",
                    new Dictionary<string, object?>
                    {
                        ["envId"] = runtime.EnvironmentId,
                        ["name"] = _context.MainFlag.Key,
                        ["fetchAll"] = true
                    },
                    cancellationToken);
                if (ApiJson.FeatureFlagListContains(activeList, _context.MainFlag.Key, "get_feature_flags"))
                {
                    throw new InvalidOperationException(
                        "The archived Feature Flag is still present in the default active list.");
                }

                return "Archived only the explicitly approved test Flag and confirmed it left the active list.";
            });
        }
        else
        {
            _report!.Skipped(
                "Step 13: Test Archive Feature Flag",
                "not_run_pending_approval; no Feature Flag was archived.");
            Console.WriteLine("[SKIPPED] Step 13: Test Archive Feature Flag — approval was not granted.");
        }

        await ExecuteStepAsync("Step 14: Audit Logs", async () =>
        {
            var auditCount = await WaitForMatchingAuditCountsAsync(
                mcp,
                runtime.EnvironmentId,
                _context.MainFlag.Key,
                mainFlag.Id,
                cancellationToken);
            return $"Both audit paths returned {auditCount} FeatureFlag entries.";
        });

        await ExecuteStepAsync("Step 15: Negative Validation", async () =>
        {
            var invalidRollout = JsonSerializer.Serialize(new[]
            {
                new { variationId = "bad1", percentage = 60 },
                new { variationId = "bad2", percentage = 20 }
            });
            var response = await mcp.CallJsonAsync(
                "update_feature_flag_rollout",
                new Dictionary<string, object?>
                {
                    ["envId"] = runtime.EnvironmentId,
                    ["key"] = _context.MainFlag.Key,
                    ["rolloutAssignments"] = invalidRollout
                },
                cancellationToken);
            var error = ApiJson.RequireString(response, "error", "update_feature_flag_rollout");
            if (!error.Contains("sum to 100", StringComparison.OrdinalIgnoreCase) ||
                !error.Contains("80", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Invalid rollout validation did not explain that percentages total 80 instead of 100.");
            }

            return "Invalid 60/20 rollout was rejected locally with the expected 100-percent validation error.";
        });

        await ExecuteCompositeScenarioAsync(mcp, runtime, cancellationToken);
    }

    private async Task ExecuteCompositeScenarioAsync(
        McpConnection mcp,
        RuntimeState runtime,
        CancellationToken cancellationToken)
    {
        await ExecuteStepAsync("Scenario C1: Tagged Flags Due For Deletion", async () =>
        {
            var projectsResponse = await mcp.CallJsonAsync("get_projects", null, cancellationToken);
            var projects = ApiJson.RequireDataArray(projectsResponse, "get_projects");
            var project = ApiJson.RequireSingleByString(
                projects,
                "key",
                _context.ProjectKey,
                "get_projects");
            ApiJson.RequireEqualUuid(
                runtime.ProjectId,
                ApiJson.RequireUuid(project, "id", "get_projects"),
                "get_projects");

            var projectResponse = await mcp.CallJsonAsync(
                "get_project",
                new Dictionary<string, object?> { ["projectId"] = runtime.ProjectId },
                cancellationToken);
            var projectDetails = ApiJson.RequireDataObject(projectResponse, "get_project");
            var projectEnvironments = ApiJson.RequireArrayProperty(
                projectDetails,
                "environments",
                "get_project");
            var confirmedEnvironment = ApiJson.RequireSingleByString(
                projectEnvironments,
                "key",
                _context.EnvironmentKey,
                "get_project");
            ApiJson.RequireEqualUuid(
                runtime.EnvironmentId,
                ApiJson.RequireUuid(confirmedEnvironment, "id", "get_project"),
                "get_project");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var expired = _context.AddFeatureFlag(
                $"MCP Delete Check Expired {_context.RunId}",
                $"mcp-delete-expired-{_context.RunId}",
                "Composite deletion-date expired fixture");
            var active = _context.AddFeatureFlag(
                $"MCP Delete Check Active {_context.RunId}",
                $"mcp-delete-active-{_context.RunId}",
                "Composite deletion-date active fixture");

            var expiredRuntime = await CreateFeatureFlagAsync(
                mcp,
                runtime.EnvironmentId,
                expired,
                $"delete-after: {today.AddDays(-1):yyyy-MM-dd}",
                "mcp-delete-check",
                cancellationToken);
            _ = await CreateFeatureFlagAsync(
                mcp,
                runtime.EnvironmentId,
                active,
                $"delete-after: {today.AddDays(30):yyyy-MM-dd}",
                "mcp-delete-check",
                cancellationToken);

            var response = await mcp.CallJsonAsync(
                "get_project_feature_flags",
                new Dictionary<string, object?>
                {
                    ["projectId"] = runtime.ProjectId,
                    ["tags"] = "mcp-delete-check",
                    ["fetchAll"] = true
                },
                cancellationToken);
            var data = ApiJson.RequireDataObject(response, "get_project_feature_flags");
            var environments = ApiJson.RequireArrayProperty(data, "environments", "get_project_feature_flags");
            var environment = ApiJson.RequireSingleByString(
                environments,
                "envKey",
                _context.EnvironmentKey,
                "get_project_feature_flags");
            var items = ApiJson.RequireArrayProperty(environment, "items", "get_project_feature_flags");
            var expiredItem = ApiJson.RequireSingleByString(
                items,
                "key",
                expired.Key,
                "get_project_feature_flags");
            var activeItem = ApiJson.RequireSingleByString(
                items,
                "key",
                active.Key,
                "get_project_feature_flags");

            var expiredDate = ApiJson.ParseDeletionDate(expiredItem, "expired fixture");
            var activeDate = ApiJson.ParseDeletionDate(activeItem, "active fixture");
            expired.DeleteAfter = expiredDate;
            expired.DeletionStatus = expiredDate < today ? "due" : "not_due";
            active.DeleteAfter = activeDate;
            active.DeletionStatus = activeDate < today ? "due" : "not_due";
            if (expiredDate >= today || activeDate < today)
            {
                throw new InvalidOperationException(
                    "Composite scenario did not classify expired and active deletion dates correctly.");
            }

            _ = await WaitForFeatureFlagAuditCountAsync(
                mcp,
                runtime.EnvironmentId,
                expiredRuntime.Id,
                cancellationToken);

            return "Resolved the generated Project and Environment, then found two tagged fixtures: " +
                "one due and one not due; both remain unarchived for inspection.";
        });
    }

    private async Task<FeatureFlagRuntime> CreateFeatureFlagAsync(
        McpConnection mcp,
        string environmentId,
        TrackedFeatureFlag tracked,
        string description,
        string tags,
        CancellationToken cancellationToken)
    {
        tracked.CreationState = "creation_attempted_outcome_unknown";
        var response = await mcp.CallJsonAsync(
            "create_feature_flag",
            new Dictionary<string, object?>
            {
                ["envId"] = environmentId,
                ["name"] = tracked.Name,
                ["key"] = tracked.Key,
                ["description"] = description,
                ["tags"] = tags
            },
            cancellationToken);
        var featureFlag = ApiJson.RequireDataObject(response, "create_feature_flag");
        var id = ApiJson.RequireUuid(featureFlag, "id", "create_feature_flag");
        _redactor.Add(id);
        ApiJson.RequireEqual(
            tracked.Name,
            ApiJson.RequireString(featureFlag, "name", "create_feature_flag"),
            "create_feature_flag",
            "name");
        ApiJson.RequireEqual(
            tracked.Key,
            ApiJson.RequireString(featureFlag, "key", "create_feature_flag"),
            "create_feature_flag",
            "key");

        var returnedTags = ApiJson.RequireArrayProperty(featureFlag, "tags", "create_feature_flag")
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var expectedTag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!returnedTags.Contains(expectedTag))
                throw new InvalidOperationException($"create_feature_flag did not return tag '{expectedTag}'.");
        }

        var variations = ApiJson.RequireArrayProperty(featureFlag, "variations", "create_feature_flag");
        var trueVariation = ApiJson.RequireSingleByString(
            variations,
            "value",
            "true",
            "create_feature_flag");
        var falseVariation = ApiJson.RequireSingleByString(
            variations,
            "value",
            "false",
            "create_feature_flag");
        var trueVariationId = ApiJson.RequireUuid(trueVariation, "id", "create_feature_flag");
        var falseVariationId = ApiJson.RequireUuid(falseVariation, "id", "create_feature_flag");
        _redactor.Add(trueVariationId);
        _redactor.Add(falseVariationId);

        tracked.CreationState = "confirmed_created";
        tracked.IsEnabled = ApiJson.RequireBoolean(featureFlag, "isEnabled", "create_feature_flag");
        tracked.IsArchived = featureFlag.TryGetProperty("isArchived", out var archived) &&
                             archived.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? archived.GetBoolean()
            : false;
        if (tracked.IsEnabled != false)
            throw new InvalidOperationException("create_feature_flag did not create the Flag disabled.");

        return new FeatureFlagRuntime(id, trueVariationId, falseVariationId);
    }

    private static async Task RequireMutationAsync(
        McpConnection mcp,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var response = await mcp.CallJsonAsync(toolName, arguments, cancellationToken);
        ApiJson.EnsureMutationSuccess(response, toolName);
    }

    private async Task<string> WaitForEvaluationAsync(
        McpConnection mcp,
        RuntimeState runtime,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runtime.EnvironmentSecret))
            throw new InvalidOperationException("No Environment Server secret is available for evaluation.");

        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            var response = await mcp.CallEvaluationJsonAsync(
                runtime.EnvironmentSecret,
                new Dictionary<string, object?>
                {
                    ["userKeyId"] = $"mcp-e2e-user-{_context.RunId}",
                    ["userName"] = "FeatBit MCP E2E",
                    ["customProperties"] = "[{\"name\":\"country\",\"value\":\"US\"}]",
                    ["flagKeys"] = _context.MainFlag.Key
                },
                cancellationToken);
            var evaluations = ApiJson.RequireDataArray(response, "evaluate_feature_flags");
            var match = evaluations.FirstOrDefault(item =>
                string.Equals(ApiJson.TryGetString(item, "key"), _context.MainFlag.Key, StringComparison.Ordinal));
            if (match.ValueKind != JsonValueKind.Undefined &&
                match.TryGetProperty("variation", out var variation) &&
                variation.ValueKind == JsonValueKind.Object)
            {
                var value = ApiJson.TryGetString(variation, "value");
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException(
            "evaluate_feature_flags did not return the generated Feature Flag within 30 seconds.");
    }

    private static async Task<long> WaitForMatchingAuditCountsAsync(
        McpConnection mcp,
        string environmentId,
        string featureFlagKey,
        string featureFlagId,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(20))
        {
            var flagAudit = await mcp.CallJsonAsync(
                "get_feature_flag_audit_logs",
                new Dictionary<string, object?>
                {
                    ["envId"] = environmentId,
                    ["flagKey"] = featureFlagKey,
                    ["fetchAll"] = true
                },
                cancellationToken);
            var flagAuditCount = ApiJson.RequireTotalCount(flagAudit, "get_feature_flag_audit_logs");

            var directAudit = await mcp.CallJsonAsync(
                "get_audit_logs",
                new Dictionary<string, object?>
                {
                    ["envId"] = environmentId,
                    ["refId"] = featureFlagId,
                    ["refType"] = "FeatureFlag",
                    ["fetchAll"] = true
                },
                cancellationToken);
            var directAuditCount = ApiJson.RequireTotalCount(directAudit, "get_audit_logs");

            if (flagAuditCount > 0 && flagAuditCount == directAuditCount)
                return flagAuditCount;

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException(
            "Feature Flag and direct reference audit queries did not converge to the same non-zero count within 20 seconds.");
    }

    private static async Task<long> WaitForFeatureFlagAuditCountAsync(
        McpConnection mcp,
        string environmentId,
        string featureFlagId,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(20))
        {
            var audit = await mcp.CallJsonAsync(
                "get_feature_flag_audit_logs",
                new Dictionary<string, object?>
                {
                    ["envId"] = environmentId,
                    ["flagId"] = featureFlagId,
                    ["fetchAll"] = true
                },
                cancellationToken);
            var count = ApiJson.RequireTotalCount(audit, "get_feature_flag_audit_logs");
            if (count > 0)
                return count;

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException("The generated Feature Flag has no audit entry after 20 seconds.");
    }

    private async Task<bool> RequestArchiveApprovalAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine("Inspection required before archive test");
        Console.WriteLine($"Project: {_context.ProjectName}");
        Console.WriteLine($"Project key: {_context.ProjectKey}");
        Console.WriteLine($"Environment: {_context.EnvironmentName}");
        Console.WriteLine($"Environment key: {_context.EnvironmentKey}");
        Console.WriteLine($"Feature Flag: {_context.MainFlag.Name}");
        Console.WriteLine($"Feature Flag key: {_context.MainFlag.Key}");
        Console.WriteLine($"Enabled: {_context.MainFlag.IsEnabled?.ToString().ToLowerInvariant() ?? "unknown"}");
        Console.WriteLine($"Archived: {_context.MainFlag.IsArchived?.ToString().ToLowerInvariant() ?? "unknown"}");
        Console.WriteLine("Tags: mcp, e2e");
        Console.WriteLine("Variations: True, False");
        Console.WriteLine("Proposed action: archive this Feature Flag solely to test archive_feature_flag");
        Console.WriteLine();
        Console.WriteLine("Inspect it in the FeatBit SaaS UI now.");
        var approvalPhrase = $"ARCHIVE {_context.MainFlag.Key}";
        Console.WriteLine($"Type the exact phrase below to approve, or press Enter to skip:");
        Console.WriteLine(approvalPhrase);
        Console.Write("> ");

        var input = await Console.In.ReadLineAsync(cancellationToken);
        _context.ArchiveApproved = string.Equals(input, approvalPhrase, StringComparison.Ordinal);
        _context.ArchiveDecision = _context.ArchiveApproved ? "approved" : "not_approved";
        return _context.ArchiveApproved;
    }

    private async Task ExecuteStepAsync(string name, Func<Task<string>> action)
    {
        _currentStep = name;
        var evidence = await action();
        _report!.Passed(name, evidence);
        Console.WriteLine($"[PASSED] {name} — {evidence}");
        _currentStep = null;
    }

    private async Task<T> ExecuteStepAsync<T>(
        string name,
        Func<Task<(T Value, string Evidence)>> action)
    {
        _currentStep = name;
        var result = await action();
        _report!.Passed(name, result.Evidence);
        Console.WriteLine($"[PASSED] {name} — {result.Evidence}");
        _currentStep = null;
        return result.Value;
    }

    private void RecordFailure(string message)
    {
        if (_report is not null)
            _report.Failed(_currentStep ?? "Runner", message);
        _currentStep = null;
    }

    private void RegisterConfigurationSecrets(FeatBitConfiguration configuration)
    {
        _redactor.Add(configuration.Authorization);
        _redactor.Add(configuration.Organization);
        _redactor.Add(configuration.Workspace);
    }

    private void PrintRunBanner()
    {
        Console.WriteLine("FeatBit MCP Server live SaaS E2E run");
        Console.WriteLine($"Run ID: {_context.RunId}");
        Console.WriteLine($"Project to be created: {_context.ProjectName}");
        Console.WriteLine($"Project key: {_context.ProjectKey}");
        Console.WriteLine("The Project, Environment, and non-archive fixtures will be retained for manual inspection.");
        Console.WriteLine();
    }

    private void PrintManualCleanupHandoff(string reportPath)
    {
        Console.WriteLine();
        Console.WriteLine($"Report: {reportPath}");
        if (_context.ProjectState == "not_attempted")
            return;

        Console.WriteLine();
        Console.WriteLine("Manual cleanup required");
        Console.WriteLine($"Project: {_context.ProjectName}");
        Console.WriteLine($"Project key: {_context.ProjectKey}");
        Console.WriteLine($"Project state: {_context.ProjectState}");
        Console.WriteLine($"Environment: {_context.EnvironmentName}");
        Console.WriteLine($"Environment key: {_context.EnvironmentKey}");
        Console.WriteLine($"Environment state: {_context.EnvironmentState}");
        Console.WriteLine("Feature Flags:");
        foreach (var featureFlag in _context.FeatureFlags.Where(flag => flag.CreationState != "not_attempted"))
        {
            Console.WriteLine(
                $"- {featureFlag.Name} (key: {featureFlag.Key}, enabled: {Format(featureFlag.IsEnabled)}, " +
                $"archived: {Format(featureFlag.IsArchived)}, creation: {featureFlag.CreationState})");
        }

        Console.WriteLine("Open this Project in the FeatBit SaaS UI, inspect it, then clean it up manually.");
    }

    private static string Format(bool? value)
        => value.HasValue ? value.Value.ToString().ToLowerInvariant() : "unknown";

    private sealed class RuntimeState
    {
        public string ProjectId { get; set; } = string.Empty;

        public string EnvironmentId { get; set; } = string.Empty;

        public string? EnvironmentSecret { get; set; }
    }

    private sealed record FeatureFlagRuntime(
        string Id,
        string TrueVariationId,
        string FalseVariationId);
}
