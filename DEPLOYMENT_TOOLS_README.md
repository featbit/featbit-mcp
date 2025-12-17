# FeatBit MCP Server - Deployment Tools

## Overview

This implementation adds comprehensive deployment tools to the FeatBit MCP Server, covering 6 deployment topics as requested:

1. **Helm Chart Deployment** (Kubernetes)
2. **Aspire Azure Deployment** (.NET Aspire + Azure Developer CLI)
3. **Terraform Azure Deployment** (Infrastructure as Code)
4. **AWS Deployment** (ECS, EKS, EC2 options)
5. **Docker Compose Deployment** (Single-server setup)
6. **Architecture Guides** (Standalone, Standard, Pro editions)

## Architecture

The implementation follows a **router pattern** with one main tool class that intelligently routes requests to specialized sub-implementations.

```
FeatBitDeploymentTools (Main Router)
    ├── GetDeploymentTutorial() - Main routing tool
    ├── ListDeployments() - List all available options
    └── CompareDeployments() - Comparison guide
         ↓ (routes to)
    ┌─────────────────────────────────────┐
    │ Deployment Implementation Classes   │
    ├─────────────────────────────────────┤
    │ HelmDeployment                      │
    │ AspireAzureDeployment               │
    │ TerraformAzureDeployment            │
    │ AwsDeployment (with sub-routing)    │
    │ DockerComposeDeployment             │
    │ FeatBitArchitecture                 │
    └─────────────────────────────────────┘
```

## File Structure

```
FeatBit/FeatBit.McpServer/
├── Tools/
│   ├── FeatBitDeploymentTools.cs          (Main Router - 3 MCP Tools)
│   └── Deployments/
│       ├── DeploymentInfo.cs              (Metadata record type)
│       ├── HelmDeployment.cs              (~500 lines)
│       ├── AspireAzureDeployment.cs       (~800 lines)
│       ├── TerraformAzureDeployment.cs    (~900 lines)
│       ├── AwsDeployment.cs               (~1000+ lines, internal routing)
│       ├── DockerComposeDeployment.cs     (~800 lines)
│       └── FeatBitArchitecture.cs         (~1200+ lines)
```

## Tools Exposed via MCP

### 1. `GetDeploymentTutorial`
**Description**: Get comprehensive deployment tutorials for FeatBit

**Parameters**:
- `topic` (string, optional): The deployment method
  - Values: `helm`, `aspire-azure`, `terraform-azure`, `aws`, `docker-compose`, `architecture`
- `subtopic` (string, optional): Specific variant
  - For AWS: `ecs`, `eks`, `ec2`
  - For Helm: `standalone`, `standard`, `pro`
  - For Architecture: `standalone`, `standard`, `pro`

**Examples**:
```
GetDeploymentTutorial(topic: "helm")
GetDeploymentTutorial(topic: "aws", subtopic: "ecs")
GetDeploymentTutorial(topic: "architecture", subtopic: "pro")
```

### 2. `ListDeployments`
**Description**: List all available deployment methods with descriptions

**Parameters**: None

**Returns**: Comprehensive list of all deployment options with:
- Topic names
- Subtopic options
- Descriptions
- Best use cases
- Difficulty levels
- Example usage

### 3. `CompareDeployments`
**Description**: Detailed comparison of deployment methods

**Parameters**: None

**Returns**: Comparison table and detailed analysis including:
- Difficulty levels
- Setup time estimates
- Cost estimates
- Pros and cons
- Recommendations by scenario
- Decision matrix

## Routing Logic

The main router (`FeatBitDeploymentTools.GetDeploymentTutorial`) uses a switch expression with pattern matching:

```csharp
return topic.ToLower().Trim() switch
{
    "helm" or "kubernetes" or "k8s" => HelmDeployment.GetTutorial(subtopic),
    "aspire" or "aspire-azure" => AspireAzureDeployment.GetTutorial(),
    "terraform" or "terraform-azure" => TerraformAzureDeployment.GetTutorial(),
    "aws" or "amazon" => AwsDeployment.GetTutorial(subtopic),
    "docker" or "docker-compose" => DockerComposeDeployment.GetTutorial(),
    "architecture" or "arch" => FeatBitArchitecture.GetArchitectureTutorial(subtopic),
    _ => GetDeploymentOverview()
};
```

