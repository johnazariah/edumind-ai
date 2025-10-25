# Playbook: Rollback Deployment

**Scenario:** Recently deployed version has critical issues and needs immediate rollback  
**Time Required:** 5-15 minutes  
**Difficulty:** Intermediate  
**Risk Level:** Medium (always test rollback procedures in staging first)

---

## Prerequisites

- [ ] Admin access to Azure or local environment
- [ ] Azure CLI authenticated (for Azure rollbacks)
- [ ] Knowledge of last known good version

---

## Quick Decision Matrix

| Scenario | Recommended Action | Time | Risk |
|----------|-------------------|------|------|
| Local dev broken | Git revert + rebuild | 5 min | Low |
| Azure app broken, DB unchanged | Revision rollback | 3 min | Low |
| Azure app + DB migration broken | Full rollback with DB restore | 15 min | Medium |
| Production critical issue | Immediate revision rollback | 2 min | Low |

---

## Local Environment Rollback

### Scenario: Recent commit broke local development

**Step 1: Identify Last Good Commit**

```bash
# View recent commits
git log --oneline -10

# Example output:
# abc1234 (HEAD) fix: update student assessment endpoint
# def5678 feat: add progress tracking
# ghi9012 (tag: v1.2.0) release: version 1.2.0
# jkl3456 fix: database connection issue
```

**Step 2: Revert to Last Good Commit**

**Option A: Soft Reset (Preserves Changes)**

```bash
# Reset to last good commit (keeps changes as uncommitted)
git reset --soft def5678

# Your changes are now uncommitted
# Review and fix, then commit again
git add .
git commit -m "fix: corrected implementation"
```

**Option B: Hard Reset (Discards Changes)**

```bash
# WARNING: This permanently discards changes!
git reset --hard def5678

# Verify you're at the right commit
git log --oneline -3
```

**Option C: Create Revert Commit (Preserves History)**

```bash
# Create a new commit that undoes the bad commit
git revert abc1234

# This creates a new commit, preserving history
# Better for shared branches
```

**Step 3: Rebuild and Verify**

```bash
# Clean build
dotnet clean EduMind.AI.sln
dotnet build EduMind.AI.sln --configuration Debug

# Restart Aspire
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj

# Verify health
curl http://localhost:5103/health
```

---

## Azure Container Apps Rollback (No Database Changes)

### Scenario: Recently deployed app version has bugs, but database schema unchanged

This is the **fastest and safest** rollback method.

**Step 1: List Available Revisions**

```bash
# List all revisions for Web API
az containerapp revision list \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --output table

# Output shows:
# - Name (revision identifier)
# - Active (true/false)
# - Created (timestamp)
# - Replicas (current)
# - TrafficWeight (percentage)
```

**Example output:**

```
Name                          Active  Created                    Replicas  TrafficWeight
----------------------------  ------  -------------------------  --------  -------------
ca-webapi-dev--abc1234        True    2024-10-20T10:30:00Z       2         100
ca-webapi-dev--def5678        False   2024-10-19T15:20:00Z       0         0
ca-webapi-dev--ghi9012        False   2024-10-18T09:15:00Z       0         0
```

**Step 2: Test Previous Revision (Zero-Downtime)**

Before fully rolling back, split traffic to test:

```bash
# Send 10% traffic to previous revision
az containerapp ingress traffic set \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision-weight ca-webapi-dev--abc1234=90 ca-webapi-dev--def5678=10

# Wait 5 minutes and monitor logs
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision ca-webapi-dev--def5678 \
  --follow
```

**Step 3: Complete Rollback**

If previous revision looks good:

```bash
# Route 100% traffic to previous revision
az containerapp ingress traffic set \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision-weight ca-webapi-dev--def5678=100

# Deactivate bad revision
az containerapp revision deactivate \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision ca-webapi-dev--abc1234
```

**Step 4: Verify Health**

```bash
# Check health endpoint
export API_URL=$(azd show --environment dev --output json | jq -r '.services.webapi.endpoint')
curl $API_URL/health

# Expected: "Healthy"

# Check Application Insights for errors
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "exceptions | where timestamp > ago(10m) | take 50"
```

**Step 5: Rollback Other Services (if needed)**

Repeat for Student App and Dashboard:

```bash
# Student App
az containerapp ingress traffic set \
  --name ca-studentapp-dev \
  --resource-group rg-dev \
  --revision-weight <previous-revision>=100

# Dashboard
az containerapp ingress traffic set \
  --name ca-dashboard-dev \
  --resource-group rg-dev \
  --revision-weight <previous-revision>=100
```

---

## Full Rollback with Database Restore

### Scenario: Deployed version included database migration that broke things

**⚠️ WARNING:** This involves downtime and potential data loss.

**Step 1: Notify Users (if production)**

