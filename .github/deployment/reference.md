# EduMind.AI Deployment Reference

**Version:** 1.0  
**Last Updated:** October 25, 2025  
**Maintainer:** DevOps Team

This is the comprehensive technical reference for all deployment operations. For step-by-step guides, see the [playbook](./playbook/) directory.

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Architecture](#architecture)
4. [Configuration Reference](#configuration-reference)
5. [Command Reference](#command-reference)
6. [Service Dependencies](#service-dependencies)
7. [Health Checks](#health-checks)
8. [Troubleshooting](#troubleshooting)
9. [Security](#security)
10. [Monitoring](#monitoring)

---

## Overview

### Deployment Targets

| Environment | Platform | Purpose | Auto-Deploy |
|-------------|----------|---------|-------------|
| **Local** | .NET Aspire | Development | N/A |
| **Dev** | Azure Container Apps | Integration testing | Yes (on push to main) |
| **Staging** | Azure Container Apps | Pre-production validation | Manual |
| **Production** | Azure Container Apps | Live system | Manual approval |

### Key Technologies

- **.NET 9.0** - Application runtime
- **.NET Aspire 9.5.1** - Local orchestration
- **Azure Container Apps** - Production hosting
- **Azure Developer CLI (azd)** - Deployment automation
- **Bicep** - Infrastructure as Code
- **GitHub Actions** - CI/CD pipeline

---

## Prerequisites

### Local Development

**Required Software:**

```bash
# .NET SDK 9.0
dotnet --version  # Should show 9.0.x

# Docker Desktop
docker --version  # Should be running

# Azure CLI (for Azure deployments)
az --version  # 2.50.0 or later

# Azure Developer CLI
azd version  # 1.5.0 or later
```

**Installation:**

```bash
# .NET 9.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Docker Desktop
# Download from: https://www.docker.com/products/docker-desktop

# Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Azure Developer CLI
curl -fsSL https://aka.ms/install-azd.sh | bash
```

### Azure Deployment

**Required Azure Resources:**

- Azure subscription with Owner or Contributor role
- Resource group creation permissions
- Container registry access
- Service principal with appropriate permissions

**Required GitHub Secrets:**

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_CLIENT_ID` | Service Principal Application ID | `az ad sp create-for-rbac` |
| `AZURE_CLIENT_SECRET` | Service Principal Password | From above command |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | `az account show --query tenantId` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | `az account show --query id` |

**Create Service Principal:**

```bash
# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "edumind-deploy-sp" \
  --role Contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID> \
  --sdk-auth

# Output will contain clientId, clientSecret, tenantId, subscriptionId
# Add these to GitHub Secrets
```

---

## Architecture

### Local Development Architecture

```
┌─────────────────────────────────────────────────────────┐
│              .NET Aspire AppHost                        │
│              (Service Orchestrator)                      │
└────────┬─────────────────────────────────┬──────────────┘
         │                                 │
    ┌────▼─────┐                     ┌────▼─────┐
    │ External │                     │ Internal │
    │ Services │                     │ Services │
    └────┬─────┘                     └────┬─────┘
         │                                 │
    ┌────▼──────────────┐           ┌─────▼──────────────┐
    │ PostgreSQL:5432   │           │ Web API:5103       │
    │ (Docker)          │           │ Student App:5049   │
    │                   │           │ Dashboard:5050     │
    │ Redis:6379        │           │                    │
    │ (Docker)          │           │                    │
    │                   │           │                    │
    │ Ollama:11434      │           │                    │
    │ (Native/Docker)   │           │                    │
    └───────────────────┘           └────────────────────┘
```

### Azure Production Architecture

```
┌──────────────────────────────────────────────────────────────┐
│          Azure Container Apps Environment                     │
│          (kindplant-xxxxxx.australiaeast.azurecontainerapps.io)│
└────────┬──────────────────────────────────┬──────────────────┘
         │                                  │
    ┌────▼─────────────┐           ┌───────▼──────────────┐
    │ Data Services    │           │ Application Services │
    │ (Managed Azure)  │           │ (Container Apps)     │
    └────┬─────────────┘           └───────┬──────────────┘
         │                                  │
    ┌────▼──────────────────┐         ┌────▼──────────────┐
    │ PostgreSQL Flexible   │         │ webapi            │
    │ psql-*.postgres.*     │         │ studentapp        │
    │ Port: 5432            │         │ dashboard         │
    │                       │         │                   │
    │ Redis (Container)     │         │ Min: 0-2 replicas │
    │ cache.internal.*      │         │ Max: 2-50 replicas│
    │ Port: 6379            │         │                   │
    └───────────────────────┘         └───────────────────┘
```

### Service Dependency Graph

```
webapi
  ├── postgres (required)
  ├── cache/redis (required)
  └── ollama (optional, dev only)

studentapp
  ├── webapi (required)
  └── cache/redis (required for SignalR)

dashboard
  ├── webapi (required)
  └── cache/redis (required for SignalR)
```

---

## Configuration Reference

### Environment Variables

#### Common (All Environments)

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | `Development`, `Production` | Yes |
| `DOTNET_ENVIRONMENT` | .NET runtime environment | `Development`, `Production` | Yes |

#### Database Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__AcademicDatabase` | PostgreSQL connection | `Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=xxx` | Yes |
| `POSTGRES_HOST` | PostgreSQL hostname (Azure) | `psql-xxx.postgres.database.azure.com` | Azure only |
| `POSTGRES_DATABASE` | Database name | `edumind` | Azure only |
| `POSTGRES_USERNAME` | Database username | `postgres` | Azure only |
| `POSTGRES_PASSWORD` | Database password | (secret) | Azure only |

#### Cache Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379,password=xxx` | Yes |
| `cache_password` | Redis password | (secret) | Azure only |

#### LLM Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `LLM__Provider` | LLM provider | `Ollama`, `AzureOpenAI`, `Stub` | Yes |
| `Ollama__BaseUrl` | Ollama endpoint | `http://localhost:11434` | If using Ollama |
| `Ollama__ModelName` | Ollama model | `llama3.2:3b` | If using Ollama |
| `AzureOpenAI__Endpoint` | Azure OpenAI endpoint | `https://xxx.openai.azure.com/` | If using Azure OpenAI |
| `AzureOpenAI__ApiKey` | Azure OpenAI API key | (secret) | If using Azure OpenAI |

#### Azure-Specific

| Variable | Description | Auto-Injected | Example |
|----------|-------------|---------------|---------|
| `CONTAINER_APP_NAME` | Container app name | Yes | `webapi` |
| `CONTAINER_APP_HOSTNAME` | Full FQDN | Yes | `webapi.kindplant-xxx.australiaeast.azurecontainerapps.io` |
| `CONTAINER_APP_REVISION` | Revision name | Yes | `webapi--xxx` |
| `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` | Environment domain | No (should be) | `kindplant-xxx.australiaeast.azurecontainerapps.io` |

### Connection String Formats

#### PostgreSQL

**Local:**

```
Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=password123;Include Error Detail=true
```

**Azure (after FQDN patching):**

```
Host=psql-c6fvx6uzvxmv6.postgres.database.azure.com;Port=5432;Database=edumind;Username=postgres;Password=xxx;SSL Mode=Require
```

#### Redis

**Local:**

```
localhost:6379
```

**Azure (after FQDN patching):**

```
cache.internal.kindplant-6461f562.australiaeast.azurecontainerapps.io:6379,password=xxx
```

### Runtime FQDN Detection (Workaround)

Due to azd template variable substitution issues, connection strings are patched at runtime:

**Detection Logic:**

```csharp
// Extract domain from CONTAINER_APP_HOSTNAME
var containerAppHostname = Environment.GetEnvironmentVariable("CONTAINER_APP_HOSTNAME");
// Example: webapi.kindplant-395ab1c0.eastus.azurecontainerapps.io
// Extract: kindplant-395ab1c0.eastus.azurecontainerapps.io

if (!string.IsNullOrEmpty(containerAppHostname) && containerAppHostname.Contains(".azurecontainerapps.io"))
{
    var parts = containerAppHostname.Split('.');
    if (parts.Length >= 4)
    {
        azureContainerAppsDomain = string.Join(".", parts.Skip(1));
    }
}
```

**Patching Logic:**

```csharp
// Patch PostgreSQL
connectionString = connectionString.Replace(
    "Host=postgres;",
    $"Host=postgres.internal.{azureContainerAppsDomain};"
);

// Patch Redis
connectionString = connectionString.Replace(
    "cache:",
    $"cache.internal.{azureContainerAppsDomain}:"
);
```

---

## Command Reference

### Local Development

#### Start Application (Aspire)

```bash
# Navigate to repository root
cd /path/to/edumind-ai

# Run Aspire AppHost
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj

# Aspire Dashboard will open automatically
# Access services:
# - Aspire Dashboard: http://localhost:15000 (or shown in console)
# - Web API: https://localhost:5103
# - Student App: https://localhost:5049
# - Dashboard: https://localhost:5050
```

#### Database Migrations

```bash
# Add new migration
dotnet ef migrations add <MigrationName> \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Apply migrations to local database
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Generate SQL script (for review)
dotnet ef migrations script \
  --project src/AcademicAssessment.Infrastructure \
  --output migrations.sql

# Rollback to specific migration
dotnet ef database update <MigrationName> \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web
```

#### Seed Demo Data

```bash
# Using psql (PostgreSQL client)
psql -h localhost -p 5432 -U edumind_user -d edumind_dev \
  -f scripts/seed-demo-data-final.sql

# Or using Docker exec
docker exec -i <postgres-container-id> psql -U edumind_user -d edumind_dev \
  < scripts/seed-demo-data-final.sql
```

#### Build and Test

```bash
# Restore dependencies
dotnet restore EduMind.AI.sln

# Build solution
dotnet build EduMind.AI.sln --configuration Release

# Run unit tests
dotnet test tests/AcademicAssessment.Tests.Unit \
  --configuration Release \
  --verbosity normal

# Run integration tests
dotnet test tests/AcademicAssessment.Tests.Integration \
  --configuration Release \
  --verbosity normal

# Run all tests with coverage
dotnet test EduMind.AI.sln \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --settings tests/coverlet.runsettings
```

### Azure Deployment

#### Azure Developer CLI (azd)

```bash
# Login to Azure
azd auth login

# Initialize new environment
azd env new <environment-name> \
  --subscription <subscription-id> \
  --location <azure-region>

# Select environment
azd env select <environment-name>

# Set environment variables
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>
azd env set AZURE_LOCATION <region>

# Deploy infrastructure and applications
azd up --no-prompt

# Deploy only applications (skip infrastructure)
azd deploy

# View deployment outputs
azd env get-values

# Delete environment and all resources
azd down --force --purge
```

#### Azure CLI Commands

**Container Apps:**

```bash
# List container apps
az containerapp list \
  --resource-group rg-<environment> \
  --output table

# Show container app details
az containerapp show \
  --name <app-name> \
  --resource-group rg-<environment> \
  --output json

# View logs (streaming)
az containerapp logs show \
  --name <app-name> \
  --resource-group rg-<environment> \
  --follow \
  --tail 100

# View logs (query)
az containerapp logs show \
  --name <app-name> \
  --resource-group rg-<environment> \
  --query "logs[?contains(message, 'ERROR')]"

# Scale container app
az containerapp update \
  --name <app-name> \
  --resource-group rg-<environment> \
  --min-replicas 2 \
  --max-replicas 10

# Update environment variables
az containerapp update \
  --name <app-name> \
  --resource-group rg-<environment> \
  --set-env-vars "KEY=VALUE"

# Restart container app
az containerapp revision restart \
  --name <app-name> \
  --resource-group rg-<environment> \
  --revision <revision-name>
```

**PostgreSQL:**

```bash
# Show database server
az postgres flexible-server show \
  --name <server-name> \
  --resource-group rg-<environment>

# List databases
az postgres flexible-server db list \
  --server-name <server-name> \
  --resource-group rg-<environment>

# Connect to database
az postgres flexible-server connect \
  --name <server-name> \
  --admin-user <username> \
  --admin-password <password> \
  --database-name <database>

# Update firewall rule
az postgres flexible-server firewall-rule create \
  --name AllowMyIP \
  --resource-group rg-<environment> \
  --server-name <server-name> \
  --start-ip-address <your-ip> \
  --end-ip-address <your-ip>

# View server parameters
az postgres flexible-server parameter list \
  --server-name <server-name> \
  --resource-group rg-<environment>
```

**Redis:**

```bash
# Show Redis cache (if using Azure Redis)
az redis show \
  --name <cache-name> \
  --resource-group rg-<environment>

# Get access keys
az redis list-keys \
  --name <cache-name> \
  --resource-group rg-<environment>

# For containerized Redis in Container Apps:
az containerapp show \
  --name cache \
  --resource-group rg-<environment>
```

**Application Insights:**

```bash
# Query logs
az monitor app-insights query \
  --app <app-insights-name> \
  --resource-group rg-<environment> \
  --analytics-query "traces | where timestamp > ago(1h) | limit 100"

# List metrics
az monitor app-insights metrics show \
  --app <app-insights-name> \
  --resource-group rg-<environment> \
  --metric requests/count \
  --aggregation count
```

---

## Service Dependencies

### Startup Order

1. **PostgreSQL** - Database must be available first
2. **Redis** - Cache required for sessions
3. **Web API** - Core API services
4. **Student App** - Depends on Web API
5. **Dashboard** - Depends on Web API

### Health Check Dependencies

Each service checks its dependencies during health checks:

**Web API Health Check (`/health`):**

- PostgreSQL connection
- Redis connection
- Agent initialization

**Student App Health Check (`/health`):**

- Web API availability
- Redis connection (for SignalR)

**Dashboard Health Check (`/health`):**

- Web API availability
- Redis connection (for SignalR)

---

## Health Checks

### Endpoints

| Service | Endpoint | Expected Response | Checks |
|---------|----------|-------------------|--------|
| Web API | `/health` | `"Healthy"` | PostgreSQL, Redis, Agents |
| Web API | `/health/ready` | `200 OK` | Service ready |
| Web API | `/health/live` | `200 OK` | Service alive |
| Student App | `/health` | `200 OK` | Web API, Redis |
| Dashboard | `/health` | `200 OK` | Web API, Redis |

### Testing Health Checks

```bash
# Local
curl http://localhost:5103/health
curl http://localhost:5049/health
curl http://localhost:5050/health

# Azure
curl https://webapi.<environment-domain>.azurecontainerapps.io/health
curl https://studentapp.<environment-domain>.azurecontainerapps.io/health
curl https://dashboard.<environment-domain>.azurecontainerapps.io/health
```

### Health Check Configuration

**ASP.NET Core (`Program.cs`):**

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: postgresConnection,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" })
    .AddRedis(
        redisConnectionString: redisConnection,
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "cache", "redis" });

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");
```

---

## Troubleshooting

### Common Issues

#### Issue: 6-Hour GitHub Actions Timeout

**Symptom:** Deployment workflow times out after 6 hours

**Cause:** Aspire AppHost running instead of generating manifest

**Solution:** Use `dotnet publish` instead of `dotnet run --dry-run`

```yaml
# WRONG (causes timeout)
- name: Validate Aspire manifest
  run: dotnet run --project src/EduMind.AppHost --dry-run

# CORRECT
- name: Generate Aspire manifest
  run: dotnet publish src/EduMind.AppHost --configuration Release --output ./aspire-manifest
  timeout-minutes: 5
```

#### Issue: Template Variable Substitution Failure

**Symptom:** Health check returns "Unhealthy", logs show connection to "postgres" instead of FQDN

**Cause:** azd template variables not substituted

**Solution:** Runtime FQDN detection is implemented as workaround

**Verify:**

```bash
# Check logs for patching messages
az containerapp logs show --name webapi --resource-group rg-<env> --tail 50 \
  | grep -E "Detected Azure|Patched.*FQDN"

# Should see:
# ✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN
# ✅ Patched Redis hostname to use Azure Container Apps internal FQDN
```

#### Issue: Blazor SignalR Navigation Failures

**Symptom:** `@onclick` events don't work, navigation unresponsive

**Cause:** SignalR connection issues during long-running operations

**Solution:** Use HTML anchor links instead of `NavigationManager`

```razor
@* CORRECT - reliable *@
<a href="/assessment/@AssessmentId/session" class="btn btn-primary">
    Start Assessment
</a>

@* AVOID - may fail *@
<button @onclick="NavigateToSession" class="btn btn-primary">
    Start Assessment
</button>
```

**Reference:** ADR-063

#### Issue: Docker Not Running

**Symptom:** Aspire fails to start with "Docker is not available"

**Solution:**

```bash
# Check Docker status
docker ps

# Start Docker Desktop (GUI)
# Or on Linux:
sudo systemctl start docker
```

#### Issue: Port Already in Use

**Symptom:** "Address already in use" error

**Solution:**

```bash
# Find process using port
netstat -an | grep 5103
# or
lsof -i :5103

# Kill process
kill -9 <PID>
```

---

## Security

### Secrets Management

**Local Development:**

- Use User Secrets: `dotnet user-secrets set "Key" "Value" --project src/AcademicAssessment.Web`
- Never commit secrets to source control

**Azure Production:**

- Store secrets in Azure Key Vault
- Reference in connection strings: `@Microsoft.KeyVault(SecretUri=...)`
- Use Managed Identity for service authentication

### Network Security

**Local:**

- Services communicate over localhost
- HTTPS with self-signed certificates

**Azure:**

- Internal service communication uses internal FQDNs (`.internal.<domain>`)
- External ingress enforces HTTPS
- Network isolation via Virtual Network integration
- Firewall rules on PostgreSQL Flexible Server

### Authentication

- Azure AD B2C for user authentication
- Service Principal for deployment automation
- Managed Identity for Azure resource access

---

## Monitoring

### Application Insights

**Query Examples:**

```kusto
// Errors in last hour
traces
| where timestamp > ago(1h)
| where severityLevel >= 3
| project timestamp, message, severityLevel
| order by timestamp desc

// Request performance
requests
| where timestamp > ago(1h)
| summarize avg(duration), count() by name
| order by avg_duration desc

// Dependency calls
dependencies
| where timestamp > ago(1h)
| where success == false
| project timestamp, name, duration, resultCode
```

### Metrics to Monitor

| Metric | Threshold | Action |
|--------|-----------|--------|
| Request failure rate | >5% | Investigate errors |
| Response time P95 | >2s | Performance optimization |
| Container CPU | >80% | Scale out |
| Container Memory | >90% | Scale out or optimize |
| Database connections | >80% of max | Increase pool size |
| Redis memory | >80% | Increase cache size |

---

## Quick Reference

### Deployment URLs

**Local:**

- Aspire Dashboard: `http://localhost:15000`
- Web API: `https://localhost:5103`
- Student App: `https://localhost:5049`
- Dashboard: `https://localhost:5050`

**Azure (Dev):**

- Web API: `https://webapi.<environment-domain>.azurecontainerapps.io`
- Student App: `https://studentapp.<environment-domain>.azurecontainerapps.io`
- Dashboard: `https://dashboard.<environment-domain>.azurecontainerapps.io`

### Key Files

| File | Purpose |
|------|---------|
| `azure.yaml` | Azure Developer CLI configuration |
| `infra/main.bicep` | Infrastructure as Code (entry point) |
| `infra/resources.bicep` | Azure resource definitions |
| `src/EduMind.AppHost/Program.cs` | Aspire orchestration |
| `.github/workflows/deploy-azure-azd.yml` | CI/CD pipeline |

### Support Resources

- **Architecture Decisions:** `.github/adr/`
- **Playbooks:** `.github/deployment/playbook/`
- **Deployment Docs:** `docs/deployment/`
- **GitHub Issues:** <https://github.com/johnazariah/edumind-ai/issues>
