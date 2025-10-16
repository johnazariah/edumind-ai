# Azure AD B2C Setup Guide with Google OAuth 2.0

## Overview

This guide walks through setting up Azure AD B2C with Google as a federated identity provider for EduMind.AI. This enables users to sign in with their Google accounts.

**Estimated Time**: 30-45 minutes

## Prerequisites

- Azure subscription with permissions to create resources
- Google account with access to Google Cloud Console
- Azure CLI installed (already available in dev container)

## Part 1: Create Google OAuth 2.0 Credentials (15 minutes)

### Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click **Select a project** → **New Project**
3. Enter project details:
   - **Project name**: `EduMind-AI-Auth`
   - **Organization**: (your organization or leave blank)
4. Click **Create**
5. Wait for project creation (takes ~30 seconds)

### Step 2: Configure OAuth Consent Screen

1. In Google Cloud Console, go to **APIs & Services** → **OAuth consent screen**
2. Select **External** user type (for public access)
3. Click **Create**
4. Fill in the OAuth consent screen:
   - **App name**: `EduMind.AI`
   - **User support email**: Your email address
   - **App logo**: (optional, can upload later)
   - **Application home page**: `https://edumind.ai` (or your domain)
   - **Authorized domains**:
     - `edumindai.b2clogin.com`
     - `edumindai.onmicrosoft.com`
   - **Developer contact information**: Your email address
5. Click **Save and Continue**
6. **Scopes**: Click **Add or Remove Scopes**
   - Select: `email`, `profile`, `openid`
   - Click **Update** → **Save and Continue**
7. **Test users**: (optional for development)
   - Add your email addresses for testing
   - Click **Save and Continue**
8. **Summary**: Review and click **Back to Dashboard**

### Step 3: Create OAuth 2.0 Credentials

1. Go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. Select **Application type**: **Web application**
4. Enter details:
   - **Name**: `EduMind.AI Azure AD B2C`
   - **Authorized JavaScript origins**: Leave empty
   - **Authorized redirect URIs**:

     ```
     https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp
     ```

     ⚠️ **IMPORTANT**: This exact URL format is required by Azure AD B2C
