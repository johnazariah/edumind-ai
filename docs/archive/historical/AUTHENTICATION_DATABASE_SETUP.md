# Authentication & Database Integration Implementation Guide

## Overview

This document describes the authentication and database integration implementation for EduMind.AI, completed on October 15, 2025.

## What Was Implemented

### 1. Database Integration ✅

#### PostgreSQL Configuration

- **Docker Compose**: PostgreSQL 16 running on port 5432
- **Connection String**: Configured in `appsettings.json` and `appsettings.Development.json`
- **Database Name**: `edumind_dev`
- **Schemas**: academic, analytics, agents (created via init-db.sql)

#### Entity Framework Core Migrations

- **Initial Migration**: `20251015005710_InitialCreate`
- **Tables Created**: Users, Schools, Classes, Students, Courses, Assessments, Questions, StudentAssessments, StudentResponses
- **Migration Applied**: Database schema created successfully

#### Repository Registration

- **Replaced**: Stub repositories with real EF Core implementations
- **Registered Services**:
  - `IStudentAssessmentRepository` → `StudentAssessmentRepository`
  - `IStudentResponseRepository` → `StudentResponseRepository`
  - `IQuestionRepository` → `QuestionRepository`
  - `IAssessmentRepository` → `AssessmentRepository`

### 2. JWT Authentication ✅

#### Packages Installed

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="3.2.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
```

#### Azure AD B2C Configuration

**appsettings.json:**

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

#### TenantContextJwt Implementation

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

#### Authentication Configuration in Program.cs

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

#### Authorization Policies

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

### 3. Tenant Context Switching

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

## Google OAuth 2.0 Federation

### Azure AD B2C Setup (TO DO)

To enable Google authentication, you need to:

1. **Create Azure AD B2C Tenant**

   ```bash
   # Azure Portal → Create a resource → Azure AD B2C
   # Choose tenant name: edumindai
   # Choose initial domain: edumindai.onmicrosoft.com
   ```

2. **Register Google as Identity Provider**
   - Go to Azure AD B2C → Identity providers → Google
   - Obtain Google OAuth 2.0 credentials:
     - Go to <https://console.cloud.google.com/>
     - Create OAuth 2.0 Client ID
     - Set redirect URI: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp`
   - Configure in Azure AD B2C:
     - Client ID: From Google Console
     - Client Secret: From Google Console

3. **Create User Flow**

   ```
   Name: B2C_1_susi_google
   Type: Sign up and sign in
   Identity providers: Select Google
   User attributes and claims:
     - Email Address (collect & return)
     - Given Name (collect & return)
     - Display Name (return)
     - User's Object ID (return)
   ```

4. **Register API Application**

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

5. **Configure Custom Attributes**

   ```
   Azure AD B2C → User attributes → Add:
     - SchoolId (GUID/String)
     - ClassIds (String, comma-separated GUIDs)
   
   Update user flow to collect these attributes during sign-up
   Update token configuration to include these claims
   ```

6. **Update appsettings.json**

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

### JWT Token Structure

**Expected JWT Claims**:

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

## Testing

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

### Production Testing (With JWT)

1. **Obtain JWT Token**:
   - Use Azure AD B2C user flow to sign in
   - Copy the access token from the response

2. **Test with Authorization Header**:

```bash
curl -H "Authorization: Bearer <jwt-token>" \
  https://api.edumind.ai/api/v1/students/00000000-0000-0000-0000-000000000001/analytics/performance-summary
```

3. **Expected Responses**:
   - `200 OK`: Successfully retrieved data
   - `401 Unauthorized`: Missing or invalid JWT token
   - `403 Forbidden`: Valid token but insufficient permissions

## Security Considerations

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

## Current Status

### ✅ Completed

1. PostgreSQL database running in Docker
2. EF Core migrations created and applied
3. Real repository implementations registered
4. JWT authentication configured
5. TenantContextJwt implementation complete
6. Authorization policies defined
7. StudentAnalyticsController protected with [Authorize]
8. Development/Production mode switching

### 🚧 Pending

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

## Files Changed

### New Files

- `src/AcademicAssessment.Infrastructure/Context/TenantContextJwt.cs`
- `src/AcademicAssessment.Infrastructure/Data/Migrations/20251015005710_InitialCreate.cs`
- `src/AcademicAssessment.Infrastructure/Data/Migrations/20251015005710_InitialCreate.Designer.cs`
- `src/AcademicAssessment.Infrastructure/Data/Migrations/AcademicContextModelSnapshot.cs`

### Modified Files

- `src/AcademicAssessment.Web/Program.cs`
  - Added authentication configuration
  - Added authorization policies
  - Switched to real repositories
  - Conditional TenantContext registration
- `src/AcademicAssessment.Web/appsettings.json`
  - Added AzureAdB2C section
  - Added Authentication section
  - Added ConnectionStrings section
- `src/AcademicAssessment.Web/appsettings.Development.json`
  - Added ConnectionStrings section
  - Added Authentication section with development flag
- `src/AcademicAssessment.Web/AcademicAssessment.Web.csproj`
  - Added Microsoft.Identity.Web package
  - Added Microsoft.EntityFrameworkCore.Design package
  - Added Npgsql.EntityFrameworkCore.PostgreSQL package
- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
  - Added [Authorize(Policy = "AllUsersPolicy")] attribute
  - Added Microsoft.AspNetCore.Authorization using statement

## Configuration Reference

### Environment Variables (Optional)

```bash
export ConnectionStrings__AcademicDatabase="Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password"
export AzureAdB2C__ClientId="<your-client-id>"
export AzureAdB2C__TenantId="<your-tenant-id>"
export Authentication__Enabled="true"
```

### Docker Compose Commands

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f postgres

# Stop services
docker-compose down

# Remove volumes (fresh start)
docker-compose down -v
```

### EF Core Migration Commands

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/AcademicAssessment.Infrastructure --startup-project src/AcademicAssessment.Web

# Apply migrations
dotnet ef database update --project src/AcademicAssessment.Infrastructure --startup-project src/AcademicAssessment.Web

# Remove last migration
dotnet ef migrations remove --project src/AcademicAssessment.Infrastructure --startup-project src/AcademicAssessment.Web
```

## Troubleshooting

### Database Connection Issues

```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Test connection
psql -h localhost -U edumind_user -d edumind_dev

# View tables
\dt
```

### Authentication Issues

1. **401 Unauthorized**: Check if JWT token is included in Authorization header
2. **403 Forbidden**: Check user role and policy requirements
3. **Invalid Token**: Verify token hasn't expired and is from correct issuer

### Development Mode Issues

- Ensure `Authentication:Enabled = false` in appsettings.Development.json
- Verify using TenantContextDevelopment (check logs)
- Restart application after configuration changes

## Additional Resources

- [Azure AD B2C Documentation](https://docs.microsoft.com/en-us/azure/active-directory-b2c/)
- [Microsoft.Identity.Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
