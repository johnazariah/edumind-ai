# Tomorrow: Azure Deployment Setup (45 minutes)

**Goal**: Configure Azure credentials and achieve first successful deployment

---

## Quick Summary

- ✅ Pipeline fixes complete (timeout & azd installation)
- ❌ Need Azure secrets in GitHub
- ⏱️ Total time: ~45 minutes

---

## Step-by-Step Guide

### 1. Azure Login & Get Subscription (5 min)

```bash
az login
az account list --output table
az account set --subscription "YOUR_SUBSCRIPTION"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo $SUBSCRIPTION_ID  # Save this!
```

### 2. Create Service Principal (5 min)

```bash
az ad sp create-for-rbac \
  --name "edumind-ai-github-deployer" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth
```

**⚠️ SAVE THE ENTIRE JSON OUTPUT** - you need:

- `clientId`
- `tenantId`  
- `subscriptionId`

### 3. Add Secrets to GitHub (10 min)

Go to: <https://github.com/johnazariah/edumind-ai/settings/secrets/actions>

Add these 3 secrets:

- `AZURE_CLIENT_ID` → clientId from JSON
- `AZURE_TENANT_ID` → tenantId from JSON
- `AZURE_SUBSCRIPTION_ID` → subscriptionId from JSON

Verify:

```bash
gh secret list
```

### 4. Trigger Deployment (2 min)

```bash
gh workflow run deploy-azure-azd.yml
```

Or push to main:

```bash
git commit --allow-empty -m "test: Deploy with Azure credentials"
git push origin main
```

### 5. Watch It Deploy (10-15 min)

```bash
gh run list --workflow=deploy-azure-azd.yml --limit 1
gh run watch [RUN_ID]
```

**Expected timeline:**

- Build & Test: 1 min
- Azure Login: 10 sec (should work now!)
- Deploy: 8-12 min
- Health Check: 30 sec

### 6. Verify Success (3 min)

```bash
# Get URL from logs
gh run view [RUN_ID] --log | grep "Deployed to:"

# Test health
curl https://[YOUR-APP-URL]/health
```

---

## Success = ✅

1. "Azure Login" succeeds
2. "Deploy" completes without errors
3. "Health check passed"
4. App URL responds

---

## If Something Fails

**Azure Login fails?**

- Check secret names are exact: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID
- Test locally: `az login --service-principal -u [CLIENT_ID] -p [SECRET] --tenant [TENANT_ID]`

**Deploy fails?**

- Try different Azure region (in workflow inputs)
- Check Azure quota limits
- View full logs: `gh run view [RUN_ID] --log`

**Health check fails?**

- Wait 2 minutes, services might still be starting
- Check Azure Portal for app status

---

## Quick Commands Reference

```bash
# List recent runs
gh run list --workflow=deploy-azure-azd.yml --limit 5

# Watch a run
gh run watch [RUN_ID]

# View logs
gh run view [RUN_ID] --log

# Cancel bad run
gh run cancel [RUN_ID]

# Check secrets
gh secret list

# Test Azure login
az account show
```

---

## Timeline

| Task | Time |
|------|------|
| Azure setup | 5 min |
| Create SP | 5 min |
| Add secrets | 10 min |
| Trigger | 2 min |
| Deploy | 10-15 min |
| Verify | 3 min |
| **TOTAL** | **35-40 min** |

---

## After Success

- [ ] Document the deployed URL
- [ ] Test all endpoints work
- [ ] Set up Azure monitoring
- [ ] Configure alerts
- [ ] Plan staging environment

---

**That's it! Follow these steps tomorrow and you'll have a deployed application in under an hour.** 🚀
