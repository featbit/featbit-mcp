# FeatBit MCP Server - Quick Start Examples

This guide provides practical examples for using the FeatBit MCP server tools.

## Tool Usage Examples

### 1. Project Management

#### List All Projects
```
Ask your AI: "Show me all FeatBit projects"
```

The AI will call: `GetProjects()`

**Expected Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "proj-123-abc",
      "name": "E-Commerce Platform",
      "key": "ecommerce",
      "environments": [...]
    }
  ]
}
```

#### Create a New Project
```
Ask your AI: "Create a FeatBit project named 'Mobile App' with key 'mobile-app'"
```

The AI will call: `CreateProject(name: "Mobile App", key: "mobile-app")`

**What happens**:
- Creates a new project
- Auto-generates two default environments: **Prod** and **Dev**
- Each environment gets Server Key and Client Key

#### Get Project Details
```
Ask your AI: "Show me details of project proj-123-abc"
```

The AI will call: `GetProject(projectId: "proj-123-abc")`

### 2. Environment Management

#### Create a New Environment
```
Ask your AI: "Create a staging environment in project proj-123-abc"
```

The AI will call: 
```
CreateEnvironment(
  projectId: "proj-123-abc",
  name: "Staging",
  key: "staging",
  description: "QA and testing environment"
)
```

**What happens**:
- Creates a new environment within the project
- Auto-generates Server Key and Client Key
- Returns the environment details including credentials

### 3. Feature Flag Management

#### Create a Simple Boolean Flag
```
Ask your AI: "Create a boolean feature flag called 'new-checkout-flow' in environment env-456-def"
```

The AI will call:
```
CreateFeatureFlag(
  envId: "env-456-def",
  name: "New Checkout Flow",
  key: "new-checkout-flow",
  isEnabled: false,
  variationType: "boolean",
  variationsJson: '[{"id":"on","name":"On","value":"true"},{"id":"off","name":"Off","value":"false"}]',
  enabledVariationId: "on",
  disabledVariationId: "off",
  description: "Enables the redesigned checkout experience",
  tags: "checkout,ui-redesign"
)
```

#### Create a Multi-Variate String Flag
```
Ask your AI: "Create a feature flag for pricing tiers with options: basic, pro, enterprise"
```

The AI will call:
```
CreateFeatureFlag(
  envId: "env-456-def",
  name: "Pricing Tier",
  key: "pricing-tier",
  isEnabled: true,
  variationType: "string",
  variationsJson: '[
    {"id":"basic-id","name":"Basic","value":"basic"},
    {"id":"pro-id","name":"Pro","value":"pro"},
    {"id":"enterprise-id","name":"Enterprise","value":"enterprise"}
  ]',
  enabledVariationId: "pro-id",
  disabledVariationId: "basic-id",
  tags: "pricing,monetization"
)
```

#### Toggle a Feature Flag
```
Ask your AI: "Enable the feature flag 'new-checkout-flow' in environment env-456-def"
```

The AI will call:
```
ToggleFeatureFlag(
  envId: "env-456-def",
  flagKey: "new-checkout-flow",
  isEnabled: true,
  comment: "Rolling out to production"
)
```

#### Get Feature Flag Details
```
Ask your AI: "Show me details of feature flag 'new-checkout-flow'"
```

The AI will call:
```
GetFeatureFlag(
  envId: "env-456-def",
  flagKey: "new-checkout-flow"
)
```

#### Update Feature Flag Properties
```
Ask your AI: "Update the description of feature flag 'new-checkout-flow' to 'V2 checkout with improved UX'"
```

The AI will call:
```
UpdateFeatureFlag(
  envId: "env-456-def",
  flagKey: "new-checkout-flow",
  description: "V2 checkout with improved UX",
  tags: "checkout,ui-redesign,v2"
)
```

### 4. Advanced API Operations

For operations not covered by core tools, use `CallAdvancedApi`:

#### Example: List Feature Flags in an Environment
```
Ask your AI: "List all feature flags in environment env-456-def using the advanced API"
```

The AI will call:
```
CallAdvancedApi(
  method: "GET",
  endpoint: "/api/v1/envs/env-456-def/feature-flags"
)
```

#### Example: Delete a Feature Flag
```
Ask your AI: "Delete the feature flag 'old-feature' from environment env-456-def"
```

The AI will call:
```
CallAdvancedApi(
  method: "DELETE",
  endpoint: "/api/v1/envs/env-456-def/feature-flags/old-feature"
)
```

## Complete Workflow Example

Here's a typical workflow for setting up a new feature in FeatBit:

```
1. AI: "Create a project called 'Recommendation Engine' with key 'rec-engine'"
   → Creates project with Prod and Dev environments

2. AI: "Create a staging environment in the new project"
   → Creates Staging environment with credentials

3. AI: "In the Dev environment, create a boolean flag 'enable-ml-recommendations'"
   → Creates the feature flag in disabled state

4. AI: "Show me the Dev environment details"
   → Returns Server Key and Client Key for SDK integration

5. AI: "Enable the 'enable-ml-recommendations' flag in Dev"
   → Toggles the flag to enabled state

6. AI: "Show me the flag details"
   → Returns current configuration including state, variations, and targeting rules
```

## Tips for Working with the AI Agent

1. **Be Specific**: Include environment IDs or flag keys when known
2. **Use Natural Language**: The AI understands context like "create a boolean flag"
3. **Check Results**: Always ask to see the created resource details
4. **Handle Errors**: If an operation fails, the AI will explain the error and suggest fixes
5. **Iterate**: Build complex configurations step by step

## Common Variation Types

### Boolean Flags
```json
{
  "variationType": "boolean",
  "variations": [
    {"id": "on", "name": "On", "value": "true"},
    {"id": "off", "name": "Off", "value": "false"}
  ]
}
```

### String Flags
```json
{
  "variationType": "string",
  "variations": [
    {"id": "v1", "name": "Version 1", "value": "v1"},
    {"id": "v2", "name": "Version 2", "value": "v2"},
    {"id": "v3", "name": "Version 3", "value": "v3"}
  ]
}
```

### Number Flags
```json
{
  "variationType": "number",
  "variations": [
    {"id": "small", "name": "Small", "value": "10"},
    {"id": "medium", "name": "Medium", "value": "50"},
    {"id": "large", "name": "Large", "value": "100"}
  ]
}
```

### JSON Flags
```json
{
  "variationType": "json",
  "variations": [
    {"id": "config-a", "name": "Config A", "value": "{\"timeout\":30,\"retries\":3}"},
    {"id": "config-b", "name": "Config B", "value": "{\"timeout\":60,\"retries\":5}"}
  ]
}
```

## Next Steps

- Review the [Configuration Guide](CONFIGURATION.md) for authentication setup
- Check [FeatBit REST API documentation](https://docs.featbit.co) for more API details
- Explore [FeatBit Skills](https://github.com/featbit/featbit) for SDK integration examples
