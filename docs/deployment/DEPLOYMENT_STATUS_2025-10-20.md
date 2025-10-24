# Deployment Status - October 20, 2025

## Current State

**Environment:** `rg-staging` in Australia East  
**Status:** ⚠️ Infrastructure deployed but application unhealthy  
**Health Endpoint:** <https://webapi.kindplant-6461f562.australiaeast.azurecontainerapps.io/health>  
**Result:** Returns "Unhealthy"

## What's Working ✅

### Infrastructure (All in Australia East)

- ✅ **Azure Database for PostgreSQL Flexible Server**
  - Name: `psql-c6fvx6uzvxmv6`
  - FQDN: `psql-c6fvx6uzvxmv6.postgres.database.azure.com`
  - SKU: Standard_B1ms (Burstable)
  - Version: PostgreSQL 16
  - Status: Ready
  - Firewall: Allows Azure services

- ✅ **Container Apps Environment**
  - Name: `cae-c6fvx6uzvxmv6`
  - Domain: `kindplant-6461f562.australiaeast.azurecontainerapps.io`
  - Location: Australia East

- ✅ **Redis Container**
  - Hostname patching: Working correctly (runtime detection)
  - Internal FQDN: `cache.internal.kindplant-6461f562.australiaeast.azurecontainerapps.io:6379`

- ✅ **Enhanced Logging**
  - Console.WriteLine() logs appearing in Container Apps
  - Connection string details visible (without passwords)
  - Azure domain detection working

- ✅ **Bicep Outputs**
  - All PostgreSQL outputs present and correct:
    - `POSTGRES_HOST="psql-c6fvx6uzvxmv6.postgres.database.azure.com"`
    - `POSTGRES_DATABASE="edumind"`
    - `POSTGRES_USERNAME="edumind_admin"`

## What's Broken ❌

### Template Variable Substitution

**CRITICAL ISSUE:** The `src/infra/webapi.tmpl.yaml` template variables are NOT being substituted during `azd deploy`.

**Template contains:**

```yaml
secrets:
  - name: connectionstrings--edumind
    value: Host={{ .Env.POSTGRES_HOST }};Port=5432;Username={{ .Env.POSTGRES_USERNAME }};Password={{ securedParameter "postgres_password" }};Database={{ .Env.POSTGRES_DATABASE }};SslMode=Require
```

**Expected after substitution:**

```
Host=psql-c6fvx6uzvxmv6.postgres.database.azure.com;Port=5432;...
```

**Actual value in container:**

```
Host=postgres;Port=5432;...
```

**Evidence from logs:**

```
PostgreSQL Host: postgres
PostgreSQL Database: edumind
```

### Health Check Failures

Both PostgreSQL and Redis health checks fail:

- PostgreSQL: Timeout trying to connect to `postgres` (short hostname)
- Redis: Connection failures (despite correct hostname patching)

## Timeline of Attempts

### Attempt 1: East US Deployment (Failed)

- Deployed Container Apps to East US
- PostgreSQL to Australia East (quota issues in East US)
- Cross-region networking issues
- Connection string FQDN patching attempted
- **Result:** Unhealthy, cross-region latency/connectivity issues

### Attempt 2: Template Variable Injection (Failed)

- Attempted to inject `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` via template
- Template syntax issues (`{{ }}` spacing)
- azd not updating container apps with template changes
- **Result:** Environment variable never set

### Attempt 3: Runtime Domain Detection (Partial Success)

- Implemented detection from `CONTAINER_APP_HOSTNAME`
- Added connection string patching in Program.cs
- Redis patching works
- PostgreSQL patching works for containerized postgres
- **Result:** Works for Redis, but doesn't help with Azure Database

### Attempt 4: Migrate to Azure Database for PostgreSQL (Infrastructure Success, Template Failure)

- Added PostgreSQL Flexible Server to Bicep
- Updated webapi template to use `{{ .Env.POSTGRES_HOST }}`
- Bicep outputs correct
- **Result:** Infrastructure deployed, template variables not substituted

### Attempt 5: Fresh Deployment in Australia East (Current State)

- Deleted old resource group
- Deployed everything to Australia East (single region)
- Enhanced logging added
- **Result:** Infrastructure perfect, template substitution still broken

## Root Cause Analysis

### Template Substitution Failure

The issue is NOT with:

- ❌ Infrastructure configuration (PostgreSQL is working)
- ❌ Network connectivity (all in same region)
- ❌ Bicep outputs (verified correct)
- ❌ Runtime code (patching logic works for Redis)

The issue IS with:

- ✅ **azd template processing** - Variables like `{{ .Env.POSTGRES_HOST }}` are not being substituted
- ✅ **Deployment timing** - Container apps may be deployed before PostgreSQL outputs are available
- ✅ **Variable scope** - `.Env.*` variables may not be in scope during template processing

### Comparison: What Works vs What Doesn't

**Redis (WORKS):**

- Short hostname `cache` in template
- Runtime patching in Program.cs converts to FQDN
- Logs show: `cache.internal.kindplant-6461f562.australiaeast.azurecontainerapps.io:6379`

