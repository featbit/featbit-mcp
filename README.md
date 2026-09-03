# FeatBit MCP Server

A self-hosted [Model Context Protocol](https://modelcontextprotocol.io) server that lets MCP clients manage FeatBit projects, environments, feature flags, rollouts, audit logs, and evaluations.

FeatBit MCP Server `v0.3.0` supports FeatBit `v5.4.4` and later. The feature-flag-gated `AddFlagTargetUser` tool is experimental and is not part of this compatibility guarantee.

## 1. Run the server

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A FeatBit `v5.4.4+` deployment whose API is reachable from the MCP server

Clone the repository:

```bash
git clone https://github.com/featbit/featbit-mcp.git
cd featbit-mcp
```

Point the server at your FeatBit services and start it. Replace the example hosts with the endpoints from your deployment.

PowerShell:

```powershell
$env:FeatBitApi__BaseUrl = "https://featbit-api.example.com"
$env:FeatBit__EventUri = "https://featbit-evaluation.example.com"
$env:FeatBit__StreamingUri = "wss://featbit-evaluation.example.com"
$env:ASPNETCORE_URLS = "http://localhost:5180"
dotnet run --project FeatBit/FeatBit.McpServer --no-launch-profile
```

Bash:

```bash
export FeatBitApi__BaseUrl="https://featbit-api.example.com"
export FeatBit__EventUri="https://featbit-evaluation.example.com"
export FeatBit__StreamingUri="wss://featbit-evaluation.example.com"
export ASPNETCORE_URLS="http://localhost:5180"
dotnet run --project FeatBit/FeatBit.McpServer --no-launch-profile
```

The Streamable HTTP MCP endpoint is now available at:

```text
http://localhost:5180/mcp
```

To run the project with the Aspire dashboard instead, use:

```bash
dotnet run --project FeatBit/FeatBit.AppHost
```

Aspire displays the assigned MCP Server endpoint in its dashboard.

## 2. Configure the server and MCP client

### Server settings

.NET configuration keys can be supplied through appsettings files or environment variables. In environment variables, replace `:` with `__`.

| Configuration key | Environment variable | Purpose |
|---|---|---|
| `FeatBitApi:BaseUrl` | `FeatBitApi__BaseUrl` | Base URL of the FeatBit management API used by project, environment, flag, targeting, and audit-log tools |
| `FeatBit:EventUri` | `FeatBit__EventUri` | Base URL of the FeatBit evaluation service; required by `EvaluateFeatureFlags` and SDK events |
| `FeatBit:StreamingUri` | `FeatBit__StreamingUri` | WebSocket URL used by the FeatBit SDK for MCP Server release flags |
| `FeatBit:EnvSecret` | `FeatBit__EnvSecret` | Optional environment secret used by the MCP Server's own release flags |
| `FeatBit:StartWaitTimeSeconds` | `FeatBit__StartWaitTimeSeconds` | Maximum SDK initialization wait; defaults to `3` seconds |
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS` | Address on which the MCP Server listens |

For example, enable the evaluation tool and the MCP Server's own release flags with:

```powershell
$env:FeatBit__EventUri = "https://featbit-evaluation.example.com"
$env:FeatBit__StreamingUri = "wss://featbit-evaluation.example.com"
$env:FeatBit__EnvSecret = "YOUR_MCP_SERVER_ENVIRONMENT_SECRET"
```

Do not commit secrets to `appsettings.json`. The repository ignores `appsettings.Development.json` and other environment-specific appsettings files for local configuration.

### Request headers

The MCP client supplies FeatBit credentials as HTTP headers. The MCP Server forwards them to FeatBit; credentials are never MCP tool parameters.

| Header | Required for | Value |
|---|---|---|
| `Authorization` | Management tools | The exact access-token or personal-token value accepted by your FeatBit API |
| `Organization` | Organization-scoped requests | FeatBit Organization ID |
| `Workspace` | Deployments that require workspace context | FeatBit Workspace ID |
| `X-FeatBit-Env-Secret` | `EvaluateFeatureFlags` only | Secret of the environment whose flags should be evaluated |

Token permissions still apply. Creating projects and environments requires `CreateProject` and `CreateEnv`; the token's project scope must also cover generated project keys.

`FeatBit__EnvSecret` configures release flags for the MCP Server itself. `X-FeatBit-Env-Secret` is a client request header used to evaluate flags in the requested FeatBit environment.

### Codex example

Set credentials in the environment that launches Codex:

```powershell
$env:FEATBIT_AUTHORIZATION = "YOUR_AUTHORIZATION_HEADER_VALUE"
$env:FEATBIT_ORGANIZATION_ID = "YOUR_ORGANIZATION_ID"
```

Then add the self-hosted MCP endpoint to `~/.codex/config.toml` or a trusted project's `.codex/config.toml`:

```toml
[mcp_servers.featbit]
url = "http://localhost:5180/mcp"
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION", Organization = "FEATBIT_ORGANIZATION_ID" }
```

For a remote self-hosted deployment, replace the URL with its public `/mcp` endpoint. Add `Workspace = "FEATBIT_WORKSPACE_ID"` or `"X-FeatBit-Env-Secret" = "FEATBIT_ENV_SECRET"` to `env_http_headers` only when those headers are needed.

Restart Codex after changing its environment, then run `codex mcp list` or use `/mcp` to verify the connection.

### Hosted endpoint (optional)

If you do not want to run the server yourself, keep the same client headers and replace only the `url` value with FeatBit's hosted endpoint:

```toml
url = "https://mcp.featbit.co/mcp"
```

## Tool reference

See [MCP Tools and REST API Reference](docs/mcp-tools-reference.md) for every exposed tool, its parameters and behavior, the FeatBit REST API it calls, and authentication forwarding details.

## License

[MIT](LICENSE)
