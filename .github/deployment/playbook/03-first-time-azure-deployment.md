# Playbook: First-Time Azure Deployment

**Scenario:** Deploying EduMind.AI to Azure Container Apps for the first time  
**Time Required:** 60-90 minutes  
**Difficulty:** Intermediate  
**Prerequisites:** Azure subscription with appropriate permissions

---

## Prerequisites

Before starting:

- [x] Azure subscription (free tier works for testing)
- [x] Azure CLI installed and authenticated
- [x] Azure Developer CLI (azd) version 1.5.0 or later
- [x] GitHub repository access (for GitHub Actions)
- [x] Local deployment working (completed [01-first-time-local-setup.md](./01-first-time-local-setup.md))

---

## Step 1: Install Azure CLI (10 minutes)

### Windows

```powershell
# Download and run MSI installer
# https://aka.ms/installazurecliwindows

# Or use winget
winget install -e --id Microsoft.AzureCLI
```

### macOS

```bash
brew update && brew install azure-cli
```

### Linux

```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Verify Installation

```bash
az --version
# Expected: azure-cli 2.50.0 or later
```

---

## Step 2: Install Azure Developer CLI (5 minutes)

### Windows

```powershell
powershell -ex AllSigned -c "Invoke-RestMethod 'https://aka.ms/install-azd.ps1' | Invoke-Expression"
```

### macOS/Linux

```bash
curl -fsSL https://aka.ms/install-azd.sh | bash
```

### Verify Installation

```bash
azd version
# Expected: azd version 1.5.0 or later
```

---

## Step 3: Authenticate with Azure (5 minutes)

### Login to Azure

```bash
# Login to Azure
az login

# If you have multiple subscriptions, list them
az account list --output table

# Set the subscription you want to use
az account set --subscription "<subscription-id-or-name>"

# Verify current subscription
az account show --output table
```

### Login to Azure Developer CLI

```bash
# Login to azd (uses same credentials as az)
azd auth login

# Verify authentication
azd auth login --check-status
```

---

## Step 4: Prepare GitHub Secrets (10 minutes)

You need to configure GitHub secrets for CI/CD deployment.

### Get Azure Credentials

```bash
# Get your subscription ID
az account show --query id --output tsv

# Create a service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "edumind-github-actions" \
  --role contributor \
  --scopes /subscriptions/<your-subscription-id> \
  --sdk-auth

# This outputs JSON - COPY THE ENTIRE JSON OUTPUT
```

**Sample output:**

```json
{
  "clientId": "xxx",
  "clientSecret": "xxx",
  "subscriptionId": "xxx",
  "tenantId": "xxx",
  ...
}
```

### Configure GitHub Secrets

1. Navigate to your GitHub repository
2. Go to **Settings → Secrets and variables → Actions**
3. Click **New repository secret**
4. Add the following secrets:

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| `AZURE_CREDENTIALS` | Entire JSON from service principal creation | From previous step |
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID | `az account show --query id -o tsv` |
| `AZURE_CLIENT_ID` | Client ID from service principal | Extract from JSON |
| `AZURE_CLIENT_SECRET` | Client secret from service principal | Extract from JSON |
| `AZURE_TENANT_ID` | Tenant ID from service principal | Extract from JSON |

**Important:** Keep these secrets secure. Never commit them to source control.

---

## Step 5: Initialize Azure Deployment (5 minutes)

### Configure azd Environment

```bash
# Navigate to repository root
cd ~/workspace/edumind-ai

# Initialize azd (if not already done)
azd init --environment dev

# You'll be prompted for:
# - Environment name: dev (or staging, prod)
# - Azure location: eastus (or australiaeast, westeurope, etc.)

# Verify configuration
cat .azure/dev/.env
```

**Key environment variables to check:**

```bash
AZURE_LOCATION=eastus
AZURE_SUBSCRIPTION_ID=<your-subscription-id>
```

### Choose Azure Region

**Recommended regions for optimal performance:**

- **US:** `eastus`, `westus2`, `centralus`
- **Europe:** `westeurope`, `northeurope`
- **Asia:** `southeastasia`, `australiaeast`
- **Global:** `eastus` (most services available)

**Check available regions:**

```bash
az account list-locations --output table
```

---

## Step 6: Deploy Infrastructure (20-30 minutes)

### First Deployment

```bash
# Deploy all infrastructure and apps
azd up --environment dev

# This will:
# 1. Provision Azure resources (Container Apps Environment, PostgreSQL, Redis, etc.)
# 2. Build Docker images for each app
# 3. Push images to Azure Container Registry
# 4. Deploy apps to Container Apps
# 5. Configure networking and environment variables
```

**Expected behavior:**

- Bicep templates deploy (~5 minutes)
- Docker images build (~10 minutes)
- Apps deploy and start (~5 minutes)
- Initial health checks run (~2 minutes)

**Watch Progress:**

- Terminal shows step-by-step progress
- Any errors will be highlighted in red
- Green checkmarks indicate successful steps

### Verify Deployment

```bash
# Get deployment status
azd show --environment dev