**PostgreSQL (DOESN'T WORK):**

- Template tries to use `{{ .Env.POSTGRES_HOST }}`
- Variable not substituted
- Connection string has `postgres` instead of Azure Database FQDN
- Can't add runtime patching (would need to know the FQDN)

## Lessons Learned

### ✅ What Worked

1. **Console.WriteLine() for Container Logging**
   - More reliable than Serilog for startup diagnostics
   - Appears immediately in Container Apps logs

2. **Single Region Deployment**
   - All resources in Australia East
   - Eliminates cross-region networking issues
   - Simplifies troubleshooting

3. **Runtime Configuration Detection**
   - `CONTAINER_APP_HOSTNAME` environment variable available
   - Can extract domain and patch connection strings
   - Works for containerized services

4. **Enhanced Logging**
   - Connection string details (without passwords)
   - Domain detection logging
   - Clear diagnostic information

### ❌ What Didn't Work

1. **azd Template Variable Substitution**
   - `{{ .Env.* }}` variables not reliably substituted
   - Deployment timing issues
   - No clear errors when substitution fails

2. **Cross-Region Deployments**
   - Container Apps in one region, database in another
   - Latency and connectivity issues
   - Quota constraints forced this approach

3. **Fighting with azd for 4+ Hours**
   - Template processing opaque
   - Hard to debug
   - Time better spent validating fundamentals

## Time Investment

- **Total Time:** ~5 hours
- **Infrastructure Setup:** 30 minutes (multiple attempts)
- **Template Debugging:** 2 hours
- **Cross-Region Issues:** 1 hour
- **Connection String Patching:** 1 hour
- **Fresh Deployment:** 30 minutes

## Next Steps (Recommended)

### 1. Validate Fundamentals Locally

**Stop fighting with azd deployment.** Instead:

1. ✅ Infrastructure is deployed and working (PostgreSQL, Container Apps, etc.)
2. ❓ Test local connection to Azure Database for PostgreSQL
3. ❓ Run webapi locally with connection string to Azure Database
4. ❓ Verify basic functionality works
5. ❓ Test Redis connectivity
6. ❓ Confirm all components can communicate

**Why:** This proves whether the issue is:

- **Deployment tooling (azd)** - we can work around this
- **Fundamental architecture** - we need to rethink approach

### 2. Manual Connection String Injection (Workaround)

If local testing succeeds, we can bypass template substitution:

```bash
# Manually set the connection string secret
az containerapp secret set \
  --name webapi \
  --resource-group rg-staging \
  --secrets connectionstrings--edumind="Host=psql-c6fvx6uzvxmv6.postgres.database.azure.com;Port=5432;Username=edumind_admin;Password=<password>;Database=edumind;SslMode=Require"

# Force new revision
az containerapp revision restart \
  --name webapi \
  --resource-group rg-staging
```

### 3. Alternative: Use Runtime Environment Variables

Instead of template substitution, inject as environment variables:

```csharp
// In Program.cs
var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST");
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
var pgDb = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
var pgPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

if (!string.IsNullOrEmpty(pgHost))
{
    connectionString = $"Host={pgHost};Port=5432;Username={pgUser};Password={pgPass};Database={pgDb};SslMode=Require";
}
```

This approach:

- ✅ Bypasses template substitution entirely
- ✅ Uses environment variables (which DO work)
- ✅ More transparent and debuggable

### 4. Consider Aspire Alternatives

If azd template processing continues to be unreliable:

- Pulumi (TypeScript/Python infrastructure as code)
- Terraform with Azure Provider
- Plain Azure CLI scripts
- Azure Developer CLI with custom templates

## Files Changed

### Infrastructure

- `infra/main.bicep` - Added PostgreSQL outputs
- `infra/resources.bicep` - Added PostgreSQL Flexible Server
- `src/infra/webapi.tmpl.yaml` - Updated connection string template (not working)

### Code

- `src/AcademicAssessment.Web/Program.cs` - Enhanced logging, runtime domain detection

### Documentation

- `docs/deployment/FRESH_AUSTRALIA_EAST_DEPLOYMENT.md`
- `docs/deployment/TEMPLATE_SUBSTITUTION_ISSUE.md`
- `docs/deployment/DEPLOYMENT_STATUS_2025-10-20.md` (this file)

### Workflow

- `.github/workflows/deploy-azure-azd.yml` - Fixed hardcoded `rg-dev` reference

## Resources Deployed

### Resource Group: rg-staging (Australia East)

| Resource Type | Name | Status | Purpose |
|--------------|------|--------|---------|
| PostgreSQL Flexible Server | psql-c6fvx6uzvxmv6 | ✅ Ready | Primary database |
| Container Apps Environment | cae-c6fvx6uzvxmv6 | ✅ Running | Application hosting |
| Container App (webapi) | webapi | ⚠️ Unhealthy | Web API |
| Container App (cache) | cache | ✅ Running | Redis cache |
| Container App (ollama) | ollama | ✅ Running | AI model server |
| Container Registry | acrc6fvx6uzvxmv6 | ✅ Running | Container images |
| Log Analytics | law-c6fvx6uzvxmv6 | ✅ Running | Monitoring |
| Storage Account | volc6fvx6uzvxmv6 | ✅ Running | Volumes |
| Managed Identity | mi-c6fvx6uzvxmv6 | ✅ Running | Authentication |

**Total Monthly Cost Estimate:** ~$30-50

- PostgreSQL B1ms: ~$12-15
- Container Apps: ~$10-20
- Redis: ~$5-10
- Storage/Logs: ~$5

## Conclusion

We have successfully:

1. ✅ Migrated from containerized PostgreSQL to Azure Database for PostgreSQL
2. ✅ Deployed all infrastructure to a single region (Australia East)
3. ✅ Added comprehensive logging
4. ✅ Identified the root cause (template variable substitution)

We need to:

1. ❓ Test local connectivity to validate fundamentals
2. ❓ Implement workaround for template substitution issue
3. ❓ Get health checks passing
4. ❓ Resume application development

**Recommendation:** Take a break, then start with local testing to prove the architecture works before debugging deployment tooling further.
