# Security and Privacy Specification

> **Version:** 1.0  
> **Last Updated:** January 2025  
> **Status:** Living Document

## Table of Contents

1. [Overview](#1-overview)
2. [Guiding Principles](#2-guiding-principles)
3. [Authentication](#3-authentication)
4. [Authorization](#4-authorization)
5. [Multi-Tenant Isolation](#5-multi-tenant-isolation)
6. [Privacy Compliance](#6-privacy-compliance)
7. [Data Protection](#7-data-protection)
8. [Audit Logging](#8-audit-logging)
9. [Security Layers](#9-security-layers)
10. [Threat Mitigation](#10-threat-mitigation)
11. [Implementation Details](#11-implementation-details)

---

## 1. Overview

### Purpose

This document defines the comprehensive security and privacy architecture for EduMind.AI, ensuring:

- **Student data protection** as the highest priority
- **Regulatory compliance** with FERPA, COPPA, and GDPR
- **Multi-tenant isolation** preventing cross-school data leaks
- **Defense-in-depth** strategy with multiple security layers
- **Privacy-preserving analytics** protecting individual identity

### Security Status

| Component | Status | Notes |
|-----------|--------|-------|
| Authentication | ✅ Implemented | Azure AD B2C + Google OAuth |
| JWT Validation | ✅ Implemented | Bearer token with claims |
| RBAC | ✅ Implemented | 6 roles with hierarchy |
| Row-Level Security | ✅ Implemented | EF Core query filters |
| Multi-Tenant Isolation | ⚠️ Partial | Physical DB partitioning documented, currently using logical |
| Audit Logging | ⚠️ Partial | Structured logging implemented, dedicated audit log planned |
| Data Encryption | ⚠️ Partial | TLS in transit, at-rest encryption via Azure |
| Privacy Aggregation | ⚠️ Partial | k-anonymity rules documented, not fully implemented |
| COPPA Compliance | 📋 Planned | Age verification + parental consent needed |
| GDPR Right to Delete | 📋 Planned | Data deletion service designed |

### Related Documents

- [Domain Model](03-domain-model.md) - Entity security properties
- [System Architecture](02-system-architecture.md) - Multi-tenancy design
- [Data Storage](05-data-storage.md) - Database isolation strategy
- [External Integrations](06-external-integrations.md) - Azure AD B2C configuration

---

## 2. Guiding Principles

### Core Privacy Principle

**"Student data is sacred and must be protected at all costs."**

Every architectural decision must prioritize student privacy and data protection. When in doubt, choose the more secure option.

### Legal and Ethical Framework

#### FERPA Compliance (Family Educational Rights and Privacy Act)

**Requirement:** Protect student educational records

**Implementation:**

- Student data classified as confidential educational records
- Access strictly limited to legitimate educational interest
- Parental consent required for data sharing (under 18)
- Students/parents can inspect and request corrections
- Annual notification of privacy rights

**Technical Controls:**

- Role-based access control (RBAC) enforcing "need to know"
- Audit logging of all access to student records
- Secure data export for parent/student review
- Data correction workflow with approval chain

#### COPPA Compliance (Children's Online Privacy Protection Act)

**Requirement:** Enhanced protections for students under 13

**Implementation:**

- Age verification during account creation
- Parental notification and verifiable consent
- Limited data collection (only educationally necessary)
- No third-party advertising or tracking
- Parental access to child's data
- Right to delete child's data

**Technical Controls:**

- Age field required in Student entity
- Parental consent flags in database
- Consent timestamp and method tracking
- Automated consent reminder emails
- Data minimization in collection forms

#### GDPR Principles (for international deployments)

**Requirement:** EU data protection regulation

**Implementation:**

- **Right to be forgotten**: Complete data deletion capability
- **Data portability**: Export all student data in machine-readable format
- **Privacy by design**: Security built into every feature
- **Data minimization**: Collect only necessary information
- **Consent management**: Clear opt-in for data processing
- **Breach notification**: 72-hour breach reporting

**Technical Controls:**

- Student data deletion service
- JSON export of all student records
- Encrypted data storage
- Minimal field collection
- Consent tracking in database
- Incident response procedures

#### Ethical Data Stewardship

**Principles:**

1. **Collect only what's necessary** - No extraneous data fields
2. **Use only for educational purposes** - No marketing or sale
3. **Never sell or share student data** - Zero third-party sharing
4. **Transparent data practices** - Clear privacy policy
5. **Student benefit first** - Features serve learning outcomes

---

## 3. Authentication

### 3.1 Azure AD B2C

**Provider:** Microsoft Azure Active Directory B2C  
**Status:** ✅ Operational  
**Documentation:** [Azure AD B2C Setup](../../docs/deployment/AUTHENTICATION_SETUP.md)

#### Configuration

**Tenant:** `edumindai.onmicrosoft.com` (example)

```json
{
  "AzureAdB2C": {
    "Instance": "https://edumindai.b2clogin.com",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "SignUpSignInPolicyId": "B2C_1_signup_signin",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

#### User Flows

1. **Sign-Up/Sign-In Flow** (`B2C_1_signup_signin`)
   - Email/password authentication
   - Google OAuth integration
   - Multi-factor authentication (optional)
   - Claims mapping: userId, email, name, role

2. **Password Reset Flow** (`B2C_1_password_reset`)
   - Email verification
   - Secure password reset link (1-hour expiration)
   - Password strength requirements

3. **Profile Edit Flow** (`B2C_1_profile_edit`)
   - Update display name
   - Change email (requires verification)
   - Update profile picture

#### Identity Providers

| Provider | Status | Use Case |
|----------|--------|----------|
| Email/Password | ✅ Operational | Primary authentication |
| Google OAuth 2.0 | ✅ Operational | Social login (students/teachers) |
| Microsoft Account | 📋 Planned | Enterprise SSO |
| SAML 2.0 (School IdP) | 📋 Planned | District-wide SSO |

### 3.2 JWT Token Structure

**Token Type:** Bearer  
**Signing Algorithm:** RS256 (RSA with SHA-256)  
**Expiration:** 1 hour (configurable)

#### Standard Claims

```json
{
  "iss": "https://edumindai.b2clogin.com/<tenant-id>/v2.0/",
  "sub": "00000000-0000-0000-0000-000000000001",
  "aud": "<client-id>",
  "exp": 1735689600,
  "iat": 1735686000,
  "nbf": 1735686000,
  "email": "student@example.com",
  "name": "John Doe",
  "given_name": "John",
  "family_name": "Doe"
}
```

#### Custom Claims (Extension Attributes)

```json
{
  "role": "Student",
  "schoolId": "00000000-0000-0000-0000-000000000010",
  "classIds": "guid1,guid2,guid3",
  "extension_SchoolCode": "SCHOOL001",
  "extension_GradeLevel": "Grade8"
}
```

**Claim Mapping:**

| Claim Type | Source | Example | Purpose |
|------------|--------|---------|---------|
| `sub` | Azure AD B2C | `<user-object-id>` | Unique user identifier |
| `email` | User profile | `student@example.com` | User email address |
| `name` | User profile | `John Doe` | Display name |
| `role` | Custom attribute | `Student` | User role (RBAC) |
| `schoolId` | Custom attribute | `<guid>` | School tenant ID |
| `classIds` | Custom attribute | `guid1,guid2` | Comma-separated class IDs |

### 3.3 Token Validation

**Implementation Location:** `src/AcademicAssessment.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{azureAdB2CSettings.Instance}/{azureAdB2CSettings.TenantId}/{azureAdB2CSettings.SignUpSignInPolicyId}/v2.0/";
        options.Audience = azureAdB2CSettings.ClientId;
        options.RequireHttpsMetadata = true; // Production only
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5min clock skew
        };
    });
```

**Validation Steps:**

1. **Issuer Validation** - Ensures token from trusted Azure AD B2C tenant
2. **Audience Validation** - Ensures token intended for this API
3. **Lifetime Validation** - Ensures token not expired
4. **Signature Validation** - Ensures token not tampered with using public key
5. **Claims Extraction** - Extracts user identity and tenant context

### 3.4 Tenant Context Extraction

**Implementation Location:** `src/AcademicAssessment.Infrastructure/Context/TenantContextJwt.cs`

After successful JWT validation, the `TenantContextJwt` service extracts tenant information from claims:

```csharp
public sealed class TenantContextJwt : ITenantContext
{
    public Guid UserId { get; } // From 'sub' or 'oid' claim
    public string Email { get; } // From 'email' claim
    public string FullName { get; } // From 'name' claim
    public UserRole Role { get; } // From 'role' claim
    public Guid? SchoolId { get; } // From 'schoolId' claim
    public IReadOnlyList<Guid> ClassIds { get; } // From 'classIds' claim (parsed)
    
    // Constructor extracts from HttpContext.User claims
    public TenantContextJwt(IHttpContextAccessor httpContextAccessor) { }
}
```

**Middleware Integration:** `TenantContextMiddleware` populates `ITenantContext` for each request.

### 3.5 Authentication Flow

```
┌──────────┐
│  Client  │
│ (Blazor) │
└────┬─────┘
     │
     │ 1. Navigate to protected page
     ▼
┌────────────────┐
│  Web Server    │
│  (ASP.NET)     │
└────┬───────────┘
     │
     │ 2. No auth token → Redirect to Azure AD B2C
     ▼
┌────────────────────┐
│   Azure AD B2C     │
│  Login Page        │
└────┬───────────────┘
     │
     │ 3. User logs in (Email/Password or Google)
     ▼
┌────────────────────┐
│   Azure AD B2C     │
│  Issues JWT Token  │
└────┬───────────────┘
     │
     │ 4. Redirect back with JWT token
     ▼
┌────────────────┐
│  Web Server    │
│  Validates JWT │
└────┬───────────┘
     │
     │ 5. Extract claims → Create TenantContext
     ▼
┌────────────────────┐
│  Application Layer │
│  (Authorized)      │
└────────────────────┘
```

---

## 4. Authorization

### 4.1 Role-Based Access Control (RBAC)

**Implementation:** 6 distinct user roles with hierarchical permissions

#### Role Hierarchy

```
SystemAdmin (Level 6)      - Full system access
    ↓
BusinessAdmin (Level 5)    - Multi-tenant management
    ↓
SchoolAdmin (Level 4)      - Single school administration
    ↓
CourseAdmin (Level 3)      - Curriculum and content
    ↓
Teacher (Level 2)          - Class and student management
    ↓
Student (Level 1)          - Individual self-service
```

**Hierarchy Rule:** Higher roles inherit permissions from lower roles.

**Implementation Location:** `src/AcademicAssessment.Core/Enums/UserRole.cs`

```csharp
public enum UserRole
{
    Student = 1,
    Teacher = 2,
    CourseAdmin = 3,
    SchoolAdmin = 4,
    BusinessAdmin = 5,
    SystemAdmin = 6
}
```

### 4.2 Authorization Policies

**Implementation Location:** `src/AcademicAssessment.Web/Program.cs`

```csharp
builder.Services.AddAuthorization(options =>
{
    // Single role policies
    options.AddPolicy("StudentPolicy", policy => policy.RequireRole("Student"));
    options.AddPolicy("TeacherPolicy", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("SchoolAdminPolicy", policy => policy.RequireRole("SchoolAdmin"));
    options.AddPolicy("CourseAdminPolicy", policy => policy.RequireRole("CourseAdmin"));
    options.AddPolicy("BusinessAdminPolicy", policy => policy.RequireRole("BusinessAdmin"));
    options.AddPolicy("SystemAdminPolicy", policy => policy.RequireRole("SystemAdmin"));
    
    // Combined policies (OR logic)
    options.AddPolicy("AdminPolicy", policy => 
        policy.RequireRole("SchoolAdmin", "BusinessAdmin", "SystemAdmin"));
    
    options.AddPolicy("EducatorPolicy", policy => 
        policy.RequireRole("Teacher", "SchoolAdmin", "CourseAdmin"));
    
    options.AddPolicy("AllUsersPolicy", policy => 
        policy.RequireAuthenticatedUser());
});
```

### 4.3 Permission Matrix

Comprehensive permission table showing what each role can do:

| Capability | Student | Teacher | SchoolAdmin | CourseAdmin | BusinessAdmin | SystemAdmin |
|-----------|---------|---------|-------------|-------------|---------------|-------------|
| **Assessments** |
| Take assessments | ✅ Own | ❌ | ❌ | ❌ | ❌ | ❌ |
| View own results | ✅ | ✅ | ✅ | 📊 Aggregated | 📊 Aggregated | 📊 Aggregated |
| Grade responses | ❌ | ✅ Own classes | ✅ Own school | ❌ | ❌ | ⚙️ Admin only |
| **Analytics** |
| Personal progress | ✅ Own | ✅ | ✅ | ❌ | ❌ | ❌ |
| Class analytics | ❌ | ✅ Own classes | ✅ All classes | 📊 Cross-school | 📊 System-wide | 📊 System-wide |
| School analytics | ❌ | ❌ | ✅ Own school | 📊 Cross-school | ✅ All schools | ✅ All schools |
| **Content Management** |
| View questions | 🔒 During test | ✅ Own courses | ✅ Own school | ✅ All | ✅ All | ✅ All |
| Create questions | ❌ | 🔒 Limited | ❌ | ✅ | ❌ | ⚙️ Config |
| Edit curriculum | ❌ | ❌ | ❌ | ✅ | ❌ | ⚙️ Config |
| **User Management** |
| Manage students | ❌ | 🔒 Own classes | ✅ Own school | ❌ | ✅ All schools | ✅ All users |
| Manage teachers | ❌ | ❌ | ✅ Own school | ❌ | ✅ All schools | ✅ All users |
| Manage schools | ❌ | ❌ | 🔒 Own school | ❌ | ✅ | ⚙️ Config |
| **System Configuration** |
| View billing | ❌ | ❌ | 📊 Own school | ❌ | ✅ All schools | 📊 System-wide |
| Manage subscriptions | ❌ | ❌ | ❌ | ❌ | ✅ | ⚙️ Config |
| Configure LLM | ❌ | ❌ | ❌ | 🔒 Prompts | ❌ | ✅ |
| System settings | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

**Legend:**

- ✅ Full Access
- ❌ No Access
- 🔒 Limited/Scoped Access
- 📊 Read-Only or Aggregated Data
- ⚙️ Configuration-Level Access

### 4.4 Attribute-Based Authorization

In addition to role-based policies, authorization also considers:

#### Tenant Context

**Rule:** Users can only access data within their tenant scope.

```csharp
public bool HasAccessToSchool(Guid schoolId)
{
    return Role switch
    {
        UserRole.SystemAdmin => true,
        UserRole.BusinessAdmin => true,
        UserRole.SchoolAdmin => SchoolId == schoolId,
        UserRole.Teacher => SchoolId == schoolId,
        UserRole.Student => SchoolId == schoolId,
        UserRole.CourseAdmin => true, // Cross-school for curriculum
        _ => false
    };
}
```

#### Resource Ownership

**Rule:** Students can only access their own data.

```csharp
public bool CanAccessStudent(Guid studentId)
{
    return Role switch
    {
        UserRole.SystemAdmin => true,
        UserRole.BusinessAdmin => true,
        UserRole.Student => UserId == studentId, // Own data only
        UserRole.Teacher => true, // Filtered by SchoolId/ClassId in query
        UserRole.SchoolAdmin => true, // Filtered by SchoolId in query
        UserRole.CourseAdmin => true, // Anonymized only
        _ => false
    };
}
```

### 4.5 Controller Authorization

**Example:** Student Analytics Controller

```csharp
[ApiController]
[Route("api/v1/students")]
[Authorize(Policy = "AllUsersPolicy")] // All endpoints require authentication
public class StudentAnalyticsController : ControllerBase
{
    [HttpGet("{studentId:guid}/analytics/performance-summary")]
    public async Task<IActionResult> GetPerformanceSummary(Guid studentId)
    {
        // Additional authorization check at method level
        if (!_tenantContext.CanAccessStudent(studentId))
        {
            return Forbid(); // 403 Forbidden
        }
        
        // Proceed with data retrieval (automatically filtered by tenant)
        var result = await _analyticsService.GetPerformanceSummaryAsync(studentId);
        return Ok(result);
    }
}
```

**Pattern:** Controller-level `[Authorize]` + method-level tenant checks.

---

## 5. Multi-Tenant Isolation

### 5.1 Isolation Strategy

**Current Implementation:** Logical isolation with row-level security  
**Planned Implementation:** Physical database partitioning per school

#### Logical Isolation (Current)

**Mechanism:** All entities have `SchoolId` and/or `ClassId` properties. EF Core query filters automatically apply tenant context.

**Implementation Location:** `src/AcademicAssessment.Infrastructure/Data/AcademicContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Global query filter for multi-tenancy
    modelBuilder.Entity<Student>().HasQueryFilter(s => 
        s.SchoolId == _tenantContext.SchoolId);
    
    modelBuilder.Entity<StudentAssessment>().HasQueryFilter(sa => 
        sa.SchoolId == _tenantContext.SchoolId);
    
    // Apply to all tenant-scoped entities
}
```

**Benefits:**

- ✅ Simple implementation
- ✅ Shared schema migrations
- ✅ Cross-tenant queries possible (for admins)

**Limitations:**

- ⚠️ Requires careful query filter maintenance
- ⚠️ Risk of filter bypass bugs
- ⚠️ No hard guarantee of isolation

#### Physical Isolation (Planned)

**Mechanism:** Each school has a dedicated PostgreSQL database.

**Database Naming:** `edumind_school_{schoolCode}_{schoolId}`

**Example:** `edumind_school_LINCOLN_00000000-0000-0000-0000-000000000010`

**Architecture:**

```
┌─────────────────────────────────────────────────────┐
│           PostgreSQL Cluster (Primary)              │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────────┐  ┌─────────────────┐         │
│  │ edumind_school_ │  │ edumind_school_ │         │
│  │ LINCOLN_001     │  │ WASHINGTON_002  │  ...    │
│  │                 │  │                 │         │
│  │ • Students      │  │ • Students      │         │
│  │ • Assessments   │  │ • Assessments   │         │
│  │ • Responses     │  │ • Responses     │         │
│  │                 │  │                 │         │
│  │ ISOLATED ✓      │  │ ISOLATED ✓      │         │
│  └─────────────────┘  └─────────────────┘         │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │           Shared System Database             │  │
│  │  • School metadata (name, contact)           │  │
│  │  • User authentication (emails, roles)       │  │
│  │  • System configuration                      │  │
│  │  • NO STUDENT DATA                           │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │           Shared Content Database            │  │
│  │  • Course curriculum                         │  │
│  │  • Question banks                            │  │
│  │  • Learning objectives                       │  │
│  │  • NO STUDENT DATA                           │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

**Benefits:**

- ✅ **Absolute isolation** - Impossible to query wrong school
- ✅ **Regulatory compliance** - Clear data custody
- ✅ **Performance isolation** - Heavy usage doesn't impact others
- ✅ **Independent scaling** - Scale per school
- ✅ **Disaster recovery** - Per-school backup/restore

**Implementation Plan:** See [PRIVACY_AND_SECURITY.md](../../docs/architecture/PRIVACY_AND_SECURITY.md)

### 5.2 Tenant Context Flow

```
┌─────────────────────────────────────────────────────────┐
│                 Request Arrives                         │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            1. Authentication Middleware                 │
│               - Validates JWT token                     │
│               - Extracts claims                         │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            2. TenantContext Middleware                  │
│               - Creates ITenantContext                  │
│                 {                                       │
│                   UserId: <guid>,                       │
│                   Role: "Teacher",                      │
│                   SchoolId: <guid>,                     │
│                   ClassIds: [<guid>, <guid>]            │
│                 }                                       │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            3. Controller Action                         │
│               - [Authorize] attribute checks role       │
│               - Method-level tenant check               │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            4. Service Layer                             │
│               - Business logic                          │
│               - Calls repository                        │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            5. Repository/EF Core                        │
│               - Query filter applied automatically:     │
│                 WHERE SchoolId = @currentSchoolId       │
│                 AND ClassId IN @currentClassIds         │
│               - Execute query                           │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│            6. Filtered Results Returned                 │
│               - Only data user is authorized to see     │
└─────────────────────────────────────────────────────────┘
```

### 5.3 Cross-Tenant Access Control

**Scenario:** CourseAdmin needs aggregated performance across schools.

**Solution:** Anonymized queries that don't expose individual schools.

```csharp
public async Task<CoursePerformanceReport> GetCoursePerformanceAsync(
    string courseId, 
    CancellationToken ct)
{
    // CourseAdmin can query across all schools but data is anonymized
    var assessments = await _context.StudentAssessments
        .IgnoreQueryFilters() // Bypass tenant filter (requires CourseAdmin role)
        .Where(sa => sa.CourseId == courseId)
        .ToListAsync(ct);
    
    // Aggregate data (no school identifiers)
    return new CoursePerformanceReport
    {
        CourseId = courseId,
        TotalStudentsAcrossAllSchools = assessments.Select(a => a.StudentId).Distinct().Count(),
        AverageScoreGlobally = assessments.Average(a => a.Score),
        // No individual school breakdown
    };
}
```

**Rule:** `.IgnoreQueryFilters()` requires elevated role and triggers audit log.

---

## 6. Privacy Compliance

### 6.1 Privacy-Preserving Analytics

**Problem:** Aggregate reports could identify individual students in small classes.

**Solution:** k-anonymity rule requiring minimum group size.

#### Minimum Aggregation Threshold

**Configuration:** `PrivacyConfig.MinimumAggregationThreshold = 5`

**Rule:** Reports are suppressed if fewer than 5 students in the group.

**Implementation Location:** `src/AcademicAssessment.Analytics/Extensions/PrivacyPreservingAggregation.cs` (documented, not yet implemented)

```csharp
public static class PrivacyConfig
{
    /// <summary>
    /// Minimum number of students required for aggregate reporting.
    /// Below this threshold, data is suppressed to prevent identification.
    /// </summary>
    public const int MinimumAggregationThreshold = 5;
    
    /// <summary>
    /// For very small classes, require higher threshold.
    /// </summary>
    public const int SmallClassThreshold = 10;
}
```

#### Privacy-Preserving Average

```csharp
public static double? PrivacyPreservingAverage(
    this IEnumerable<double> values,
    int minimumThreshold = PrivacyConfig.MinimumAggregationThreshold)
{
    var valuesList = values.ToList();
    
    return valuesList.Count >= minimumThreshold
        ? valuesList.Average()
        : null; // Suppress if below threshold
}
```

**Usage:**

```csharp
var classAverageScore = assessments
    .Select(a => a.Score ?? 0.0)
    .PrivacyPreservingAverage(); // Returns null if < 5 students
```

#### Suppressed Report Example

```json
{
  "classId": "00000000-0000-0000-0000-000000000100",
  "className": "AP Calculus - Section A",
  "subject": "Mathematics",
  "isDataSuppressed": true,
  "suppressionReason": "Insufficient students (3) for privacy-preserving aggregation. Minimum required: 5",
  "averageScore": null,
  "studentCount": null,
  "distribution": null
}
```

### 6.2 Anonymization for Course Administrators

**Requirement:** CourseAdmins analyze content effectiveness without accessing student PII.

**Implementation:**

1. **Remove PII** - Student names, emails, schools not included
2. **Assign anonymous IDs** - Replace real IDs with hashed identifiers
3. **Aggregate data** - Show distributions, not individual responses

**Example Query:**

```csharp
public async Task<QuestionPerformanceReport> GetQuestionPerformanceAsync(
    Guid questionId,
    CancellationToken ct)
{
    var responses = await _context.StudentResponses
        .IgnoreQueryFilters() // CourseAdmin can see cross-school
        .Where(r => r.QuestionId == questionId)
        .ToListAsync(ct);
    
    return new QuestionPerformanceReport
    {
        QuestionId = questionId,
        TotalResponses = responses.Count,
        CorrectResponses = responses.Count(r => r.IsCorrect),
        AverageTimeSeconds = responses.Average(r => r.TimeSpentSeconds),
        // No student names, no school names
        DifficultyDistribution = responses
            .GroupBy(r => r.DifficultyLevel)
            .ToDictionary(g => g.Key, g => g.Count())
    };
}
```

### 6.3 Right to Be Forgotten

**Regulation:** GDPR Article 17 - Right to Erasure

**Implementation Location:** `src/AcademicAssessment.Infrastructure/Services/StudentDataDeletionService.cs` (documented, not yet implemented)

```csharp
public interface IStudentDataDeletionService
{
    /// <summary>
    /// Permanently delete all data for a student.
    /// This is irreversible and used for "right to be forgotten".
    /// </summary>
    Task<Result<Unit>> DeleteStudentDataAsync(
        Guid studentId,
        string requestReason,
        CancellationToken ct = default);
    
    /// <summary>
    /// Export all data for a student (GDPR data portability).
    /// </summary>
    Task<Result<StudentDataExport>> ExportStudentDataAsync(
        Guid studentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Anonymize student data (keep for research/analytics).
    /// </summary>
    Task<Result<Unit>> AnonymizeStudentDataAsync(
        Guid studentId,
        CancellationToken ct = default);
}
```

**Deletion Workflow:**

1. **Verify authorization** - Only SchoolAdmin or parent can request
2. **Cascade delete:**
   - Student responses
   - Student assessments
   - Student progress records
   - Student profile
3. **Audit log** - Log deletion request and completion
4. **Notify stakeholders** - Email confirmation to requester

**Note:** Physical database partitioning ensures deletion only affects the student's school database.

### 6.4 COPPA Age Verification

**Requirement:** Students under 13 require parental consent.

**Implementation Plan:**

1. **Collect age** during account creation
2. **Flag accounts** where age < 13
3. **Generate consent form** with unique link
4. **Email parent** at verified email address
5. **Track consent** in database with timestamp
6. **Block access** until consent granted

**Database Fields:**

```sql
ALTER TABLE "Students"
ADD COLUMN "date_of_birth" DATE,
ADD COLUMN "requires_parental_consent" BOOLEAN DEFAULT FALSE,
ADD COLUMN "parental_consent_given" BOOLEAN DEFAULT FALSE,
ADD COLUMN "parental_consent_timestamp" TIMESTAMPTZ,
ADD COLUMN "parental_email" VARCHAR(255);
```

**Consent Link:** `https://edumind.ai/parental-consent/{token}` (time-limited, one-time use)

---

## 7. Data Protection

### 7.1 Data Encryption

#### In Transit

**Protocol:** TLS 1.2+ (HTTPS)  
**Certificate:** Azure-managed SSL certificate  
**Enforcement:** `RequireHttpsMetadata = true` in production

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = 443;
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    });
}
```

#### At Rest

**Database:** Azure PostgreSQL Flexible Server with Transparent Data Encryption (TDE)  
**Secrets:** Azure Key Vault for connection strings and API keys  
**Backups:** Encrypted backups with customer-managed keys (optional)

### 7.2 Sensitive Data Handling

#### Personally Identifiable Information (PII)

**Fields Classified as PII:**

- Student name (first, last)
- Student email
- Student date of birth
- Parent email
- User authentication credentials

**Protection Measures:**

- ✅ Excluded from logs (custom log formatter)
- ✅ Encrypted in database (TDE)
- ✅ Not cached in Redis
- ✅ Excluded from error messages
- ✅ Masked in admin interfaces (unless authorized)

**Implementation:** Serilog destructuring policies

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<Student>(s => new
    {
        s.Id,
        s.SchoolId,
        s.GradeLevel,
        // Email and name intentionally excluded
    })
    .WriteTo.Console()
    .CreateLogger();
```

### 7.3 Secrets Management

**Azure Key Vault Integration:**

```json
{
  "KeyVault": {
    "VaultUri": "https://edumind-kv.vault.azure.net/"
  }
}
```

**Secrets Stored:**

- Database connection strings (per school)
- Azure AD B2C client secrets
- OLLAMA API keys (if applicable)
- Redis connection string
- Azure Blob Storage SAS tokens

**Retrieval:** Secrets loaded at application startup via Azure SDK.

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUri),
    new DefaultAzureCredential());
```

---

## 8. Audit Logging

### 8.1 Audit Log Structure

**Current Implementation:** Structured logging with Serilog  
**Planned Enhancement:** Dedicated audit log table

**Audit Log Entry:**

```csharp
public record DataAccessAuditLog
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required Guid UserId { get; init; }
    public required string UserEmail { get; init; }
    public required UserRole UserRole { get; init; }
    public required Guid? SchoolId { get; init; }
    public required string Action { get; init; }  // "VIEW", "EXPORT", "MODIFY", "DELETE"
    public required string Resource { get; init; } // "StudentAssessment", "StudentProfile"
    public required Guid? ResourceId { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public bool WasAuthorized { get; init; }
    public string? DenialReason { get; init; }
    public Dictionary<string, string> AdditionalMetadata { get; init; } = new();
}
```

### 8.2 Logged Events

| Event Type | Action Code | Example |
|------------|-------------|---------|
| Student data viewed | `VIEW_STUDENT` | Teacher views student profile |
| Assessment graded | `GRADE_ASSESSMENT` | Teacher assigns score |
| Report exported | `EXPORT_REPORT` | Admin downloads class performance report |
| Student data deleted | `DELETE_STUDENT_DATA` | Parent requests data deletion |
| Unauthorized access attempt | `ACCESS_DENIED` | User tries to view other school's data |
| Authentication failure | `AUTH_FAILED` | Invalid JWT token |
| Configuration changed | `CONFIG_CHANGE` | SystemAdmin modifies LLM settings |

### 8.3 Audit Log Middleware

**Implementation Location:** `src/AcademicAssessment.Infrastructure/Middleware/DataAccessAuditMiddleware.cs` (documented, not yet implemented)

```csharp
public class DataAccessAuditMiddleware
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogRepository auditRepo,
        ITenantContext tenantContext)
    {
        var startTime = DateTimeOffset.UtcNow;
        var path = context.Request.Path.Value;
        
        // Only audit data access endpoints
        if (path?.Contains("/api/student/") == true ||
            path?.Contains("/api/teacher/") == true)
        {
            await _next(context);
            
            // Log after request completes
            var auditLog = new DataAccessAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = startTime,
                UserId = tenantContext.UserId,
                UserEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown",
                UserRole = tenantContext.Role,
                SchoolId = tenantContext.SchoolId,
                Action = context.Request.Method,
                Resource = ExtractResourceType(path),
                ResourceId = ExtractResourceId(path),
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                WasAuthorized = context.Response.StatusCode < 400
            };
            
            await auditRepo.AddAsync(auditLog);
        }
        else
        {
            await _next(context);
        }
    }
}
```

### 8.4 Audit Log Retention

**Retention Policy:**

- **Active logs:** 90 days in primary database
- **Archive:** 7 years in cold storage (FERPA requirement)
- **Compliance exports:** Available on demand

**Implementation:** Azure Blob Storage archival with lifecycle management.

---

## 9. Security Layers

**Defense-in-Depth Strategy:** Multiple independent security layers ensure that a single failure doesn't compromise the system.

### Layer 1: Network Security

- ✅ **Azure Virtual Network** - Private network for resources
- ✅ **Network Security Groups (NSG)** - Firewall rules
- ✅ **Private Endpoints** - Database not exposed to internet
- ⚠️ **DDoS Protection** - Basic (Standard tier planned)

### Layer 2: Authentication

- ✅ **Azure AD B2C** - Enterprise-grade identity provider
- ✅ **JWT Bearer Tokens** - Signed, time-limited tokens
- ⚠️ **Multi-Factor Authentication** - Planned for admin roles
- 📋 **Certificate-Based Auth** - Planned for district SSO

### Layer 3: Authorization

- ✅ **Role-Based Access Control (RBAC)** - 6 hierarchical roles
- ✅ **Tenant Context Enforcement** - Query filters
- ✅ **Resource Ownership Checks** - Method-level authorization
- ⚠️ **Attribute-Based Access Control (ABAC)** - Planned for fine-grained permissions

### Layer 4: Data Isolation

- ✅ **Row-Level Security** - EF Core query filters
- ⚠️ **Physical Database Partitioning** - Planned per school
- ✅ **Tenant Context Middleware** - Automatic filtering
- ✅ **Connection String Isolation** - Separate credentials per tenant (planned)

### Layer 5: Data Protection

- ✅ **TLS 1.2+ Encryption** - In-transit encryption
- ✅ **Transparent Data Encryption (TDE)** - At-rest encryption
- ✅ **Azure Key Vault** - Secrets management
- ⚠️ **Field-Level Encryption** - Planned for extra-sensitive fields

### Layer 6: Monitoring and Auditing

- ✅ **Structured Logging** - Serilog with contextual data
- ⚠️ **Audit Log Table** - Dedicated audit trail (planned)
- ✅ **Azure Application Insights** - Telemetry and anomaly detection
- 📋 **Security Information and Event Management (SIEM)** - Planned integration

---

## 10. Threat Mitigation

### 10.1 Common Threats and Mitigations

#### SQL Injection

**Risk:** Attacker injects malicious SQL to access unauthorized data.

**Mitigation:**

- ✅ **Parameterized Queries** - EF Core uses parameterized queries exclusively
- ✅ **ORM Abstraction** - No raw SQL in application code
- ✅ **Input Validation** - All user input validated

**Example:**

```csharp
// SAFE: Parameterized query via EF Core
var student = await _context.Students
    .FirstOrDefaultAsync(s => s.Id == studentId);

