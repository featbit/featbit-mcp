-----
Real Content of FeatBit/FeatBit.McpServer/README.md
-----

# MCP Server for FeatBit

## Introduction

A Model Context Protocol (MCP) server that connects AI coding agents directly to FeatBit's REST API for programmatic feature flag management. This server provides tools for creating projects, environments, and feature flags through FeatBit's API.

## Features

### REST API Integration

The MCP server provides direct integration with FeatBit's REST API:

- **Project Management**: Create and manage projects
- **Environment Management**: Create environments within projects
- **Feature Flag Management**: Create, update, toggle, and query feature flags
- **Advanced API Operations**: Access any FeatBit API endpoint for custom scenarios

### Core Tools (8 Tools)

1. **CreateProject** - Create a new FeatBit project
2. **GetProjects** - List all projects in the organization
3. **GetProject** - Get detailed information about a specific project
4. **CreateEnvironment** - Create a new environment within a project
5. **CreateFeatureFlag** - Create a new feature flag with custom variations
6. **GetFeatureFlag** - Retrieve feature flag details
7. **UpdateFeatureFlag** - Update feature flag properties
8. **ToggleFeatureFlag** - Enable or disable a feature flag

### Advanced Tool

- **CallAdvancedApi** - Call any FeatBit REST API endpoint for advanced scenarios

## Configuration

Configure the server by setting the following in `appsettings.json` or environment variables:

```json
{
  "FeatBitApi": {
    "BaseUrl": "https://app.featbit.co",
    "ApiKey": "your-openapi-key-here",
    "JwtToken": ""
  }
}
```

### Authentication Methods

Choose one of the following authentication methods:

1. **OpenAPI Key** (Recommended for MCP servers)
   - Best for automation and machine-to-machine communication
   - Set `FeatBitApi:ApiKey` in configuration
   - No expiration unless revoked

2. **JWT Bearer Token**
   - For user-scoped operations
   - Set `FeatBitApi:JwtToken` in configuration
   - Session-based expiration

## Design Pattern

The server uses a **hybrid approach** balancing token efficiency with AI usability:

- **Core tools** for common operations (8 tools): Provides clear semantics and type safety
- **Advanced tool** for edge cases (1 tool): Handles less common API operations dynamically
- Total: 9 tools (~2-3K tokens in context)



-----
Below is the complete content of the file located at FeatBit/FeatBit.McpServer/README.md:
-----

# MCP Server

This README was created using the C# MCP server project template.
It demonstrates how you can easily create an MCP server using C# and publish it as a NuGet package.

The MCP server is built as a self-contained application and does not require the .NET runtime to be installed on the target machine.
However, since it is self-contained, it must be built for each target platform separately.
By default, the template is configured to build for:
* `win-x64`
* `win-arm64`
* `osx-arm64`
* `linux-x64`
* `linux-arm64`
* `linux-musl-x64`

If your users require more platforms to be supported, update the list of runtime identifiers in the project's `<RuntimeIdentifiers />` element.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](http://aka.ms/dotnet-mcp-template-survey).

## Checklist before publishing to NuGet.org

- Test the MCP server locally using the steps below.
- Update the package metadata in the .csproj file, in particular the `<PackageId>`.
- Update `.mcp/server.json` to declare your MCP server's inputs.
  - See [configuring inputs](https://aka.ms/nuget/mcp/guide/configuring-inputs) for more details.
- Pack the project using `dotnet pack`.

The `bin/Release` directory will contain the package file (.nupkg), which can be [published to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package).

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.

```json
{
  "servers": {
    "FeatBitMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<PATH TO PROJECT DIRECTORY>"
      ]
    }
  }
}
```

## Testing the MCP Server

Once configured, you can ask Copilot Chat for a random number, for example, `Give me 3 random numbers`. It should prompt you to use the `get_random_number` tool on the `FeatBitMcpServer` MCP server and show you the results.

## Publishing to NuGet.org

1. Run `dotnet pack -c Release` to create the NuGet package
2. Publish to NuGet.org with `dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json`

## Using the MCP Server from NuGet.org

Once the MCP server package is published to NuGet.org, you can configure it in your preferred IDE. Both VS Code and Visual Studio use the `dnx` command to download and install the MCP server package from NuGet.org.

- **VS Code**: Create a `<WORKSPACE DIRECTORY>/.vscode/mcp.json` file
- **Visual Studio**: Create a `<SOLUTION DIRECTORY>\.mcp.json` file

For both VS Code and Visual Studio, the configuration file uses the following server definition:

```json
{
  "servers": {
    "FeatBitMcpServer": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "<your package ID here>",
        "--version",
        "<your package version here>",
        "--yes"
      ]
    }
  }
}
```

## More information

.NET MCP servers use the [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) C# SDK. For more information about MCP:

- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