- Put application in maintenance mode
- Send notification to users
- Estimated downtime: 10-15 minutes

**Step 2: Backup Current Database State (Just in Case)**

```bash
# Get PostgreSQL connection info
az postgres flexible-server show \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --query fullyQualifiedDomainName \
  --output tsv

# Backup current state
pg_dump -h <postgres-fqdn> \
  -U edumind_admin \
  -d edumind \
  -F c \
  -f backup-before-rollback-$(date +%Y%m%d-%H%M%S).dump

# Store backup safely
az storage blob upload \
  --account-name <storage-account> \
  --container-name backups \
  --name backup-before-rollback-$(date +%Y%m%d-%H%M%S).dump \
  --file backup-before-rollback-*.dump
```

**Step 3: Identify Last Good Database State**

```bash
# List available backups
az postgres flexible-server backup list \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --output table

# Azure keeps automated backups for 7-35 days (depending on configuration)
```

**Option A: Point-in-Time Restore (Azure Automated Backups)**

```bash
# Restore to time before bad migration
az postgres flexible-server restore \
  --resource-group rg-dev \
  --name psql-dev-<suffix>-restored \
  --source-server psql-dev-<suffix> \
  --restore-time "2024-10-19T15:00:00Z" \
  --location eastus

# This creates a NEW server with restored data
# Takes 10-15 minutes
```

**Option B: Manual Backup Restore**

If you have a manual pg_dump backup:

```bash
# Connect to database
psql -h <postgres-fqdn> -U edumind_admin -d postgres

# Drop and recreate database
DROP DATABASE edumind;
CREATE DATABASE edumind;
\q

# Restore from backup
pg_restore -h <postgres-fqdn> \
  -U edumind_admin \
  -d edumind \
  -F c \
  backup-last-good-state.dump

# Or if SQL format:
psql -h <postgres-fqdn> -U edumind_admin -d edumind < backup-last-good-state.sql
```

**Step 4: Rollback Application to Matching Version**

```bash
# Rollback Container Apps to version that matches database schema
az containerapp revision activate \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision <revision-matching-db-schema>

# Update traffic weight
az containerapp ingress traffic set \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision-weight <revision-matching-db-schema>=100
```

**Step 5: Verify System Health**

```bash
# Wait for services to stabilize
sleep 60

# Check health
curl $API_URL/health

# Test database connectivity
curl $API_URL/api/v1/assessment | jq '.[0]'

# Check for errors in logs
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 100 | grep -i error
```

**Step 6: Remove Bad Migration from Code**

```bash
# Identify bad migration
ls src/AcademicAssessment.Infrastructure/Data/Migrations/

# Remove bad migration file
rm src/AcademicAssessment.Infrastructure/Data/Migrations/<bad-migration>.cs
rm src/AcademicAssessment.Infrastructure/Data/Migrations/<bad-migration>.Designer.cs

# Update ModelSnapshot if needed
# (or regenerate by creating a new migration and then deleting it)

# Commit changes
git add .
git commit -m "revert: remove broken database migration"
git push
```

---

## GitHub Actions Deployment Rollback

### Scenario: Automated deployment pushed bad version to Azure

**Step 1: Identify Last Good Deployment**

1. Go to GitHub repository → **Actions** tab
2. Find last successful deployment before the broken one
3. Note the commit SHA

**Step 2: Create Rollback Branch**

```bash
# Create branch from last good commit
git checkout -b rollback-to-<short-sha> <full-commit-sha>

# Push to GitHub
git push origin rollback-to-<short-sha>
```

**Step 3: Manually Trigger Deployment**

1. Go to **Actions** tab
2. Select "Deploy to Azure (azd)" workflow
3. Click "Run workflow"
4. Select branch: `rollback-to-<short-sha>`
5. Click "Run workflow"

**Step 4: Monitor Deployment**

Watch the workflow execution in Actions tab.

**Expected duration:** 15-20 minutes

**Step 5: Verify and Clean Up**

```bash
# After successful rollback, verify health
curl $API_URL/health

# Merge rollback branch to main (to prevent re-deploying bad code)
git checkout main
git merge rollback-to-<short-sha>
git push origin main

# Delete rollback branch
git branch -d rollback-to-<short-sha>
git push origin --delete rollback-to-<short-sha>
```

---

## Emergency Rollback (Production Critical)

### Scenario: Production is down, need immediate rollback

**Time Target:** Under 5 minutes

**Step 1: Immediate Traffic Shift (30 seconds)**

```bash
# Rollback Web API to previous revision
az containerapp ingress traffic set \
  --name ca-webapi-prod \
  --resource-group rg-prod \
  --revision-weight <previous-revision>=100

# Rollback Student App
az containerapp ingress traffic set \
  --name ca-studentapp-prod \
  --resource-group rg-prod \
  --revision-weight <previous-revision>=100

# Rollback Dashboard
az containerapp ingress traffic set \
  --name ca-dashboard-prod \
  --resource-group rg-prod \
  --revision-weight <previous-revision>=100
```

