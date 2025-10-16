# Automated Deployment Guide for EduMind.AI

This guide provides **fully automated** deployment infrastructure for EduMind.AI using Infrastructure as Code (IaC) with Azure Bicep.

## 🎯 What Gets Automated

✅ **Azure Resources** (via Bicep IaC):

- App Service Plan & Web App
- PostgreSQL Flexible Server
- Azure Cache for Redis
- Azure Key Vault
- Application Insights & Log Analytics

✅ **Authentication** (semi-automated):

- Azure AD B2C tenant creation (manual portal step)
- Google OAuth setup (interactive script)
- Identity provider configuration
- User flow creation

✅ **CI/CD Pipeline**:

- GitHub Actions workflow
- Automated builds and tests
- Azure deployment
- Database migrations
- Health checks

✅ **Configuration Management**:

- Secrets in Azure Key Vault
- Environment-specific parameters
- Automated GitHub secrets setup

---

## 📋 Prerequisites

Before running the deployment:

1. **Azure Subscription** with permissions to:
   - Create resource groups
   - Create Azure AD B2C tenants
   - Deploy resources

2. **Azure CLI** installed and authenticated:

   ```bash
   az --version  # Should show 2.50.0+
   az login
   ```

3. **GitHub CLI** (optional, for automated secrets):

   ```bash
   gh --version
   gh auth login
   ```

4. **.NET 8.0 SDK**:

   ```bash
   dotnet --version  # Should show 8.0.x
   ```

5. **Google Cloud Account** (for Google OAuth)

---

## 🚀 Quick Start - Automated Deployment

### **Option 1: Full Automated Deployment (Recommended)**

Run the master deployment script:

```bash
cd /workspaces/edumind-ai

# Deploy to development environment
./deployment/scripts/deploy-infrastructure.sh dev

# Or deploy to staging
./deployment/scripts/deploy-infrastructure.sh staging eastus

# Or deploy to production
./deployment/scripts/deploy-infrastructure.sh prod westus2
```

**What this script does:**

1. ✅ Checks prerequisites
2. ✅ Authenticates with Azure
3. ✅ Creates resource group
4. ✅ Generates secure passwords
5. ⚠️  **Guides you through** Google OAuth setup (interactive)
6. ⚠️  **Guides you through** Azure AD B2C tenant creation (Azure CLI limitation)
7. ⚠️  **Guides you through** B2C identity provider configuration
8. ⚠️  **Guides you through** B2C user flow creation
9. ⚠️  **Guides you through** B2C app registration
10. ✅ Deploys all Azure resources via Bicep
11. ✅ Stores secrets in Key Vault
12. ✅ Runs database migrations
13. ✅ Configures GitHub secrets for CI/CD
14. ✅ Saves deployment configuration

**Time estimate:** 30-45 minutes (mostly interactive steps)

---

### **Option 2: Step-by-Step Manual Deployment**

If you prefer more control, follow these steps:

#### **Step 1: Setup Google OAuth**

```bash
./deployment/scripts/setup-google-oauth.sh edumindai
```

This interactive script guides you through:

- Creating Google Cloud project
- Configuring OAuth consent screen
- Creating OAuth credentials
- Saving credentials securely

**Output:** `.credentials/google-oauth.env`

#### **Step 2: Create Azure AD B2C Tenant**

**Note:** Azure CLI cannot create B2C tenants. Use Azure Portal:

1. Go to <https://portal.azure.com>
2. Search for "Azure Active Directory B2C"
3. Click "Create a new Azure AD B2C Tenant"
4. Fill in:
   - Organization: `EduMind.AI`
   - Domain: `edumindai`
   - Country: (your country)
5. Wait 2-3 minutes for creation
6. Switch to new directory
7. Copy Tenant ID from Overview page

#### **Step 3: Configure B2C Identity Provider**

In Azure AD B2C portal:

1. Go to **Identity providers** → **+ New OpenID Connect provider**
2. Enter:

   ```
   Name: Google
   Metadata URL: https://accounts.google.com/.well-known/openid-configuration
   Client ID: (from Step 1)
   Client secret: (from Step 1)
   Scope: openid email profile
   Response type: code
   Response mode: form_post
   ```

3. Claims mapping:

   ```
   User ID: sub
   Display name: name
   Given name: given_name
   Surname: family_name
   Email: email
   ```

4. Save

#### **Step 4: Create B2C User Flow**

