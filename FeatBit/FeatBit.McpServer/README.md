# FeatBit MCP Server

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that connects AI coding agents directly to [FeatBit](https://www.featbit.co/)'s REST API for programmatic feature flag management.

## Tools

| Tool | Description |
|------|-------------|
| `GetProjects` | Get the list of all projects within the organization |
| `GetProject` | Get a single project by ID, including its environments and credentials |
| `GetFeatureFlags` | Get the list of feature flags in an environment, with filtering and pagination |
| `ToggleFeatureFlag` | Enable or disable a feature flag |

## Configuration

### VS Code (`mcp.json`)

```jsonc
{
  "inputs": [
    {
      "id": "featbit-token",
      "type": "promptString",
      "description": "FeatBit Access Token",
      "password": true
    },
    {
      "id": "featbit-org",
      "type": "promptString",
      "description": "FeatBit Organization ID"
    }
  ],
  "servers": {
    "featbit-mcp": {
      "type": "http",
      "url": "https://<your-mcp-server-host>/mcp",
      "headers": {
        "Authorization": "${input:featbit-token}",
        "Organization": "${input:featbit-org}"
      }
    }
  }
}
```

### Required Headers

| Header | Description |
|--------|-------------|
| `Authorization` | FeatBit access token (OpenAPI key) |
| `Organization` | Organization ID. Required by most FeatBit APIs |
| `Workspace` | *(Optional)* Workspace ID, when using workspace-scoped APIs |

### Server Configuration (`appsettings.json`)

Only `BaseUrl` needs to be set. Credentials come from the MCP client headers at runtime.

```json
{
  "FeatBitApi": {
    "BaseUrl": "https://app.featbit.co"
  }
}
```

Set via environment variable: `FeatBitApi__BaseUrl=https://app.featbit.co`

## Running Locally

```sh
cd FeatBit/FeatBit.McpServer
dotnet run
```

The server starts on `http://localhost:5180` by default. The MCP endpoint is at `/mcp`.

For local development, update your `.vscode/mcp.json`:

```jsonc
{
  "servers": {
    "featbit-mcp": {
      "type": "http",
      "url": "http://localhost:5180/mcp",
      "headers": {
        "Authorization": "${input:featbit-token}",
        "Organization": "${input:featbit-org}"
      }
    }
  }
}
```

## Architecture

- **Credential forwarding**: `Authorization`, `Organization`, and `Workspace` headers are forwarded from the incoming MCP HTTP request directly to the FeatBit API — no server-side credential storage needed.
- **One tool per operation**: Each tool maps to one specific API call for maximum agent accuracy.
- **Raw JSON responses**: Tools return the FeatBit API JSON response as-is, so agents always have the full response available.
