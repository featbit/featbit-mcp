# MCP Tools and FeatBit REST API Reference

This document maps every tool exposed by `FeatBit.McpServer` to the FeatBit REST API request or requests it performs. It describes the current implementation; the FeatBit API's own authorization and validation rules still apply.

## Conventions

- C# methods use `PascalCase`; the MCP C# SDK exposes them to clients as `snake_case` names.
- Caller-provided string values used in paths and query strings are URI-encoded before they are sent upstream.
- `fetchAll` is handled inside the MCP server. It is never sent to the FeatBit API as a query parameter.
- Unless stated otherwise, request and response bodies are JSON.

## Base URLs and authentication

### Management API

All project, feature flag, targeting, and audit-log requests use `FeatBitApi:BaseUrl` as their base URL. The default is:

```text
https://app-api.featbit.co
```

The MCP server forwards these incoming HTTP headers unchanged on every management API request:

| Incoming MCP header | Outgoing FeatBit header | Purpose |
|---|---|---|
| `Authorization` | `Authorization` | FeatBit access token or personal token |
| `Organization` | `Organization` | Organization context |
| `Workspace` | `Workspace` | Optional workspace context |

Credentials are request headers, not MCP tool parameters, and are not stored by the server.

### Evaluation API

`evaluate_feature_flags` uses `FeatBit:EventUri` as its base URL. The default is:

```text
https://app-eval.featbit.co
```

For this request only, the server reads the incoming `X-FeatBit-Env-Secret` header and forwards its value as the outgoing `Authorization` header without adding a `Bearer` prefix.

## Tool inventory

| MCP tool | C# method | Purpose | FeatBit REST API request |
|---|---|---|---|
| `get_projects` | `GetProjects` | List projects in the current organization | `GET /api/v1/projects` |
| `get_project` | `GetProject` | Get one project, including its environments and credentials | `GET /api/v1/projects/{projectId}` |
| `get_project_feature_flags` | `GetProjectFeatureFlags` | List flags across every environment in a project | `GET /api/v1/projects/{projectId}`, then `GET /api/v1/envs/{envId}/feature-flags` for each environment |
| `get_feature_flags` | `GetFeatureFlags` | List and filter flags in one environment | `GET /api/v1/envs/{envId}/feature-flags` |
| `get_feature_flag` | `GetFeatureFlag` | Get one flag by key | `GET /api/v1/envs/{envId}/feature-flags/{key}` |
| `create_feature_flag` | `CreateFeatureFlag` | Create a disabled Boolean flag with `True` and `False` variations | `POST /api/v1/envs/{envId}/feature-flags` |
| `toggle_feature_flag` | `ToggleFeatureFlag` | Enable or disable a flag | `PUT /api/v1/envs/{envId}/feature-flags/{key}/toggle/{status}` |
| `archive_feature_flag` | `ArchiveFeatureFlag` | Archive a flag | `PUT /api/v1/envs/{envId}/feature-flags/{key}/archive` |
| `update_feature_flag_rollout` | `UpdateFeatureFlagRollout` | Replace the flag's default rollout (`fallthrough`) | `PATCH /api/v1/envs/{envId}/feature-flags/{key}` |
| `add_flag_target_user` | `AddFlagTargetUser` | Add an individual user to a flag's targeting list | `POST /api/v1/envs/{envId}/feature-flags/{flagKey}/target-users` |
| `get_audit_logs` | `GetAuditLogs` | Search environment audit logs | `GET /api/v1/envs/{envId}/audit-logs` |
| `get_feature_flag_audit_logs` | `GetFeatureFlagAuditLogs` | Search audit logs for one feature flag | Optional flag lookup, followed by `GET /api/v1/envs/{envId}/audit-logs` |
| `evaluate_feature_flags` | `EvaluateFeatureFlags` | Evaluate flags for an end user | `POST {FeatBit:EventUri}/api/public/featureflag/evaluate` |

This inventory includes every declared MCP tool. At runtime, `tools/list` can omit the feature-flag-gated `add_flag_target_user` tool, as described below.

## Project tools

### `get_projects`

Returns all projects visible under the authentication and organization context of the incoming MCP request.

```http
GET /api/v1/projects
```

### `get_project`

Returns one project by ID, including the environments and credentials returned by FeatBit.

```http
GET /api/v1/projects/{projectId}
```

### `get_project_feature_flags`

This is a composite tool. It first retrieves the project:

```http
GET /api/v1/projects/{projectId}
```

