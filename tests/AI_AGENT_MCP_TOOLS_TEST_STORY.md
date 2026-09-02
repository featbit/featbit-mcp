# AI Agent Test Specification - FeatBit MCP Tools

This repository keeps fast, isolated contract tests in
`tests/FeatBit.McpServer.Tests`. Run them with:

```powershell
dotnet test FeatBit\FeatBit.sln
```

The live validation in this document complements those tests. An AI agent starts
the MCP server locally, connects over HTTP MCP transport, invokes tools exactly as
an MCP client would, and records observed behavior against a real FeatBit service.

## Objective

Prove that `FeatBit.McpServer` can safely and correctly expose the FeatBit API capabilities that exist in `featbit-cli`:

- discover MCP tools through `tools/list`
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

## Dedicated Test Project

All live tests in this story must use the dedicated FeatBit test project:

| Name | Value |
| --- | --- |
| `PROJECT_KEY` | `featbit-cli-testing` |
| `PROJECT_ID` | local/runtime only; redact in reports |
| `HOST` | `https://app-api.featbit.co` |
| `MCP_URL` | `http://localhost:5180/mcp` |

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
  "organization": "<organization-id>"
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
- `get_project`
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

### Flow Step 5: Discover Project And Environment

**MCP tools**

```text
get_projects
get_project
```

**Expected**

- `get_projects` contains project key `featbit-cli-testing`
- `get_project` returns environments
- Agent selects one environment, preferably key `dev`
- Project ID is redacted in the report

### Flow Step 6: Create Disposable Feature Flag

**MCP tool**

```text
create_feature_flag
```

**Arguments**

```json
{
  "envId": "<env-id>",
  "name": "MCP E2E Test Flag",
  "key": "mcp-e2e-<yyyyMMdd-HHmmss>",
  "description": "Created by Codex MCP live integration test",
  "tags": "mcp,e2e"
}
```

**Expected**

- Response success is `true`
- Returned flag key matches the generated key
- Returned tags include `mcp` and `e2e`
- Returned variations include boolean `true` and `false`

### Flow Step 7: Read Created Flag

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

### Flow Step 8: Toggle, Rollout, And Re-enable

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

### Flow Step 9: Evaluate

**MCP tool**

```text
evaluate_feature_flags
```

**Expected**

- With `X-FeatBit-Env-Secret` set to the selected environment secret, evaluation returns an array
- For newly created flags, allow a short synchronization delay before evaluation
- The result should include the test flag key and a non-empty variation

### Flow Step 10: Archive And Confirm Cleanup

**MCP tools**

```text
archive_feature_flag
get_feature_flags
```

**Expected**

- Archive returns success
- Default non-archived flag list no longer returns the test flag

### Flow Step 11: Audit Logs

**MCP tools**

```text
get_feature_flag_audit_logs
get_audit_logs
```

**Expected**

- Audit log total count is greater than `0`
- Returned logs reference `FeatureFlag`
- Direct audit log query by `refId` returns the same count as key-based feature flag audit log query

### Flow Step 12: Negative Validation

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
archive_feature_flag
```

**Required reasoning**

1. Resolve the current project by key `featbit-cli-testing`.
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
- Both disposable flags are archived after the scenario.
- A post-cleanup default query no longer returns the disposable flags.

## Report Artifact

Every execution of this story must create a Markdown report under `tests/reports/`.

Minimum report sections:

- Summary
- Environment and sanitized configuration
- MCP transport setup
- Tool discovery
- Flow results
- Disposable flag lifecycle
- Cleanup status
- Findings
- Evidence snippets

Never include the full access token, environment secret, or Authorization header.
