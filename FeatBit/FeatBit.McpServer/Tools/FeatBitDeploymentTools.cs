using System.ComponentModel;
using FeatBit.McpServer.Tools.Deployments;
using ModelContextProtocol.Server;

namespace FeatBit.McpServer.Tools;

[McpServerToolType]
public static class FeatBitDeploymentTools
{
    [McpServerTool]
    [Description("Get comprehensive deployment tutorials for FeatBit including Helm charts, Azure (Aspire/Terraform), AWS (ECS/EKS/EC2), Docker Compose, and Architecture guides (Standalone/Standard/Pro).")]
    public static string GetDeploymentTutorial(
        [Description("The deployment topic: helm, aspire-azure, terraform-azure, aws, docker-compose, architecture")] 
        string? topic = null,
        [Description("Optional subtopic: For AWS: ecs, eks, ec2. For Architecture: standalone, standard, pro")] 
        string? subtopic = null)
    {
        // If no topic specified, return overview
        if (string.IsNullOrWhiteSpace(topic))
        {
            return GetDeploymentOverview();
        }

        // Route to appropriate deployment guide
        return topic.ToLower().Trim() switch
        {
            "helm" or "kubernetes" or "k8s" or "helm-chart" => 
                HelmDeployment.GetTutorial(subtopic),
            
            "aspire" or "aspire-azure" or "azure-aspire" or "dotnet-aspire" => 
                AspireAzureDeployment.GetTutorial(),
            
            "terraform" or "terraform-azure" or "azure-terraform" or "iac" => 
                TerraformAzureDeployment.GetTutorial(),
            
            "aws" or "amazon" => 
                AwsDeployment.GetTutorial(subtopic),
            
            "docker" or "docker-compose" or "compose" => 
                DockerComposeDeployment.GetTutorial(),
            
            "architecture" or "arch" or "versions" or "editions" => 
                FeatBitArchitecture.GetArchitectureTutorial(subtopic),
            
            _ => GetDeploymentOverview() + $"\n\nUnknown topic: '{topic}'. Please choose from: helm, aspire-azure, terraform-azure, aws, docker-compose, architecture"
        };
    }

    [McpServerTool]
    [Description("List all available FeatBit deployment methods and architectures with their descriptions.")]
    public static string ListDeployments()
    {
        return """
        # ============================================================================
        # FeatBit Deployment Methods
        # ============================================================================
        
        ## Available Deployment Tutorials
        
        ### 1. Helm Chart (Kubernetes)
        **Topic**: `helm` or `kubernetes`
        **Subtopics**: 
          - `standalone`: Single replica deployment
          - `standard`: Production-ready HA deployment
          - `pro`: Enterprise deployment with full features
        
        **Description**: Deploy FeatBit to any Kubernetes cluster using Helm charts.
        Perfect for cloud-native environments (EKS, AKS, GKE, or on-premises).
        
        **Best For**:
        - Production Kubernetes environments
        - Multi-environment deployments (dev/staging/prod)
        - Teams familiar with Kubernetes
        - Auto-scaling requirements
        
        **Difficulty**: ⭐⭐⭐ (Medium)
        
        ---
        
        ### 2. .NET Aspire Azure Deployment
        **Topic**: `aspire-azure` or `dotnet-aspire`
        
        **Description**: Deploy FeatBit to Azure using .NET Aspire and Azure Developer CLI (azd).
        Deploys to Azure Container Apps with Cosmos DB and Azure Cache for Redis.
        
        **Best For**:
        - .NET developers
        - Azure-first organizations
        - Rapid Azure deployment
        - Serverless/managed services preference
        
        **Difficulty**: ⭐⭐ (Easy-Medium)
        
        ---
        
        ### 3. Terraform Azure Deployment
        **Topic**: `terraform-azure` or `iac`
        
        **Description**: Infrastructure as Code deployment to Azure using Terraform.
        Provides full control over infrastructure configuration.
        
        **Best For**:
        - Infrastructure as Code workflows
        - GitOps practices
        - Custom infrastructure requirements
        - Multi-environment management
        
        **Difficulty**: ⭐⭐⭐⭐ (Medium-Hard)
        
        ---
        
        ### 4. AWS Deployment
        **Topic**: `aws`
        **Subtopics**:
          - `ecs`: AWS Elastic Container Service (Fargate)
          - `eks`: AWS Elastic Kubernetes Service
          - `ec2`: AWS EC2 instances
        
        **Description**: Multiple deployment options for Amazon Web Services.
        Choose based on your AWS expertise and requirements.
        
        **Best For**:
        - AWS-first organizations
        - Various AWS compute options
        - Integration with AWS services
        
        **Difficulty**: ⭐⭐⭐ to ⭐⭐⭐⭐ (Medium to Hard)
        
        ---
        
        ### 5. Docker Compose
        **Topic**: `docker-compose` or `docker`
        
        **Description**: Simple Docker Compose deployment for single-server setup.
        Fastest way to get started with FeatBit.
        
        **Best For**:
        - Development and testing
        - POC/Demo environments
        - Small deployments
        - Quick local setup
        
        **Difficulty**: ⭐ (Easy)
        
        ---
        
        ### 6. Architecture Guides
        **Topic**: `architecture`
        **Subtopics**:
          - `standalone`: Single-server architecture
          - `standard`: Production multi-service architecture
          - `pro` or `enterprise`: Enterprise multi-region architecture
        
        **Description**: Detailed architecture documentation for different FeatBit editions.
        Includes diagrams, capacity planning, and scaling strategies.
        
        **Best For**:
        - Architecture planning
        - Capacity planning
        - Understanding deployment options
        - Choosing the right edition
        
        **Difficulty**: N/A (Documentation)
        
        ============================================================================
        
        ## How to Use
        
        To get a specific tutorial, use the `get_deployment_tutorial` tool with:
        - **topic**: The deployment method (e.g., "helm", "aws", "docker-compose")
        - **subtopic**: Optional specific variant (e.g., "ecs", "standalone")
        
        ### Examples:
        
        ```
        # Get Helm deployment tutorial
        get_deployment_tutorial(topic="helm")
        
        # Get Helm tutorial for production deployment
        get_deployment_tutorial(topic="helm", subtopic="standard")
        
        # Get AWS ECS deployment tutorial
        get_deployment_tutorial(topic="aws", subtopic="ecs")
        
        # Get architecture guide for enterprise edition
        get_deployment_tutorial(topic="architecture", subtopic="pro")
        
        # Get Docker Compose tutorial
        get_deployment_tutorial(topic="docker-compose")
        
        # Get Aspire Azure tutorial
        get_deployment_tutorial(topic="aspire-azure")
        ```
        
        ============================================================================
        
        ## Quick Decision Guide
        
        **I want the fastest setup**: → docker-compose
        
        **I'm using Azure**: → aspire-azure (easiest) or terraform-azure (more control)
        
        **I'm using AWS**: → aws (specify ecs, eks, or ec2)
        
        **I have Kubernetes**: → helm
        
        **I need to understand the architecture first**: → architecture
        
        **I need production-ready with HA**: → helm (standard) or architecture (standard/pro)
        
        **I need enterprise features**: → architecture (pro)
        
        ============================================================================
        """;
    }

