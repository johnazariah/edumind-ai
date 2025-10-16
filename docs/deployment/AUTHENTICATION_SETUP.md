# Authentication Setup Guide

This comprehensive guide covers authentication and database integration for EduMind.AI, including CLI-based setup for different authentication providers and database configuration.

## 🎯 Quick Reference

| Component | Status | CLI Tool | Setup Script |
|-----------|--------|----------|--------------|
| **Microsoft Entra ID** | ✅ Primary | Azure CLI (`az`) | `./deployment/scripts/setup-azure-auth.sh` |
| **PostgreSQL Database** | ✅ Configured | Docker Compose | `docker-compose up -d` |
| **JWT Authentication** | ✅ Implemented | N/A | Configuration in `appsettings.json` |
| **GitHub** | ✅ Branch protection | GitHub CLI (`gh`) | `./deployment/scripts/setup-branch-protection.sh` |
| **Google OAuth** | ⏳ Planned | Google Cloud CLI (`gcloud`) | See Azure AD B2C section |

---

## Table of Contents

1. [Database Setup](#database-setup)
2. [Microsoft Entra ID Setup](#microsoft-entra-id-setup)
3. [JWT Authentication Configuration](#jwt-authentication-configuration)
4. [Azure AD B2C with Google OAuth](#azure-ad-b2c-with-google-oauth)
5. [Testing Authentication](#testing-authentication)
6. [Security Best Practices](#security-best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Database Setup

### PostgreSQL Configuration

**Docker Compose**: PostgreSQL 16 running on port 5432

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: edumind_dev
      POSTGRES_USER: edumind_user
      POSTGRES_PASSWORD: edumind_dev_password
    ports:
      - "5432:5432"
```

**Quick Start:**

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Check status
docker ps | grep postgres

# View logs
docker-compose logs -f postgres

# Test connection
psql -h localhost -U edumind_user -d edumind_dev
```

### Entity Framework Core Migrations

**Initial Migration**: `20251015005710_InitialCreate`

**Tables Created**:
- Users, Schools, Classes, Students, Courses
- Assessments, Questions
- StudentAssessments, StudentResponses

**Apply Migrations:**

```bash
# Apply migrations to database
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Create new migration
dotnet ef migrations add MigrationName \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Remove last migration
dotnet ef migrations remove \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web
```

### Database Connection Configuration

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "AcademicDatabase": "Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password"
  }
}
```

**Environment Variables (Optional):**

```bash
export ConnectionStrings__AcademicDatabase="Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password"
```

---

## Microsoft Entra ID Setup

You're using **Microsoft Identity (Entra ID)** for authentication. This is essential for production deployment and integration tests.

### Quick Setup (3 Commands)

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

### What the Script Does

1. ✅ Checks Azure CLI installation (pre-installed in dev container)
2. ✅ Verifies you're authenticated
3. ✅ Checks you have permissions to create app registrations
4. ✅ Creates/updates app registration for "EduMind.AI"
5. ✅ Configures redirect URIs for local development
6. ✅ Creates client secret
7. ✅ Configures API permissions (Microsoft Graph)
8. ✅ Outputs configuration for `appsettings.json` or User Secrets

### Authentication Methods

#### Option 1: Browser Login (Recommended)

```bash
az login
```

- Opens browser for interactive authentication
- Shows available subscriptions
- Easiest for development

#### Option 2: Device Code (for SSH/remote)

```bash
az login --use-device-code
```

- Displays a code
- Visit: https://microsoft.com/devicelogin
- Enter code and authenticate

#### Option 3: Service Principal (for CI/CD)

```bash
az login --service-principal \
  --username <app-id> \
  --password <password> \
  --tenant <tenant-id>
```

### Required Permissions

To create app registrations, you need one of these Azure AD roles:

- **Application Administrator**
- **Cloud Application Administrator**
- **Global Administrator**

Check your roles: https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RolesAndAdministrators

### Useful Azure CLI Commands

```bash
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

---

## JWT Authentication Configuration

### Packages Installed

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="3.2.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
```

### Configuration in appsettings.json

```json
{
  "AzureAdB2C": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "common",
    "ClientId": "",
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

### TenantContextJwt Implementation

**File**: `src/AcademicAssessment.Infrastructure/Context/TenantContextJwt.cs`

**Features**:
- Extracts user information from JWT claims
- Supports standard claims (sub, oid, email, name, role)
- Supports Azure AD B2C extension attributes (extension_SchoolId, extension_ClassIds)
- Implements row-level security logic
- Role hierarchy support (SystemAdmin > BusinessAdmin > SchoolAdmin > CourseAdmin > Teacher > Student)

**Key Properties**:
- `UserId`: From `sub`, `oid`, or `NameIdentifier` claim
- `Email`: From `email` or `preferred_username` claim
- `FullName`: From `name` claim (falls back to email)
- `Role`: From `role` or `roles` claim
- `SchoolId`: From `schoolId`, `school_id`, or `extension_SchoolId` claim
- `ClassIds`: From `classIds`, `class_ids`, or `extension_ClassIds` claim (comma-separated)

**Key Methods**:
- `HasAccessToSchool(Guid schoolId)`: Checks if user can access a specific school
- `HasAccessToClass(Guid classId)`: Checks if user can access a specific class
- `HasRole(UserRole minimumRole)`: Checks if user has at least the specified role level

### Authentication Configuration in Program.cs

**Production Mode** (when `Authentication:Enabled = true` and not Development):

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
```

**Development Mode** (when `Authentication:Enabled = false` or Development environment):

```csharp
builder.Services.AddAuthentication();  // No authentication required
// Uses TenantContextDevelopment stub
```

### Authorization Policies

**Role-Based Policies**:
- `StudentPolicy`: Requires Student role
- `TeacherPolicy`: Requires Teacher role
- `SchoolAdminPolicy`: Requires SchoolAdmin role
- `CourseAdminPolicy`: Requires CourseAdmin role
- `BusinessAdminPolicy`: Requires BusinessAdmin role
- `SystemAdminPolicy`: Requires SystemAdmin role

**Combined Policies**:
- `AdminPolicy`: Requires SchoolAdmin, BusinessAdmin, or SystemAdmin role
- `EducatorPolicy`: Requires Teacher, SchoolAdmin, or CourseAdmin role
- `AllUsersPolicy`: Requires any authenticated user

**Controller Authorization**:

```csharp
[Authorize(Policy = "AllUsersPolicy")]
public class StudentAnalyticsController : ControllerBase
{
    // All endpoints require authentication
    // Individual authorization checks done via TenantContext
}
```

### Tenant Context Switching

**Conditional Registration in Program.cs**:

```csharp
if (authEnabled && !builder.Environment.IsDevelopment())
{
    // Production: JWT-based tenant context
    builder.Services.AddScoped<ITenantContext, TenantContextJwt>();
}
else
{
    // Development: Stub tenant context
    builder.Services.AddScoped<ITenantContext, TenantContextDevelopment>();
}
```

---

## Azure AD B2C with Google OAuth

To enable Google authentication federated through Azure AD B2C, follow these steps:

### 1. Create Azure AD B2C Tenant

```bash
# Via Azure Portal:
# 1. Go to Create a resource → Azure AD B2C
# 2. Choose tenant name: edumindai
# 3. Choose initial domain: edumindai.onmicrosoft.com
# 4. Select subscription and resource group
```

### 2. Configure Google as Identity Provider

**Step 1: Get Google OAuth 2.0 Credentials**

```bash
# Option A: Use Google Cloud CLI
gcloud alpha iap oauth-clients create $BRAND

# Option B: Use Google Cloud Console (Easier)
```

**Google Cloud Console Setup**:
1. Go to: https://console.cloud.google.com/apis/credentials
2. Create OAuth 2.0 Client ID
3. Application type: Web application
4. Authorized redirect URIs: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp`
5. Copy Client ID and Client Secret

**Step 2: Register Google in Azure AD B2C**

1. Azure AD B2C → Identity providers → Google
2. Enter:
   - **Client ID**: From Google Console
   - **Client Secret**: From Google Console
3. Save

### 3. Create User Flow

```
Name: B2C_1_susi_google
Type: Sign up and sign in
Identity providers: Select Google
User attributes and claims:
  - Email Address (collect & return)
  - Given Name (collect & return)
  - Display Name (return)
  - User's Object ID (return)
  - Custom attributes: SchoolId, ClassIds
```

### 4. Register API Application

```
Name: EduMind.AI API
Application (client) ID: <copy to appsettings.json ClientId>
Redirect URI: https://localhost:5001/signin-oidc (development)
API Permissions:
  - openid
  - profile
  - email
Expose an API:
  - API URI: https://edumindai.onmicrosoft.com/api
  - Scope: user_impersonation
```

### 5. Configure Custom Attributes

```
Azure AD B2C → User attributes → Add:
  - SchoolId (GUID/String)
  - ClassIds (String, comma-separated GUIDs)

Update user flow to collect these attributes during sign-up
Update token configuration to include these claims
```

### 6. Update Application Configuration

**appsettings.json:**

```json
{
  "AzureAdB2C": {
    "Instance": "https://edumindai.b2clogin.com/",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "SignUpSignInPolicyId": "B2C_1_susi_google",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Expected JWT Token Structure

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "oid": "00000000-0000-0000-0000-000000000001",
  "email": "student@example.com",
  "name": "John Student",
  "role": "Student",
  "extension_SchoolId": "11111111-1111-1111-1111-111111111111",
  "extension_ClassIds": "22222222-2222-2222-2222-222222222222,33333333-3333-3333-3333-333333333333",
  "iss": "https://edumindai.b2clogin.com/<tenant-id>/v2.0/",
  "aud": "<client-id>",
  "exp": 1697500800,
  "nbf": 1697497200,
  "iat": 1697497200
}
```

---

## Testing Authentication

### Development Testing (No Auth)

**appsettings.Development.json**:

```json
{
  "Authentication": {
    "Enabled": false,
    "Development": {
      "UseStubAuth": true
    }
  }
}
```

**Test endpoints without authentication**:

```bash
curl http://localhost:5103/api/v1/students/00000000-0000-0000-0000-000000000001/analytics/performance-summary
```

### Integration Tests Configuration

#### Option A: Use User Secrets (Recommended)

```bash
cd src/AcademicAssessment.Web

# Set secrets
dotnet user-secrets set "AzureAd:TenantId" "<from-script-output>"
dotnet user-secrets set "AzureAd:ClientId" "<from-script-output>"
dotnet user-secrets set "AzureAd:ClientSecret" "<from-script-output>"

# Verify
dotnet user-secrets list
```

#### Option B: Create appsettings.Testing.json

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

#### Option C: Mock Authentication (Best for CI/CD)

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

### Production Testing (With JWT)

**1. Obtain JWT Token:**
- Use Azure AD B2C user flow to sign in
- Copy the access token from the response

**2. Test with Authorization Header:**

```bash
curl -H "Authorization: Bearer <jwt-token>" \
  https://api.edumind.ai/api/v1/students/00000000-0000-0000-0000-000000000001/analytics/performance-summary
```

**3. Expected Responses:**
- `200 OK`: Successfully retrieved data
- `401 Unauthorized`: Missing or invalid JWT token
- `403 Forbidden`: Valid token but insufficient permissions

---

## Security Best Practices

### Row-Level Security

- All queries filtered by `SchoolId` from TenantContext
- Students can only access their own data
- Teachers can access students in their school
- Admins have broader access based on role

### JWT Token Validation

- **Issuer Validation**: Ensures token comes from Azure AD B2C
- **Audience Validation**: Ensures token is for our API
- **Lifetime Validation**: Ensures token hasn't expired
- **Signature Validation**: Ensures token hasn't been tampered with

### HTTPS Requirement

- Production requires HTTPS (`RequireHttpsMetadata: true`)
- Development allows HTTP for local testing

### Role Hierarchy

```
SystemAdmin (Level 6)
    ↓
BusinessAdmin (Level 5)
    ↓
SchoolAdmin (Level 4)
    ↓
CourseAdmin (Level 3)
    ↓
Teacher (Level 2)
    ↓
Student (Level 1)
```

### Secret Management

**1. Never commit secrets to git**

```bash
# .gitignore should include:
appsettings.*.json
!appsettings.json
**/secrets.json
```

**2. Use User Secrets for local development**

```bash
dotnet user-secrets set "Key:SubKey" "Value"
```

**3. Use environment variables for CI/CD**

```yaml
# GitHub Actions
env:
  AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
  AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
```

**4. Use Azure Key Vault for production**

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**5. Rotate secrets regularly**
- Azure AD: Every 90 days
- GitHub PAT: Every 30-90 days
- Google: Every 90 days

---

## Troubleshooting

### Database Connection Issues

```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Test connection
psql -h localhost -U edumind_user -d edumind_dev

# View tables
\dt

# View database logs
docker-compose logs -f postgres

# Fresh start (removes data)
docker-compose down -v
docker-compose up -d
```

### Azure: "You don't have permission to manage app registrations"

**Solution:**
- Ask Azure AD admin to grant you "Application Administrator" role
- Or ask admin to run `setup-azure-auth.sh` and give you the output

### Azure: "Browser authentication fails in SSH/remote session"

**Solution:**

```bash
az login --use-device-code
```

### Authentication: 401 Unauthorized

**Possible Causes:**
1. Missing JWT token in Authorization header
2. Token has expired
3. Token issuer doesn't match configuration
4. Authentication:Enabled is false

**Solution:**
- Check Authorization header format: `Bearer <token>`
- Verify token hasn't expired (check `exp` claim)
- Verify `AzureAdB2C:Instance` and `AzureAdB2C:TenantId` match token issuer

### Authentication: 403 Forbidden

**Possible Causes:**
1. Valid token but insufficient permissions
2. User role doesn't meet policy requirements
3. Row-level security blocks access (wrong SchoolId)

**Solution:**
- Check user role in JWT token
- Verify authorization policy requirements
- Check `extension_SchoolId` matches resource

### Migration Issues

```bash
# Reset database
dotnet ef database drop --project src/AcademicAssessment.Infrastructure --startup-project src/AcademicAssessment.Web

# Reapply all migrations
dotnet ef database update --project src/AcademicAssessment.Infrastructure --startup-project src/AcademicAssessment.Web
```

### GitHub: "Not authenticated"

**Solution:**

```bash
gh auth login
# Choose "Login with a web browser"
```

### CLI Tools Not Found

```bash
# Azure CLI (pre-installed in dev container)
az --version

# GitHub CLI (pre-installed in dev container)
gh --version

# Google Cloud CLI (not installed by default)
curl https://sdk.cloud.google.com | bash
exec -l $SHELL
gcloud init
```

---

## GitHub Authentication

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

## Current Implementation Status

### ✅ Completed

1. PostgreSQL database running in Docker
2. EF Core migrations created and applied
3. Real repository implementations registered
4. JWT authentication configured
5. TenantContextJwt implementation complete
6. Authorization policies defined
7. StudentAnalyticsController protected with [Authorize]
8. Development/Production mode switching
9. GitHub authentication and branch protection

### ⏳ Pending

1. Azure AD B2C tenant creation
2. Google OAuth 2.0 provider configuration
3. User flow creation (sign-up/sign-in)
4. Custom attribute configuration (SchoolId, ClassIds)
5. API application registration
6. Production ClientId/TenantId configuration
7. Integration tests with JWT tokens
8. Stub infrastructure removal

### 📋 Next Steps

1. **Set up Azure AD B2C** (30-45 minutes)
   - Create tenant
   - Configure Google identity provider
   - Create user flow
   - Register API application

2. **Update Configuration** (5 minutes)
   - Add real ClientId and TenantId to appsettings.json
   - Configure custom claims mapping

3. **Test Authentication** (15 minutes)
   - Sign in with Google via B2C
   - Obtain JWT token
   - Test API endpoints with token
   - Verify authorization rules

4. **Update Integration Tests** (1-2 hours)
   - Add JWT token generation for tests
   - Update test setup to include Authorization headers
   - Add authorization failure tests

5. **Remove Stub Infrastructure** (15 minutes)
   - Delete stub repository files
   - Delete TenantContextDevelopment
   - Clean up Program.cs

---

## Additional Resources

- **Azure CLI Docs:** https://docs.microsoft.com/en-us/cli/azure/
- **GitHub CLI Docs:** https://cli.github.com/manual/
- **Google Cloud CLI Docs:** https://cloud.google.com/sdk/docs
- **Microsoft Identity Docs:** https://docs.microsoft.com/en-us/azure/active-directory/develop/
- **ASP.NET Core Authentication:** https://docs.microsoft.com/en-us/aspnet/core/security/authentication/
- **Azure AD B2C Documentation:** https://docs.microsoft.com/en-us/azure/active-directory-b2c/
- **Microsoft.Identity.Web Documentation:** https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web
- **EF Core Migrations:** https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **PostgreSQL Docker Image:** https://hub.docker.com/_/postgres

---

**Need help?** See the setup scripts in `deployment/scripts/` for detailed guidance!
