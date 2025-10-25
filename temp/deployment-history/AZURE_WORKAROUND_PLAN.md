# Azure Template Substitution Workaround Plan

**Date:** 2025-01-24  
**Status:** 📋 PLANNED  
**Priority:** HIGH - Unblocks Azure deployment

## Problem Statement

The azd template variable substitution in `src/infra/webapi.tmpl.yaml` is not working correctly. Template variables like `{{ .Env.POSTGRES_HOST }}` are not being replaced with actual values from Bicep outputs, resulting in connection strings with short hostnames like `Host=postgres` instead of Azure Database FQDNs like `Host=psql-c6fvx6uzvxmv6.postgres.database.azure.com`.

**Evidence:**

- Local validation: ✅ Health endpoint returns "Healthy" with proper connection strings
- Azure deployment: ❌ Health endpoint returns "Unhealthy" with template literals unsubstituted
- Azure logs show: `PostgreSQL Host: postgres` (should be full FQDN)

## Available Workarounds

### Option A: Manual Secret Injection (RECOMMENDED)

**Approach:** Bypass template substitution entirely by manually injecting connection strings as secrets using Azure CLI.

**Pros:**

- ✅ Immediate solution - no code changes required
- ✅ Works with existing deployment
- ✅ Verifies hypothesis quickly
- ✅ Can be scripted for repeatability

**Cons:**

- ❌ Manual step in deployment process
- ❌ Requires secret management outside IaC
- ❌ Password must be passed as argument

**Implementation:**

```bash
#!/bin/bash
# File: scripts/inject-azure-secrets.sh

set -e

RESOURCE_GROUP="${1:-rg-staging}"
POSTGRES_PASSWORD="${2}"

if [ -z "$POSTGRES_PASSWORD" ]; then
    echo "Usage: $0 <resource-group> <postgres-password>"
    exit 1
fi

echo "🔧 Injecting connection strings for resource group: $RESOURCE_GROUP"

# Get PostgreSQL FQDN
POSTGRES_HOST=$(az postgres flexible-server list \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0].fullyQualifiedDomainName" -o tsv)

echo "📊 PostgreSQL Host: $POSTGRES_HOST"

# Construct connection string
CONNECTION_STRING="Host=${POSTGRES_HOST};Port=5432;Username=edumind_admin;Password=${POSTGRES_PASSWORD};Database=edumind;SslMode=Require"

# Inject as secret
echo "💉 Injecting connection string secret..."
az containerapp secret set \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --secrets connectionstrings--edumind="$CONNECTION_STRING"

# Update environment variable to use secret
echo "🔗 Updating environment variable..."
az containerapp update \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "ConnectionStrings__DefaultConnection=secretref:connectionstrings--edumind"

echo "✅ Connection string injected successfully"
echo "🔄 Container app will restart automatically"
echo ""
echo "⏳ Wait ~30s then test: https://webapi.kindplant-6461f562.australiaeast.azurecontainerapps.io/health"
```

**Usage:**

```bash
# Get PostgreSQL password from Key Vault or environment
POSTGRES_PASS=$(az keyvault secret show --name postgres-password --vault-name <vault> --query value -o tsv)

# Run injection script
chmod +x scripts/inject-azure-secrets.sh
./scripts/inject-azure-secrets.sh rg-staging "$POSTGRES_PASS"

# Test health endpoint
sleep 30
curl https://webapi.kindplant-6461f562.australiaeast.azurecontainerapps.io/health
```

---

### Option B: Runtime Environment Variable Construction

**Approach:** Modify `Program.cs` to construct connection string from individual environment variables at runtime.

**Pros:**

- ✅ No manual deployment steps
- ✅ Works with standard environment variables
- ✅ Maintainable in code

**Cons:**

- ❌ Requires code changes
- ❌ Moves configuration logic into application
- ❌ Still needs template files to provide individual variables

**Implementation:**

```csharp
// In src/AcademicAssessment.Web/Program.cs

// Add after line 180 (current connection string detection)
var postgresHost = builder.Configuration["POSTGRES_HOST"];
var postgresDb = builder.Configuration["POSTGRES_DATABASE"];
var postgresUser = builder.Configuration["POSTGRES_USERNAME"];
var postgresPass = builder.Configuration["POSTGRES_PASSWORD"];

if (!string.IsNullOrEmpty(postgresHost) && 
    !string.IsNullOrEmpty(postgresDb) && 
    !string.IsNullOrEmpty(postgresUser) && 
    !string.IsNullOrEmpty(postgresPass))
{
    Console.WriteLine("🔧 Constructing PostgreSQL connection string from environment variables");
    var constructedConnectionString = $"Host={postgresHost};Port=5432;Username={postgresUser};Password={postgresPass};Database={postgresDb};SslMode=Require";
    
    builder.Configuration["ConnectionStrings:DefaultConnection"] = constructedConnectionString;
    Console.WriteLine($"📊 Constructed PostgreSQL Host: {postgresHost}");
}
```

**Template Changes:**

