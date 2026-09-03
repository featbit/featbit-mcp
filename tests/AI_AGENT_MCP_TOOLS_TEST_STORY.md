# AI Agent Test Specification - FeatBit MCP Tools

This repository keeps fast, isolated contract tests in
`tests/FeatBit.McpServer.Tests`. Run them with:

```powershell
dotnet test FeatBit\FeatBit.sln
```

The live validation in this document complements those tests. The executable E2E
runner (or an AI agent following the same story) starts the MCP server locally,
connects over HTTP MCP transport, invokes tools exactly as an MCP client would,
and records observed behavior against a real FeatBit service.

## Executable Runner

The local-only Console runner lives in `tests/FeatBit.McpServer.E2ETests`. It is
not an xUnit project and is never discovered by `dotnet test`.

Run the full live scenario from the repository root:

```powershell
dotnet run --project tests\FeatBit.McpServer.E2ETests -- --execute
```

To use a token stored in an environment variable without copying the secret into
the config file or command line, pass only the variable name:

```powershell
dotnet run --project tests\FeatBit.McpServer.E2ETests -- --execute --token-env FEATBIT_TEST_SERVICE_TOKEN
```

The environment variable value overrides only the `token` property. The API host,
organization, and optional workspace still come from the configuration file.

The `--execute` acknowledgement is mandatory. Without it, the program prints
usage and makes no SaaS calls. By default, the runner builds, starts, and later
stops its own local MCP Server process. To connect to a server you started
yourself, use:

```powershell
dotnet run --project tests\FeatBit.McpServer.E2ETests -- --execute --use-existing-server
```

To verify the build, MCP initialization, tool inventory, and a locally validated
`tools/call` without loading credentials or issuing any FeatBit REST API request,
run:

```powershell
dotnet run --project tests\FeatBit.McpServer.E2ETests -- --preflight
```

Safety behavior:

- refuses to run when the `CI` environment variable indicates a CI environment
- refuses to send credentials to a non-loopback MCP URL
- performs no runner-level mutation retries; each mutation is invoked only where the ordered story requires it
- never deletes Projects or Environments
- never archives a Feature Flag as cleanup
- pauses before the dedicated archive test and requires the exact displayed approval phrase
- writes a sanitized report to `tests/reports/<RUN_ID>-featbit-mcp-e2e.md`

Exit code `0` means the complete scenario passed. Exit code `1` means a failure
or cancellation occurred. Exit code `2` means the scenario completed but the
archive test was skipped because explicit approval was not given.

## Objective

Prove that `FeatBit.McpServer` can safely and correctly expose its current
project, environment, feature flag, evaluation, and audit capabilities:

- discover MCP tools through `tools/list`
- create an isolated project
- create an environment under that project
- list projects
- get a single project by ID
- list feature flags in an environment
- list feature flags across all environments in a project
- get a single feature flag by key
- create a feature flag
- toggle a feature flag on and off
- update a feature flag rollout
- evaluate feature flags for a user
- archive a feature flag
- list feature flag audit logs
- list audit logs directly by reference

## Execution Rules

1. Treat this file as the source of truth for agent-driven MCP validation.
2. Execute the end-to-end MCP flow in order.
3. Record pass/fail and captured evidence per case.
4. Stop only when a blocking failure makes later cases meaningless.
5. Prefer MCP `tools/call` over direct REST calls; direct REST calls do not satisfy this story.
6. Never write the full token, environment secret, or `Authorization` header into reports or repository files.
7. Never archive a Feature Flag as general cleanup. The only permitted automatic archive is the dedicated `archive_feature_flag` test step, and it requires explicit operator approval immediately before the call.

## Run-Scoped Test Resources

Every live run must create and use an isolated Project and Environment. Never run
the mutation steps against an existing business Project or Environment.

| Name | Value |
| --- | --- |
| `RUN_ID` | `<yyyyMMdd-HHmmss>-<short-random-suffix>` |
| `PROJECT_KEY` | `mcp-e2e-<RUN_ID>` |
| `ENVIRONMENT_KEY` | `e2e` |
| `PROJECT_ID` | local/runtime only; redact in reports |
| `ENVIRONMENT_ID` | local/runtime only; redact in reports |
| `HOST` | `https://app-api.featbit.co` |
| `MCP_URL` | `http://localhost:5180/mcp` |

The current MCP tool inventory cannot delete Projects or Environments. After the
run, leave both resources available for inspection and report their names and keys
to the operator for manual cleanup.

## Local Configuration

The agent reads credentials from:

```text
%APPDATA%\featbit\config.json
```

Expected local-only shape:

```json
{
  "host": "https://app-api.featbit.co",
  "token": "<redacted>",
  "organization": "<organization-id>",
  "workspace": "<optional-workspace-id>"
}
```

Do not commit this file.

## Required MCP Headers

Every MCP request must include:

| Header | Source |
| --- | --- |
| `Authorization` | `config.json` token |
| `Organization` | `config.json` organization |
| `Accept` | `application/json, text/event-stream` |

For `evaluate_feature_flags`, also include:

| Header | Source |
| --- | --- |
| `X-FeatBit-Env-Secret` | dynamically discovered environment secret; redact in reports |

## End-to-End MCP Flow

### Flow Step 1: Build

**Command**

```powershell
dotnet build FeatBit\FeatBit.McpServer\FeatBit.McpServer.csproj --no-restore
```

**Expected**

- Exit code is `0`
- No build errors

### Flow Step 2: Start MCP Server

**Command**

```powershell
dotnet run --project FeatBit\FeatBit.McpServer\FeatBit.McpServer.csproj --no-build --launch-profile http
```

**Expected**

- Server listens on `http://localhost:5180`
- `FeatBitApi:BaseUrl` resolves to `https://app-api.featbit.co`

### Flow Step 3: MCP Initialize

**MCP method**

```text
initialize
```

**Expected**

- HTTP status is `200`
- Response contains server info
- Response does not contain `Mcp-Session-Id`; the server uses stateless HTTP transport

### Flow Step 4: Tool Discovery

**MCP method**

```text
tools/list
```

**Expected tools**

- `get_projects`
- `create_project`
- `get_project`
- `create_environment`
- `get_project_feature_flags`
- `get_feature_flags`
- `get_feature_flag`
- `create_feature_flag`
- `toggle_feature_flag`
- `archive_feature_flag`
- `update_feature_flag_rollout`
- `get_audit_logs`
- `get_feature_flag_audit_logs`
- `evaluate_feature_flags`

`add_flag_target_user` may also be present when enabled by feature flag gate.

### Flow Step 5: Create Run-Scoped Project

**MCP tool**

```text
create_project
```

**Arguments**

```json
{
  "name": "MCP E2E <RUN_ID>",
  "key": "mcp-e2e-<RUN_ID>"
}
```

**Expected**

- Response success is `true`
- Returned Project name and key match the generated values
- Returned Project ID is a non-empty UUID
- Project ID is retained only in memory and redacted from the report
- Server-created default Environments may be present, but none is selected as the run-scoped Environment

### Flow Step 6: Create Run-Scoped Environment

**MCP tool**

```text
create_environment
```

**Arguments**

```json
{
  "projectId": "<created-project-id>",
  "name": "MCP E2E",
  "key": "e2e",
  "description": "Created by FeatBit MCP live integration test <RUN_ID>"
}
```

**Expected**

- Response success is `true`
- Returned Environment ID is a non-empty UUID
- Returned name and description match the request
- The create response may omit the Environment key; the next canonical Project read must confirm it
- Any returned Environment secret is retained only in memory and redacted from the report

### Flow Step 7: Confirm Project And Environment

**MCP tools**

```text
get_projects
get_project
```

**Expected**

- `get_projects` contains the generated `PROJECT_KEY`
- `get_project` returns the generated Project and its Environments
- Exactly one Environment has key `e2e`
- The confirmed `e2e` Environment ID is used by every remaining environment-scoped step
- The Environment secret used for evaluation is discovered dynamically and never written to the report
- Project and Environment IDs are redacted in the report

### Flow Step 8: Create Disposable Feature Flag

**MCP tool**

```text
create_feature_flag
```

**Arguments**

```json
{
  "envId": "<env-id>",
  "name": "MCP E2E Main <RUN_ID>",
  "key": "mcp-e2e-main-<RUN_ID>",
  "description": "Created by FeatBit MCP live integration test <RUN_ID>",
  "tags": "mcp,e2e"
}
```

**Expected**

- Response success is `true`
- Returned flag key matches the generated key
- Returned tags include `mcp` and `e2e`
- Returned variations include boolean `true` and `false`

### Flow Step 9: Read Created Flag

**MCP tools**

```text
get_feature_flags
get_project_feature_flags
get_feature_flag
```

**Expected**

- Project-wide query returns the test flag under the selected environment
- Single flag query returns the test flag by key
- Environment list query returns either full response or a shaped response, depending on the `flag-list` feature flag variation

### Flow Step 10: Toggle, Rollout, And Re-enable

**MCP tools**

```text
toggle_feature_flag
update_feature_flag_rollout
toggle_feature_flag
```

**Expected**

- Enable returns a successful API response
- Disable returns a successful API response
- Rollout update with 70/30 assignments returns success
- Re-enable returns a successful API response

### Flow Step 11: Evaluate

**MCP tool**

```text
evaluate_feature_flags
```

**Expected**

- With `X-FeatBit-Env-Secret` set to the selected environment secret, evaluation returns an array
- For newly created flags, allow a short synchronization delay before evaluation
- The result should include the test flag key and a non-empty variation

### Flow Step 12: Manual Inspection And Archive Approval

Before testing `archive_feature_flag`, pause the run and show the operator:

- Project name and key
- Environment name and key
- Generated Feature Flag name and key
- Current enabled and archived state
- Tags and variation names; do not expose internal IDs
- The exact Feature Flag that the next step proposes to archive

Use this handoff format:

```text
Inspection required before archive test
Project: MCP E2E <RUN_ID>
Project key: mcp-e2e-<RUN_ID>
Environment: MCP E2E
Environment key: e2e
Feature Flag: MCP E2E Test Flag
Feature Flag key: <generated-flag-key>
Proposed action: archive this Feature Flag to test archive_feature_flag
```

Wait for explicit operator approval before continuing. Viewing the message or
failing to respond does not count as approval. If approval is not given, preserve
the Feature Flag and record Step 13 as `not_run_pending_approval`.

### Flow Step 13: Test Archive Feature Flag

**MCP tools**

```text
archive_feature_flag
get_feature_flags
```

**Expected**

- Archive returns success
- Default non-archived flag list no longer returns the test flag
- This is the only Feature Flag the automated run may archive

### Flow Step 14: Audit Logs

**MCP tools**

```text
get_feature_flag_audit_logs
get_audit_logs
```

**Expected**

- Audit log total count is greater than `0`
- Returned logs reference `FeatureFlag`
- Direct audit log query by `refId` returns the same count as key-based feature flag audit log query

### Flow Step 15: Negative Validation

**MCP tool**

```text
update_feature_flag_rollout
```

**Arguments**

```json
{
  "rolloutAssignments": "[{\"variationId\":\"bad1\",\"percentage\":60},{\"variationId\":\"bad2\",\"percentage\":20}]"
}
```

**Expected**

- Tool returns JSON containing an `error`
- Error mentions percentages must sum to `100`

## Composite Agent Scenario

This scenario validates a realistic multi-tool coding-agent request:

> Please check which feature flags in the current project with tag `xxx` have reached their deletion date.

### Scenario C1: Tagged Flags Due For Deletion

**Purpose**

Prove an agent can combine MCP tools and local reasoning to answer a business question, not just invoke one tool.

**Fixture setup**

Create two disposable feature flags in the selected test environment:

| Flag | Tags | Description |
| --- | --- | --- |
| expired fixture | `mcp-delete-check` | `delete-after: <yesterday>` |
| active fixture | `mcp-delete-check` | `delete-after: <future-date>` |

**Required MCP tools**

```text
get_projects
get_project
create_feature_flag
get_project_feature_flags
get_feature_flag_audit_logs
```

**Required reasoning**

1. Resolve the run-scoped Project by the generated `PROJECT_KEY`.
2. Call `get_project_feature_flags` with `tags: "mcp-delete-check"` and `fetchAll: true`.
3. Parse deletion date from flag metadata. For this test, use `delete-after: yyyy-MM-dd` in `description`.
4. Compare deletion date with the execution date.
5. Produce a table:

| env_key | flag_key | delete_after | deletion_status |
| --- | --- | --- | --- |
| ... | ... | ... | `due` / `not_due` / `unknown` |

**Expected**

- The expired fixture is reported as `due`.
- The active fixture is reported as `not_due`.
- The report includes the table and row counts.
- Both fixture flags remain unchanged and available for operator inspection.
- The automated run does not archive either fixture flag.

## Cleanup Requirements

- Do not archive Feature Flags as cleanup, including after a failed assertion.
- The only permitted automated archive is Flow Step 13, whose purpose is to test `archive_feature_flag`; it must not run without the explicit approval required by Flow Step 12.
- Leave every other generated Feature Flag unchanged for operator inspection and manual cleanup.
- Do not delete the run-scoped Project or Environment automatically. Preserve them so the operator can inspect the completed E2E run.
- At the end of the run, clearly tell the operator to open the FeatBit SaaS Projects page and inspect the exact Project, Environment, and generated Feature Flags:

  ```text
  Manual cleanup required
  Project: MCP E2E <RUN_ID>
  Project key: mcp-e2e-<RUN_ID>
  Environment: MCP E2E
  Environment key: e2e
  Feature Flags:
  - <flag-name> (key: <flag-key>, enabled: <true-or-false>, archived: <true-or-false>)
  ```

- Record the Project, Environment, and Feature Flag names and keys in the report; never record their IDs or secrets.
- Mark retained resources as `awaiting_manual_cleanup`. The operator decides when to archive Feature Flags and delete the Project after inspection.
- Do not schedule repeated or unattended runs while cleanup remains manual.

## Report Artifact

Every execution of this story must create a Markdown report under `tests/reports/`.

Minimum report sections:

- Summary
- Environment and sanitized configuration
- MCP transport setup
- Tool discovery
- Flow results
- Run-scoped Project and Environment lifecycle
- Disposable flag lifecycle, including whether the explicitly approved archive test ran
- Manual cleanup handoff, including exact Project, Environment, and Feature Flag names, keys, and current states
- Findings
- Evidence snippets

Never include the full access token, environment secret, or Authorization header.