// UNSAFE: Raw SQL (not used in codebase)
// var student = await _context.Students
//     .FromSqlRaw($"SELECT * FROM Students WHERE Id = '{studentId}'");
```

#### Cross-Site Scripting (XSS)

**Risk:** Attacker injects malicious scripts into web pages.

**Mitigation:**

- ✅ **Blazor Auto-Escaping** - Razor syntax escapes output by default
- ✅ **Content Security Policy (CSP)** - HTTP headers restrict script sources
- ✅ **Input Sanitization** - User input sanitized before storage

#### Cross-Site Request Forgery (CSRF)

**Risk:** Attacker tricks user into executing unwanted actions.

**Mitigation:**

- ✅ **Anti-Forgery Tokens** - ASP.NET Core generates tokens for forms
- ✅ **SameSite Cookies** - Prevents cross-site cookie usage
- ✅ **JWT Bearer Tokens** - State less authentication

#### Unauthorized Data Access

**Risk:** User accesses data outside their tenant scope.

**Mitigation:**

- ✅ **Tenant Context Enforcement** - Automatic query filters
- ✅ **Authorization Checks** - Method-level tenant validation
- ✅ **Audit Logging** - All access attempts logged
- ⚠️ **Physical Database Partitioning** - Planned for absolute isolation

**Example:**

```csharp
// Automatic tenant filter applied by EF Core
var students = await _context.Students.ToListAsync(); // Only current school's students
```

#### Token Theft/Replay

**Risk:** Attacker steals JWT token and impersonates user.

**Mitigation:**

- ✅ **Short Token Lifetime** - 1 hour expiration
- ✅ **HTTPS Only** - Tokens never transmitted over HTTP
- ⚠️ **Token Rotation** - Planned refresh token mechanism
- 📋 **Token Revocation** - Planned blacklist for compromised tokens

#### Denial of Service (DoS)

**Risk:** Attacker overwhelms system with requests.

**Mitigation:**

- ✅ **Rate Limiting** - Per-user request limits
- ⚠️ **Azure DDoS Protection** - Basic tier (Standard planned)
- ✅ **Auto-Scaling** - Azure Container Apps scale on demand
- ✅ **Circuit Breaker** - Polly resilience policies

#### Insider Threats

**Risk:** Authorized user misuses access to student data.

**Mitigation:**

- ✅ **Audit Logging** - All data access logged
- ✅ **Least Privilege** - Roles grant minimum necessary access
- ✅ **Separation of Duties** - BusinessAdmin can't access student data
- 📋 **Anomaly Detection** - Planned ML-based access pattern analysis

### 10.2 Security Testing

#### Automated Security Testing

- ✅ **Dependency Scanning** - GitHub Dependabot checks for vulnerable packages
- ⚠️ **Static Application Security Testing (SAST)** - Planned SonarCloud integration
- 📋 **Dynamic Application Security Testing (DAST)** - Planned OWASP ZAP scanning

#### Penetration Testing

- 📋 **Internal Penetration Test** - Planned before production launch
- 📋 **Third-Party Security Audit** - Planned annual audit

#### Compliance Audits

- 📋 **FERPA Compliance Audit** - Planned with education compliance firm
- 📋 **SOC 2 Type II** - Planned for enterprise customers

---

## 11. Implementation Details

### 11.1 Key Files

| File Path | Purpose |
|-----------|---------|
| `src/AcademicAssessment.Infrastructure/Context/TenantContextJwt.cs` | JWT claim extraction and tenant context |
| `src/AcademicAssessment.Infrastructure/Middleware/TenantContextMiddleware.cs` | Populates tenant context from claims |
| `src/AcademicAssessment.Infrastructure/Data/AcademicContext.cs` | EF Core query filters for multi-tenancy |
| `src/AcademicAssessment.Web/Program.cs` | Authentication and authorization configuration |
| `tests/AcademicAssessment.Tests.Integration/Helpers/JwtTokenGenerator.cs` | Test JWT token generation |
| `tests/AcademicAssessment.Tests.Integration/Controllers/StudentAnalyticsControllerAuthTests.cs` | Authentication/authorization tests |

### 11.2 Configuration Example

**appsettings.json:**

```json
{
  "AzureAdB2C": {
    "Instance": "https://edumindai.b2clogin.com",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "SignUpSignInPolicyId": "B2C_1_signup_signin",
    "CallbackPath": "/signin-oidc"
  },
  "Authentication": {
    "Enabled": true,
    "RequireHttpsMetadata": true,
    "TokenLifetimeMinutes": 60
  },
  "Security": {
    "MinimumAggregationThreshold": 5,
    "EnableAuditLogging": true,
    "RequireParentalConsent": true,
    "ParentalConsentAgeThreshold": 13
  }
}
```

### 11.3 Testing Strategy

#### Unit Tests

- ✅ JWT token parsing and claim extraction
- ✅ Tenant context initialization
- ✅ Authorization policy evaluation
- ✅ Privacy-preserving aggregation functions

#### Integration Tests

- ✅ Authentication flow (JWT validation)
- ✅ Multi-tenant data isolation (query filters)
- ✅ Cross-tenant access control
- ✅ Role-based endpoint access
- ⚠️ Audit logging end-to-end (planned)

**Test Location:** `tests/AcademicAssessment.Tests.Integration/Controllers/StudentAnalyticsControllerAuthTests.cs`

#### Security Tests

- ⚠️ SQL injection attempts (planned)
- ⚠️ XSS vulnerability scanning (planned)
- ⚠️ CSRF token validation (planned)
- ⚠️ Unauthorized access attempts (partial coverage)

### 11.4 Deployment Checklist

Before production deployment:

- [ ] **Azure AD B2C configured** with production tenant
- [ ] **Google OAuth integrated** with verified domain
- [ ] **SSL certificate** installed and HTTPS enforced
- [ ] **Connection strings** stored in Azure Key Vault
- [ ] **Audit logging** enabled and tested
- [ ] **Privacy aggregation thresholds** configured
- [ ] **COPPA age verification** implemented
- [ ] **Data deletion workflow** tested
- [ ] **Penetration test** completed
- [ ] **Security audit** passed
- [ ] **Incident response plan** documented
- [ ] **Compliance documentation** prepared

---

## Summary

EduMind.AI implements a comprehensive, defense-in-depth security architecture prioritizing student data protection:

### Core Security Features

1. ✅ **Azure AD B2C Authentication** - Enterprise-grade identity management with Google OAuth
2. ✅ **JWT Bearer Token Authorization** - Secure, stateless authentication
3. ✅ **6-Tier Role-Based Access Control** - Hierarchical permissions model
4. ✅ **Multi-Tenant Row-Level Security** - Automatic query filtering
5. ✅ **TLS Encryption** - All data encrypted in transit
6. ✅ **Azure Key Vault** - Secure secrets management

### Privacy Protections

1. ⚠️ **k-Anonymity Aggregation** - Minimum 5 students for reports (documented)
2. ✅ **Anonymized Cross-School Analytics** - CourseAdmins see no PII
3. ⚠️ **Audit Logging** - All data access tracked (structured logging implemented)
4. 📋 **Right to Be Forgotten** - GDPR-compliant data deletion (designed)
5. 📋 **COPPA Age Verification** - Parental consent for under-13 (designed)

### Planned Enhancements

1. Physical database partitioning per school (absolute isolation)
2. Dedicated audit log table with retention policies
3. Multi-factor authentication for administrative roles
4. Full k-anonymity implementation with suppression logic
5. Data deletion and export services (GDPR compliance)
6. COPPA parental consent workflow
7. Penetration testing and security audit

**Current Maturity:** Production-ready for pilot deployment with comprehensive authentication and authorization. Privacy features partially implemented with clear roadmap for full compliance.

---

**Related Documentation:**

- [RBAC Architecture](../../docs/architecture/RBAC_ARCHITECTURE.md)
- [Privacy and Security Architecture](../../docs/architecture/PRIVACY_AND_SECURITY.md)
- [Authentication Setup Guide](../../docs/deployment/AUTHENTICATION_SETUP.md)
- [JWT Authentication Testing](../../docs/development/testing/JWT_AUTHENTICATION_TESTING.md)

*Last Updated: January 2025*
