# Configuration Guide for FeatBit MCP Server

## Authentication Setup

The FeatBit MCP server requires authentication to communicate with your FeatBit instance. You can use either an OpenAPI Key or a JWT Bearer Token.

### Method 1: OpenAPI Key (Recommended)

OpenAPI Keys are best for automation and MCP servers as they don't expire.

1. **Obtain an OpenAPI Key**:
   - Log in to your FeatBit portal
   - Navigate to **Organization Settings** → **API Keys**
   - Click **Generate New Key**
   - Copy the generated key

2. **Configure the MCP Server**:

   **Option A: Using appsettings.json**
   ```json
   {
     "FeatBitApi": {
       "BaseUrl": "https://app.featbit.co",
       "ApiKey": "your-api-key-here",
       "JwtToken": ""
     }
   }
   ```

   **Option B: Using Environment Variables**
   ```bash
   # Windows PowerShell
   $env:FeatBitApi__BaseUrl = "https://app.featbit.co"
   $env:FeatBitApi__ApiKey = "your-api-key-here"

   # Linux/Mac
   export FeatBitApi__BaseUrl="https://app.featbit.co"
   export FeatBitApi__ApiKey="your-api-key-here"
   ```

### Method 2: JWT Bearer Token

JWT tokens are tied to user sessions and will expire.

1. **Obtain a JWT Token**:
   - Log in to your FeatBit portal
   - The JWT token is issued during authentication
   - Copy the token from your browser's developer tools or authentication response

2. **Configure the MCP Server**:
   ```json
   {
     "FeatBitApi": {
       "BaseUrl": "https://app.featbit.co",
       "ApiKey": "",
       "JwtToken": "eyJhbGciOiJIUzI1NiIs..."
     }
   }
   ```

## Base URL Configuration

**For SaaS Users** (default):
```json
{
  "FeatBitApi": {
    "BaseUrl": "https://app.featbit.co"
  }
}
```

**For Self-Hosted FeatBit**:
```json
{
  "FeatBitApi": {
    "BaseUrl": "https://your-featbit-instance.com"
  }
}
```

## Example: Complete Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "FeatBitApi": {
    "BaseUrl": "https://app.featbit.co",
    "ApiKey": "api-abc123def456...",
    "JwtToken": ""
  }
}
```

## Testing the Configuration

Once configured, you can test the MCP server by:

1. **Start the server**:
   ```bash
   dotnet run --project FeatBit.McpServer
   ```

2. **Use the tools through your AI coding agent**:
   - "List all projects in FeatBit"
   - "Create a new project called 'Test Project' with key 'test-project'"
   - "Show me the environments in project [project-id]"

## Security Best Practices

1. **Never commit credentials** to version control
2. **Use environment variables** in production environments
3. **Rotate API keys** periodically
4. **Use least privilege** - create keys with only necessary permissions
5. **Monitor API usage** through FeatBit's audit logs

## Troubleshooting

### Authentication Errors (401)

- Verify your API key or JWT token is correct
- Check if the JWT token has expired
- Ensure the key has proper permissions

### Connection Errors

- Verify the `BaseUrl` is correct
- Check network connectivity to your FeatBit instance
- Ensure firewall rules allow outbound HTTPS connections

### API Errors (400, 403, 404)

- Check the tool parameters are correct
- Verify you have permissions for the requested operation
- Review the error message in the response for specific details
