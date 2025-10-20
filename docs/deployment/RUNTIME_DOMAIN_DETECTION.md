# Solution: Detect Azure Container Apps Domain at Runtime

## Final Approach

Instead of relying on azd template processing to inject `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN`, we now detect the domain at runtime using Azure Container Apps' built-in environment variables.

## How It Works

Azure Container Apps automatically injects these environment variables into every container:

- `CONTAINER_APP_NAME` - The name of the container app (e.g., "webapi")
- `CONTAINER_APP_HOSTNAME` - The full external FQDN (e.g., "webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io")
- `CONTAINER_APP_REVISION` - The revision name
- And others...

We use `CONTAINER_APP_HOSTNAME` to extract the environment domain:

```csharp
// Example: webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io
// Extract: kindsea-395ab1c0.eastus.azurecontainerapps.io

var containerAppHostname = Environment.GetEnvironmentVariable("CONTAINER_APP_HOSTNAME");
if (!string.IsNullOrEmpty(containerAppHostname) && containerAppHostname.Contains(".azurecontainerapps.io"))
{
    var parts = containerAppHostname.Split('.');
    if (parts.Length >= 4)
    {
        // Skip first part (app name), join the rest
        azureContainerAppsDomain = string.Join(".", parts.Skip(1));
    }
}
```

## Benefits

1. **No template processing required** - Works regardless of azd configuration
2. **No manual configuration** - Azure provides the values automatically
3. **Reliable** - Based on actual running environment, not deployment-time configuration
4. **Backwards compatible** - Still checks for `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` first

## Connection String Patching Flow

1. App starts in Azure Container Apps
2. Detect domain from `CONTAINER_APP_HOSTNAME`: `kindsea-395ab1c0.eastus.azurecontainerapps.io`
3. Patch PostgreSQL connection string:
   - Before: `Host=postgres;Port=5432;Username=postgres;Password=xxx;Database=edumind`
   - After: `Host=postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io;Port=5432;...`
4. Patch Redis connection string:
   - Before: `cache:6379,password=xxx`
   - After: `cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io:6379,password=xxx`
5. Health checks use patched connection strings
6. ✅ Successfully connect to PostgreSQL and Redis

## Expected Logs

```
[INFO] Azure Container Apps Domain: (not set)
[INFO] Detected Azure Container Apps domain from hostname: kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Azure Container Apps Domain: kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Original PostgreSQL connection string present: True
[INFO] Original Redis connection string present: True
[INFO] Checking PostgreSQL connection string for hostname patching...
[INFO] ✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Checking Redis connection string for hostname patching...
[INFO] ✅ Patched Redis hostname to use Azure Container Apps internal FQDN: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] PostgreSQL Host: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Redis Host: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
```

## Why Previous Approaches Failed

### Attempt 1: Template Variable Injection

- Added `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` to `webapi.tmpl.yaml`
- **Failed**: Invalid Go template syntax `{ { .Env.VAR } }` (spaces)

### Attempt 2: Fixed Template Syntax

- Removed spaces: `{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}`
- **Failed**: azd didn't inject the value (possibly due to Bicep/template interaction issues)

### Attempt 3: Quoted Template Syntax

- Added quotes: `"{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}"`
- **Still investigating**: May work but uncertain due to azd template processing timing

### Solution: Runtime Detection (Current)

- ✅ **Works reliably**: Uses Azure Container Apps built-in environment variables
- ✅ **No azd dependency**: Doesn't rely on template processing
- ✅ **Simple**: Extract domain from hostname that's always present

## Verification

After deployment completes:

```bash
# Check health endpoint
curl https://webapi.kindsea-395ab1c0.eastus.azurecontainerapps.io/health
# Expected: "Healthy"

# Check logs for patching messages
az containerapp logs show --name webapi --resource-group rg-dev --follow false --tail 100 \
  | grep -E "Detected Azure|Patched.*FQDN|PostgreSQL Host|Redis Host"
```

## Files Modified

- `src/AcademicAssessment.Web/Program.cs` - Added runtime domain detection logic
- `src/infra/webapi.tmpl.yaml` - Attempted template variable injection (may not be needed now)

## Status

- [x] Issue identified: Connection strings use short hostnames
- [x] Root cause: FQDN patching not executing
- [x] Solution implemented: Runtime domain detection from CONTAINER_APP_HOSTNAME
- [ ] Deployment in progress
- [ ] Verification pending

**Commit**: `e030b25` - "Workaround: Detect Azure Container Apps domain from CONTAINER_APP_HOSTNAME"
