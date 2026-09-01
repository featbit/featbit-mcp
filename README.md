# FeatBit MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that lets AI coding agents manage [FeatBit](https://featbit.co) feature flags through natural language. It acts as a thin proxy — your FeatBit API credentials are forwarded with each request, so the hosted server never stores them.

Built with .NET 10, ASP.NET Core, and Aspire.

---

## Integration

The FeatBit MCP server is publicly hosted at **`https://mcp.featbit.co/mcp`**.

> **Where to find these values**:
> - **Access token / personal token**: Use the FeatBit access token or personal token value you would send in the `Authorization` header when calling the FeatBit API directly. In some FeatBit consoles this is managed under Organization Settings → API Keys.
> - **Organization ID**: FeatBit console → Organization Settings → General → Organization ID

Set FeatBit credentials as MCP request headers. `Authorization` is forwarded as-is to the FeatBit API, so use the same access token or personal token value you would use when calling the API directly. `Organization` should be set when your FeatBit token or deployment requires an organization context. `Workspace` is also forwarded when provided. For `EvaluateFeatureFlags`, also set `X-FeatBit-Env-Secret` to the environment secret key. Do not pass credentials as MCP tool parameters.

---

### Remote (Hosted)

Connect to the hosted server — no installation or build required.

#### VS Code / GitHub Copilot

Add or update `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp",
      "headers": {
        "Authorization": "${input:featbitAccessToken}",
        "Organization": "${input:featbitOrgId}"
      }
    }
  },
  "inputs": [
    {
      "id": "featbitAccessToken",
      "type": "promptString",
      "description": "FeatBit access token or personal token",
      "password": true
    },
    {
      "id": "featbitOrgId",
      "type": "promptString",
      "description": "FeatBit Organization ID"
    }
  ]
}
```

[VS Code MCP Guide](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)

#### Claude Code

Add to your project `.mcp.json` or run:

```bash
claude mcp add --transport http featbit https://mcp.featbit.co/mcp
```

To include your access token or personal token, edit `.mcp.json`:

```json
{
  "mcpServers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
        "Organization": "YOUR_ORGANIZATION_ID"
      }
    }
  }
}
```

[Claude Code MCP Guide](https://docs.anthropic.com/en/docs/claude-code/mcp)

#### Cursor

Add to `~/.cursor/mcp.json` (macOS/Linux) or `%USERPROFILE%\.cursor\mcp.json` (Windows):

```json
{
  "mcpServers": {
    "featbit": {
      "url": "https://mcp.featbit.co/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
        "Organization": "YOUR_ORGANIZATION_ID"
      }
    }
  }
}
```

[Cursor MCP Guide](https://docs.cursor.com/context/model-context-protocol)

#### Codex CLI

Add via CLI:

```bash
codex mcp add featbit --url "https://mcp.featbit.co/mcp"
```

Or add to `~/.codex/mcp_servers.json`:

```json
{
  "featbit": {
    "url": "https://mcp.featbit.co/mcp",
    "headers": {
      "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
      "Organization": "YOUR_ORGANIZATION_ID"
    }
  }
}
```

[Codex MCP Guide](https://platform.openai.com/docs/codex)

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

#### Connect AI Clients to Local Server

**VS Code / GitHub Copilot** (`.vscode/mcp.json`):

```json
{
  "servers": {
    "featbit-local": {
      "type": "http",
      "url": "http://localhost:5180/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
        "Organization": "YOUR_ORGANIZATION_ID"
      }
    }
  }
}
```

**Claude Code** (`.mcp.json`):

```json
{
  "mcpServers": {
    "featbit-local": {
      "type": "http",
      "url": "http://localhost:5180/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
        "Organization": "YOUR_ORGANIZATION_ID"
      }
    }
  }
}
```

**Cursor** (`~/.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "featbit-local": {
      "url": "http://localhost:5180/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_ACCESS_TOKEN_OR_PERSONAL_TOKEN",
        "Organization": "YOUR_ORGANIZATION_ID"
      }
    }
  }
}
```

---

## Available Tools

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
