# Deployment Documentation Resources

This directory contains deployment guidance documentation for FeatBit MCP Server.

## Files

- **AspireAzureDeployment.md** - Guidance for deploying FeatBit to Azure using .NET Aspire

## How to Update

These markdown files are used by the MCP server to provide deployment guidance to AI assistants.

**To update the content:**

1. Edit the markdown file directly (e.g., `AspireAzureDeployment.md`)
2. The changes will be available in two ways:
   - **Embedded Resource**: Compiled into the assembly (requires rebuild)
   - **File System**: Loaded at runtime from the output directory (no rebuild needed)

**Runtime Updates (No Rebuild Required):**

After building the project, the markdown files are copied to:
```
bin/Debug/net10.0/Resources/Deployments/
```

You can update these files directly, and the MCP server will load the updated content on the next request.

**For Embedded Updates (Requires Rebuild):**

If you want the changes to be permanently embedded in the assembly:
```bash
dotnet build
```

## Content Guidelines

When updating deployment documentation:

1. **Always prominently feature the GitHub repository URL**
2. **Use a guidance approach** - tell the AI how to guide users, not just provide tutorial steps
3. **Keep content up-to-date** with the actual implementation in the repository
4. **Include key commands and quick references** that AI can share with users
5. **Emphasize security and best practices**