It then reads the project's `environments` array and sends one flag-list request for each environment:

```http
GET /api/v1/envs/{envId}/feature-flags
```

The optional MCP parameters map to the environment requests as follows:

| MCP parameter | REST query parameter | Behavior |
|---|---|---|
| `name` | `Name` | Partial match against flag name or key |
| `tags` | Repeated `Tags` values | A comma-separated input is split and trimmed |
| `pageIndex` | `PageIndex` | Zero-based page index for every environment |
| `pageSize` | `PageSize` | Page size for every environment |
| `fetchAll` | Not sent | Repeats requests for every environment, starting at `pageIndex`, until the remaining pages are collected |

When `fetchAll` is `true`, the default page index is `0` and the default page size is `100` if they are omitted. The tool returns one aggregate object containing project metadata and a result for each environment.

## Feature flag tools

### `get_feature_flags`

```http
GET /api/v1/envs/{envId}/feature-flags
```

| MCP parameter | REST query parameter | Behavior |
|---|---|---|
| `name` | `Name` | Partial match against flag name or key |
| `tags` | Repeated `Tags` values | A comma-separated input is split and trimmed |
| `isEnabled` | `IsEnabled` | Sent as lowercase `true` or `false` when supplied |
| `isArchived` | `IsArchived` | Sent as lowercase `true` or `false` when supplied |
| `sortBy` | `SortBy` | Sort field |
| `pageIndex` | `PageIndex` | Zero-based page index |
| `pageSize` | `PageSize` | Requested page size |
| `fetchAll` | Not sent | Repeats requests from `pageIndex` until the remaining pages are collected |

For `fetchAll`, the default page index is `0` and the default page size is `100` when omitted. After the API response is collected, the `flag-list` release flag can shape the MCP result:

| `flag-list` variation | MCP result shape |
|---|---|
| `full` | Original FeatBit response |
| `short` | Array of flag keys |
| `key-ct-ut` | Array containing only `key`, `createdAt`, and `updatedAt` |

### `get_feature_flag`

```http
GET /api/v1/envs/{envId}/feature-flags/{key}
```

### `create_feature_flag`

Creates a disabled Boolean feature flag:

```http
POST /api/v1/envs/{envId}/feature-flags
Content-Type: application/json
```

Example request body:

```json
{
  "envId": "<environment-id>",
  "name": "Checkout redesign",
  "key": "checkout-redesign",
  "isEnabled": false,
  "variationType": "boolean",
  "variations": [
    {
      "id": "<generated-true-variation-guid>",
      "value": "true",
      "name": "True"
    },
    {
      "id": "<generated-false-variation-guid>",
      "value": "false",
      "name": "False"
    }
  ],
  "enabledVariationId": "<generated-true-variation-guid>",
  "disabledVariationId": "<generated-false-variation-guid>",
  "description": "Optional description",
  "tags": ["checkout", "frontend"]
}
```

The server generates two distinct variation IDs. `description` and `tags` are omitted when their MCP arguments are blank. This request contract is covered for FeatBit versions 5.4.4 through 5.4.8.

### `toggle_feature_flag`

```http
PUT /api/v1/envs/{envId}/feature-flags/{key}/toggle/{status}
```

`status` is sent as lowercase `true` or `false`.

### `archive_feature_flag`

```http
PUT /api/v1/envs/{envId}/feature-flags/{key}/archive
```

### `update_feature_flag_rollout`

Accepts `rolloutAssignments` as a JSON array:

```json
[
  { "variationId": "variation-a", "percentage": 70 },
  { "variationId": "variation-b", "percentage": 30 }
]
```

The percentages must sum to `100`, within a tolerance of `0.01`. If they do not, the tool returns an error without calling the REST API. Valid percentages are converted to contiguous ranges and sent as a JSON Patch request:

```http
PATCH /api/v1/envs/{envId}/feature-flags/{key}
Content-Type: application/json
```

```json
[
  {
    "op": "replace",
    "path": "/fallthrough",
    "value": {
      "dispatchKey": "email",
      "includedInExpt": false,
      "variations": [
        {
          "id": "variation-a",
          "rollout": [0.0, 0.7],
          "exptRollout": 1.0
        },
        {
          "id": "variation-b",
          "rollout": [0.7, 1.0],
          "exptRollout": 1.0
        }
      ]
    }
  }
]
```

Only `/fallthrough` is replaced. Other feature flag settings are not included in the patch.