**Benefits**:
- Multiple aliases supported (e.g., "helm", "kubernetes", "k8s" all work)
- Graceful fallback to overview if topic not recognized
- Clean separation of concerns
- Easy to extend with new deployment methods

## Internal Sub-Routing

The `AwsDeployment` class demonstrates internal routing for multiple deployment variants:

```csharp
public static string GetTutorial(string? method = null)
{
    return method?.ToLower() switch
    {
        "ecs" or "fargate" => GetEcsTutorial(),
        "eks" or "kubernetes" => GetEksTutorial(),
        "ec2" or "vm" => GetEc2Tutorial(),
        _ => GetOverviewTutorial()
    };
}
```

Similarly, `FeatBitArchitecture` routes to different architecture editions:

```csharp
public static string GetArchitectureTutorial(string? version = null)
{
    return version?.ToLower() switch
    {
        "standalone" => GetStandaloneArchitecture(),
        "standard" => GetStandardArchitecture(),
        "pro" or "enterprise" => GetProArchitecture(),
        _ => GetOverviewArchitecture()
    };
}
```

## Tutorial Content

Each deployment tutorial includes:

### Common Sections
- **Prerequisites**: Required tools, knowledge, accounts
- **Step-by-Step Instructions**: Detailed deployment steps
- **Configuration**: Sample configuration files (YAML, JSON, HCL, etc.)
- **Best Practices**: Production recommendations
- **Security**: Security considerations and hardening
- **Monitoring**: Health checks and observability
- **Troubleshooting**: Common issues and solutions
- **Cost Estimates**: Expected infrastructure costs
- **Scaling**: Horizontal and vertical scaling guidance

### Specific Content Highlights

**HelmDeployment**:
- Helm chart values for 3 editions (standalone, standard, pro)
- Kubernetes manifests
- Multi-AZ deployment strategies
- Auto-scaling configurations

**AspireAzureDeployment**:
- Azure Developer CLI (azd) workflow
- AppHost configuration for Aspire
- Azure Container Apps setup
- Cosmos DB and Azure Cache for Redis integration
- Application Insights monitoring

**TerraformAzureDeployment**:
- Complete Terraform modules (main.tf, variables.tf, outputs.tf)
- Azure resource configurations
- State management
- CI/CD integration with GitHub Actions
- Terraform best practices

**AwsDeployment**:
- Three deployment methods:
  - **ECS**: Fargate serverless containers
  - **EKS**: Managed Kubernetes
  - **EC2**: Traditional VM deployment
- CloudFormation templates
- IAM roles and policies
- VPC and networking setup

**DockerComposeDeployment**:
- Complete docker-compose.yml
- Multi-platform installation (Windows/macOS/Linux)
- Environment variable configuration
- Backup and restore scripts
- Nginx SSL/TLS setup
- Performance tuning

**FeatBitArchitecture**:
- Three architectural patterns with ASCII diagrams:
  - **Standalone**: Single-server (dev/test)
  - **Standard**: Production multi-service (HA)
  - **Pro/Enterprise**: Multi-region global deployment
- Capacity planning
- System requirements
- Network architecture
- Disaster recovery strategies
- SLA monitoring

## Design Decisions

### 1. Maximum of 3 MCP Tools
As requested, the implementation exposes exactly **3 MCP tools**:
- `GetDeploymentTutorial` (main routing tool)
- `ListDeployments` (discovery tool)
- `CompareDeployments` (decision support tool)

### 2. Router Pattern
The router pattern allows:
- Single entry point for users
- Intelligent routing based on natural language inputs
- Multiple aliases for flexibility
- Easy maintenance and extension

### 3. No Azure OpenAI Integration (Yet)
Currently using simple switch-based routing with pattern matching. This is:
- Fast and deterministic
- No external API calls
- Sufficient for well-defined topics

**Future Enhancement**: If needed, Azure OpenAI can be integrated for:
- Natural language understanding
- Fuzzy matching
- Handling ambiguous queries
- Suggesting related topics