**Step 2: Verify Immediately (30 seconds)**

```bash
# Quick health check
curl https://api.edumind.ai/health

# If "Healthy", rollback successful
```

**Step 3: Monitor (2 minutes)**

```bash
# Watch for new errors in last 5 minutes
az monitor app-insights query \
  --app edumind-prod \
  --analytics-query "exceptions | where timestamp > ago(5m) | summarize count() by type"

# Check active users
az monitor app-insights query \
  --app edumind-prod \
  --analytics-query "requests | where timestamp > ago(5m) | summarize count() by resultCode"
```

**Step 4: Post-Incident (After Stabilization)**

- Document what happened
- Create incident report
- Schedule post-mortem meeting
- Fix root cause in code
- Test thoroughly before next deployment

---

## Rollback Verification Checklist

After any rollback, verify:

- [ ] All health checks pass (`/health` returns "Healthy")
- [ ] All services show correct revision in Azure Portal
- [ ] No spike in error rates in Application Insights
- [ ] User-facing apps load correctly in browser
- [ ] Critical user workflows work end-to-end
- [ ] Database queries respond within normal latency
- [ ] No authentication errors
- [ ] Integration tests pass

**Run smoke tests:**

```bash
# From tests/AcademicAssessment.Tests.Integration
dotnet test --filter "Category=SmokeTest"
```

---

## Prevent Future Issues

### Pre-Deployment Checklist

Before deploying to production:

1. **Test in staging environment first**

   ```bash
   azd deploy --environment staging
   # Wait 30 minutes, monitor for issues
   ```

2. **Run all tests**

   ```bash
   dotnet test --configuration Release
   ```

3. **Database migration dry run**

   ```bash
   # Generate SQL script, review before applying
   dotnet ef migrations script \
     --project src/AcademicAssessment.Infrastructure \
     --output migration.sql
   ```

4. **Backup database before migration**

   ```bash
   pg_dump -h <postgres-fqdn> -U edumind_admin -d edumind -F c -f pre-deploy-backup.dump
   ```

5. **Deploy during low-traffic window**

   - Check analytics for low-traffic times
   - Schedule maintenance window
   - Notify users

### Automated Rollback in CI/CD

Add automatic rollback to GitHub Actions workflow:

```yaml
- name: Health Check with Rollback
  run: |
    for i in {1..10}; do
      STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ env.API_URL }}/health)
      if [ $STATUS -eq 200 ]; then
        echo "Deployment successful"
        exit 0
      fi
      sleep 10
    done
    
    echo "Health check failed, rolling back..."
    
    # Get previous revision
    PREV_REVISION=$(az containerapp revision list \
      --name ca-webapi-${{ env.ENVIRONMENT }} \
      --resource-group rg-${{ env.ENVIRONMENT }} \
      --query "[1].name" \
      --output tsv)
    
    # Rollback
    az containerapp ingress traffic set \
      --name ca-webapi-${{ env.ENVIRONMENT }} \
      --resource-group rg-${{ env.ENVIRONMENT }} \
      --revision-weight $PREV_REVISION=100
    
    exit 1
```

### Blue-Green Deployment (Advanced)

For zero-downtime deployments with instant rollback:

```bash
# Deploy new version as "green"
azd deploy --environment green

# Test green environment
curl https://api-green.edumind.ai/health

# Switch traffic (instant)
az traffic-manager endpoint update \
  --resource-group rg-prod \
  --profile-name edumind-tm \
  --name api-green \
  --endpoint-status Enabled

az traffic-manager endpoint update \
  --resource-group rg-prod \
  --profile-name edumind-tm \
  --name api-blue \
  --endpoint-status Disabled

# Rollback if issues (instant)
# Just reverse the enable/disable
```

---

## Rollback Decision Tree

```
Issue Detected
│
├─ Local Dev
│  └─ git reset/revert + rebuild (5 min)
│
├─ Azure - App Only
│  ├─ Non-Critical
│  │  ├─ Test previous revision (split traffic) (10 min)
│  │  └─ Full rollback if successful (2 min)
│  │
│  └─ Critical (Production Down)
│     └─ Immediate revision rollback (2 min)
│
└─ Azure - App + Database
   ├─ Database Schema Unchanged
   │  └─ App rollback only (3 min)
   │
   └─ Database Migration Broken
      ├─ Backup current state (2 min)
      ├─ Restore database to point-in-time (15 min)
      └─ Rollback app to matching version (3 min)
```

---

**Status:** ⏮️ Rollback Complete  
**Next Playbook:** [06-database-migrations.md](./06-database-migrations.md)