## Targeting tools

### `add_flag_target_user`

Adds one user to a flag's targeting list:

```http
POST /api/v1/envs/{envId}/feature-flags/{flagKey}/target-users
Content-Type: application/json
```

```json
{
  "keyId": "<userKey>",
  "name": "<userEmail>"
}
```

This experimental tool is gated by the `add-feature-flag-target-user` release flag. When the release flag is disabled, the MCP list-tools filter removes the tool from the `tools/list` response.

## Audit-log tools

### `get_audit_logs`

```http
GET /api/v1/envs/{envId}/audit-logs
```

| MCP parameter | REST query parameter | Behavior |
|---|---|---|
| `query` | `Query` | Keyword or comment-fragment filter |
| `creatorId` | `CreatorId` | Creator ID filter |
| `refId` | `RefId` | Referenced resource ID filter |
| `refType` | `RefType` | Referenced resource type filter |
| `from` | `From` | Start timestamp in Unix milliseconds |
| `to` | `To` | End timestamp in Unix milliseconds |
| `crossEnvironment` | `CrossEnvironment=true` | Included only when the MCP value is `true` |
| `pageIndex` | `PageIndex` | Zero-based page index |
| `pageSize` | `PageSize` | Requested page size |
| `fetchAll` | Not sent | Repeats requests from `pageIndex` until the remaining pages are collected |

For `fetchAll`, the default page index is `0` and the default page size is `100` when omitted.

### `get_feature_flag_audit_logs`

The tool accepts either `flagId` or `flagKey`.

- When `flagId` is supplied, the tool queries audit logs directly.
- When only `flagKey` is supplied, it first resolves the feature flag ID:

```http
GET /api/v1/envs/{envId}/feature-flags/{flagKey}
```

It then calls:

```http
GET /api/v1/envs/{envId}/audit-logs?RefId={resolvedFlagId}&RefType=FeatureFlag
```

The other filters and pagination behavior are the same as `get_audit_logs`. If neither `flagId` nor `flagKey` is provided, the tool returns an error without calling the API.

## Evaluation tool

### `evaluate_feature_flags`

Evaluates feature flags for an end user through the evaluation service rather than the management API:

```http
POST {FeatBit:EventUri}/api/public/featureflag/evaluate
Authorization: <X-FeatBit-Env-Secret value>
Content-Type: application/json
```

Example request body:

```json
{
  "user": {
    "keyId": "user-1",
    "name": "Ada",
    "customizedProperties": [
      { "name": "country", "value": "US" },
      { "name": "age", "value": 30 }
    ]
  },
  "filter": {
    "keys": ["checkout", "search"],
    "tags": ["frontend", "mobile"],
    "tagFilterMode": "or"
  }
}
```

Request-building behavior:

- `userKeyId` is always sent as `user.keyId`.
- Blank `userName` is omitted.
- `customProperties` is parsed as a JSON array. Invalid JSON is ignored so evaluation can continue with the base user.
- `flagKeys` and `tags` are comma-separated MCP arguments that become JSON arrays.
- `filter` is omitted when both arrays are empty.
- `tagFilterMode` is included only when tags are present and defaults to `and` when omitted.

## Response and error behavior

- Successful FeatBit response bodies are returned unchanged unless a tool explicitly aggregates or shapes them.
- A non-success HTTP response with a body is returned unchanged so callers can see FeatBit validation and authorization details.
- A non-success HTTP response with an empty body becomes `{"error":"HTTP <status-code> <status-name>"}`.
- Exceptions caught inside `FeatBitApiClient`, such as transport failures, become `{"error":"<exception-message>"}`.
- Composite tools return an upstream JSON response unchanged when it does not contain the data needed for the next step.

## Implementation sources

- [Project tools](../FeatBit/FeatBit.McpServer/Tools/FeatBitApiTools.Projects.cs)
- [Feature flag and targeting tools](../FeatBit/FeatBit.McpServer/Tools/FeatBitApiTools.FeatureFlags.cs)
- [Audit-log tools](../FeatBit/FeatBit.McpServer/Tools/FeatBitApiTools.AuditLogs.cs)
- [Evaluation tool](../FeatBit/FeatBit.McpServer/Tools/FeatBitApiTools.Evaluation.cs)
- [REST client and header forwarding](../FeatBit/FeatBit.McpServer/Infrastructure/FeatBitApiClient.cs)
- [Route and payload contract tests](../tests/FeatBit.McpServer.Tests)