### 4. Static Classes
All implementation classes are static because:
- No state needed
- Pure functions (input → output)
- Better performance (no instantiation)
- Simpler dependency management

### 5. Markdown Format
All tutorials use Markdown with:
- Code blocks with syntax highlighting
- Tables for comparisons
- ASCII diagrams for visual clarity
- Bullet points for readability
- Sections with clear headings

## Registration

The tools are automatically registered via the `[McpServerToolType]` attribute on `FeatBitDeploymentTools`.

In `Program.cs`, the server uses:
```csharp
.WithToolsFromAssembly()
```

This automatically discovers and registers all classes with `[McpServerToolType]` attribute.

## Usage Examples

### Example 1: Get Helm Tutorial
```
GetDeploymentTutorial(topic: "helm")
```
Returns comprehensive Helm deployment guide with chart values.

### Example 2: Get AWS ECS Tutorial
```
GetDeploymentTutorial(topic: "aws", subtopic: "ecs")
```
Returns AWS ECS-specific deployment guide with Fargate configuration.

### Example 3: Compare All Methods
```
CompareDeployments()
```
Returns detailed comparison table with recommendations.

### Example 4: List Available Options
```
ListDeployments()
```
Returns formatted list of all deployment methods and architectures.

### Example 5: Get Architecture Guide
```
GetDeploymentTutorial(topic: "architecture", subtopic: "standard")
```
Returns Standard edition architecture with diagrams and capacity planning.

## Extensibility

To add a new deployment method:

1. **Create implementation class** in `Tools/Deployments/`:
   ```csharp
   public static class NewDeployment
   {
       public static string GetTutorial(string? option = null)
       {
           return "Tutorial content...";
       }
   }
   ```

2. **Add route** in `FeatBitDeploymentTools.GetDeploymentTutorial()`:
   ```csharp
   "new-method" => NewDeployment.GetTutorial(subtopic),
   ```

3. **Update documentation** in `ListDeployments()` and `CompareDeployments()`

## Benefits

1. **User-Friendly**: Natural language topic selection
2. **Comprehensive**: Covers all major deployment scenarios
3. **Organized**: Clear separation between routing and implementation
4. **Maintainable**: Easy to update individual tutorials
5. **Discoverable**: List and comparison tools help users find what they need
6. **Flexible**: Multiple aliases and optional parameters
7. **Production-Ready**: Includes best practices, security, monitoring
8. **Cost-Aware**: Provides cost estimates for planning

## Future Enhancements

### 1. Azure OpenAI Integration
Add intelligent routing for ambiguous queries:
```csharp
private static async Task<string> RouteWithAI(string userQuery)
{
    // Use Azure OpenAI to understand intent
    // Return appropriate topic/subtopic
}
```

### 2. Interactive Wizards
Add step-by-step wizards for complex deployments:
```csharp
[McpServerTool]
public static string StartDeploymentWizard()
{
    // Multi-turn conversation to guide user through deployment
}
```

### 3. Deployment Validation
Add tools to validate deployment configurations:
```csharp
[McpServerTool]
public static ValidationResult ValidateConfiguration(string yaml)
{
    // Validate Helm values, docker-compose.yml, etc.
}
```

### 4. Cost Calculator
Add tool to estimate costs based on parameters:
```csharp
[McpServerTool]
public static CostEstimate CalculateCost(
    string method, 
    int users, 
    int requestsPerSecond)
{
    // Calculate infrastructure costs
}
```

## Testing

To test the implementation:

1. **Build the project**: ✅ Completed successfully
   ```bash
   dotnet build FeatBit.McpServer.csproj
   ```

2. **Run the MCP server**:
   ```bash
   dotnet run --project FeatBit.AppHost
   ```

3. **Test via MCP client**:
   - Call `ListDeployments()` to see all options
   - Call `GetDeploymentTutorial(topic: "helm")` for specific tutorial
   - Call `CompareDeployments()` for comparison

## Conclusion

This implementation provides a robust, extensible deployment guidance system for FeatBit through the MCP protocol. It follows the requested architecture of 2-3 tools with intelligent routing to handle all 6 deployment topics, with room for future AI-powered enhancements if needed.

The router pattern ensures clean separation of concerns while maintaining a simple user interface through just 3 MCP tools.
