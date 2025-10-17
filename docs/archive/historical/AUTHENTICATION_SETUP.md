# Authentication Provider Setup Guide

This guide covers CLI-based setup for different authentication providers used in EduMind.AI.

## 🎯 Quick Reference

| Provider | Your Project Uses? | CLI Tool | Setup Script |
|----------|-------------------|----------|--------------|
| **Microsoft Entra ID** | ✅ YES (Primary) | Azure CLI (`az`) | `./deployment/scripts/setup-azure-auth.sh` |
| **GitHub** | ✅ YES (Branch protection) | GitHub CLI (`gh`) | `./deployment/scripts/setup-branch-protection.sh` |
| **Google** | ❌ No | Google Cloud CLI (`gcloud`) | N/A |

---

## 🔐 Microsoft Entra ID (Azure AD) - Your Current Setup

You're using **Microsoft Identity (Entra ID)** for authentication. This is causing your integration test failures.

### **Quick Setup (3 Commands)**

```bash
# 1. Authenticate with Azure
az login

# 2. Create app registration and get credentials
./deployment/scripts/setup-azure-auth.sh

# 3. Configure using User Secrets (secure)
cd src/AcademicAssessment.Web
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>"
dotnet user-secrets set "AzureAd:ClientId" "<client-id>"
dotnet user-secrets set "AzureAd:ClientSecret" "<client-secret>"
```

### **What the Script Does**

1. ✅ Checks Azure CLI installation (already installed in dev container)
2. ✅ Verifies you're authenticated
3. ✅ Checks you have permissions to create app registrations
4. ✅ Creates/updates app registration for "EduMind.AI"
5. ✅ Configures redirect URIs for local development
6. ✅ Creates client secret
7. ✅ Configures API permissions (Microsoft Graph)
8. ✅ Outputs configuration for `appsettings.json` or User Secrets

### **Authentication Methods**

#### **Option 1: Browser Login (Recommended)**

```bash
az login
```

- Opens browser for interactive authentication
- Shows available subscriptions
- Easiest for development

#### **Option 2: Device Code (for SSH/remote)**

```bash
az login --use-device-code
```

- Displays a code
- Visit: <https://microsoft.com/devicelogin>
- Enter code and authenticate

#### **Option 3: Service Principal (for CI/CD)**

```bash
az login --service-principal \
  --username <app-id> \
  --password <password> \
  --tenant <tenant-id>
```

### **Required Permissions**

To create app registrations, you need one of these Azure AD roles:

- **Application Administrator**
- **Cloud Application Administrator**
- **Global Administrator**

Check your roles: <https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RolesAndAdministrators>

---

## 🔧 Fixing Your Integration Tests

Your tests fail because they need Microsoft Identity configuration. After running the Azure setup script:

### **Option A: Use User Secrets (Recommended)**

```bash
cd src/AcademicAssessment.Web

# Set secrets
dotnet user-secrets set "AzureAd:TenantId" "<from-script-output>"
dotnet user-secrets set "AzureAd:ClientId" "<from-script-output>"
dotnet user-secrets set "AzureAd:ClientSecret" "<from-script-output>"

# Verify
dotnet user-secrets list
```

### **Option B: Create `appsettings.Testing.json`**

```bash
cd tests/AcademicAssessment.Tests.Integration
cat > appsettings.Testing.json <<EOF
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<from-script-output>",
    "ClientId": "<from-script-output>",
    "ClientSecret": "<from-script-output>",
    "CallbackPath": "/signin-oidc"
  }
}
EOF

# Add to .gitignore
echo "appsettings.Testing.json" >> .gitignore
```

### **Option C: Mock Authentication (Best for CI/CD)**

For integration tests, you might want to bypass real authentication:

```csharp
// tests/AcademicAssessment.Tests.Integration/TestAuthenticationHandler.cs
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("emails", "test@example.com")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

---

## 🐙 GitHub Authentication - Already Configured

You've already set this up for branch protection!

```bash
# Check if authenticated
gh auth status

# Login if needed
gh auth login

# Configure branch protection
./deployment/scripts/setup-branch-protection.sh
```

---

## 🔍 Google Authentication (If You Need It)

If you ever want to add Google as an auth provider:

### **1. Install Google Cloud CLI**

```bash
# Install
curl https://sdk.cloud.google.com | bash
exec -l $SHELL

# Initialize
gcloud init
```

### **2. Create OAuth Credentials**

```bash
# Set project
gcloud config set project YOUR_PROJECT_ID

