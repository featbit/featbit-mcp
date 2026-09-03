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

To run the project with the Aspire dashboard instead, use:

```bash
dotnet run --project FeatBit/FeatBit.AppHost
```

Aspire displays the assigned MCP Server endpoint in its dashboard.

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
| `Authorization` | Management tools | The exact access-token or personal-token value accepted by your FeatBit API |
| `X-FeatBit-Env-Secret` | `EvaluateFeatureFlags` only | Secret of the environment whose flags should be evaluated |

The examples use a FeatBit access token. FeatBit resolves the organization and workspace from that token, so their IDs do not need to be configured separately.

Token permissions still apply. Creating projects and environments requires `CreateProject` and `CreateEnv`; the token's project scope must also cover generated project keys.

### Codex client

The following settings belong to the **Codex client**, on the machine running Codex. They do not configure the MCP Server itself.

In `[mcp_servers.featbit]`, `mcp_servers` means “the MCP servers that Codex should connect to.” It is a client-side connection entry, not configuration read by the remote server.

Set credentials in the environment that launches Codex:

```powershell
$env:FEATBIT_AUTHORIZATION = "YOUR_FEATBIT_ACCESS_OR_PERSONAL_TOKEN"
```

Then add one of the following configurations to `~/.codex/config.toml` or a trusted project's `.codex/config.toml`.

When Codex and the MCP Server run on the same computer:

```toml
[mcp_servers.featbit_local]
url = "http://localhost:5180/mcp"
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION" }
```

When the MCP Server is deployed to Azure or another remote host:

```toml
[mcp_servers.featbit]
url = "https://your-app-name.azurewebsites.net/mcp"
env_http_headers = { Authorization = "FEATBIT_AUTHORIZATION" }
```

Replace `url` with the public HTTPS `/mcp` endpoint of your deployment. This value tells Codex where to send MCP requests; the remote MCP Server does not read this TOML file.

`env_http_headers` tells Codex to read the value of the `FEATBIT_AUTHORIZATION` environment variable and send it as the `Authorization` HTTP header. The access token or personal token is therefore supplied at runtime without being stored in `config.toml`.

Codex also supports a static header value. To store the credential directly in `config.toml`, replace the `env_http_headers` line with:

```toml
http_headers = { Authorization = "YOUR_FEATBIT_ACCESS_OR_PERSONAL_TOKEN" }
```

This works, but stores the token as plaintext. Prefer `env_http_headers`, especially in a project-level `.codex/config.toml` that could be committed.

`EvaluateFeatureFlags` also requires the `X-FeatBit-Env-Secret` header. Add it to whichever header map you choose.

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
