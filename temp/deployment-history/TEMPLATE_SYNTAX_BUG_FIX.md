# Critical Template Syntax Bug Fix

## Issue

Health checks failing with:

- **Redis**: `WRONGPASS invalid username-password pair`
- **PostgreSQL**: `Timeout during reading attempt`

Even after adding `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` to the webapi template, the environment variable was NOT being set in the deployed container.

## Root Cause

**Invalid Go template syntax in `src/infra/webapi.tmpl.yaml`:**

```yaml
# ❌ WRONG (with spaces):
- name: AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
  value: { { .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN } }
```

The spaces inside the template delimiters `{ {` and `} }` made it invalid Go template syntax. Azure Developer CLI (`azd`) couldn't process this template correctly, so the environment variable was never injected into the container.

## Investigation Steps

1. **Checked deployed container environment variables:**

   ```bash
   az containerapp show --name webapi --resource-group rg-dev \
     --query "properties.template.containers[0].env[] | [?name=='AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN']"
   ```

   Result: `[]` (empty - variable not set!)

2. **Checked container logs:**
   - No "Azure Container Apps Domain" log messages
   - No "Patched PostgreSQL hostname" messages
   - Still showing `PostgreSQL Host: postgres` (short hostname)

3. **Verified template file:**

   ```bash
   grep "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN" src/infra/webapi.tmpl.yaml
   ```

   Found the syntax error: `{ { .Env.VAR } }` instead of `{{ .Env.VAR }}`

## Solution

**Fixed template syntax (removed spaces):**

```yaml
# ✅ CORRECT (no spaces):
- name: AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
  value: {{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}
```

**Commit**: `05df064` - "Fix: Remove spaces from template syntax in webapi.tmpl.yaml"

## Why This Matters

The entire FQDN patching strategy depends on this environment variable being set:

1. `infra/resources.bicep` outputs the Container Apps environment domain
2. `azd` makes this available as `{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}`
3. Template file injects it as an environment variable into the container
4. `Program.cs` reads it: `builder.Configuration["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]`
5. Connection strings are patched: `postgres` → `postgres.internal.{domain}`

**Without the environment variable, the patching code never runs!**

## Expected Outcome

After this fix is deployed, logs should show:

```
[INFO] Azure Container Apps Domain: kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Checking PostgreSQL connection string for hostname patching...
[INFO] ✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Checking Redis connection string for hostname patching...
[INFO] ✅ Patched Redis hostname to use Azure Container Apps internal FQDN: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] PostgreSQL Host: postgres.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
[INFO] Redis Host: cache.internal.kindsea-395ab1c0.eastus.azurecontainerapps.io
```

And health check should return **200 Healthy**.

## Lessons Learned

1. **Go template syntax is strict**: `{{` must not have spaces: `{ {` won't work
2. **Verify environment variables are actually set**: Don't assume templates work - check the deployed container
3. **Template errors can be silent**: `azd` may not fail loudly on template syntax errors
4. **Test at each layer**:
   - ✓ Template file has the variable
   - ✓ Deployed container has the environment variable
   - ✓ Application code can read the variable
   - ✓ Functionality works end-to-end

## Related Files

- `src/infra/webapi.tmpl.yaml` - Container app template (fixed)
- `src/AcademicAssessment.Web/Program.cs` - FQDN patching logic
- `infra/resources.bicep` - Infrastructure outputs
- `FQDN_PATCHING_FIX.md` - Overall patching strategy documentation

## Timeline

- **Oct 20 11:40 UTC**: Identified health checks still failing after previous fix
- **Oct 20 11:56 UTC**: Discovered environment variable not set in container
- **Oct 20 11:58 UTC**: Found template syntax error (spaces in delimiters)
- **Oct 20 11:59 UTC**: Applied fix, deployment in progress

## Status

- [x] Bug identified
- [x] Root cause determined (template syntax)
- [x] Fix implemented
- [ ] Deployment in progress
- [ ] Verification pending
