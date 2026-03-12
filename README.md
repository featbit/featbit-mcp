# FeatBit MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that lets AI coding agents manage [FeatBit](https://featbit.co) feature flags through natural language. It acts as a thin proxy — your FeatBit API credentials are forwarded with each request, so the hosted server never stores them.

Built with .NET 10, ASP.NET Core, and Aspire.

---

## Integration

The FeatBit MCP server is publicly hosted at **`https://mcp.featbit.co/mcp`**.

> **Where to find these values**:
> - **API Key**: FeatBit console → Organization Settings → API Keys → Generate New Key
> - **Organization ID**: FeatBit console → Organization Settings → General → Organization ID

Two headers are required in every request: `Authorization` (the raw API key — no `Bearer` prefix) and `Organization` (your organization ID). The server forwards them as-is to the FeatBit REST API without persisting them.

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
        "Authorization": "${input:featbitApiKey}",
        "Organization": "${input:featbitOrgId}"
      }
    }
  },
  "inputs": [
    {
      "id": "featbitApiKey",
      "type": "promptString",
      "description": "FeatBit API Key",
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

To include your API key, edit `.mcp.json`:

```json
{
  "mcpServers": {
    "featbit": {
      "type": "http",
      "url": "https://mcp.featbit.co/mcp",
      "headers": {
        "Authorization": "YOUR_FEATBIT_API_KEY",
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
        "Authorization": "YOUR_FEATBIT_API_KEY",
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
      "Authorization": "YOUR_FEATBIT_API_KEY",
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
        "Authorization": "YOUR_FEATBIT_API_KEY",
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
        "Authorization": "YOUR_FEATBIT_API_KEY",
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
        "Authorization": "YOUR_FEATBIT_API_KEY",
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
| `GetProjects` | List all projects in the organization | — |
| `GetProject` | Get a project's details, environments, and credentials (Server Key, Client Key) | `projectId` |
| `GetFeatureFlags` | List feature flags in an environment. Supports filtering by name/key, tags, enabled/disabled status, archived status, and pagination | `envId` *(required)*, `name`, `tags`, `isEnabled`, `isArchived`, `sortBy`, `pageIndex`, `pageSize` |
| `GetFeatureFlag` | Get a single feature flag by key | `envId` *(required)*, `key` *(required)* |
| `ToggleFeatureFlag` | Enable or disable a feature flag | `envId` *(required)*, `key` *(required)*, `status` *(required)* |
| `ArchiveFeatureFlag` | Archive a feature flag. Archived flags are hidden from the main list by default but can be restored later | `envId` *(required)*, `key` *(required)* |
| `CreateFeatureFlag` | Create a feature flag with the given name, key, and description. The flag is created disabled; use `ToggleFeatureFlag` to enable it | `envId` *(required)*, `name` *(required)*, `key` *(required)*, `description` |
| `UpdateFeatureFlagRollout` | Update the default rollout (fallthrough) of a feature flag. Only the `/fallthrough` path is modified — other flag settings are left unchanged. Accepts rollout assignments as `[{"variationId", "percentage"}]` where percentages must sum to 100 | `envId` *(required)*, `key` *(required)*, `rolloutAssignments` *(required)*, `dispatchKey` |

---

https://app-api.featbit.co/docs/index.html
https://app-api.featbit.co/swagger/OpenApi/swagger.json

Todo List:
- **Evaluate Feature flags for a given user**: Evaluate all feature flags in an environment for a given user. The request body should include the user's attributes, and the response will indicate which flags are enabled for that user. [https://docs.featbit.co/api-docs/flag-evaluation-api](https://docs.featbit.co/api-docs/flag-evaluation-api)

---

## Additional Resources

- [featbit/featbit-skills](https://github.com/featbit/featbit-skills) — Agent skills with feature flag best practices, SDK integration guides, and code examples for use with AI coding agents.
- [FeatBit Documentation](https://docs.featbit.co) — Official FeatBit docs.
