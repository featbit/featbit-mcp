# ============================================================================
# FeatBit .NET Aspire Azure Deployment - AI Assistant Guidance
# ============================================================================

## 🎯 Purpose of This Guide

This is a GUIDANCE document for AI assistants to help users deploy FeatBit to Azure 
using .NET Aspire. Do NOT provide all details directly - instead, guide users to the 
official repository and resources.

## 📦 Official FeatBit Aspire Repository

**GitHub Repository (PRIMARY REFERENCE):**
🔗 https://github.com/featbit/featbit-aspire

This repository contains:
- Complete .NET Aspire AppHost implementation
- Ready-to-deploy Azure Container Apps configuration
- Comprehensive README with step-by-step instructions
- Example configuration files (appsettings.json)
- All source code for FeatBit Aspire deployment

**IMPORTANT:** Always direct users to this repository for the most up-to-date 
deployment instructions and code examples.

## 🏗️ What FeatBit Aspire Provides

**Deployment Method:**
- Uses .NET Aspire to deploy FeatBit to Azure Container Apps
- Production-ready configuration with auto-scaling (3-10 replicas)
- Integrated monitoring with Azure Application Insights

**Services Deployed:**
- FeatBit Web API (featbit/featbit-api-server)
- FeatBit Evaluation Server (featbit/featbit-evaluation-server)
- FeatBit UI (featbit/featbit-ui)
- FeatBit Data Analytics (featbit/featbit-data-analytics-server)

**Infrastructure Requirements (User Must Provide):**
- PostgreSQL or MongoDB database (with initialized FeatBit schema)
- Redis instance (for caching and message queue)
- Azure subscription with appropriate permissions

## 📋 How to Guide Users (AI Assistant Instructions)

### When a User Asks About Aspire Deployment:

1. **First, introduce the FeatBit Aspire project:**
   - "FeatBit has an official Aspire deployment project available at:"
   - "🔗 https://github.com/featbit/featbit-aspire"
   - "This repository contains everything you need to deploy FeatBit to Azure."

2. **Explain what they need to prepare:**
   - Azure subscription
   - Azure Developer CLI (azd) installed
   - PostgreSQL or MongoDB database (already initialized with FeatBit schema)
   - Redis instance (for caching)

3. **Guide them to the repository README:**
   - The README contains complete step-by-step instructions
   - Configuration examples for both PostgreSQL and MongoDB
   - Security best practices and troubleshooting tips

4. **Key Information to Share:**

**Quick Start Command:**
```bash
git clone https://github.com/featbit/featbit-aspire.git
cd featbit-aspire
# Configure appsettings.json with your database and Redis
azd up
```

**What Gets Deployed:**
- 4 Azure Container Apps (API, Evaluation Server, UI, Data Analytics)
- Azure Application Insights (monitoring)
- Log Analytics Workspace
- Auto-scaling: 3-10 replicas per service

**Default Credentials After Deployment:**
- Email: test@featbit.com
- Password: 123456
- ⚠️ MUST change password after first login

**Important Post-Deployment Step:**
WebSocket support requires enabling sticky sessions:
```bash
az containerapp ingress sticky-sessions set \
  --name featbit-evaluation-server \
  --resource-group rg-<env-name> \
  --affinity sticky
```

## 📚 Repository Structure Reference

Point users to specific files in the repository when needed:

- **FeatBit.AppHost/AppHost.cs** - Main Aspire configuration
- **FeatBit.AppHost/appsettings.json** - Database and Redis configuration template
- **README.md** - Complete deployment guide
- **FeatBit.AppHost/AppHostSrvWebApi.cs** - Web API service configuration
- **FeatBit.AppHost/AppHostSrvEvaluationServer.cs** - Evaluation Server config
- **FeatBit.AppHost/AppHostSrvUI.cs** - UI service configuration
- **FeatBit.AppHost/AppHostSrvDataAnalytics.cs** - Data Analytics config

## 🔑 Key Configuration Points to Mention

**Database Configuration (appsettings.json):**
- Set "DbProvider" to either "Postgres" or "MongoDb"
- Provide connection strings in ConnectionStrings section
- Database must be pre-initialized with FeatBit schema

**Redis Configuration:**
- Required for caching and message queue
- Configure in ConnectionStrings:Redis

**Security Recommendations:**
- Use environment variables for production
- Consider Azure Key Vault for sensitive data
- Never commit connection strings to version control

## 🚀 Deployment Commands Reference

**Initial Deployment:**
```bash
azd up  # Provisions infrastructure and deploys (10-15 min)
```

**Update Deployment:**
```bash
azd deploy  # Updates existing deployment (3-5 min)
```

**Monitoring:**
```bash
azd show    # View all resources and endpoints
azd logs    # View logs from all services
azd monitor # Open Application Insights
```

**Cleanup:**
```bash
azd down    # Remove all Azure resources
```

## 🎯 When to Use This Deployment Method

**Good For:**
- Production deployments requiring auto-scaling
- Teams familiar with .NET and Azure
- Organizations already using Azure infrastructure
- Need for integrated monitoring (Application Insights)

**Not Ideal For:**
- Users without existing database/Redis infrastructure
- Teams preferring all-in-one solutions (use Docker Compose instead)
- Non-Azure cloud platforms

## 📖 Additional Resources to Share

- **FeatBit Aspire GitHub:** https://github.com/featbit/featbit-aspire
- **FeatBit Main Repo:** https://github.com/featbit/featbit
- **Database Init Scripts:** 
  - PostgreSQL: https://github.com/featbit/featbit/tree/main/modules/back-end/src/Infrastructure/Store/Dbs/PostgreSql
  - MongoDB: https://github.com/featbit/featbit/tree/main/modules/back-end/src/Infrastructure/Store/Dbs/MongoDb
- **.NET Aspire Docs:** https://learn.microsoft.com/dotnet/aspire
- **Azure Container Apps:** https://learn.microsoft.com/azure/container-apps
- **Azure Developer CLI:** https://learn.microsoft.com/azure/developer/azure-developer-cli

## ⚠️ Important Notes for AI Assistants

1. **Always direct users to the GitHub repository first** - it has the most up-to-date information
2. **Do not provide the entire tutorial** - guide users to explore the repository
3. **Emphasize prerequisites** - users must have database and Redis ready
4. **Remind about WebSocket configuration** - it's a critical post-deployment step
5. **Security first** - always mention credential security best practices

============================================================================