# Create OAuth brand (one-time)
gcloud alpha iap oauth-brands create \
  --application_title="EduMind.AI" \
  --support_email="your-email@example.com"

# Get brand name
BRAND=$(gcloud alpha iap oauth-brands list --format="value(name)")

# Create OAuth client
gcloud alpha iap oauth-clients create $BRAND
```

### **3. Or Use Web Console (Easier)**

1. Go to: <https://console.cloud.google.com/apis/credentials>
2. Create OAuth 2.0 Client ID
3. Application type: Web application
4. Authorized redirect URIs: `https://localhost:5103/signin-google`
5. Copy Client ID and Client Secret

### **4. Configure in ASP.NET Core**

```csharp
// Program.cs
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });
```

```json
// appsettings.json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
}
```

---

## 🔒 Security Best Practices

### **For All Providers:**

1. **Never commit secrets to git**

   ```bash
   # .gitignore should include:
   appsettings.*.json
   !appsettings.json
   **/secrets.json
   ```

2. **Use User Secrets for local development**

   ```bash
   dotnet user-secrets set "Key:SubKey" "Value"
   ```

3. **Use environment variables for CI/CD**

   ```yaml
   # GitHub Actions
   env:
     AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
     AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
   ```

4. **Use Azure Key Vault for production**

   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri($"https://{keyVaultName}.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

5. **Rotate secrets regularly**
   - Azure AD: Every 90 days
   - GitHub PAT: Every 30-90 days
   - Google: Every 90 days

---

## 📋 Quick Commands Reference

### **Azure CLI**

```bash
# Login
az login

# Check current account
az account show

# List subscriptions
az account list

# Set subscription
az account set --subscription <subscription-id>

# List app registrations
az ad app list --display-name "EduMind.AI"

# Get tenant ID
az account show --query tenantId

# Logout
az logout
```

### **GitHub CLI**

```bash
# Login
gh auth login

# Check status
gh auth status

# Logout
gh auth logout

# List repos
gh repo list

# View repo
gh repo view
```

### **Google Cloud CLI**

```bash
# Login
gcloud auth login

# Check current account
gcloud auth list

# Set project
gcloud config set project PROJECT_ID

# Get project ID
gcloud config get-value project

# Logout
gcloud auth revoke
```

---

## 🎯 What to Do Next

Based on your current situation:

### **1. Fix Integration Tests (Immediate)**

```bash
# Option A: Quick fix with Azure setup
az login
./deployment/scripts/setup-azure-auth.sh
# Then add credentials to User Secrets as shown in output

# Option B: Mock authentication for tests
# See "Option C: Mock Authentication" above
```

### **2. Configure Branch Protection (If not done)**

```bash
gh auth login
./deployment/scripts/setup-branch-protection.sh
```

### **3. Verify Everything Works**

```bash
# Run integration tests
cd /workspaces/edumind-ai
dotnet test tests/AcademicAssessment.Tests.Integration/

# Should see 59/59 passing instead of 1/59
```

---

## ❓ Troubleshooting

### **Azure: "You don't have permission to manage app registrations"**

**Solution:**

- Ask Azure AD admin to grant you "Application Administrator" role
- Or ask admin to run `setup-azure-auth.sh` and give you the output

### **Azure: "Browser authentication fails in SSH/remote session"**

**Solution:**

```bash
az login --use-device-code
```

### **GitHub: "Not authenticated"**

**Solution:**

```bash
gh auth login
# Choose "Login with a web browser"
```

### **All: "CLI not found"**

**Solution:**

```bash
# Azure CLI (pre-installed in dev container)
az --version

# GitHub CLI (pre-installed in dev container)
gh --version

# Google Cloud CLI (not installed by default)
curl https://sdk.cloud.google.com | bash
```

---

## 📚 Additional Resources

- **Azure CLI Docs:** <https://docs.microsoft.com/en-us/cli/azure/>
- **GitHub CLI Docs:** <https://cli.github.com/manual/>
- **Google Cloud CLI Docs:** <https://cloud.google.com/sdk/docs>
- **Microsoft Identity Docs:** <https://docs.microsoft.com/en-us/azure/active-directory/develop/>
- **ASP.NET Core Authentication:** <https://docs.microsoft.com/en-us/aspnet/core/security/authentication/>

---

**Need help?** See the setup scripts in `deployment/scripts/` for detailed guidance!