# Output shows:
# - Resource group name
# - Container Apps Environment
# - All deployed services with URLs
```

**Copy the Web API URL** (you'll need it for testing).

---

## Step 7: Configure Database (10 minutes)

### Apply Migrations

The database schema needs to be initialized.

```bash
# Get PostgreSQL connection string from Azure
az postgres flexible-server show \
  --resource-group rg-dev \
  --name psql-dev-<unique-suffix> \
  --query fullyQualifiedDomainName \
  --output tsv

# Store connection string
export POSTGRES_HOST="<fqdn-from-above>"
export POSTGRES_USER="edumind_admin"
export POSTGRES_DB="edumind"

# Apply migrations using dotnet ef
# Note: This connects from your local machine to Azure PostgreSQL
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --connection "Host=$POSTGRES_HOST;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=<password-from-keyvault>"
```

**Alternative: Use Azure Cloud Shell**

If local connection fails due to firewall:

1. Open Azure Portal
2. Navigate to Azure Cloud Shell (icon in top bar)
3. Clone repository in Cloud Shell
4. Run migrations from there (inside Azure network)

### Seed Demo Data (Optional)

```bash
# Connect to Azure PostgreSQL
psql "host=$POSTGRES_HOST dbname=$POSTGRES_DB user=$POSTGRES_USER password=<password> sslmode=require"

# Inside psql, run seed script
\i scripts/seed-demo-data-final.sql
\q
```

---

## Step 8: Verify Deployment (10 minutes)

### Check Health Endpoints

```bash
# Get Web API URL
export API_URL=$(azd show --environment dev --output json | jq -r '.services.webapi.endpoint')

# Test health endpoint
curl $API_URL/health
# Expected: "Healthy"

# Test detailed health endpoint
curl $API_URL/health/ready
# Should show status of dependencies (PostgreSQL, Redis, Agents)
```

### Check All Services

```bash
# List all Container Apps
az containerapp list \
  --resource-group rg-dev \
  --output table

# Expected services:
# - ca-webapi-dev
# - ca-studentapp-dev
# - ca-dashboard-dev
# - (5 admin apps)
```

### Test Student App

```bash
# Get Student App URL
export STUDENT_APP_URL=$(azd show --environment dev --output json | jq -r '.services.studentapp.endpoint')

# Open in browser
echo $STUDENT_APP_URL

# Visit URL and verify:
# - Page loads without errors
# - Assessment dashboard visible
# - No authentication errors
```

### Check Logs

```bash
# View Web API logs
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --follow

# Check for errors or warnings
# Press Ctrl+C to exit
```

---

## Step 9: Configure GitHub Actions (5 minutes)

### Verify Workflow File

Ensure `.github/workflows/deploy-azure-azd.yml` exists and is configured correctly.

### Trigger First CI/CD Deployment

```bash
# Push to main branch (or create PR and merge)
git add .
git commit -m "chore: trigger initial Azure deployment"
git push origin main

# Or manually trigger workflow:
# 1. Go to GitHub repository
# 2. Navigate to Actions tab
# 3. Select "Deploy to Azure (azd)" workflow
# 4. Click "Run workflow"
# 5. Select branch: main
# 6. Click "Run workflow"
```

### Monitor GitHub Actions

1. Go to **Actions** tab in GitHub
2. Click on running workflow
3. Watch each job (Build, Test, Deploy)
4. Verify successful completion (green checkmarks)

**Expected duration:** 15-20 minutes

---

## Step 10: Configure Monitoring (10 minutes)

### Enable Application Insights

Application Insights is automatically provisioned. Verify it's working:

```bash
# Get Application Insights name
az monitor app-insights component list \
  --resource-group rg-dev \
  --output table

# Get instrumentation key
az monitor app-insights component show \
  --resource-group rg-dev \
  --app <app-insights-name> \
  --query instrumentationKey \
  --output tsv
```

### View Telemetry

```bash
# Query recent traces
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | take 50" \
  --offset 1h
```

**Or use Azure Portal:**

1. Navigate to Application Insights resource
2. Click on "Logs" in left menu
3. Run query:

   ```kql
   traces
   | where timestamp > ago(1h)
   | order by timestamp desc
   | take 50
   ```

### Set Up Alerts (Optional)

Create alert for unhealthy services:

```bash
az monitor metrics alert create \
  --name "High-Error-Rate" \
  --resource-group rg-dev \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.App/containerApps/ca-webapi-dev \
  --condition "count requests > 100 where resultCode > 499" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action <action-group-id>
```

---

## Step 11: Test End-to-End (10 minutes)

### Create Test Assessment

Use API to create test data:

```bash
# Get API URL
export API_URL=$(azd show --environment dev --output json | jq -r '.services.webapi.endpoint')

