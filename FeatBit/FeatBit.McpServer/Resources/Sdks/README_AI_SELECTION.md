# Azure OpenAI Configuration for SDK Document Selection

This MCP server uses Azure OpenAI to intelligently select the most appropriate SDK documentation based on user queries.

## Configuration Options

You can configure Azure OpenAI in two ways (in order of priority):

### Option 1: appsettings.json (Recommended)

Edit `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4"
  }
}
```

### Option 2: Environment Variables

Set the following environment variables:

```bash
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key-here
AZURE_OPENAI_DEPLOYMENT=gpt-4
```

> **Note**: Configuration file settings take priority over environment variables.

## How It Works

When a user requests SDK documentation with a topic:

1. **AI-Powered Selection** (if configured):
   - Sends the topic to Azure OpenAI GPT-4
   - AI analyzes the context and selects the best matching documentation file
   - Returns: `NetServerSdkAspNetCore.md` or `NetServerSdkConsole.md`

2. **Fallback - Keyword Matching** (if Azure OpenAI not configured or fails):
   - Uses simple keyword matching against predefined lists
   - ASP.NET Core keywords: aspnet, web, api, mvc, blazor, di, middleware
   - Console keywords: console, worker, background, service, cli, batch

3. **Default Behavior** (no topic provided):
   - Returns ASP.NET Core documentation (most common use case)

## Available Documentation Files

### 1. NetServerSdkAspNetCore.md
**Best for:**
- ASP.NET Core web applications
- REST APIs and GraphQL services
- MVC, Razor Pages, and Blazor apps
- Applications using dependency injection
- Web-hosted services with middleware

### 2. NetServerSdkConsole.md
**Best for:**
- Console applications
- Worker services and background services
- Scheduled jobs and batch processing
- CLI tools
- Non-web standalone applications

## Example Usage

```csharp
// User asks: "How do I integrate FeatBit in my ASP.NET Core API?"
// AI selects: NetServerSdkAspNetCore.md

// User asks: "How do I use FeatBit in a background worker?"
// AI selects: NetServerSdkConsole.md

// User asks: "How do I setup FeatBit?"
// AI considers context and selects the most appropriate
```

## Benefits of AI Selection

- **Context-aware**: Understands nuanced queries beyond simple keywords
- **Natural language**: Works with conversational queries
- **Adaptive**: Can handle variations and synonyms
- **Fallback safe**: Degrades gracefully to keyword matching if needed

## Cost Considerations

- Each selection uses ~100 tokens (very minimal cost)
- Consider caching selections for repeated queries
- Fallback to keyword matching reduces costs when AI is unavailable