    [McpServerTool]
    [Description("Get a detailed comparison of different FeatBit deployment methods to help choose the best option.")]
    public static string CompareDeployments()
    {
        return """
        # ============================================================================
        # FeatBit Deployment Methods Comparison
        # ============================================================================
        
        ## Overview Table
        
        | Method | Difficulty | Setup Time | Cloud | On-Prem | HA | Auto-Scale | Cost (Est/month) |
        |--------|-----------|------------|-------|---------|----|-----------|-----------------| 
        | Docker Compose | ⭐ Easy | 10 min | ✅ | ✅ | ❌ | ❌ | $30-50 |
        | Helm (Standalone) | ⭐⭐ Medium | 30 min | ✅ | ✅ | ❌ | ✅ | $100-200 |
        | Helm (Standard) | ⭐⭐⭐ Medium | 1 hour | ✅ | ✅ | ✅ | ✅ | $450-700 |
        | Aspire Azure | ⭐⭐ Easy-Medium | 20 min | Azure | ❌ | ✅ | ✅ | $400-600 |
        | Terraform Azure | ⭐⭐⭐⭐ Hard | 2 hours | Azure | ❌ | ✅ | ✅ | $500-800 |
        | AWS ECS | ⭐⭐⭐ Medium | 1 hour | AWS | ❌ | ✅ | ✅ | $400-700 |
        | AWS EKS | ⭐⭐⭐⭐ Hard | 2 hours | AWS | ❌ | ✅ | ✅ | $600-900 |
        | AWS EC2 | ⭐⭐ Medium | 45 min | AWS | ❌ | ⚠️ | ❌ | $200-400 |
        
        ## Detailed Comparison
        
        ### 1. Docker Compose
        
        **Pros:**
        - ✅ Fastest setup (5-10 minutes)
        - ✅ Works anywhere (cloud, on-prem, local)
        - ✅ Simple configuration
        - ✅ Easy backup and restore
        - ✅ Minimal prerequisites
        - ✅ Lowest cost
        
        **Cons:**
        - ❌ No high availability
        - ❌ Manual scaling
        - ❌ Single point of failure
        - ❌ Not production-ready for critical apps
        
        **Best For:**
        - Development and testing
        - POC/demos
        - Small teams (< 10 users)
        - Low-traffic applications
        
        **Not Recommended For:**
        - Production (mission-critical)
        - High availability requirements
        - Large scale deployments
        
        ---
        
        ### 2. Helm on Kubernetes
        
        **Pros:**
        - ✅ Industry standard for Kubernetes
        - ✅ Works on any K8s (EKS, AKS, GKE, on-prem)
        - ✅ Easy upgrades and rollbacks
        - ✅ High availability (Standard/Pro)
        - ✅ Auto-scaling capabilities
        - ✅ Declarative configuration
        
        **Cons:**
        - ❌ Requires Kubernetes knowledge
        - ❌ More complex than Docker Compose
        - ❌ Initial cluster setup overhead
        
        **Best For:**
        - Teams already using Kubernetes
        - Multi-environment deployments
        - Production with HA requirements
        - Cloud-agnostic deployments
        
        **Not Recommended For:**
        - Teams unfamiliar with Kubernetes
        - Simple/small deployments
        - Quick POCs
        
        ---
        
        ### 3. .NET Aspire Azure
        
        **Pros:**
        - ✅ Easiest Azure deployment
        - ✅ Fully managed services (Container Apps, Cosmos DB, Redis)
        - ✅ Integrated with Azure Developer CLI
        - ✅ Built-in monitoring (App Insights)
        - ✅ Auto-scaling included
        - ✅ Great for .NET teams
        
        **Cons:**
        - ❌ Azure only
        - ❌ Less control over infrastructure
        - ❌ Vendor lock-in
        
        **Best For:**
        - .NET developers
        - Azure-first organizations
        - Rapid Azure deployment
        - Serverless preference
        
        **Not Recommended For:**
        - Multi-cloud strategies
        - On-premises deployments
        - Non-Azure environments
        
        ---
        
        ### 4. Terraform Azure
        
        **Pros:**
        - ✅ Full infrastructure control
        - ✅ Infrastructure as Code
        - ✅ Version controlled infrastructure
        - ✅ Reusable modules
        - ✅ GitOps compatible
        - ✅ Great for complex setups
        
        **Cons:**
        - ❌ Steep learning curve
        - ❌ More complex than Aspire
        - ❌ Requires Terraform knowledge
        - ❌ Longer setup time
        
        **Best For:**
        - Infrastructure as Code workflows
        - Complex Azure environments
        - Multi-environment management
        - Teams with Terraform expertise
        
        **Not Recommended For:**
        - Quick deployments
        - Teams unfamiliar with IaC
        - Simple requirements
        
        ---
        
        ### 5. AWS Deployments
        
        #### AWS ECS (Fargate)
        
        **Pros:**
        - ✅ Serverless containers
        - ✅ No server management
        - ✅ Pay per use
        - ✅ Integrated with AWS services
        - ✅ Easier than EKS
        
        **Cons:**
        - ❌ AWS vendor lock-in
        - ❌ Less flexible than EKS
        
        **Best For:**
        - AWS teams preferring simplicity
        - Serverless container workflows
        - Cost optimization (pay per use)
        
        #### AWS EKS (Kubernetes)
        
        **Pros:**
        - ✅ Managed Kubernetes
        - ✅ Full Kubernetes features
        - ✅ Cloud-agnostic (can migrate)
        - ✅ Large ecosystem
        
        **Cons:**
        - ❌ More expensive than ECS
        - ❌ Requires K8s knowledge
        - ❌ More complex setup
        
        **Best For:**
        - Teams using Kubernetes
        - Complex microservices
        - Avoiding vendor lock-in
        
        #### AWS EC2
        
        **Pros:**
        - ✅ Full VM control
        - ✅ Flexible configuration
        - ✅ Works with existing scripts
        
        **Cons:**
        - ❌ Manual management
        - ❌ No built-in HA
        - ❌ More maintenance
        
        **Best For:**
        - Simple AWS deployments
        - Teams familiar with VMs
        - Lift-and-shift scenarios
        
        ---
        
        ## Decision Matrix
        
        ### By Team Expertise
        
        | Experience Level | Recommended Method |
        |-----------------|-------------------|
        | Beginner | Docker Compose |
        | .NET Developer (Azure) | Aspire Azure |
        | Kubernetes Expert | Helm |
        | AWS User (Simple) | AWS ECS |
        | AWS User (Advanced) | AWS EKS |
        | IaC/Terraform Expert | Terraform Azure |
        | DevOps Professional | Helm or Terraform |
        
        ### By Environment
        
        | Environment | Recommended Method |
        |------------|-------------------|
        | Development | Docker Compose |
        | Testing/Staging | Docker Compose or Helm (Standalone) |
        | Production (Small) | Helm (Standard) or Aspire Azure |
        | Production (Large) | Helm (Pro) or AWS EKS |
        | Multi-Cloud | Helm (portable) |
        | Azure Only | Aspire or Terraform Azure |
        | AWS Only | ECS or EKS |
        | On-Premises | Helm or Docker Compose |
        
        ### By Requirements
        
        | Requirement | Recommended Method |
        |------------|-------------------|
        | High Availability | Helm (Standard/Pro), Aspire, Terraform, AWS ECS/EKS |
        | Auto-Scaling | Helm, Aspire, Terraform, AWS ECS/EKS |
        | Lowest Cost | Docker Compose |
        | Fastest Setup | Docker Compose or Aspire Azure |
        | Most Control | Terraform or Helm |
        | Least Management | Aspire Azure or AWS ECS |
        | Best for Learning | Docker Compose |
        | Enterprise Grade | Helm (Pro), Terraform (with Pro arch) |
        
        ## Migration Paths
        
        ### Common Upgrade Paths:
        
        ```
        Docker Compose
           ↓
        Helm (Standalone)
           ↓
        Helm (Standard)
           ↓
        Helm (Pro/Enterprise)
        ```
        
        ```
        Docker Compose
           ↓
        Aspire Azure
           ↓
        Terraform Azure (for more control)
        ```
        
        ```
        Docker Compose
           ↓
        AWS ECS
           ↓
        AWS EKS (for K8s features)
        ```
        
        ## Cost Comparison (Monthly)
        
        ### Development/Testing
        - Docker Compose: $30-50 (single VM)
        - Helm Standalone: $100-150 (small cluster)
        
        ### Production (Small - Medium)
        - Helm Standard: $450-700
        - Aspire Azure: $400-600
        - AWS ECS: $400-700
        - Terraform Azure: $500-800
        
        ### Production (Large/Enterprise)
        - Helm Pro: $2,000-5,000
        - AWS EKS: $600-2,000
        - Multi-region Enterprise: $6,000-10,000+
        
        **Note**: Costs vary significantly based on:
        - Traffic volume
        - Data storage
        - Number of users
        - High availability requirements
        - Region choices
        - Reserved capacity discounts
        
        ============================================================================
        
        ## Recommendations by Scenario
        
        ### "I just want to try FeatBit"
        → **Docker Compose** (10 minutes to running)
        
        ### "I need this in production quickly on Azure"
        → **Aspire Azure** (20 minutes, fully managed)
        
        ### "I need production-ready with HA on any cloud"
        → **Helm Standard** (works everywhere)
        
        ### "I want Infrastructure as Code"
        → **Terraform Azure** (if Azure) or **Helm** (if multi-cloud)
        
        ### "I'm an AWS shop"
        → **AWS ECS** (simpler) or **AWS EKS** (more features)
        
        ### "I need enterprise features and 99.99% SLA"
        → **Helm Pro** or **Terraform with Pro architecture**
        
        ============================================================================
        """;
    }

