# Story 008: Implement Dedicated Audit Logging System

**Priority:** P1 - Production Quality  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/12

---

## Problem Statement

EduMind.AI currently uses **structured logging** (Serilog) for application logs, but lacks a dedicated **audit trail** required for compliance (SOC 2, FERPA, GDPR, COPPA).

**Current State:**

- Application logs mixed with audit events
- Logs can be modified/deleted by admins
- No tamper-proof audit trail
- No compliance-ready reporting
- Log retention not configured for regulatory requirements

**Compliance Requirements:**

| Regulation | Audit Requirement | Retention |
|------------|-------------------|-----------|
| **FERPA** | Log all access to student education records | 7 years |
| **GDPR** | Log all data subject requests (access, delete) | 7 years |
| **COPPA** | Log parental consent events | Until child turns 13 + 7 years |
| **SOC 2** | Log authentication, authorization, data changes | 7 years |

**Business Impact:**

- Cannot pass compliance audits (SOC 2, FERPA, GDPR)
- No forensic capability for security incidents
- Cannot demonstrate compliance to enterprise customers
- Risk of regulatory fines for non-compliance

---

## Goals & Success Criteria

### Functional Goals

1. **Separate audit database**
   - Dedicated PostgreSQL database for audit logs
   - Write-only access (no updates/deletes)
   - Separate from application database

2. **Comprehensive audit logging**
   - Authentication events (login, logout, failed attempts)
   - Authorization events (permission checks, role changes)
   - Data access (who accessed which student records)
   - Data modifications (create, update, delete)
   - Privacy events (COPPA consent, GDPR deletion)

3. **Compliance reporting**
   - Pre-built reports for FERPA, GDPR, COPPA audits
   - Export to CSV/PDF for auditors
   - Time-range filtering
   - User/tenant filtering

### Non-Functional Goals

- **Tamper-proof:** Append-only, no modifications allowed
- **High performance:** <10ms overhead per audit log
- **Retention:** 7-year automatic retention policy
- **Searchability:** Fast queries on user, tenant, action, timestamp

### Success Criteria

- [ ] Separate audit database operational
- [ ] All required events logged automatically
- [ ] Audit logs immutable (write-only)
- [ ] Compliance reports generate correctly
- [ ] 7-year retention policy enforced
- [ ] Integration tests validate audit logging
- [ ] Performance overhead <10ms per event

---

## Technical Approach

### Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│                Application Layer                          │
│  - Controllers, Services, Repositories                    │
└─────────────────────┬────────────────────────────────────┘
                      │
                      ├─────────────────┬─────────────────┐
                      │                 │                 │
                      ▼                 ▼                 ▼
         ┌────────────────────┐  ┌──────────────┐  ┌──────────────┐
         │ Application DB     │  │  Audit DB    │  │  Redis Cache │
         │ (Read/Write)       │  │ (Write-Only) │  │              │
         └────────────────────┘  └──────────────┘  └──────────────┘
```

### Audit Database Schema

```sql
CREATE DATABASE edumind_audit;

CREATE TABLE audit_log (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Who did it?
    user_id UUID,
    user_email VARCHAR(255),
    user_role VARCHAR(50),
    
    -- Which tenant?
    tenant_id UUID NOT NULL,
    tenant_name VARCHAR(255),
    
    -- What happened?
    event_type VARCHAR(50) NOT NULL, -- 'Authentication', 'DataAccess', 'DataModification', 'Privacy'
    event_action VARCHAR(100) NOT NULL, -- 'Login', 'ViewStudentRecord', 'DeleteAssessment', 'COPPAConsent'
    
    -- Context
    resource_type VARCHAR(100), -- 'Student', 'Assessment', 'Question'
    resource_id UUID,
    ip_address INET,
    user_agent TEXT,
    
    -- Details
    details JSONB, -- Flexible field for event-specific data
    
    -- Result
    success BOOLEAN NOT NULL,
    error_message TEXT
);

