#!/bin/bash
# Script to configure Microsoft Entra ID (Azure AD) authentication for EduMind.AI
# This creates an app registration and configures authentication

set -e

APP_NAME="EduMind.AI"
REDIRECT_URIS="https://localhost:5103/signin-oidc http://localhost:5103/signin-oidc"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔐 Microsoft Entra ID Setup for EduMind.AI"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# =============================================
# Step 1: Check Azure CLI Installation
# =============================================
echo "🔍 Checking Azure CLI installation..."

if ! command -v az &> /dev/null; then
    echo "❌ ERROR: Azure CLI is not installed"
    echo ""
    echo "📦 To install Azure CLI:"
    echo ""
    echo "For Debian/Ubuntu (this dev container):"
    echo "  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
    echo ""
    echo "For macOS:"
    echo "  brew install azure-cli"
    echo ""
    echo "For Windows:"
    echo "  winget install Microsoft.AzureCLI"
    echo ""
    echo "Or see: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

echo "✅ Azure CLI is installed ($(az --version | head -n1))"
echo ""

# =============================================
# Step 2: Check Authentication
# =============================================
echo "🔐 Checking Azure authentication..."

if ! az account show &> /dev/null; then
    echo "⚠️  You are not authenticated with Azure CLI"
    echo ""
    echo "🔑 To authenticate, choose one of these methods:"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Option 1: Interactive Browser Login (RECOMMENDED)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Run:"
    echo "  az login"
    echo ""
    echo "This will:"
    echo "  - Open a browser for authentication"
    echo "  - Show available subscriptions"
    echo "  - Set default subscription"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Option 2: Service Principal (for automation)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Run:"
    echo "  az login --service-principal \\"
    echo "    --username <app-id> \\"
    echo "    --password <password-or-cert> \\"
    echo "    --tenant <tenant-id>"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Option 3: Device Code (for remote/SSH sessions)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Run:"
    echo "  az login --use-device-code"
    echo ""
    echo "Then:"
    echo "  - Visit https://microsoft.com/devicelogin"
    echo "  - Enter the code shown"
    echo "  - Complete authentication"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "After authentication, run this script again:"
    echo "  ./deployment/scripts/setup-azure-auth.sh"
    echo ""
    exit 1
fi

echo "✅ Azure CLI is authenticated"
echo ""

# Get current subscription and tenant
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
USER_NAME=$(az account show --query user.name -o tsv)

echo "👤 Authenticated as: $USER_NAME"
echo "🏢 Tenant ID: $TENANT_ID"
echo "📋 Subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"
echo ""

# =============================================
# Step 3: Check Permissions
# =============================================
echo "🔍 Checking permissions to create app registrations..."

# Try to list apps (this will fail if user doesn't have permission)
if ! az ad app list --filter "displayName eq 'Test'" --query "[0].appId" -o tsv &> /dev/null; then
    echo "❌ ERROR: You don't have permission to manage app registrations"
    echo ""
    echo "Required Azure AD role: 'Application Administrator' or 'Cloud Application Administrator'"
    echo ""
    echo "Ask your Azure AD administrator to:"
    echo "  1. Grant you one of the required roles, OR"
    echo "  2. Run this script themselves"
    echo ""
    echo "To check your roles:"
    echo "  https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RolesAndAdministrators"
    echo ""
    exit 1
fi

echo "✅ You have permission to manage app registrations"
echo ""

# =============================================
# Step 4: Create or Update App Registration
# =============================================
echo "🔧 Creating/updating app registration..."
echo ""

# Check if app already exists
EXISTING_APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)

if [ -n "$EXISTING_APP_ID" ]; then
    echo "ℹ️  App registration '$APP_NAME' already exists (App ID: $EXISTING_APP_ID)"
    echo ""
    read -p "Update existing app? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Using existing app registration: $EXISTING_APP_ID"
        APP_ID=$EXISTING_APP_ID
    else
        echo "Updating app registration..."
        APP_ID=$EXISTING_APP_ID
        
        # Update redirect URIs
        az ad app update --id "$APP_ID" \
            --web-redirect-uris $REDIRECT_URIS \
            --enable-id-token-issuance true \
            --enable-access-token-issuance true
        
        echo "✅ App registration updated"
    fi
else
    echo "Creating new app registration..."
    
    # Create app registration
    APP_ID=$(az ad app create \
        --display-name "$APP_NAME" \
        --sign-in-audience AzureADMyOrg \
        --web-redirect-uris $REDIRECT_URIS \
        --enable-id-token-issuance true \
        --enable-access-token-issuance true \
        --query appId -o tsv)
    
    echo "✅ App registration created: $APP_ID"
fi

echo ""

# =============================================
# Step 5: Create Client Secret
# =============================================
echo "🔑 Creating client secret..."
echo ""

# Create a client secret
CLIENT_SECRET=$(az ad app credential reset --id "$APP_ID" --append --query password -o tsv)

echo "✅ Client secret created"
echo ""

# =============================================
# Step 6: Configure API Permissions
# =============================================
echo "🔧 Configuring API permissions..."
echo ""

# Add Microsoft Graph API permissions
# User.Read (delegated) - Sign in and read user profile
GRAPH_USER_READ="e1fe6dd8-ba31-4d61-89e7-88639da4683d"

az ad app permission add --id "$APP_ID" \
    --api 00000003-0000-0000-c000-000000000000 \
    --api-permissions "$GRAPH_USER_READ=Scope"

echo "✅ API permissions configured"
echo ""
echo "⚠️  NOTE: Admin consent may be required for some permissions"
echo "   Grant consent at: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$APP_ID"
echo ""

# =============================================
# Step 7: Generate Configuration
# =============================================
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Setup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📋 Configuration Details:"
echo ""
echo "App Name:     $APP_NAME"
echo "App ID:       $APP_ID"
echo "Tenant ID:    $TENANT_ID"
echo "Client Secret: $CLIENT_SECRET"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📝 Add to appsettings.Development.json:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
cat <<EOF
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "$(az account show --query "user.name" -o tsv | cut -d@ -f2)",
    "TenantId": "$TENANT_ID",
    "ClientId": "$APP_ID",
    "ClientSecret": "$CLIENT_SECRET",
    "CallbackPath": "/signin-oidc",
    "Scopes": "user.read"
  }
}
EOF
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔒 Or use User Secrets (RECOMMENDED for local dev):"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "cd src/AcademicAssessment.Web"
echo "dotnet user-secrets set \"AzureAd:TenantId\" \"$TENANT_ID\""
echo "dotnet user-secrets set \"AzureAd:ClientId\" \"$APP_ID\""
echo "dotnet user-secrets set \"AzureAd:ClientSecret\" \"$CLIENT_SECRET\""
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔗 Manage App Registration:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Portal: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/$APP_ID"
echo ""
echo "⚠️  SECURITY NOTE:"
echo "   - Never commit client secrets to git"
echo "   - Use User Secrets for local development"
echo "   - Use Azure Key Vault for production"
echo "   - Rotate secrets regularly"
echo ""
