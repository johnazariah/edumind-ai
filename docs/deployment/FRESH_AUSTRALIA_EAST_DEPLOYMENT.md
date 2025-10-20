# Fresh Deployment in Australia East

**Date:** 2025-10-20  
**Status:** In Progress  
**Run ID:** 18654916236  
**Environment:** staging (using `rg-staging` to avoid conflict with still-deleting `rg-dev`)  
**Previous Attempts:**
- 18654670570: Failed (rg-dev still being deleted)
- 18654845505: Skipped deploy job (prod environment has safety check for manual triggers)

## Context

After multiple failed deployment attempts with mixed regions (Container Apps in East US, PostgreSQL in Australia East), we decided to start fresh with everything in a single region.

## Actions Taken

### 1. Deleted Old Resource Group

```bash
az group delete --name rg-dev --yes --no-wait
```

### 2. Updated Infrastructure Code

- **infra/resources.bicep**: Removed hardcoded `australiaeast` location for PostgreSQL
  - Now uses `location` parameter consistently
  - Removed `-ause` suffix from PostgreSQL server name
  
- **src/AcademicAssessment.Web/Program.cs**: Added comprehensive console logging
  - Direct `Console.WriteLine()` calls to ensure logs appear in Container Apps
  - Detailed connection string information (without passwords)
  - Azure Container Apps domain detection logging

### 3. Triggered Fresh Deployment

```bash
# First attempt (failed - rg-dev still being deleted)
gh workflow run deploy-azure-azd.yml \
  --field environment=dev \
  --field azure_location=australiaeast

# Second attempt (using 'prod' environment - SKIPPED due to safety check)
gh workflow run deploy-azure-azd.yml \
  --field environment=prod \
  --field azure_location=australiaeast

# Third attempt (using 'staging' environment)
gh workflow run deploy-azure-azd.yml \
  --field environment=staging \
  --field azure_location=australiaeast
```

## Expected Outcome

All resources will be deployed in **Australia East**:

- ✅ Container Apps Environment
- ✅ Azure Database for PostgreSQL Flexible Server (Standard_B1ms)
- ✅ Redis (containerized)
- ✅ Ollama (containerized)
- ✅ Web API, Dashboard, Student App
- ✅ Azure Container Registry
- ✅ Log Analytics Workspace
- ✅ Managed Identity

## Benefits

1. **No Cross-Region Networking**: All components in same region = lower latency
2. **No Quota Issues**: Australia East has capacity
3. **Clean Slate**: No conflicts with reserved names or old configurations
4. **Better Debugging**: Enhanced logging will show exact connection strings being used

## Monitoring

Watch the deployment at:
https://github.com/johnazariah/edumind-ai/actions/runs/18654916236

Expected completion: ~10-15 minutes

**Resource Group:** `rg-staging` (in Australia East)

**Note:** The workflow has a safety check that prevents manual deployment to `prod` environment. 
Use `staging` or `dev` for manual workflow_dispatch triggers.

## Next Steps

Once deployment completes:

1. Check health endpoint: `https://webapi.<new-fqdn>/health`
2. Review container logs for connection string details
3. Verify PostgreSQL and Redis connectivity
4. Test basic API endpoints
5. If successful, proceed with local testing for comparison

## Lessons Learned

- **Single Region Deployment**: Keep all resources in same region for Azure Container Apps
- **Aspire Limitations**: azd doesn't always handle cross-region deployments well
- **Logging is Critical**: Console.WriteLine() more reliable than Serilog in containers
- **Fresh Start Sometimes Best**: After 3+ hours troubleshooting, clean deployment was pragmatic choice