-- Indexes for fast queries
CREATE INDEX idx_audit_timestamp ON audit_log(timestamp DESC);
CREATE INDEX idx_audit_user ON audit_log(user_id);
CREATE INDEX idx_audit_tenant ON audit_log(tenant_id);
CREATE INDEX idx_audit_event_type ON audit_log(event_type, event_action);
CREATE INDEX idx_audit_resource ON audit_log(resource_type, resource_id);
CREATE INDEX idx_audit_details ON audit_log USING GIN (details); -- For JSONB queries

-- Prevent updates and deletes (write-only)
CREATE OR REPLACE FUNCTION prevent_audit_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Audit logs are immutable';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER prevent_audit_update
    BEFORE UPDATE ON audit_log
    FOR EACH ROW
    EXECUTE FUNCTION prevent_audit_modification();

CREATE TRIGGER prevent_audit_delete
    BEFORE DELETE ON audit_log
    FOR EACH ROW
    EXECUTE FUNCTION prevent_audit_modification();
```

### Event Types to Log

**1. Authentication Events:**

- User login (success/failure)
- User logout
- Password change
- MFA enrollment/verification
- OAuth token refresh

**2. Authorization Events:**

- Permission check (allow/deny)
- Role assignment/removal
- Access denied (403 errors)

**3. Data Access Events:**

- View student profile
- View assessment results
- View analytics report
- Export data (CSV, PDF)

**4. Data Modification Events:**

- Create/update/delete student
- Create/update/delete assessment
- Submit assessment response
- Modify user profile

**5. Privacy Events:**

- COPPA parental consent granted/revoked
- GDPR data deletion request
- GDPR data export request
- Privacy settings changed

---

## Task Decomposition

### Task 1: Create Audit Database and Schema

- **Description:** Set up dedicated PostgreSQL database for audit logs
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Data/AuditDbContext.cs`
  - `database/migrations/audit_schema.sql`
- **Steps:**
  1. Create `edumind_audit` database
  2. Run schema migration (table + triggers)
  3. Configure separate connection string
  4. Test write-only constraint
- **Acceptance:** Audit database exists, triggers prevent modification
- **Dependencies:** None

### Task 2: Create Audit Log Domain Model

- **Description:** Define audit log entity and value objects
- **Files to Create:**
  - `src/AcademicAssessment.Core/Entities/AuditLog.cs`
  - `src/AcademicAssessment.Core/ValueObjects/AuditEvent.cs`
- **Implementation:**

  ```csharp
  public sealed record AuditLog
  {
      public required long Id { get; init; }
      public required Guid EventId { get; init; }
      public required DateTime Timestamp { get; init; }
      
      public Guid? UserId { get; init; }
      public string? UserEmail { get; init; }
      public string? UserRole { get; init; }
      
      public required Guid TenantId { get; init; }
      public string? TenantName { get; init; }
      
      public required string EventType { get; init; }
      public required string EventAction { get; init; }
      
      public string? ResourceType { get; init; }
      public Guid? ResourceId { get; init; }
      public string? IpAddress { get; init; }
      public string? UserAgent { get; init; }
      
      public Dictionary<string, object>? Details { get; init; }
      
      public required bool Success { get; init; }
      public string? ErrorMessage { get; init; }
  }
  
  public enum AuditEventType
  {
      Authentication,
      Authorization,
      DataAccess,
      DataModification,
      Privacy
  }
  ```

- **Acceptance:** Domain models defined with XML documentation
- **Dependencies:** None

### Task 3: Create Audit Logging Service

