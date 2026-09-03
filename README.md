# FeatBit MCP Server

**IMPORTANT NOTE**: 

**We're moving this MCP server into FeatBit's main project. Its tools will be exposed through the FeatBit API service.**

---

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that lets AI coding agents manage [FeatBit](https://featbit.co) feature flags through natural language. It acts as a thin proxy — your FeatBit API credentials are forwarded with each request, so the hosted server never stores them.

Built with .NET 10, ASP.NET Core, Aspire, and MCP C# SDK 2.2.0.

## Compatibility

FeatBit MCP Server `v0.2.2` supports FeatBit `v5.4.4` through `v5.4.8`. These FeatBit releases use the same REST API contracts for the generally available tools exposed by this server. The feature-flag-gated `AddFlagTargetUser` tool is experimental and is not part of this compatibility guarantee.

Use `v0.2.2` or later when creating feature flags. Earlier MCP releases, including `v0.2.1`, omitted the boolean variation fields required by FeatBit and could return HTTP 500. See [issue #2](https://github.com/featbit/featbit-mcp/issues/2).

---

## Integration

The FeatBit MCP server is publicly hosted at **`https://mcp.featbit.co/mcp`**.

Every MCP client needs the same connection data:

| Setting | Value | When required |
|---------|-------|---------------|
| MCP URL | `https://mcp.featbit.co/mcp` | Always |
| `Authorization` header | The exact access-token or personal-token value accepted by your FeatBit API | Management tools |
| `Organization` header | FeatBit Organization ID | When required by the token or deployment |
| `Workspace` header | FeatBit Workspace ID | Optional deployment context |
| `X-FeatBit-Env-Secret` header | Environment secret key | `EvaluateFeatureFlags` only |

The server forwards these headers to FeatBit and never accepts credentials as tool parameters. You can find access tokens under **Organization Settings → API Keys** in consoles that use that layout, and the Organization ID under **Organization Settings → General**.

### Codex (Hosted)

Codex reads MCP servers from `~/.codex/config.toml`, or from a trusted project's `.codex/config.toml`. Keep credentials in environment variables so they are not committed to the repository.

Set the required values in the environment that launches Codex:

```powershell
$env:FEATBIT_AUTHORIZATION = "YOUR_AUTHORIZATION_HEADER_VALUE"
$env:FEATBIT_ORGANIZATION_ID = "YOUR_ORGANIZATION_ID"
```

```bash
export FEATBIT_AUTHORIZATION='YOUR_AUTHORIZATION_HEADER_VALUE'
export FEATBIT_ORGANIZATION_ID='YOUR_ORGANIZATION_ID'
```

Then add:

```toml
[mcp_servers.featbit]
url = "https://mcp.featbit.co/mcp"
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION", Organization = "FEATBIT_ORGANIZATION_ID" }
```

If you also need workspace context or `EvaluateFeatureFlags`, set `FEATBIT_WORKSPACE_ID` and `FEATBIT_ENV_SECRET`, then replace `env_http_headers` with:

```toml
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION", Organization = "FEATBIT_ORGANIZATION_ID", Workspace = "FEATBIT_WORKSPACE_ID", "X-FeatBit-Env-Secret" = "FEATBIT_ENV_SECRET" }
```

Restart Codex after changing its environment. Run `codex mcp list` or use `/mcp` in Codex to verify the connection.

[Codex MCP documentation](https://learn.chatgpt.com/docs/extend/mcp)

---

### Local (Self-Hosted)

Run the server from source when you need a custom `BaseUrl` pointing to a self-hosted FeatBit instance, or when you want to extend the server with your own tools.

#### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

#### Setup

1. Clone the repo:
   ```bash
   git clone https://github.com/featbit/featbit-mcp.git
   cd featbit-mcp
   ```

2. Set the FeatBit API base URL in `FeatBit/FeatBit.McpServer/appsettings.Development.json`:
   ```json
   {
     "FeatBitApi": {
       "BaseUrl": "https://app-api.featbit.co"
     }
   }
   ```
   Change `BaseUrl` to your self-hosted instance URL if needed.

3. Run the server:
   ```bash
   # With Aspire (recommended — includes observability dashboard)
   dotnet run --project FeatBit/FeatBit.AppHost

   # Or run the MCP server directly
   dotnet run --project FeatBit/FeatBit.McpServer
   ```

   The server starts on `http://localhost:5180`.

#### Connect Codex to the Local Server

Reuse the same environment variables and point Codex at the local endpoint:

```toml
[mcp_servers.featbit_local]
url = "http://localhost:5180/mcp"
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION", Organization = "FEATBIT_ORGANIZATION_ID" }
```

---

## Available Tools

The table below is a quick overview. For the exact FeatBit REST endpoints, query parameters, request bodies, authentication forwarding, and multi-request tool behavior, see the [MCP Tools and REST API Reference](docs/mcp-tools-reference.md).

| Tool | Description | Parameters |
|------|-------------|------------|
| `GetProjects` | List all projects within the current organization | — |
| `GetProject` | Get a project by ID with its environments and credentials (Server Key, Client Key) | `projectId` *(required)* |
| `GetProjectFeatureFlags` | List feature flags across every environment in a project. Supports filtering by name/key, tags, pagination, and all-page fetching per environment | `projectId` *(required)*, `name`, `tags`, `pageIndex`, `pageSize`, `fetchAll` |
| `GetFeatureFlags` | List feature flags in an environment. Supports filtering by name/key, tags, enabled/disabled status, archived status, sorting, pagination, and all-page fetching | `envId` *(required)*, `name`, `tags`, `isEnabled`, `isArchived`, `sortBy`, `pageIndex`, `pageSize`, `fetchAll` |
| `GetFeatureFlag` | Get a single feature flag by key | `envId` *(required)*, `key` *(required)* |
| `CreateFeatureFlag` | Create a disabled boolean feature flag with default `True` / `False` variations | `envId` *(required)*, `name` *(required)*, `key` *(required)*, `description`, `tags` |
| `ToggleFeatureFlag` | Enable or disable a feature flag | `envId` *(required)*, `key` *(required)*, `status` *(required)* |
| `ArchiveFeatureFlag` | Archive a feature flag. Archived flags are hidden from the main list by default but can be restored later | `envId` *(required)*, `key` *(required)* |
| `UpdateFeatureFlagRollout` | Update the default rollout (fallthrough) of a feature flag. Only the `/fallthrough` path is modified; other flag settings are left unchanged. Accepts rollout assignments as `[{"variationId","percentage"}]` where percentages must sum to 100 | `envId` *(required)*, `key` *(required)*, `rolloutAssignments` *(required)*, `dispatchKey` |
| `AddFlagTargetUser` | Add an individual user to a feature flag's targeting list. This tool is gated by the `add-feature-flag-target-user` feature flag and may be unavailable when that release flag is disabled | `envId` *(required)*, `flagKey` *(required)*, `userKey` *(required)*, `userEmail` *(required)* |
| `GetAuditLogs` | List audit logs in an environment with keyword, creator, resource, time range, cross-environment, pagination, and all-page filters | `envId` *(required)*, `query`, `creatorId`, `refId`, `refType`, `from`, `to`, `crossEnvironment`, `pageIndex`, `pageSize`, `fetchAll` |
| `GetFeatureFlagAuditLogs` | List audit logs for a feature flag. Accepts either `flagId` or `flagKey`; resolves `flagKey` to the flag ID automatically | `envId` *(required)*, `flagId`, `flagKey`, `query`, `creatorId`, `from`, `to`, `crossEnvironment`, `pageIndex`, `pageSize`, `fetchAll` |
| `EvaluateFeatureFlags` | Evaluate feature flags for a given end user and return served variations, match reasons, and experiment tracking info. Requires the `X-FeatBit-Env-Secret` request header | `userKeyId` *(required)*, `userName`, `customProperties`, `flagKeys`, `tags`, `tagFilterMode` |

---

## Guidance for Coding Agents

When a user asks about feature flags but does not provide environment IDs, first call `GetProjects`, identify or ask for the intended project, then call `GetProjectFeatureFlags` with `fetchAll: true`.

For example, for a request like:

> Please check which feature flags in the current project with tag `xxx` have reached their deletion date.

Use this flow:

1. Call `GetProjects` if the project ID is not known.
2. Call `GetProjectFeatureFlags` with `projectId`, `tags: "xxx"`, and `fetchAll: true`.
3. Inspect returned flag metadata such as `key`, `name`, `description`, `tags`, `createdAt`, `updatedAt`, and `lastChange`.
4. If the deletion date is encoded in a tag or description, compare it with today's date and report matching flags.
5. If change history is needed, call `GetFeatureFlagAuditLogs` with the environment ID and `flagKey`.
6. Do not archive or toggle flags unless the user explicitly asks for that action.

---

https://app-api.featbit.co/docs/index.html
https://app-api.featbit.co/swagger/OpenApi/swagger.json

---

## Additional Resources

- [featbit/featbit-skills](https://github.com/featbit/featbit-skills) — Agent skills with feature flag best practices, SDK integration guides, and code examples for use with AI coding agents.
- [FeatBit Documentation](https://docs.featbit.co) — Official FeatBit docs.