1. Go to **User flows** → **+ New user flow**
2. Select **Sign up and sign in** → **Recommended**
3. Configure:

   ```
   Name: susi_google
   Identity providers: [✓] Google
   User attributes: Display Name, Email, Given Name, Surname
   Application claims: Display Name, Email, Given Name, Surname, User's Object ID
   ```

4. Create

#### **Step 5: Register B2C Application**

1. Go to **App registrations** → **+ New registration**
2. Fill in:

   ```
   Name: EduMind.AI API
   Supported account types: Accounts in any identity provider
   Redirect URI: https://localhost:5001/signin-oidc
   ```

3. After creation:
   - **Authentication** → Check "ID tokens" → Save
   - **Certificates & secrets** → New secret → Save secret value
4. Copy Application (client) ID

#### **Step 6: Deploy Infrastructure with Bicep**

```bash
# Set variables
ENVIRONMENT=dev
LOCATION=eastus
RESOURCE_GROUP=rg-edumind-$ENVIRONMENT

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy infrastructure
az deployment group create \
  --name edumind-deploy-$(date +%Y%m%d-%H%M%S) \
  --resource-group $RESOURCE_GROUP \
  --template-file deployment/bicep/main.bicep \
  --parameters environmentName=$ENVIRONMENT \
               location=$LOCATION \
               baseName=edumind \
               adminEmail=admin@edumind.ai \
               postgresAdminPassword='<SECURE_PASSWORD>' \
               googleClientId='<FROM_STEP_1>' \
               googleClientSecret='<FROM_STEP_1>' \
               b2cTenantId='<FROM_STEP_2>'
```

#### **Step 7: Store Secrets in Key Vault**

```bash
KEY_VAULT_NAME=edumind-dev-kv

az keyvault secret set --vault-name $KEY_VAULT_NAME --name AzureAdB2C-ClientId --value '<FROM_STEP_5>'
az keyvault secret set --vault-name $KEY_VAULT_NAME --name AzureAdB2C-ClientSecret --value '<FROM_STEP_5>'
az keyvault secret set --vault-name $KEY_VAULT_NAME --name AzureAdB2C-TenantId --value '<FROM_STEP_2>'
```

#### **Step 8: Run Database Migrations**

```bash
# Get connection string from deployment outputs
POSTGRES_SERVER=edumind-dev-postgres.postgres.database.azure.com

dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --connection "Host=$POSTGRES_SERVER;Database=edumind;Username=edumind_admin;Password=<PASSWORD>;SslMode=Require"
```

#### **Step 9: Configure GitHub Secrets**

```bash
gh secret set AZURE_CREDENTIALS --body "$(az ad sp create-for-rbac --name edumind-deploy --role contributor --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/$RESOURCE_GROUP --sdk-auth)"
gh secret set AZURE_SUBSCRIPTION_ID --body "<SUBSCRIPTION_ID>"
gh secret set AZURE_RESOURCE_GROUP --body "$RESOURCE_GROUP"
gh secret set AZURE_WEBAPP_NAME --body "edumind-dev-api"
gh secret set AZURE_KEY_VAULT_NAME --body "$KEY_VAULT_NAME"
gh secret set AZURE_B2C_TENANT_ID --body "<B2C_TENANT_ID>"
gh secret set POSTGRES_CONNECTION_STRING --body "Host=$POSTGRES_SERVER;..."
```

---

## 🔄 CI/CD Pipeline

Once infrastructure is deployed, the GitHub Actions workflow (`.github/workflows/deploy-azure.yml`) will:

1. **On every push to `main`:**
   - Build and test the application
   - Publish artifacts
   - Deploy to Azure App Service
   - Run database migrations
   - Perform health checks
   - Run integration tests (non-prod)

2. **Manual deployments:**
   - Go to Actions → "Deploy to Azure" → "Run workflow"
   - Select environment (dev/staging/prod)

---

## 📁 File Structure

```
deployment/
├── bicep/
│   ├── main.bicep                    # Main infrastructure template
│   ├── main.parameters.dev.json      # Development parameters
│   └── main.parameters.prod.json     # Production parameters
├── scripts/
│   ├── deploy-infrastructure.sh      # Master deployment script
│   ├── setup-google-oauth.sh         # Google OAuth setup
│   ├── setup-azure-auth.sh           # Azure AD setup (for non-B2C)
│   └── setup-branch-protection.sh    # GitHub branch protection
└── config/
    ├── dev.env                       # Dev environment config
    ├── staging.env                   # Staging environment config
    └── prod.env                      # Prod environment config

.github/
└── workflows/
    ├── deploy-azure.yml              # CD pipeline
    ├── ci.yml                        # CI pipeline (existing)
    └── ollama-integration.yml        # OLLAMA tests (existing)
```