- **Description:** Service to write audit logs to dedicated database
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Auditing/IAuditLogService.cs`
  - `src/AcademicAssessment.Infrastructure/Auditing/AuditLogService.cs`
- **Implementation:**

  ```csharp
  public interface IAuditLogService
  {
      Task LogAsync(
          AuditEventType eventType,
          string eventAction,
          bool success,
          string? resourceType = null,
          Guid? resourceId = null,
          Dictionary<string, object>? details = null,
          string? errorMessage = null,
          CancellationToken cancellationToken = default);
  }
  
  public sealed class AuditLogService : IAuditLogService
  {
      private readonly AuditDbContext _auditDb;
      private readonly ITenantContextAccessor _tenantContext;
      private readonly IHttpContextAccessor _httpContext;
      
      public async Task LogAsync(
          AuditEventType eventType,
          string eventAction,
          bool success,
          string? resourceType = null,
          Guid? resourceId = null,
          Dictionary<string, object>? details = null,
          string? errorMessage = null,
          CancellationToken cancellationToken = default)
      {
          var user = _httpContext.HttpContext?.User;
          var auditLog = new AuditLog
          {
              EventId = Guid.NewGuid(),
              Timestamp = DateTime.UtcNow,
              
              UserId = user?.FindFirst("sub")?.Value != null ? Guid.Parse(user.FindFirst("sub")!.Value) : null,
              UserEmail = user?.FindFirst("email")?.Value,
              UserRole = user?.FindFirst("role")?.Value,
              
              TenantId = _tenantContext.TenantId ?? Guid.Empty,
              TenantName = _tenantContext.TenantName,
              
              EventType = eventType.ToString(),
              EventAction = eventAction,
              
              ResourceType = resourceType,
              ResourceId = resourceId,
              IpAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString(),
              UserAgent = _httpContext.HttpContext?.Request.Headers["User-Agent"].ToString(),
              
              Details = details,
              
              Success = success,
              ErrorMessage = errorMessage
          };
          
          _auditDb.AuditLogs.Add(auditLog);
          await _auditDb.SaveChangesAsync(cancellationToken);
      }
  }
  ```

- **Acceptance:** Service writes to audit database correctly
- **Dependencies:** Tasks 1-2

### Task 4: Create Audit Logging Middleware

- **Description:** Middleware to automatically log HTTP requests
- **Files to Create:**
  - `src/AcademicAssessment.Web/Middleware/AuditLoggingMiddleware.cs`
- **Implementation:**

  ```csharp
  public class AuditLoggingMiddleware
  {
      private readonly RequestDelegate _next;
      
      public async Task InvokeAsync(
          HttpContext context,
          IAuditLogService auditLog)
      {
          var stopwatch = Stopwatch.StartNew();
          
          try
          {
              await _next(context);
              stopwatch.Stop();
              
              // Log successful requests (200-299)
              if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
              {
                  await auditLog.LogAsync(
                      AuditEventType.DataAccess,
                      $"{context.Request.Method} {context.Request.Path}",
                      success: true,
                      details: new Dictionary<string, object>
                      {
                          ["StatusCode"] = context.Response.StatusCode,
                          ["DurationMs"] = stopwatch.ElapsedMilliseconds
                      });
              }
          }
          catch (Exception ex)
          {
              stopwatch.Stop();
              
              // Log failed requests
              await auditLog.LogAsync(
                  AuditEventType.DataAccess,
                  $"{context.Request.Method} {context.Request.Path}",
                  success: false,
                  errorMessage: ex.Message,
                  details: new Dictionary<string, object>
                  {
                      ["StatusCode"] = context.Response.StatusCode,
                      ["DurationMs"] = stopwatch.ElapsedMilliseconds
                  });
              
              throw;
          }
      }
  }
  ```

- **Acceptance:** All HTTP requests logged automatically
- **Dependencies:** Task 3

### Task 5: Add Audit Logging to Critical Operations

- **Description:** Manually log critical business operations
- **Files to Modify:**
  - `src/AcademicAssessment.Web/Controllers/AuthenticationController.cs`
  - `src/AcademicAssessment.Web/Controllers/StudentsController.cs`
  - `src/AcademicAssessment.Web/Controllers/AssessmentsController.cs`
  - `src/AcademicAssessment.Infrastructure/Services/PrivacyService.cs`
- **Examples:**

  ```csharp
  // Authentication
  await _auditLog.LogAsync(
      AuditEventType.Authentication,
      "Login",
      success: true,
      details: new Dictionary<string, object>
      {
          ["Provider"] = "Google",
          ["Method"] = "OAuth"
      });
  
  // Data Access
  await _auditLog.LogAsync(
      AuditEventType.DataAccess,
      "ViewStudentProfile",
      success: true,
      resourceType: "Student",
      resourceId: studentId);
  
  // Data Modification
  await _auditLog.LogAsync(
      AuditEventType.DataModification,
      "DeleteAssessment",
      success: true,
      resourceType: "Assessment",
      resourceId: assessmentId);
  
  // Privacy Event
  await _auditLog.LogAsync(
      AuditEventType.Privacy,
      "COPPAConsentGranted",
      success: true,
      resourceType: "Student",
      resourceId: studentId,
      details: new Dictionary<string, object>
      {
          ["ParentEmail"] = parentEmail,
          ["ConsentMethod"] = "Email"
      });
  ```

- **Acceptance:** All critical operations logged
- **Dependencies:** Task 3

### Task 6: Create Compliance Reporting Service

- **Description:** Generate compliance reports from audit logs
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Auditing/IComplianceReportService.cs`
  - `src/AcademicAssessment.Infrastructure/Auditing/ComplianceReportService.cs`
