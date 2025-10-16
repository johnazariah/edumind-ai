#!/bin/bash
# Master deployment script for EduMind.AI infrastructure
# This script automates the complete Azure infrastructure setup

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="${1:-dev}"
LOCATION="${2:-eastus}"
BASE_NAME="edumind"
RESOURCE_GROUP="rg-${BASE_NAME}-${ENVIRONMENT}"
B2C_TENANT_NAME="edumindai"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${BLUE}🚀 EduMind.AI Infrastructure Deployment${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Environment: $ENVIRONMENT"
echo "Location: $LOCATION"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# ============================================================================
# Step 1: Prerequisites Check
# ============================================================================
echo -e "${BLUE}Step 1: Checking prerequisites...${NC}"

if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI not installed${NC}"
    echo "Install: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}⚠️  jq not installed. Installing...${NC}"
    sudo apt-get update && sudo apt-get install -y jq
fi

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK not installed${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Prerequisites OK${NC}"
echo ""

# ============================================================================
# Step 2: Azure Authentication
# ============================================================================
echo -e "${BLUE}Step 2: Azure authentication...${NC}"

if ! az account show &> /dev/null; then
    echo -e "${YELLOW}Not authenticated. Running az login...${NC}"
    az login
fi

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo -e "${GREEN}✅ Authenticated${NC}"
echo "Subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"
echo "Tenant: $TENANT_ID"
echo ""

# ============================================================================
# Step 3: Create Resource Group
# ============================================================================
echo -e "${BLUE}Step 3: Creating resource group...${NC}"

if az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
    echo -e "${YELLOW}⚠️  Resource group already exists${NC}"
else
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --tags Environment="$ENVIRONMENT" Application="EduMind.AI"
    echo -e "${GREEN}✅ Resource group created${NC}"
fi
echo ""

# ============================================================================
# Step 4: Generate Secure Passwords
# ============================================================================
echo -e "${BLUE}Step 4: Generating secure passwords...${NC}"

POSTGRES_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
echo "PostgreSQL Admin Password: [GENERATED]"
echo ""

# ============================================================================
# Step 5: Setup Google OAuth (Interactive)
# ============================================================================
echo -e "${BLUE}Step 5: Google OAuth Configuration${NC}"
echo ""
echo -e "${YELLOW}⚠️  Manual step required: Configure Google OAuth${NC}"
echo ""
echo "1. Go to: https://console.cloud.google.com/"
echo "2. Create project: 'EduMind-AI-Auth'"
echo "3. Configure OAuth consent screen:"
echo "   - App name: EduMind.AI"
echo "   - Authorized domains: ${B2C_TENANT_NAME}.b2clogin.com"
echo "4. Create OAuth 2.0 credentials:"
echo "   - Application type: Web application"
echo "   - Authorized redirect URI: https://${B2C_TENANT_NAME}.b2clogin.com/${B2C_TENANT_NAME}.onmicrosoft.com/oauth2/authresp"
echo ""
read -p "Press Enter when you have your Google Client ID and Secret ready..."
echo ""
read -p "Enter Google Client ID: " GOOGLE_CLIENT_ID
read -s -p "Enter Google Client Secret: " GOOGLE_CLIENT_SECRET
echo ""
echo -e "${GREEN}✅ Google credentials captured${NC}"
echo ""

# ============================================================================
# Step 6: Create Azure AD B2C Tenant (Interactive)
# ============================================================================
echo -e "${BLUE}Step 6: Azure AD B2C Tenant Setup${NC}"
echo ""
echo -e "${YELLOW}⚠️  Manual step required: Create Azure AD B2C Tenant${NC}"
echo ""
echo "Azure CLI cannot create B2C tenants automatically."
echo ""
echo "1. Go to: https://portal.azure.com"
echo "2. Search for 'Azure Active Directory B2C'"
echo "3. Click 'Create'"
echo "4. Select 'Create a new Azure AD B2C Tenant'"
echo "5. Fill in:"
echo "   - Organization name: EduMind.AI"
echo "   - Initial domain: ${B2C_TENANT_NAME}"
echo "   - Country: (your country)"
echo "   - Subscription: $SUBSCRIPTION_NAME"
echo "   - Resource group: $RESOURCE_GROUP"
echo "6. Wait for creation (2-3 minutes)"
echo "7. Switch to the new directory"
echo ""
read -p "Press Enter when B2C tenant is created..."
echo ""
read -p "Enter B2C Tenant ID (from Overview page): " B2C_TENANT_ID
echo ""
echo -e "${GREEN}✅ B2C Tenant ID captured${NC}"
echo ""

# ============================================================================
# Step 7: Configure B2C Identity Provider
# ============================================================================
echo -e "${BLUE}Step 7: Configuring Google as B2C Identity Provider...${NC}"
echo ""
echo -e "${YELLOW}⚠️  Manual step required: Configure Identity Provider${NC}"
echo ""
echo "1. In Azure Portal, switch to B2C directory: ${B2C_TENANT_NAME}.onmicrosoft.com"
echo "2. Go to Azure AD B2C → Identity providers"
echo "3. Click '+ New OpenID Connect provider'"
echo "4. Enter:"
echo "   - Name: Google"
echo "   - Metadata URL: https://accounts.google.com/.well-known/openid-configuration"
echo "   - Client ID: $GOOGLE_CLIENT_ID"
echo "   - Client secret: [paste your secret]"
echo "   - Scope: openid email profile"
echo "   - Response type: code"
echo "   - Response mode: form_post"
echo "5. Claims mapping:"
echo "   - User ID: sub"
echo "   - Display name: name"
echo "   - Given name: given_name"
echo "   - Surname: family_name"
echo "   - Email: email"
echo "6. Click Save"
echo ""
read -p "Press Enter when Google identity provider is configured..."
echo ""
echo -e "${GREEN}✅ Identity provider configured${NC}"
echo ""

# ============================================================================
# Step 8: Create B2C User Flow
# ============================================================================
echo -e "${BLUE}Step 8: Creating B2C User Flow...${NC}"
echo ""
echo -e "${YELLOW}⚠️  Manual step required: Create User Flow${NC}"
echo ""
echo "1. In Azure AD B2C, go to User flows"
echo "2. Click '+ New user flow'"
echo "3. Select 'Sign up and sign in' → Recommended"
echo "4. Enter:"
echo "   - Name: susi_google (becomes B2C_1_susi_google)"
echo "   - Identity providers: Check 'Google'"
echo "5. User attributes and claims:"
echo "   Collect: Display Name, Email Address, Given Name, Surname"
echo "   Return: Display Name, Email Addresses, Given Name, Surname, User's Object ID"
echo "6. Click Create"
echo ""
read -p "Press Enter when user flow is created..."
echo ""
echo -e "${GREEN}✅ User flow created${NC}"
echo ""

# ============================================================================
# Step 9: Register B2C Application
# ============================================================================
echo -e "${BLUE}Step 9: Registering B2C Application...${NC}"
echo ""
echo "Switching to B2C tenant..."

# Note: B2C app registration requires specific API calls
echo -e "${YELLOW}⚠️  Manual step required: Register Application${NC}"
echo ""
echo "1. In Azure AD B2C, go to App registrations"
echo "2. Click '+ New registration'"
echo "3. Enter:"
echo "   - Name: EduMind.AI API"
echo "   - Supported account types: Accounts in any identity provider"
echo "   - Redirect URI: Web → https://localhost:5001/signin-oidc"
echo "4. After creation:"
echo "   - Go to Authentication"
echo "   - Check 'ID tokens'"
echo "   - Click Save"
echo "5. Go to Certificates & secrets"
echo "   - Click '+ New client secret'"
echo "   - Description: API Secret"
echo "   - Expires: 24 months"
echo "   - Click Add"
echo "   - COPY THE SECRET VALUE"
echo ""
read -p "Press Enter when application is registered..."
echo ""
read -p "Enter Application (client) ID: " B2C_CLIENT_ID
read -s -p "Enter Client Secret: " B2C_CLIENT_SECRET
echo ""
echo ""
echo -e "${GREEN}✅ Application registered${NC}"
echo ""

# ============================================================================
# Step 10: Deploy Azure Infrastructure
# ============================================================================
echo -e "${BLUE}Step 10: Deploying Azure infrastructure with Bicep...${NC}"
echo ""

DEPLOYMENT_NAME="edumind-deploy-$(date +%Y%m%d-%H%M%S)"

# Create parameter file with actual values
PARAM_FILE="/tmp/bicep-params-${ENVIRONMENT}.json"
cat > "$PARAM_FILE" <<EOF
{
  "\$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": { "value": "$ENVIRONMENT" },
    "location": { "value": "$LOCATION" },
    "baseName": { "value": "$BASE_NAME" },
    "adminEmail": { "value": "admin@edumind.ai" },
    "enableB2C": { "value": true },
    "postgresAdminLogin": { "value": "edumind_admin" },
    "postgresAdminPassword": { "value": "$POSTGRES_PASSWORD" },
    "googleClientId": { "value": "$GOOGLE_CLIENT_ID" },
    "googleClientSecret": { "value": "$GOOGLE_CLIENT_SECRET" },
    "b2cTenantId": { "value": "$B2C_TENANT_ID" }
  }
}
EOF

echo "Deploying infrastructure..."
az deployment group create \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$PROJECT_ROOT/deployment/bicep/main.bicep" \
  --parameters "@$PARAM_FILE" \
  --output table

echo -e "${GREEN}✅ Infrastructure deployed${NC}"
echo ""

# Get deployment outputs
OUTPUTS=$(az deployment group show \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs -o json)

WEB_API_URL=$(echo "$OUTPUTS" | jq -r '.webApiUrl.value')
POSTGRES_SERVER=$(echo "$OUTPUTS" | jq -r '.postgresServerFqdn.value')
KEY_VAULT_NAME=$(echo "$OUTPUTS" | jq -r '.keyVaultName.value')
APP_INSIGHTS_KEY=$(echo "$OUTPUTS" | jq -r '.appInsightsInstrumentationKey.value')

echo "Deployed resources:"
echo "  Web API URL: $WEB_API_URL"
echo "  PostgreSQL: $POSTGRES_SERVER"
echo "  Key Vault: $KEY_VAULT_NAME"
echo ""

# ============================================================================
# Step 11: Store Secrets in Key Vault
# ============================================================================
echo -e "${BLUE}Step 11: Storing secrets in Key Vault...${NC}"

az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "PostgresAdminPassword" --value "$POSTGRES_PASSWORD" --output none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "GoogleClientId" --value "$GOOGLE_CLIENT_ID" --output none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "GoogleClientSecret" --value "$GOOGLE_CLIENT_SECRET" --output none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "AzureAdB2C-ClientId" --value "$B2C_CLIENT_ID" --output none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "AzureAdB2C-ClientSecret" --value "$B2C_CLIENT_SECRET" --output none
az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "AzureAdB2C-TenantId" --value "$B2C_TENANT_ID" --output none

echo -e "${GREEN}✅ Secrets stored in Key Vault${NC}"
echo ""

# ============================================================================
# Step 12: Run Database Migrations
# ============================================================================
echo -e "${BLUE}Step 12: Running database migrations...${NC}"

CONNECTION_STRING="Host=$POSTGRES_SERVER;Database=edumind;Username=edumind_admin;Password=$POSTGRES_PASSWORD;SslMode=Require"

cd "$PROJECT_ROOT"
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --connection "$CONNECTION_STRING"

echo -e "${GREEN}✅ Database migrations applied${NC}"
echo ""

# ============================================================================
# Step 13: Create GitHub Secrets for CI/CD
# ============================================================================
echo -e "${BLUE}Step 13: Setting up GitHub Secrets for CI/CD...${NC}"

if command -v gh &> /dev/null; then
    echo "Creating GitHub secrets..."
    
    gh secret set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID" --repo johnazariah/edumind-ai
    gh secret set AZURE_RESOURCE_GROUP --body "$RESOURCE_GROUP" --repo johnazariah/edumind-ai
    gh secret set AZURE_WEBAPP_NAME --body "${BASE_NAME}-${ENVIRONMENT}-api" --repo johnazariah/edumind-ai
    gh secret set AZURE_KEY_VAULT_NAME --body "$KEY_VAULT_NAME" --repo johnazariah/edumind-ai
    gh secret set AZURE_B2C_TENANT_ID --body "$B2C_TENANT_ID" --repo johnazariah/edumind-ai
    
    echo -e "${GREEN}✅ GitHub secrets created${NC}"
else
    echo -e "${YELLOW}⚠️  GitHub CLI not found. Skipping GitHub secrets setup.${NC}"
    echo "Manually add these secrets to GitHub:"
    echo "  AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID"
    echo "  AZURE_RESOURCE_GROUP=$RESOURCE_GROUP"
    echo "  AZURE_WEBAPP_NAME=${BASE_NAME}-${ENVIRONMENT}-api"
    echo "  AZURE_KEY_VAULT_NAME=$KEY_VAULT_NAME"
    echo "  AZURE_B2C_TENANT_ID=$B2C_TENANT_ID"
fi
echo ""

# ============================================================================
# Step 14: Save Deployment Configuration
# ============================================================================
echo -e "${BLUE}Step 14: Saving deployment configuration...${NC}"

CONFIG_FILE="$PROJECT_ROOT/deployment/config/${ENVIRONMENT}.env"
mkdir -p "$PROJECT_ROOT/deployment/config"

cat > "$CONFIG_FILE" <<EOF
# EduMind.AI Deployment Configuration - $ENVIRONMENT
# Generated: $(date)

ENVIRONMENT=$ENVIRONMENT
AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID
AZURE_RESOURCE_GROUP=$RESOURCE_GROUP
AZURE_LOCATION=$LOCATION

# Web App
WEB_API_URL=$WEB_API_URL
AZURE_WEBAPP_NAME=${BASE_NAME}-${ENVIRONMENT}-api

# Database
POSTGRES_SERVER=$POSTGRES_SERVER
POSTGRES_DATABASE=edumind
POSTGRES_USER=edumind_admin
# POSTGRES_PASSWORD stored in Key Vault

# Key Vault
KEY_VAULT_NAME=$KEY_VAULT_NAME

# Application Insights
APP_INSIGHTS_KEY=$APP_INSIGHTS_KEY

# Azure AD B2C
B2C_TENANT_ID=$B2C_TENANT_ID
B2C_TENANT_NAME=${B2C_TENANT_NAME}.onmicrosoft.com
B2C_POLICY_ID=B2C_1_susi_google
# B2C_CLIENT_ID and B2C_CLIENT_SECRET stored in Key Vault

# Google OAuth
# GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET stored in Key Vault
EOF

echo -e "${GREEN}✅ Configuration saved to: $CONFIG_FILE${NC}"
echo ""

# Clean up temporary files
rm -f "$PARAM_FILE"

# ============================================================================
# Deployment Complete
# ============================================================================
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}🎉 Deployment Complete!${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📋 Summary:"
echo "  Environment: $ENVIRONMENT"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Web API: $WEB_API_URL"
echo "  Key Vault: $KEY_VAULT_NAME"
echo ""
echo "📝 Next Steps:"
echo "  1. Update B2C app redirect URIs to include: $WEB_API_URL/signin-oidc"
echo "  2. Test authentication: $WEB_API_URL/health"
echo "  3. Deploy application code via GitHub Actions"
echo "  4. Monitor via Application Insights"
echo ""
echo "📖 Documentation:"
echo "  Configuration: $CONFIG_FILE"
echo "  Secrets: Azure Key Vault ($KEY_VAULT_NAME)"
echo "  Logs: Azure Log Analytics"
echo ""