    private static string GetDeploymentOverview()
    {
        return """
        # ============================================================================
        # FeatBit Deployment Guide
        # ============================================================================
        
        Welcome to the FeatBit deployment guide! This tool provides comprehensive
        deployment tutorials for various platforms and scenarios.
        
        ## Available Deployment Methods
        
        ### 1. **Helm Chart** (Kubernetes)
        Deploy to any Kubernetes cluster using Helm charts.
        - Topic: `helm`, `kubernetes`, or `k8s`
        - Subtopics: `standalone`, `standard`, `pro`
        
        ### 2. **Aspire Azure**
        Deploy to Azure using .NET Aspire and Azure Developer CLI.
        - Topic: `aspire-azure`, `dotnet-aspire`, or `aspire`
        
        ### 3. **Terraform Azure**
        Infrastructure as Code deployment to Azure.
        - Topic: `terraform-azure`, `terraform`, or `iac`
        
        ### 4. **AWS Deployment**
        Deploy to Amazon Web Services (multiple options).
        - Topic: `aws` or `amazon`
        - Subtopics: `ecs`, `eks`, or `ec2`
        
        ### 5. **Docker Compose**
        Simple single-server deployment with Docker Compose.
        - Topic: `docker-compose`, `docker`, or `compose`
        
        ### 6. **Architecture Guides**
        Detailed architecture documentation for different editions.
        - Topic: `architecture`, `arch`, or `versions`
        - Subtopics: `standalone`, `standard`, or `pro`/`enterprise`
        
        ## Quick Start Examples
        
        ```
        # Get Helm deployment tutorial
        get_deployment_tutorial("helm")
        
        # Get AWS ECS deployment tutorial
        get_deployment_tutorial("aws", "ecs")
        
        # Get architecture guide for standard edition
        get_deployment_tutorial("architecture", "standard")
        
        # List all available deployments
        list_deployments()
    
        ```
        
        ## Need Help Choosing?
        
        Or use the `list_deployments()` tool to see all available options
        with descriptions and difficulty levels.
        
        ============================================================================
        """;
    }
}