- **Reports:**

  ```csharp
  public interface IComplianceReportService
  {
      // FERPA: Who accessed which student records?
      Task<Result<byte[]>> GenerateFerpaAccessReportAsync(
          Guid studentId,
          DateTime startDate,
          DateTime endDate,
          CancellationToken cancellationToken = default);
      
      // GDPR: All actions related to a user
      Task<Result<byte[]>> GenerateGdprDataSubjectReportAsync(
          Guid userId,
          CancellationToken cancellationToken = default);
      
      // COPPA: All parental consent events
      Task<Result<byte[]>> GenerateCoppaConsentReportAsync(
          Guid tenantId,
          DateTime startDate,
          DateTime endDate,
          CancellationToken cancellationToken = default);
      
      // SOC 2: Authentication and authorization events
      Task<Result<byte[]>> GenerateSoc2SecurityReportAsync(
          Guid tenantId,
          DateTime startDate,
          DateTime endDate,
          CancellationToken cancellationToken = default);
  }
  ```

- **Format:** CSV export for Excel analysis
- **Acceptance:** All report types generate correctly
- **Dependencies:** Task 3

### Task 7: Create Audit Query API Endpoints

- **Description:** Admin API to query audit logs
- **Files to Create:**
  - `src/AcademicAssessment.Web/Controllers/AuditLogsController.cs`
- **Endpoints:**

  ```csharp
  [ApiController]
  [Route("api/admin/audit-logs")]
  [Authorize(Roles = "SystemAdmin,TenantAdmin")]
  public class AuditLogsController : ControllerBase
  {
      [HttpGet]
      public async Task<IActionResult> QueryAuditLogs(
          [FromQuery] DateTime? startDate,
          [FromQuery] DateTime? endDate,
          [FromQuery] string? eventType,
          [FromQuery] Guid? userId,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 50);
      
      [HttpGet("reports/ferpa/{studentId}")]
      public async Task<IActionResult> GetFerpaReport(Guid studentId, DateTime startDate, DateTime endDate);
      
      [HttpGet("reports/gdpr/{userId}")]
      public async Task<IActionResult> GetGdprReport(Guid userId);
      
      [HttpGet("reports/coppa")]
      public async Task<IActionResult> GetCoppaReport(DateTime startDate, DateTime endDate);
      
      [HttpGet("reports/soc2")]
      public async Task<IActionResult> GetSoc2Report(DateTime startDate, DateTime endDate);
  }
  ```

- **Acceptance:** Endpoints return correct data with pagination
- **Dependencies:** Task 6

### Task 8: Implement Retention Policy

- **Description:** Automatic cleanup of audit logs older than 7 years
- **Files to Create:**
  - `database/stored-procedures/cleanup_old_audit_logs.sql`
  - `src/AcademicAssessment.Infrastructure/BackgroundJobs/AuditLogRetentionJob.cs`
