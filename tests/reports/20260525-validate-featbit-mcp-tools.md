# AI Agent Test Report - FeatBit MCP Tools

## Summary

- Status: passed with documented observations
- Timestamp: 2026-05-25
- Scope: live MCP tool validation against the dedicated FeatBit test project
- MCP endpoint: `http://localhost:5180/mcp`
- FeatBit API host: `https://app-api.featbit.co`
- Project key: `featbit-cli-testing`
- Credential handling: token and environment secret were read from local config/API and redacted from this report

## Environment

| Name | Value |
| --- | --- |
| MCP server | `FeatBit.McpServer` local HTTP transport |
| Target project key | `featbit-cli-testing` |
| Project ID | `<redacted>` |
| Test environment key | `dev` |
| Test environment ID | `b9085e64-8926-4a15-b7d4-8896143d1394` |
| Config source | `%APPDATA%\featbit\config.json` |
| Token in report | redacted |
| Environment secret in report | redacted |

## MCP Transport Setup

| Case | Action | Status | Evidence |
| --- | --- | --- | --- |
| M1 | Build `FeatBit.McpServer` | passed | `dotnet build ... --no-restore` completed with 0 errors |
| M2 | Start local server | passed | Server listened on `http://localhost:5180` |
| M3 | `initialize` | passed | HTTP `200`; response included `Mcp-Session-Id` |
| M4 | `tools/list` | passed | 13 tools returned; no expected tool missing |

Build warnings:

- Existing NU1902 warnings were reported for OpenTelemetry packages.
- No compile errors were reported.

## Tool Discovery

Expected CLI-equivalent tools were present:

| MCP tool | Status |
| --- | --- |
| `get_projects` | present |
| `get_project` | present |
| `get_project_feature_flags` | present |
| `get_feature_flags` | present |
| `get_feature_flag` | present |
| `create_feature_flag` | present |
| `toggle_feature_flag` | present |
| `archive_feature_flag` | present |
| `update_feature_flag_rollout` | present |
| `get_audit_logs` | present |
| `get_feature_flag_audit_logs` | present |
| `evaluate_feature_flags` | present |

Additional tool present:

| MCP tool | Status |
| --- | --- |
| `add_flag_target_user` | present |

## Disposable Flags

| Purpose | Flag key | Flag ID | Tags | Cleanup |
| --- | --- | --- | --- | --- |
| Main MCP lifecycle flow | `mcp-e2e-20260525-144048` | `019e5ddd-620f-7009-8c53-685e01d71573` | `mcp`, `e2e` | archived |
| Evaluation synchronization check | `mcp-eval-dyn-20260525-144310` | redacted in raw report evidence | `mcp`, `eval` | archived |

An earlier interrupted local runner also created disposable MCP flags. The final cleanup path archived all disposable flags created by the completed flow.

## End-To-End Flow Results

| Step | Tool/action | Status | Observed |
| --- | --- | --- | --- |
| 1 | `get_projects` | passed | Found project key `featbit-cli-testing` |
| 2 | `get_project` | passed | Selected environment key `dev` |
| 3 | `create_feature_flag` | passed | Created `mcp-e2e-20260525-144048` with `mcp,e2e` tags |
| 4 | `get_project_feature_flags` | passed | Project-wide query returned the created flag under the selected environment |
| 5 | `get_feature_flag` | passed | Single flag lookup returned key `mcp-e2e-20260525-144048` |
| 6 | `toggle_feature_flag` on | passed | API returned successful mutation ID |
| 7 | `toggle_feature_flag` off | passed | API returned successful mutation ID |
| 8 | `update_feature_flag_rollout` | passed | 70/30 rollout update returned `success: true` |
| 9 | `toggle_feature_flag` on again | passed | API returned successful mutation ID |
| 10 | `evaluate_feature_flags` | passed after sync check | Dynamic env secret returned the eval test flag with variation `true` |
| 11 | `archive_feature_flag` | passed | Archive returned `true` |
| 12 | post-archive `get_feature_flags` | passed | Default non-archived query returned `totalCount: 0` for the archived key |
| 13 | `get_feature_flag_audit_logs` | passed | Returned 6 audit log entries |
| 14 | `get_audit_logs` by `refId` | passed | Returned 6 audit log entries |
| 15 | invalid rollout total | passed | Returned `{"error":"Percentages must sum to 100, but got 80."}` |

