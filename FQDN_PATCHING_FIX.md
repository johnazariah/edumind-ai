# FQDN Patching Fix for Azure Container Apps Health Checks

## Issue

Health checks were failing with status **503 Unhealthy** at:

- <https://webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io/health>

**Root Cause:**
The connection string FQDN patching code in `Program.cs` was never executing because the required environment variable `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` was not being set in the container.

## Investigation

### Logs Showed

```
[2025-10-20 11:41:15.783 +00:00] [INF] [] PostgreSQL Host: postgres
[2025-10-20 11:41:15.783 +00:00] [INF] [] Redis Host: cache:6379
```

This indicated that the FQDN patching was NOT working, as the hostnames should have been:

- `postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io`
- `cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io`

### Analysis

1. **Bicep Infrastructure** (`infra/resources.bicep` and `infra/main.bicep`):
   - Correctly outputs `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` from infrastructure
   - This is available to azd templating system

2. **Container App Template** (`src/infra/webapi.tmpl.yaml`):
   - ❌ **MISSING**: Environment variable not passed to the container
   - The variable was used in templating (e.g., for Ollama URL) but NOT passed to the running container
   - Other containers (studentapp, dashboard) already use this variable in their env configurations

3. **Program.cs** (`src/AcademicAssessment.Web/Program.cs`):
   - FQDN patching code was correct
   - But it checks: `var azureContainerAppsDomain = builder.Configuration["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"];`
   - Since the env var was never set in the container, this was always `null`
   - Therefore, patching never executed

## Solution

### Fix 1: Add Environment Variable to Container (CRITICAL)

**File**: `src/infra/webapi.tmpl.yaml`

**Change**: Added the environment variable to the container definition:

```yaml
- name: AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
  value: {{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}
```

This ensures the runtime application can access the domain name for FQDN patching.

**Commit**: `a09f85b` - "Fix: Add AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN env var to webapi container"

### Fix 2: Enhanced Logging

**File**: `src/AcademicAssessment.Web/Program.cs`

**Change**: Added detailed logging to diagnose patching behavior:

```csharp
Log.Information("Azure Container Apps Domain: {Domain}", azureContainerAppsDomain ?? "(not set)");
Log.Information("Checking PostgreSQL connection string for hostname patching...");
// ... with success/failure indicators
```

**Commit**: `fe7f5a7` - "Add detailed logging for connection string FQDN patching"

## Expected Outcome

After deployment, logs should show:

```
[INFO] Azure Container Apps Domain: kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Checking PostgreSQL connection string for hostname patching...
[INFO] ✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Checking Redis connection string for hostname patching...
[INFO] ✅ Patched Redis hostname to use Azure Container Apps internal FQDN: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] PostgreSQL Host: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Redis Host: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
```

Health check should then return **200 Healthy**.

## Timeline

- **Oct 20, 2025 11:40 UTC**: Identified health check returning 503
- **Oct 20, 2025 11:41 UTC**: Analyzed container logs, found short hostnames still in use
- **Oct 20, 2025 11:45 UTC**: Discovered missing environment variable in container template
- **Oct 20, 2025 11:47 UTC**: Applied fix and redeployed

## Related Files

- `src/infra/webapi.tmpl.yaml` - Container app template
- `src/AcademicAssessment.Web/Program.cs` - Connection string patching logic
- `infra/resources.bicep` - Infrastructure outputs
- `infra/main.bicep` - Main Bicep template

## Lessons Learned

1. **Environment variables must be explicitly passed** to containers, even if they're available in the azd templating environment
2. **Detailed logging is critical** for diagnosing configuration issues in deployed environments
3. **Template variables vs runtime environment variables** are different:
   - `{{ .Env.VAR }}` in templates: Available during infrastructure generation
   - Runtime `builder.Configuration["VAR"]`: Must be explicitly set as container env var

## Verification Steps

After deployment completes:

1. Check health endpoint:

   ```bash
   curl https://webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io/health
   ```

   Expected: `Healthy` response with 200 status

2. Check logs:

   ```bash
   az containerapp logs show --name webapi --resource-group rg-dev --follow false --tail 100
   ```

   Expected: See "✅ Patched" messages and internal FQDNs

3. Verify PostgreSQL connection:

   ```bash
   az containerapp exec --name webapi --resource-group rg-dev --command "psql -h postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io -U postgres -d edumind -c 'SELECT 1'"
   ```

   Expected: Successful connection

## Status

- [x] Issue identified
- [x] Root cause determined
- [x] Fix implemented
- [ ] Deployment in progress
- [ ] Verification pending