```yaml
# src/infra/webapi.tmpl.yaml
env:
  - name: POSTGRES_HOST
    value: {{ .Env.POSTGRES_HOST }}
  - name: POSTGRES_DATABASE
    value: {{ .Env.POSTGRES_DATABASE }}
  - name: POSTGRES_USERNAME
    value: {{ .Env.POSTGRES_USERNAME }}
  - name: POSTGRES_PASSWORD
    secretRef: postgres-password
```

**Issues:**

- Still relies on template substitution for individual variables
- Doesn't solve root cause if `{{ .Env.* }}` isn't working

---

### Option C: Direct Azure CLI Deployment

**Approach:** Bypass azd entirely, use direct Azure CLI commands with Bicep outputs.

**Pros:**

- ✅ Complete control over deployment
- ✅ No template substitution issues
- ✅ Can be scripted reliably

**Cons:**

- ❌ Most invasive change
- ❌ Loses azd integration
- ❌ More complex deployment script

**Implementation:**

```bash
#!/bin/bash
# File: scripts/deploy-direct.sh

set -e

RESOURCE_GROUP="${1:-rg-staging}"
LOCATION="${2:-australiaeast}"

echo "🚀 Direct Azure Deployment"

# Deploy Bicep infrastructure
echo "📦 Deploying infrastructure..."
az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infra/main.bicep \
    --parameters infra/main.parameters.json

# Get outputs
POSTGRES_HOST=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name main \
    --query properties.outputs.POSTGRES_HOST.value -o tsv)

CONTAINER_APP_ENV=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name main \
    --query properties.outputs.CONTAINER_APP_ENVIRONMENT_NAME.value -o tsv)

# Build and push container image
echo "🐳 Building container image..."
docker build -t edumind-webapi:latest -f src/AcademicAssessment.Web/Dockerfile .

# Tag and push to ACR
ACR_NAME=$(az acr list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)
az acr login --name "$ACR_NAME"
docker tag edumind-webapi:latest "${ACR_NAME}.azurecr.io/webapi:latest"
docker push "${ACR_NAME}.azurecr.io/webapi:latest"

# Create/update Container App with direct values
echo "📦 Deploying Container App..."
CONNECTION_STRING="Host=${POSTGRES_HOST};Port=5432;Username=edumind_admin;Password=${POSTGRES_PASSWORD};Database=edumind;SslMode=Require"

az containerapp create \
    --name webapi \
    --resource-group "$RESOURCE_GROUP" \
    --environment "$CONTAINER_APP_ENV" \
    --image "${ACR_NAME}.azurecr.io/webapi:latest" \
    --target-port 8080 \
    --ingress external \
    --secrets "connectionstrings--edumind=$CONNECTION_STRING" \
    --env-vars "ConnectionStrings__DefaultConnection=secretref:connectionstrings--edumind" \
    --min-replicas 1 \
    --max-replicas 3 \
    --cpu 0.5 \
    --memory 1.0Gi

echo "✅ Deployment complete"
```

---

## Recommended Approach

**🎯 Start with Option A (Manual Secret Injection)**

### Rationale

1. **Fastest validation:** Proves hypothesis within 5 minutes
2. **Zero code changes:** Tests with existing deployment
3. **Low risk:** Reversible - can revert if unsuccessful
4. **Immediate feedback:** Health endpoint should return "Healthy" right away

### If Option A Succeeds

1. Document as temporary workaround in runbook
2. Consider implementing Option B for long-term solution
3. File issue with azd team about template substitution
4. Update deployment documentation

### If Option A Fails

1. Investigate Container App logs for new error
2. Verify secret injection using `az containerapp show`
3. Consider network connectivity issues (NSG, firewall rules)
4. Escalate to Azure support

## Success Criteria

✅ Health endpoint returns: `"Healthy"`  
✅ Container App logs show: `PostgreSQL Host: psql-c6fvx6uzvxmv6.postgres.database.azure.com`  
✅ Database connectivity confirmed from Container App  
✅ Redis connectivity confirmed from Container App  

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Secret injection fails | Low | High | Test with dry-run, validate secret exists |
| Container App doesn't restart | Low | Medium | Manual restart: `az containerapp restart` |
| Password in script logs | Medium | High | Use `set +x`, clear history, store in Key Vault |
| Template substitution works later | Low | Low | Monitor azd updates, revert workaround if fixed |

## Implementation Timeline

1. **Immediate:** Implement Option A injection script (15 min)
2. **Test:** Run against rg-staging environment (5 min)
3. **Validate:** Check health endpoint and logs (5 min)
4. **Document:** Update deployment runbook (10 min)
5. **Total:** ~35 minutes to working Azure deployment

## Success Metrics

- **Time to Healthy:** < 60 seconds after injection
- **Deployment Reliability:** 100% success rate for secret injection
- **Manual Steps:** 1 script execution per deployment
- **Maintenance Overhead:** Low - script updates as needed

## Next Steps After Success

1. ✅ Verify health checks pass on Azure
2. ✅ Run end-to-end API tests
3. ✅ Update deployment documentation
4. ✅ Resume feature development (NEXT_STEPS.md)
5. 📋 File azd template substitution bug report
6. 📋 Consider long-term solution (Option B or automated Option A)

---

**Note:** Local validation confirmed application works correctly. This workaround addresses only the Azure template substitution issue, not application architecture.
