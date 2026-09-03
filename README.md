# FeatBit MCP Server

A self-hosted [Model Context Protocol](https://modelcontextprotocol.io) server that lets MCP clients manage FeatBit projects, environments, feature flags, rollouts, audit logs, and evaluations.

FeatBit MCP Server `v0.3.0` supports FeatBit `v5.4.4` and later. The feature-flag-gated `AddFlagTargetUser` tool is experimental and is not part of this compatibility guarantee.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A FeatBit `v5.4.4+` deployment whose API is reachable from the MCP server

## 1. Run the server

Clone the repository:

```bash
git clone https://github.com/featbit/featbit-mcp.git
cd featbit-mcp
```

Point the server at your FeatBit services and start it. Replace the example hosts with the endpoints from your deployment.

PowerShell:

```powershell
$env:FeatBitApi__BaseUrl = "https://featbit-api.example.com"
dotnet run --project FeatBit/FeatBit.McpServer --launch-profile http
```

Bash:

```bash
export FeatBitApi__BaseUrl="https://featbit-api.example.com"
dotnet run --project FeatBit/FeatBit.McpServer --launch-profile http
```

The Streamable HTTP MCP endpoint is now available at:

```text
http://localhost:5180/mcp
```

For Azure or another remote host, deploy this project as an ASP.NET Core web app and expose its `/mcp` route over HTTPS. For example, an Azure App Service endpoint could be `https://your-app-name.azurewebsites.net/mcp`.

## 2. Configure

### FeatBit endpoints

.NET configuration keys can be supplied through appsettings files or environment variables. In environment variables, replace `:` with `__`.

| Configuration key | Environment variable | Purpose |
|---|---|---|
| `FeatBitApi:BaseUrl` | `FeatBitApi__BaseUrl` | Base URL of the FeatBit management API used by project, environment, flag, targeting, and audit-log tools |
| `FeatBit:EventUri` | `FeatBit__EventUri` | Base URL of the FeatBit evaluation service; needed only by `EvaluateFeatureFlags` |

If you use `EvaluateFeatureFlags`, point it at your FeatBit evaluation service:

```powershell
$env:FeatBit__EventUri = "https://featbit-evaluation.example.com"
```

Do not commit secrets to `appsettings.json`. The repository ignores `appsettings.Development.json` and other environment-specific appsettings files for local configuration.

### Authentication

The MCP client supplies FeatBit credentials as HTTP headers. The MCP Server forwards them to FeatBit; credentials are never MCP tool parameters.

| Header | Required for | Value |
|---|---|---|
| `Authorization` | Management tools | An active FeatBit Personal or Service access token |
| `X-FeatBit-Env-Secret` | `EvaluateFeatureFlags` only | Secret of the environment whose flags should be evaluated |

Both Personal and Service access tokens are supported. FeatBit resolves the organization and workspace from either token, so their IDs do not need to be configured separately.

A Personal token uses its creator's current member permissions, while a Service token uses the permissions assigned to the token. Creating projects and environments requires `CreateProject` and `CreateEnv`; the applicable project scope must also cover generated project keys.

### Use with a coding agent

Configure the MCP endpoint and a FeatBit Personal or Service access token in your coding agent. For example, add this to Codex's `~/.codex/config.toml`:

```toml
[mcp_servers.featbit]
url = "http://localhost:5180/mcp"
http_headers = { Authorization = "YOUR_FEATBIT_ACCESS_OR_PERSONAL_TOKEN" }
```

For a remote deployment, replace `url` with its public HTTPS `/mcp` endpoint. Other coding agents use the same endpoint and `Authorization` header in their MCP configuration. Keep the token secret and do not commit it.

### Hosted endpoint (optional)

If you do not want to run the server yourself, keep the same client headers and replace only the `url` value with FeatBit's hosted endpoint:

```toml
url = "https://mcp.featbit.co/mcp"
```

## Tool reference

See [MCP Tools and REST API Reference](docs/mcp-tools-reference.md) for every exposed tool, its parameters and behavior, the FeatBit REST API it calls, and authentication forwarding details.

## License

[MIT](LICENSE)