5. Click **Create**
6. **Save these credentials** (you'll need them in Part 2):
   - **Client ID**: `xxxxxxxxxxxx-xxxxxxxxxxxx.apps.googleusercontent.com`
   - **Client Secret**: `GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxx`
7. Click **OK**

### Step 4: Verify Configuration

1. Go to **OAuth consent screen**
2. Verify status is "Testing" or "In production"
3. Note: For production, you'll need to submit for verification

## Part 2: Create Azure AD B2C Tenant (10 minutes)

### Step 1: Create B2C Tenant

**Option A: Using Azure Portal**

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource**
3. Search for "Azure Active Directory B2C"
4. Click **Create**
5. Select **Create a new Azure AD B2C Tenant**
6. Enter tenant details:
   - **Organization name**: `EduMind.AI`
   - **Initial domain name**: `edumindai` (must be globally unique)
   - **Country/Region**: Select your region
   - **Subscription**: Select your subscription
   - **Resource group**: Create new: `rg-edumind-b2c`
7. Click **Review + create** → **Create**
8. Wait for deployment (takes 2-3 minutes)
9. After creation, click **Go to resource**

**Option B: Using Azure CLI**

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-edumind-b2c --location eastus

# Note: B2C tenant creation via CLI is limited
# Recommend using Azure Portal for tenant creation
```

### Step 2: Switch to B2C Tenant

1. In Azure Portal, click on your user icon (top right)
2. Click **Switch directory**
3. Select your B2C directory: `EduMind.AI (edumindai.onmicrosoft.com)`
4. You should now see "Azure AD B2C" in the portal

### Step 3: Get Tenant Information

Record these values (you'll need them later):

```bash
# Tenant Name: edumindai
# Tenant ID: Go to Azure AD B2C → Overview → Directory ID
# Domain: edumindai.onmicrosoft.com
# B2C Login URL: edumindai.b2clogin.com
```

## Part 3: Configure Google Identity Provider in Azure AD B2C (10 minutes)

### Step 1: Add Google Identity Provider

1. In Azure AD B2C, go to **Identity providers** (left menu)
2. Click **+ New OpenID Connect provider**
3. Enter configuration:
   - **Name**: `Google`
   - **Metadata URL**:

     ```
     https://accounts.google.com/.well-known/openid-configuration
     ```

   - **Client ID**: (paste from Google Cloud Console - Step 3)
   - **Client secret**: (paste from Google Cloud Console - Step 3)
   - **Scope**: `openid email profile`
   - **Response type**: `code`
   - **Response mode**: `form_post`
   - **Domain hint**: `gmail.com` (optional)
4. **Claims mapping**:
   - **User ID**: `sub`
   - **Display name**: `name`
   - **Given name**: `given_name`
   - **Surname**: `family_name`
   - **Email**: `email`
5. Click **Save**

### Step 2: Verify Provider

1. Go to **Identity providers**
2. You should see "Google" in the list
3. Status should show as "Enabled"

## Part 4: Create Custom Attributes (5 minutes)

### Step 1: Define Custom Attributes

1. In Azure AD B2C, go to **User attributes**
2. Click **+ Add**
3. Create attribute: **SchoolId**
   - **Name**: `SchoolId`
   - **Data Type**: `String`
   - **Description**: `School/Organization ID for B2B users`
4. Click **Create**
5. Click **+ Add** again
6. Create attribute: **ClassIds**
   - **Name**: `ClassIds`
   - **Data Type**: `String`
   - **Description**: `Comma-separated class IDs for students/teachers`
7. Click **Create**

### Step 2: Verify Attributes

1. Go to **User attributes**
2. Verify both `extension_SchoolId` and `extension_ClassIds` are listed
3. Note: The "extension_" prefix is automatic

## Part 5: Create User Flow (10 minutes)

### Step 1: Create Sign Up and Sign In Flow

1. In Azure AD B2C, go to **User flows**
2. Click **+ New user flow**
3. Select **Sign up and sign in**
4. Select **Recommended** version
5. Enter configuration:
   - **Name**: `susi_google` (will become B2C_1_susi_google)
   - **Identity providers**:
     - Check ✅ **Google**
     - (Optional) Check **Email signup** for non-Google users
6. **Multifactor authentication**:
   - Select **Optional** or **Conditional** based on security needs
7. **User attributes and token claims**:

   **Collect attributes** (what user provides during signup):
   - ✅ Display Name
   - ✅ Email Address
   - ✅ Given Name
   - ✅ Surname
   - ✅ SchoolId (custom)
   - ✅ ClassIds (custom)

   **Return claims** (what's included in JWT token):
   - ✅ Display Name
   - ✅ Email Addresses
   - ✅ Given Name
   - ✅ Surname
   - ✅ User's Object ID
   - ✅ SchoolId (custom)
   - ✅ ClassIds (custom)

8. Click **Create**

### Step 2: Test User Flow

1. Go to **User flows** → **B2C_1_susi_google**
2. Click **Run user flow**
3. Select your application (will set up in Part 6)
4. Click **Run user flow**
5. You should see Google sign-in option
6. Test the sign-in process

## Part 6: Register API Application (10 minutes)

### Step 1: Register Application

1. In Azure AD B2C, go to **App registrations**
2. Click **+ New registration**
3. Enter application details:
   - **Name**: `EduMind.AI API`
   - **Supported account types**:
     - Select "Accounts in any identity provider or organizational directory (for authenticating users with user flows)"
   - **Redirect URI**:
     - Platform: **Web**
     - URL: `https://localhost:5001/signin-oidc` (for development)
4. Click **Register**

### Step 2: Configure Application

1. After registration, note the **Application (client) ID**
2. Go to **Authentication**:
   - **Implicit grant and hybrid flows**:
     - Check ✅ **ID tokens** (for development/testing)
   - **Allow public client flows**: No
3. Click **Save**

4. Go to **Certificates & secrets**:
   - Click **+ New client secret**
   - Description: `API Secret`
   - Expires: 24 months (or choose based on policy)
   - Click **Add**
   - **Copy the secret value** (won't be shown again!)

5. Go to **API permissions**:
   - Default permissions are OK for now
   - Permissions: `openid`, `offline_access`

6. Go to **Expose an API**:
   - Click **+ Add a scope**
   - **Application ID URI**: Click **Set** → Use default: `https://edumindai.onmicrosoft.com/api`
   - Click **Save and continue**
   - **Scope name**: `user_impersonation`
   - **Admin consent display name**: `Access EduMind.AI API`
   - **Admin consent description**: `Allow the application to access EduMind.AI API on behalf of the signed-in user.`
   - **State**: Enabled
   - Click **Add scope**

### Step 3: Add Additional Redirect URIs (for production)

1. Go back to **Authentication**
2. Add production redirect URIs:

   ```
   https://api.edumind.ai/signin-oidc
   https://app.edumind.ai/signin-oidc
   https://student.edumind.ai/signin-oidc
   https://teacher.edumind.ai/signin-oidc
   https://admin.edumind.ai/signin-oidc
   ```

3. Click **Save**

## Part 7: Update Application Configuration

### Step 1: Update appsettings.json

Update your `src/AcademicAssessment.Web/appsettings.json` with the real values:

```json
{
  "AzureAdB2C": {
    "Instance": "https://edumindai.b2clogin.com/",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "<YOUR-TENANT-ID>",
    "ClientId": "<YOUR-APPLICATION-CLIENT-ID>",
    "ClientSecret": "<YOUR-CLIENT-SECRET>",
    "SignUpSignInPolicyId": "B2C_1_susi_google",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "Authentication": {
    "Enabled": true,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "RequireHttpsMetadata": true
  }
}
```

### Step 2: Set Environment Variables (Recommended for secrets)

For better security, use environment variables or Azure Key Vault:

```bash
# Linux/Mac
export AzureAdB2C__TenantId="<YOUR-TENANT-ID>"
export AzureAdB2C__ClientId="<YOUR-APPLICATION-CLIENT-ID>"
export AzureAdB2C__ClientSecret="<YOUR-CLIENT-SECRET>"

# Windows PowerShell
$env:AzureAdB2C__TenantId="<YOUR-TENANT-ID>"
$env:AzureAdB2C__ClientId="<YOUR-APPLICATION-CLIENT-ID>"
$env:AzureAdB2C__ClientSecret="<YOUR-CLIENT-SECRET>"
```

### Step 3: Update User Secrets (Development)

```bash
cd /workspaces/edumind-ai/src/AcademicAssessment.Web

# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "AzureAdB2C:TenantId" "<YOUR-TENANT-ID>"
dotnet user-secrets set "AzureAdB2C:ClientId" "<YOUR-APPLICATION-CLIENT-ID>"
dotnet user-secrets set "AzureAdB2C:ClientSecret" "<YOUR-CLIENT-SECRET>"

# Verify secrets
dotnet user-secrets list
```

## Part 8: Test Authentication (15 minutes)

### Step 1: Enable Authentication in Development

Update `appsettings.Development.json`:

```json
{
  "Authentication": {
    "Enabled": true,
    "Development": {
      "UseStubAuth": false
    }
  }
}
```

### Step 2: Run the Application

```bash
cd /workspaces/edumind-ai
dotnet run --project src/AcademicAssessment.Web/AcademicAssessment.Web.csproj
```

### Step 3: Test Authentication Flow

**Option A: Using Browser**

1. Navigate to: `https://localhost:5001/swagger`
2. You should be redirected to B2C login
3. Click "Google" sign-in button
4. Sign in with your Google account
5. After successful login, you should see Swagger UI
6. Click "Authorize" button
7. You should see your JWT token

**Option B: Using Postman**

1. Create new request in Postman
2. Go to **Authorization** tab
3. Type: **OAuth 2.0**
4. Configure:
   - **Grant Type**: Authorization Code (with PKCE)
   - **Auth URL**: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/B2C_1_susi_google/oauth2/v2.0/authorize`
   - **Access Token URL**: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/B2C_1_susi_google/oauth2/v2.0/token`
   - **Client ID**: (your client ID)
   - **Client Secret**: (your client secret)
   - **Scope**: `openid profile email https://edumindai.onmicrosoft.com/api/user_impersonation`
   - **State**: (auto-generated)
5. Click **Get New Access Token**
6. Sign in with Google
7. Copy the access token

### Step 4: Test API Endpoint

```bash
# Replace <JWT_TOKEN> with your token from Step 3
curl -X GET "https://localhost:5001/api/v1/students/00000000-0000-0000-0000-000000000001/analytics/performance-summary" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -H "accept: application/json"
```

**Expected Response**:

- `200 OK` with data (if student exists and has data)
- `403 Forbidden` (if token is valid but user doesn't have access)
- `404 Not Found` (if student doesn't exist)
- `401 Unauthorized` (if token is invalid or missing)

### Step 5: Verify JWT Token Claims

Decode your JWT token at [jwt.io](https://jwt.io) and verify it contains:

```json
{
  "sub": "google-user-id",
  "oid": "azure-object-id",
  "email": "user@gmail.com",
  "name": "User Name",
  "given_name": "User",
  "family_name": "Name",
  "extension_SchoolId": "school-guid-if-provided",
  "extension_ClassIds": "class1-guid,class2-guid",
  "iss": "https://edumindai.b2clogin.com/tenant-id/v2.0/",
  "aud": "your-client-id",
  "exp": 1697500800,
  "nbf": 1697497200
}
```

## Part 9: Configure Role Claims (Optional - 10 minutes)

Azure AD B2C doesn't provide role claims by default. You have two options:

### Option A: Use Custom Attributes (Simpler)

Add a `Role` custom attribute and have users select their role during sign-up.

### Option B: Use API Connector (More Flexible)

1. Create an Azure Function that assigns roles based on business logic
2. Configure it as an API connector in the user flow
3. The function can query your database to determine roles

### Option C: Use App Roles (Most Robust)

1. Go to **App registrations** → Your app → **App roles**
2. Create roles:
   - Student
   - Teacher
   - SchoolAdmin
   - CourseAdmin
   - BusinessAdmin
   - SystemAdmin
3. Assign users to roles via Azure Portal
4. Roles will appear in JWT as `roles` claim

**For now, we'll use Option A with custom attributes.**

## Troubleshooting

### Issue: "The redirect URI is not valid"

**Solution**:

- Verify redirect URI exactly matches: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp`
- No trailing slash
- Check for typos in domain name

### Issue: "AADB2C90273: An invalid redirect_uri was provided"

**Solution**:

- Add the redirect URI to your application registration
- Wait 5-10 minutes for changes to propagate

### Issue: "Custom attributes not appearing in token"

**Solution**:

- Ensure attributes are selected in "Return claims" in user flow
- User must provide values during sign-up for attributes to appear
- Use exact claim names: `extension_SchoolId`, `extension_ClassIds`

### Issue: "Google OAuth consent screen error"

**Solution**:

- Verify authorized domains include `edumindai.b2clogin.com`
- Ensure OAuth consent screen is published or in testing mode
- Add test users if in testing mode

### Issue: "Authentication works but claims are missing"

**Solution**:

- Check user flow configuration - ensure claims are in "Return claims"
- Verify claims mapping in Google identity provider
- Check scope includes all required scopes

## Security Checklist

- [ ] Client secret stored securely (Key Vault or user secrets)
- [ ] HTTPS required in production (`RequireHttpsMetadata: true`)
- [ ] Token lifetime appropriate (default 1 hour)
- [ ] Refresh tokens enabled for long-lived sessions
- [ ] MFA enabled for admin roles
- [ ] Rate limiting configured
- [ ] Logging enabled for authentication events
- [ ] Regular audit of user permissions

## Production Considerations

1. **Google OAuth Verification**:
   - Submit app for verification in Google Cloud Console
   - Provide privacy policy and terms of service
   - May take 1-2 weeks for approval

2. **Azure AD B2C Limits**:
   - First 50,000 authentications/month: Free
   - Additional authentications: $0.0055/authentication
   - First 50,000 MFA/month: Free
   - Plan for scale

3. **Custom Domains**:
   - Configure custom domain: `login.edumind.ai`
   - Requires Azure Front Door Premium
   - Better branding and trust

4. **Monitoring**:
   - Enable Application Insights
   - Monitor sign-in logs
   - Set up alerts for failures

5. **Backup & Recovery**:
   - Export user flow configurations
   - Document all settings
   - Have rollback plan

## Next Steps

1. ✅ Azure AD B2C configured with Google
2. ⏭️ Update integration tests with JWT tokens
3. ⏭️ Test all 7 analytics endpoints with authentication
4. ⏭️ Remove stub infrastructure
5. ⏭️ Deploy to staging environment

## Resources

- [Azure AD B2C Documentation](https://docs.microsoft.com/en-us/azure/active-directory-b2c/)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [Microsoft.Identity.Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [JWT.io - Token Decoder](https://jwt.io)
- [Azure AD B2C Samples](https://github.com/azure-ad-b2c/samples)

## Summary

You've successfully configured:

- ✅ Google OAuth 2.0 credentials
- ✅ Azure AD B2C tenant
- ✅ Google as identity provider
- ✅ Custom attributes (SchoolId, ClassIds)
- ✅ User flow for sign-up/sign-in
- ✅ API application registration
- ✅ Local application configuration

Your application is now ready for Google-authenticated users! 🎉