- **Stored Procedure:**

  ```sql
  CREATE OR REPLACE PROCEDURE cleanup_old_audit_logs()
  LANGUAGE plpgsql
  AS $$
  BEGIN
      DELETE FROM audit_log
      WHERE timestamp < NOW() - INTERVAL '7 years';
  END;
  $$;
  ```

- **Background Job:**

  ```csharp
  public class AuditLogRetentionJob : BackgroundService
  {
      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
          while (!stoppingToken.IsCancellationRequested)
          {
              // Run once per day at 2am UTC
              await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
              
              // Execute cleanup stored procedure
              await _auditDb.Database.ExecuteSqlRawAsync(
                  "CALL cleanup_old_audit_logs()",
                  stoppingToken);
          }
      }
  }
  ```

- **Acceptance:** Old logs deleted automatically
- **Dependencies:** Task 1

### Task 9: Write Integration Tests

- **Description:** Test audit logging for all critical operations
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.Integration/Auditing/AuditLoggingTests.cs`
- **Test Cases:**

  ```csharp
  [Fact]
  public async Task Login_CreatesAuditLog()
  {
      // Arrange: Login request
      // Act: POST /api/auth/login
      // Assert: Audit log created with correct event type
  }
  
  [Fact]
  public async Task ViewStudentRecord_CreatesAuditLog()
  {
      // Arrange: Student record
      // Act: GET /api/students/{id}
      // Assert: Audit log created with resource details
  }
  
  [Fact]
  public async Task AuditLog_CannotBeModified()
  {
      // Arrange: Existing audit log
      // Act: Try UPDATE or DELETE
      // Assert: Exception thrown
  }
  
  [Fact]
  public async Task FerpaReport_GeneratesCorrectly()
  {
      // Arrange: Student with access history
      // Act: Generate FERPA report
      // Assert: CSV contains all access events
  }
  ```

- **Acceptance:** All tests pass
- **Dependencies:** Tasks 1-7

### Task 10: Update Documentation

- **Description:** Document audit logging system and compliance
- **Files to Create:**
  - `docs/compliance/AUDIT_LOGGING.md`
  - `docs/operations/COMPLIANCE_REPORTING.md`
- **Content:**
  - What events are logged
  - How to generate compliance reports
  - Retention policy
  - Troubleshooting
- **Acceptance:** Documentation complete and reviewed
- **Dependencies:** Task 9

---

## Acceptance Criteria

### Functional Requirements

- [ ] Separate audit database operational
- [ ] All authentication events logged
- [ ] All data access events logged
- [ ] All data modification events logged
- [ ] All privacy events logged
- [ ] Compliance reports generate correctly (FERPA, GDPR, COPPA, SOC 2)
- [ ] Admin API for audit log queries

### Security Requirements

- [ ] Audit logs immutable (triggers prevent modification)
- [ ] Audit database write-only (no updates/deletes)
- [ ] Sensitive data not logged in plaintext (PII redacted)

### Performance Requirements

- [ ] Audit logging overhead <10ms per event
- [ ] Query performance: <1 second for 90-day reports

### Compliance Requirements

- [ ] 7-year retention policy enforced
- [ ] FERPA report includes all student record access
- [ ] GDPR report includes all user-related events
- [ ] COPPA report includes all parental consent events

---

## Context & References

### Documentation

- [System Specification - Security & Privacy](.github/specification/07-security-privacy.md)
- [Privacy Architecture](docs/architecture/PRIVACY_AND_SECURITY.md)

### External References

- [SOC 2 Audit Logging Requirements](https://www.vanta.com/resources/soc-2-audit-logging)
- [FERPA Audit Trail Requirements](https://studentprivacy.ed.gov/resources/ferpa-general-guidance-parents)
- [GDPR Article 30 (Records of Processing)](https://gdpr-info.eu/art-30-gdpr/)

---

## Notes

- **Performance:** Audit logging adds <10ms overhead - acceptable for compliance
- **Storage:** Estimate 1GB per 1M audit logs (7 years ≈ 50GB for large tenant)
- **Cost:** Separate database adds ~$50/month Azure PostgreSQL hosting

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