# Create assessment (example)
curl -X POST $API_URL/api/v1/assessment \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Azure Deployment Test",
    "subject": "Testing",
    "gradeLevel": "HighSchool",
    "difficulty": "Medium"
  }'
```

### Verify in Student App

1. Open Student App URL in browser
2. Navigate to assessments
3. Verify test assessment appears
4. Start assessment and answer questions
5. Submit and view results

---

## Success Criteria

Your Azure deployment is successful if:

- [x] All infrastructure provisioned without errors
- [x] All Container Apps show "Running" status
- [x] Health endpoint returns "Healthy"
- [x] Database migrations applied successfully
- [x] Student App loads in browser without errors
- [x] GitHub Actions workflow completes successfully
- [x] Application Insights receiving telemetry
- [x] End-to-end test assessment works

---

## Common Issues and Solutions

### Issue: "Insufficient quota" Error

**Symptom:** Deployment fails with quota exceeded message

**Solution:**

```bash
# Check current quota
az vm list-usage --location eastus --output table

# Request quota increase:
# 1. Go to Azure Portal
# 2. Navigate to Subscriptions → Usage + quotas
# 3. Search for "Container Apps"
# 4. Click "Request increase"
# 5. Wait for approval (usually 24-48 hours)

# Or try different region with available quota
azd up --environment dev --location westus2
```

### Issue: Database Connection Timeout

**Symptom:** Apps show "Unhealthy", logs show PostgreSQL connection errors

**Solution:**

```bash
# Check PostgreSQL firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --output table

# Allow Container Apps subnet
az postgres flexible-server firewall-rule create \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --rule-name AllowContainerApps \
  --start-ip-address <container-apps-subnet-start> \
  --end-ip-address <container-apps-subnet-end>
```

### Issue: GitHub Actions Failing with Authentication Error

**Symptom:** Workflow fails with "Unable to authenticate" message

**Solution:**

1. Verify GitHub secrets are set correctly
2. Recreate service principal:

   ```bash
   # Delete old service principal
   az ad sp delete --id <client-id>

   # Create new one
   az ad sp create-for-rbac \
     --name "edumind-github-actions-new" \
     --role contributor \
     --scopes /subscriptions/<subscription-id> \
     --sdk-auth
   ```

3. Update `AZURE_CREDENTIALS` secret in GitHub

### Issue: Container Apps Not Starting

**Symptom:** Apps stuck in "Provisioning" state

**Solution:**

```bash
# Check revision status
az containerapp revision list \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --output table

# View logs for failed revision
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision <revision-name>

# Common causes:
# - Missing environment variables
# - Image pull errors
# - Application startup errors
```

### Issue: Intermittent FQDN Connection Errors

**Symptom:** Apps sometimes can't connect to PostgreSQL or Redis

**Solution:** This is a known issue with azd template variable substitution.

The workaround is already implemented (runtime FQDN detection), but verify it's working:

```bash
# Check Web API logs for FQDN detection
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --follow | grep "FQDN"

# Should see: "Detected FQDN: kindsea-xxx.eastus.azurecontainerapps.io"
# And: "Patched connection string"
```

If not working, see [reference.md#runtime-fqdn-detection](../reference.md#runtime-fqdn-detection).

---

## Cost Estimation

**Expected monthly costs for dev environment:**

| Service | SKU/Tier | Estimated Cost |
|---------|----------|----------------|
| Container Apps Environment | Consumption | $0.00 (free tier) |
| Container Apps (7 apps) | 0.5 vCPU, 1 GB | ~$35/month |
| PostgreSQL Flexible Server | Burstable B1ms | ~$15/month |
| Redis (container in ACA) | 0.25 vCPU, 0.5 GB | ~$5/month |
| Container Registry | Basic | ~$5/month |
| Application Insights | Pay-as-you-go | ~$5/month |
| **Total** | | **~$65/month** |

**Cost optimization tips:**

- Use free tier Container Apps Environment
- Stop non-critical apps when not in use
- Use burstable PostgreSQL tier for dev/test
- Scale down replicas to 0 during off-hours
- Delete staging environments when not actively testing

**Stop all services (preserve data):**

```bash
azd down --purge --force --environment dev
```

---

## Next Steps

Now that your Azure deployment is complete:

1. **Set up staging environment:** Repeat this process with `--environment staging`
2. **Configure custom domain:** See [06-configure-custom-domain.md](./06-configure-custom-domain.md)
3. **Set up CI/CD for feature branches:** Modify GitHub Actions workflow
4. **Configure backup and disaster recovery:** See [08-backup-and-recovery.md](./08-backup-and-recovery.md)
5. **Performance testing:** See [09-performance-tuning.md](./09-performance-tuning.md)

---

**Status:** ✅ Azure Deployment Complete  
**Next Playbook:** [04-troubleshoot-unhealthy-services.md](./04-troubleshoot-unhealthy-services.md)