---

## 🔒 Security Best Practices

### **Secrets Management**

1. **Local Development:**

   ```bash
   dotnet user-secrets set "AzureAdB2C:ClientSecret" "value"
   ```

2. **Azure:**
   - All secrets stored in Azure Key Vault
   - App Service uses Managed Identity to access Key Vault
   - No secrets in code or config files

3. **GitHub Actions:**
   - Secrets stored in GitHub repository secrets
   - Accessed via `${{ secrets.SECRET_NAME }}`

### **Network Security**

- PostgreSQL: SSL required, firewall rules enabled
- Redis: SSL only, no public access
- App Service: HTTPS only, TLS 1.2 minimum

### **Authentication**

- Azure AD B2C with Google federated identity
- JWT token validation
- Role-based access control (RBAC)

---

## 🧪 Testing Deployment

### **1. Health Check**

```bash
curl https://edumind-dev-api.azurewebsites.net/health
```

Expected: `{"status": "Healthy"}`

### **2. Authentication Test**

```bash
# Get token (use Postman or browser)
TOKEN="<YOUR_JWT_TOKEN>"

# Test authenticated endpoint
curl -H "Authorization: Bearer $TOKEN" \
  https://edumind-dev-api.azurewebsites.net/api/v1/students
```

### **3. Database Test**

```bash
# Connect to PostgreSQL
psql "host=edumind-dev-postgres.postgres.database.azure.com port=5432 dbname=edumind user=edumind_admin sslmode=require"

# Check tables
\dt
```

---

## 🐛 Troubleshooting

### **Issue: "Azure CLI not authenticated"**

```bash
az login
az account set --subscription <subscription-id>
```

### **Issue: "Bicep deployment failed"**

```bash
# View deployment logs
az deployment group show \
  --name <deployment-name> \
  --resource-group <resource-group> \
  --query properties.error

# Redeploy with debug
az deployment group create ... --debug
```

### **Issue: "Database migration failed"**

```bash
# Check PostgreSQL firewall
az postgres flexible-server firewall-rule list \
  --resource-group <rg> \
  --name <server-name>

# Add your IP
az postgres flexible-server firewall-rule create \
  --resource-group <rg> \
  --name <server-name> \
  --rule-name AllowMyIP \
  --start-ip-address <your-ip> \
  --end-ip-address <your-ip>
```

### **Issue: "Google OAuth redirect URI mismatch"**

Ensure redirect URI in Google Cloud Console matches EXACTLY:

```
https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp
```

No trailing slash, exact case.

---

## 📊 Monitoring & Observability

### **Application Insights**

```bash
# View in Azure Portal
az portal show --resource-id "<app-insights-id>"

# Query logs
az monitor app-insights query \
  --app <app-id> \
  --analytics-query "requests | where timestamp > ago(1h) | summarize count() by resultCode"
```

### **Log Analytics**

- All logs centralized in Log Analytics Workspace
- Custom dashboards in Azure Portal
- Alerts configured for errors and performance

### **Health Monitoring**

- `/health` endpoint for readiness probes
- Automatic restarts on health check failures
- Application Insights availability tests

---

## 🔄 Updating Infrastructure

To update infrastructure:

1. **Modify Bicep template:**

   ```bash
   vi deployment/bicep/main.bicep
   ```

2. **Test locally:**

   ```bash
   az bicep build --file deployment/bicep/main.bicep
   ```

3. **Deploy changes:**

   ```bash
   az deployment group create \
     --resource-group <rg> \
     --template-file deployment/bicep/main.bicep \
     --parameters @deployment/bicep/main.parameters.dev.json
   ```

4. **Verify:**

   ```bash
   az deployment group show --name <deployment-name> --resource-group <rg>
   ```

---

## 📚 Additional Resources

- **Azure Bicep Docs:** <https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/>
- **Azure AD B2C:** <https://docs.microsoft.com/en-us/azure/active-directory-b2c/>
- **Google OAuth 2.0:** <https://developers.google.com/identity/protocols/oauth2>
- **GitHub Actions Azure Deploy:** <https://github.com/Azure/webapps-deploy>

---

## 🎯 Next Steps After Deployment

1. ✅ Verify all resources in Azure Portal
2. ✅ Test authentication flow end-to-end
3. ✅ Run full integration test suite
4. ✅ Configure custom domains (optional)
5. ✅ Set up monitoring alerts
6. ✅ Review and tune performance settings
7. ✅ Plan backup and disaster recovery

---

**Questions?** See troubleshooting section or file an issue.
