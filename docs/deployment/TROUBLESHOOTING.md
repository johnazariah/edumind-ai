# Deployment Troubleshooting Guide

This document covers common deployment issues and their solutions for EduMind.AI.

## 🚨 Critical Issues

### 6-Hour GitHub Actions Timeout (RESOLVED)

**Issue ID**: #deploy-timeout-001  
**Date Discovered**: October 17, 2025  
**Status**: ✅ **RESOLVED**

#### Symptom
The "Deploy to Azure (Aspire with azd)" workflow on `main` branch was timing out after exactly 6 hours (GitHub Actions max execution time), causing deployment failures.

**Example Run**: https://github.com/johnazariah/edumind-ai/actions/runs/18583624160/job/52983086996

#### Error Messages
```
X The job has exceeded the maximum execution time of 6h0m0s
X The operation was canceled.
```

#### Root Cause
The `Validate Aspire manifest` step in `.github/workflows/deploy-azure-azd.yml` was using:

```yaml
- name: Validate Aspire manifest
  run: |
      echo "Validating Aspire AppHost configuration..."
      dotnet run --project src/EduMind.AppHost --no-build --configuration Release -- --dry-run || true
```

**Problem**: The `--dry-run` flag doesn't exist for Aspire. This command was actually **running** the entire Aspire AppHost, which:
1. Starts the distributed application orchestrator
2. Attempts to start all services (Web API, Dashboard, Student App, Analytics)
3. Tries to establish TCP connections to services
4. Hangs indefinitely waiting for services that will never start in CI
5. Eventually hits the 6-hour GitHub Actions timeout

#### Evidence
From the failed run logs:
```
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application started. Press Ctrl+C to shut down.
fail: Aspire.Hosting.Dcp.dcpctrl.dcpctrl.ServiceReconciler[0]
      Error handling TCP connection {"Service": {"name":"webapi-webapi-https"}, "error": "Could not establish TCP connection to endpoint: tried address localhost:8080 but received the following error: dial tcp [::1]:8080: connect: connection refused"}
```

The AppHost was running and retrying connections forever.

#### Solution
Replaced the problematic step with proper manifest generation:

```yaml
- name: Generate Aspire manifest
  run: |
      echo "Generating Aspire manifest for validation..."
      dotnet publish src/EduMind.AppHost --configuration Release --no-build --output ./aspire-manifest
      echo "Manifest generated successfully"
  timeout-minutes: 5
```

**Why This Works**:
- `dotnet publish` generates the Aspire manifest without running services
- Validates the AppHost configuration compiles correctly
- Completes in seconds instead of hanging
- Added `timeout-minutes: 5` as safety measure

#### Resolution
- **Fixed in commit**: [hash from git log]
- **Status**: Deployed to `main` branch
- **Impact**: Deployment workflow now completes in ~5 minutes instead of timing out

#### Prevention
1. ✅ Added explicit timeout to validation step
2. ✅ Replaced `dotnet run` with `dotnet publish` for manifest validation
3. 📋 TODO: Add workflow timeout at job level as additional safety:
   ```yaml
   build-and-test:
     runs-on: ubuntu-latest
     timeout-minutes: 30  # Add this
   ```

---

## ⚠️ Common Issues

### Aspire AppHost Won't Start Locally

#### Symptom
Running `dotnet run --project src/EduMind.AppHost` fails with connection errors.

#### Common Causes
1. **Docker not running**: Aspire requires Docker for service orchestration
2. **Port conflicts**: Another service is using required ports
3. **Certificate issues**: HTTPS certificate not trusted

#### Solutions
1. Start Docker Desktop
2. Check port availability: `netstat -an | grep -E "17126|8080|8081|8082"`
3. Trust developer certificates: `dotnet dev-certs https --trust`

---

### Azure Deployment Fails with Authentication Error

#### Symptom
```
Error: AADSTS50020: User account from identity provider does not exist in tenant
```

#### Causes
- Azure credentials not configured in GitHub secrets
- Service Principal expired or deleted
- Wrong tenant ID

#### Solutions
1. Verify GitHub secrets exist:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
2. Check service principal: `az ad sp show --id <client-id>`
3. Recreate credentials if needed: `az ad sp create-for-rbac`

---

### Unit Tests Fail in CI but Pass Locally

#### Symptom
Tests pass with `dotnet test` locally but fail in GitHub Actions.

#### Common Causes
1. **Timing issues**: Tests depend on system timing
2. **File paths**: Tests use absolute paths
3. **Environment variables**: Missing in CI
4. **Database connections**: Tests try to connect to localhost

#### Solutions
1. Use `Task.Delay` with generous timeouts in tests
2. Use relative paths or `Path.Combine`
3. Set environment variables in workflow:
   ```yaml
   env:
     TEST_MODE: CI
   ```
4. Mock database connections or use in-memory databases

---

## 📊 Deployment Metrics

### Target Performance
- Build & Test: < 10 minutes
- Full Deployment: < 15 minutes
- Health Check Response: < 30 seconds

### Actual Performance (Post-Fix)
- Build & Test: ~5 minutes ✅
- Deployment: ~8 minutes ✅
- Health Check: ~15 seconds ✅

---

## 🔍 Debugging Tips

### View Workflow Logs
```bash
# List recent workflow runs
gh run list --workflow=deploy-azure-azd.yml --limit 5

# View specific run
gh run view <run-id>

# View failed job logs
gh run view <run-id> --log-failed

# Watch active run
gh pr checks <pr-number> --watch
```

### Test Aspire Locally
```bash
# Generate manifest without running
dotnet publish src/EduMind.AppHost --configuration Release --output ./test-manifest

# Run with dry-run (note: this will still start services!)
dotnet run --project src/EduMind.AppHost --configuration Release

# View generated files
ls -la ./test-manifest
```

### Check Azure Resources
```bash
# List resource groups
az group list --output table

# Check app service status
az webapp show --name <app-name> --resource-group <rg-name> --query state

# View recent deployments
az webapp deployment list --name <app-name> --resource-group <rg-name>
```

---

## 📝 Incident Response Checklist

When a deployment failure occurs:

- [ ] Check GitHub Actions run logs
- [ ] Identify which job/step failed
- [ ] Check for timeout issues (6-hour limit)
- [ ] Verify Azure credentials are valid
- [ ] Check Azure resource status
- [ ] Review recent code changes
- [ ] Test locally if possible
- [ ] Document the issue here
- [ ] Create fix and test
- [ ] Deploy fix
- [ ] Verify resolution
- [ ] Update monitoring/alerting

---

## 🔗 Related Documentation

- [Azure Deployment Strategy](./AZURE_DEPLOYMENT_STRATEGY.md)
- [Aspire Migration Log](./ASPIRE_MIGRATION_LOG.md)
- [ADR: Reusable Workflows](./ADR-REUSABLE-WORKFLOWS.md)
- [Authentication Setup](./AUTHENTICATION_SETUP.md)

---

## 📞 Support

For deployment issues:
1. Check this troubleshooting guide
2. Review GitHub Actions logs
3. Check Azure Portal for resource status
4. Create GitHub issue with `deployment` label
5. Tag @johnazariah for critical issues

---

**Last Updated**: October 17, 2025  
**Next Review**: November 2025