## Evidence Snippets

Tool discovery result:

```text
tools/list => count: 13, missing expected tools: []
```

Created flag evidence:

```text
createdKey: mcp-e2e-20260525-144048
createdTags: mcp, e2e
trueVariationFound: true
falseVariationFound: true
```

Project-wide flag lookup:

```text
get_project_feature_flags(tags: "mcp", fetchAll: true)
=> projectFlagsContainsTest: true
```

Archive and cleanup:

```text
archive_feature_flag => true
get_feature_flags(name: "mcp-e2e-20260525-144048", fetchAll: true)
=> totalCount: 0
```

Audit logs:

```text
get_feature_flag_audit_logs(flagKey: "mcp-e2e-20260525-144048", fetchAll: true)
=> totalCount: 6

get_audit_logs(refType: "FeatureFlag", refId: "<created-flag-id>", fetchAll: true)
=> totalCount: 6
```

Evaluation synchronization check:

```text
key: mcp-eval-dyn-20260525-144310
eval response:
[
  {
    "key": "mcp-eval-dyn-20260525-144310",
    "variation": {
      "type": "boolean",
      "value": "true",
      "matchReason": "default"
    }
  }
]
cleanup: archived
```

Negative rollout validation:

```text
update_feature_flag_rollout(60 + 20)
=> {"error":"Percentages must sum to 100, but got 80."}
```

## Composite Agent Scenario

Scenario tested:

> Please check which feature flags in the current project with tag `mcp-delete-check` have reached their deletion date.

Fixture flags:

| Flag key | Description date marker | Expected |
| --- | --- | --- |
| `mcp-delete-expired-20260525-150819` | `delete-after: 2026-05-24` | due |
| `mcp-delete-active-20260525-150819` | `delete-after: 2026-06-24` | not due |

Tool chain used:

```text
get_projects
get_project
create_feature_flag
get_project_feature_flags(tags: "mcp-delete-check", fetchAll: true)
get_feature_flag_audit_logs
archive_feature_flag
get_project_feature_flags(tags: "mcp-delete-check", fetchAll: true)
```

Business result table, using execution date `2026-05-25`:

| env_key | flag_key | delete_after | deletion_status |
| --- | --- | --- | --- |
| `dev` | `mcp-delete-active-20260525-150819` | `2026-06-24` | `not_due` |
| `dev` | `mcp-delete-expired-20260525-150819` | `2026-05-24` | `due` |

Composite scenario result:

| Check | Result |
| --- | --- |
| Tagged flags found | 2 |
| Due count | 1 |
| Not-due count | 1 |
| Unknown count | 0 |
| Audit logs checked for expired fixture | 1 log |
| Expired fixture archived | true |
| Active fixture archived | true |
| Post-cleanup visible disposable flags | 0 |

## Observations

- MCP C# SDK exposes tool names as snake_case, for example `GetProjects` is discovered as `get_projects`.
- `get_feature_flags` response shape is controlled by the `flag-list` feature flag. During the run, environment-level list results may be shaped, while `get_project_feature_flags` returned the full project-wide response and was used for full-object assertions.
- Newly created flags may require a short synchronization delay before evaluation returns them from the evaluation endpoint. A dedicated evaluation check with a 12-second wait returned the expected variation.
- The composite deletion-date scenario passed. It verifies that an agent can combine project discovery, tagged flag search, metadata parsing, date comparison, audit lookup, and cleanup.
- Local development logs in `C:\tmp` may contain FeatBit SDK startup details. The committed report redacts credentials and does not include Authorization or environment secret values.

## Redaction Check

- Full access token: not included.
- Authorization header: not included.
- Environment secret: not included.
- Project ID: redacted in narrative sections; command snippets avoid full project ID.

## Cleanup Status

- Main lifecycle flag `mcp-e2e-20260525-144048`: archived.
- Evaluation synchronization flag `mcp-eval-dyn-20260525-144310`: archived.
- Composite deletion-date flags `mcp-delete-expired-20260525-150819` and `mcp-delete-active-20260525-150819`: archived.
- Local MCP server: no listener remained on port `5180` after test completion.
