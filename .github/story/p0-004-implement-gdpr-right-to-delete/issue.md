# Story 004: Implement GDPR Right to Delete (Data Deletion Service)

**Priority:** P0 - Critical Blocker  
**Status:** Ready for Implementation  
**Effort:** Large (1-2 weeks)  
**Dependencies:** Story 002 (K-Anonymity)


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/7

---

## Problem Statement

EduMind.AI currently lacks implementation of the GDPR "Right to be Forgotten" (Article 17), which requires the ability to completely delete user data upon request. This is a critical compliance requirement for operating in the EU and protecting user privacy.

**Legal Requirements:**

- Complete data deletion within 30 days of request
- Cascade deletion across all related entities
- Preserve audit trail of deletion (without PII)
- Handle deletion in multi-tenant context
- Maintain data integrity after deletion

**Current Status:** Data deletion service designed but not implemented.

---

## Goals & Success Criteria

### Goals

- Implement complete data deletion capability
- Achieve GDPR Article 17 compliance
- Build self-service data deletion portal
- Maintain referential integrity after deletions
- Create comprehensive audit trail

### Success Criteria

- [ ] Users can request data deletion from UI
- [ ] Complete data deletion within 30 days
- [ ] Cascade deletion across all entities
- [ ] Audit trail preserved (without PII)
- [ ] Aggregated analytics remain valid post-deletion
- [ ] No orphaned records after deletion

---

## Technical Approach

### Architecture Overview

```
Data Deletion Flow:
1. User requests deletion (or parent for child)
2. System validates request and creates deletion job
3. Grace period of 7 days (allows cancellation)
4. Background worker executes deletion
5. Cascade through all related entities
6. Anonymize audit trail entries
7. Send confirmation email
8. Log deletion to compliance audit
```

### Entities to Delete

**User-Related Data:**

- `users` - User profile
- `students` - Student-specific data
- `student_assessments` - Assessment sessions
- `student_responses` - Individual answers
- `parental_consents` - Consent records (anonymized, not deleted)
- `age_verifications` - Age verification records

**Multi-Tenant Context:**

- Maintain school/class aggregations
- Remove individual from aggregations
- Preserve k-anonymity after deletion

### Database Strategy

**Soft Delete vs Hard Delete:**

- **Soft Delete:** Mark as deleted, retain for 30 days
- **Hard Delete:** Permanent removal after grace period
- **Anonymization:** Replace PII with tombstone values

**Schema Changes:**

```sql
ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP;
ALTER TABLE users ADD COLUMN deletion_requested_at TIMESTAMP;
ALTER TABLE users ADD COLUMN deletion_confirmed BOOLEAN DEFAULT FALSE;

CREATE TABLE deletion_jobs (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    requested_at TIMESTAMP NOT NULL,
    scheduled_for TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    status VARCHAR(20) NOT NULL, -- 'Pending', 'InProgress', 'Completed', 'Cancelled'
    deletion_type VARCHAR(20) NOT NULL, -- 'UserRequested', 'ParentRequested', 'AdminRequested'
    created_by UUID,
    notes TEXT
);
```

---

## Task Decomposition

### Task 1: Create Data Deletion Domain Models

- **Description:** Define domain entities for data deletion workflow
- **Files:**
  - `src/AcademicAssessment.Core/Entities/DeletionJob.cs` (new)
  - `src/AcademicAssessment.Core/Enums/DeletionStatus.cs` (new)
  - `src/AcademicAssessment.Core/Enums/DeletionType.cs` (new)
- **Code:**

  ```csharp
  public record DeletionJob
  {
      public Guid Id { get; init; }
      public Guid UserId { get; init; }
      public DateTime RequestedAt { get; init; }
      public DateTime ScheduledFor { get; init; }
      public DateTime? CompletedAt { get; init; }
      public DeletionStatus Status { get; init; }
      public DeletionType Type { get; init; }
      public Guid? CreatedBy { get; init; }
      public string? Notes { get; init; }
  }
  
  public enum DeletionStatus
  {
      Pending,
      InProgress,
      Completed,
      Cancelled,
      Failed
  }
  
  public enum DeletionType
  {
      UserRequested,
      ParentRequested,
      AdminRequested,
      Automated // e.g., account expiration
  }
  ```

- **Acceptance:** Models compiled, follow Railway-Oriented Programming patterns
- **Dependencies:** None

### Task 2: Create Database Migration for Deletion Tables

- **Description:** Add deletion tracking tables and soft delete columns
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Data/Migrations/AddDataDeletionSupport.cs` (new)
- **SQL:**

  ```sql
  -- Add soft delete columns to users table
  ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP;
  ALTER TABLE users ADD COLUMN deletion_requested_at TIMESTAMP;
  ALTER TABLE users ADD COLUMN deletion_confirmed BOOLEAN DEFAULT FALSE;
  
  -- Create deletion jobs table
  CREATE TABLE deletion_jobs (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      user_id UUID NOT NULL REFERENCES users(id),
      requested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      scheduled_for TIMESTAMP NOT NULL,
      completed_at TIMESTAMP,
      status VARCHAR(20) NOT NULL DEFAULT 'Pending',
      deletion_type VARCHAR(20) NOT NULL,
      created_by UUID,
      notes TEXT,
      CONSTRAINT chk_deletion_status CHECK (status IN ('Pending', 'InProgress', 'Completed', 'Cancelled', 'Failed')),
      CONSTRAINT chk_deletion_type CHECK (deletion_type IN ('UserRequested', 'ParentRequested', 'AdminRequested', 'Automated'))
  );
  
  CREATE INDEX idx_deletion_jobs_user ON deletion_jobs(user_id);
  CREATE INDEX idx_deletion_jobs_status ON deletion_jobs(status);
  CREATE INDEX idx_deletion_jobs_scheduled ON deletion_jobs(scheduled_for);
  
  -- Add deletion audit table
  CREATE TABLE deletion_audit_log (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      deletion_job_id UUID NOT NULL REFERENCES deletion_jobs(id),
      entity_type VARCHAR(100) NOT NULL,
      entity_id UUID NOT NULL,
      deleted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      record_count INT NOT NULL DEFAULT 1
  );
  ```

- **Acceptance:** Migration applies successfully
- **Dependencies:** Task 1

### Task 3: Implement Data Deletion Service Interface

- **Description:** Define service interface for data deletion operations
- **Files:**
  - `src/AcademicAssessment.Core/Services/IDataDeletionService.cs` (new)
- **Methods:**

  ```csharp
  public interface IDataDeletionService
  {
      /// <summary>
      /// Request deletion of user data with 7-day grace period
      /// </summary>
      Task<Result<DeletionJob>> RequestDeletionAsync(Guid userId, DeletionType type, Guid? requestedBy, CancellationToken cancellationToken);
      
      /// <summary>
      /// Cancel pending deletion during grace period
      /// </summary>
      Task<Result> CancelDeletionAsync(Guid deletionJobId, CancellationToken cancellationToken);
      
      /// <summary>
      /// Execute scheduled deletions (called by background worker)
      /// </summary>
      Task<Result> ExecutePendingDeletionsAsync(CancellationToken cancellationToken);
      
      /// <summary>
      /// Get deletion status for user
      /// </summary>
      Task<Result<DeletionJob?>> GetDeletionStatusAsync(Guid userId, CancellationToken cancellationToken);
      
      /// <summary>
      /// Permanently delete user and all related data
      /// </summary>
      Task<Result<int>> DeleteUserDataAsync(Guid userId, Guid deletionJobId, CancellationToken cancellationToken);
  }
  ```

- **Acceptance:** Interface defined with XML documentation
- **Dependencies:** Task 1

### Task 4: Implement Data Deletion Service

- **Description:** Full implementation of data deletion logic
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Services/DataDeletionService.cs` (new)
- **Implementation:**

  ```csharp
  public class DataDeletionService : IDataDeletionService
  {
      private readonly IUserRepository _userRepository;
      private readonly IStudentRepository _studentRepository;
      private readonly IStudentAssessmentRepository _assessmentRepository;
      private readonly IStudentResponseRepository _responseRepository;
      private readonly IDeletionJobRepository _deletionJobRepository;
      private readonly IUnitOfWork _unitOfWork;
      private readonly ILogger<DataDeletionService> _logger;
      
      private const int GracePeriodDays = 7;
      
      public async Task<Result<DeletionJob>> RequestDeletionAsync(
          Guid userId, 
          DeletionType type, 
          Guid? requestedBy, 
          CancellationToken cancellationToken)
      {
          // Validate user exists
          var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
          if (user is null)
              return Result<DeletionJob>.Failure(new Error("User.NotFound", "User not found"));
          
          // Check for existing pending deletion
          var existing = await GetDeletionStatusAsync(userId, cancellationToken);
          if (existing.Value is not null && existing.Value.Status == DeletionStatus.Pending)
              return Result<DeletionJob>.Failure(new Error("Deletion.AlreadyPending", "Deletion already requested"));
          
          // Create deletion job
          var deletionJob = new DeletionJob
          {
              Id = Guid.NewGuid(),
              UserId = userId,
              RequestedAt = DateTime.UtcNow,
              ScheduledFor = DateTime.UtcNow.AddDays(GracePeriodDays),
              Status = DeletionStatus.Pending,
              Type = type,
              CreatedBy = requestedBy
          };
          
          await _deletionJobRepository.AddAsync(deletionJob, cancellationToken);
          
          // Mark user as pending deletion
          user = user with 
          { 
              DeletionRequestedAt = DateTime.UtcNow,
              IsActive = false 
          };
          await _userRepository.UpdateAsync(user, cancellationToken);
          
          await _unitOfWork.CommitAsync(cancellationToken);
          
          _logger.LogInformation("Deletion requested for user {UserId}, scheduled for {ScheduledFor}", 
              userId, deletionJob.ScheduledFor);
          
          return Result<DeletionJob>.Success(deletionJob);
      }
      
      public async Task<Result<int>> DeleteUserDataAsync(
          Guid userId, 
          Guid deletionJobId, 
          CancellationToken cancellationToken)
      {
          var deletedCount = 0;
          
          using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
          
          try
          {
              // 1. Delete student responses
              var responses = await _responseRepository.GetByStudentIdAsync(userId, cancellationToken);
              await _responseRepository.DeleteRangeAsync(responses, cancellationToken);
              deletedCount += responses.Count;
              await LogDeletionAsync(deletionJobId, "StudentResponse", userId, responses.Count, cancellationToken);
              
              // 2. Delete student assessments
              var assessments = await _assessmentRepository.GetByStudentIdAsync(userId, cancellationToken);
              await _assessmentRepository.DeleteRangeAsync(assessments, cancellationToken);
              deletedCount += assessments.Count;
              await LogDeletionAsync(deletionJobId, "StudentAssessment", userId, assessments.Count, cancellationToken);
              
              // 3. Anonymize parental consents (keep for audit)
              // Note: Use anonymization instead of deletion
              
              // 4. Delete student record
              var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken);
              if (student is not null)
              {
                  await _studentRepository.DeleteAsync(student, cancellationToken);
                  deletedCount++;
                  await LogDeletionAsync(deletionJobId, "Student", userId, 1, cancellationToken);
              }
              
              // 5. Delete user record
              var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
              if (user is not null)
              {
                  await _userRepository.DeleteAsync(user, cancellationToken);
                  deletedCount++;
                  await LogDeletionAsync(deletionJobId, "User", userId, 1, cancellationToken);
              }
              
              // 6. Update deletion job status
              var job = await _deletionJobRepository.GetByIdAsync(deletionJobId, cancellationToken);
              job = job with 
              { 
                  Status = DeletionStatus.Completed, 
                  CompletedAt = DateTime.UtcNow 
              };
              await _deletionJobRepository.UpdateAsync(job, cancellationToken);
              
              await transaction.CommitAsync(cancellationToken);
              
              _logger.LogInformation("Successfully deleted {Count} records for user {UserId}", 
                  deletedCount, userId);
              
              return Result<int>.Success(deletedCount);
          }
          catch (Exception ex)
          {
              await transaction.RollbackAsync(cancellationToken);
              _logger.LogError(ex, "Failed to delete user data for {UserId}", userId);
              return Result<int>.Failure(new Error("Deletion.Failed", "Data deletion failed"));
          }
      }
      
      private async Task LogDeletionAsync(
          Guid deletionJobId, 
          string entityType, 
          Guid entityId, 
          int count, 
          CancellationToken cancellationToken)
      {
          // Log to deletion_audit_log table
          // Implementation details...
      }
  }
  ```

- **Acceptance:** Service implements all interface methods with error handling
- **Dependencies:** Task 3

### Task 5: Create Background Worker for Scheduled Deletions

- **Description:** Background service to process scheduled deletions
- **Files:**
  - `src/AcademicAssessment.Web/Workers/DeletionWorker.cs` (new)
- **Implementation:**

  ```csharp
  public class DeletionWorker : BackgroundService
  {
      private readonly IServiceProvider _serviceProvider;
      private readonly ILogger<DeletionWorker> _logger;
      private const int CheckIntervalMinutes = 60;
      
      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
          _logger.LogInformation("Deletion Worker started");
          
          while (!stoppingToken.IsCancellationRequested)
          {
              try
              {
                  using var scope = _serviceProvider.CreateScope();
                  var deletionService = scope.ServiceProvider.GetRequiredService<IDataDeletionService>();
                  
                  await deletionService.ExecutePendingDeletionsAsync(stoppingToken);
                  
                  await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
              }
              catch (Exception ex)
              {
                  _logger.LogError(ex, "Error in Deletion Worker");
                  await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
              }
          }
      }
  }
  ```

- **Registration:** Add to `Program.cs`: `builder.Services.AddHostedService<DeletionWorker>();`
- **Acceptance:** Worker runs hourly, processes scheduled deletions
- **Dependencies:** Task 4

### Task 6: Create Data Deletion API Endpoints

- **Description:** REST API for data deletion management
- **Files:**
  - `src/AcademicAssessment.Web/Controllers/DataDeletionController.cs` (new)
- **Endpoints:**

  ```csharp
  [ApiController]
  [Route("api/deletion")]
  public class DataDeletionController : ControllerBase
  {
      [HttpPost("request")]
      [Authorize]
      public async Task<IActionResult> RequestDeletion(CancellationToken cancellationToken)
      {
          // User requests deletion of their own data
      }
      
      [HttpPost("request/{userId}")]
      [Authorize(Roles = "Parent,Admin")]
      public async Task<IActionResult> RequestDeletionForUser(Guid userId, CancellationToken cancellationToken)
      {
          // Parent or admin requests deletion for user
      }
      
      [HttpPost("cancel/{deletionJobId}")]
      [Authorize]
      public async Task<IActionResult> CancelDeletion(Guid deletionJobId, CancellationToken cancellationToken)
      {
          // Cancel pending deletion during grace period
      }
      
      [HttpGet("status")]
      [Authorize]
      public async Task<IActionResult> GetDeletionStatus(CancellationToken cancellationToken)
      {
          // Get current user's deletion status
      }
      
      [HttpGet("status/{userId}")]
      [Authorize(Roles = "Admin")]
      public async Task<IActionResult> GetUserDeletionStatus(Guid userId, CancellationToken cancellationToken)
      {
          // Admin checks deletion status for user
      }
  }
  ```

- **Acceptance:** All endpoints functional with proper authorization
- **Dependencies:** Task 4

### Task 7: Build Data Deletion UI in Student Portal

- **Description:** Self-service data deletion interface
- **Files:**
  - `src/AcademicAssessment.StudentApp/Pages/DeleteAccount.razor` (new)
- **UI Components:**
  - Warning message about permanence
  - List of data to be deleted
  - Confirmation checkbox
  - "Request Deletion" button
  - Grace period explanation
  - "Cancel Deletion" button (if pending)
- **Flow:**

  ```razor
  @page "/account/delete"
  @attribute [Authorize]
  
  <h2>Delete Your Account</h2>
  
  <div class="alert alert-danger">
      <strong>Warning:</strong> This action will permanently delete all your data.
  </div>
  
  <h4>The following data will be deleted:</h4>
  <ul>
      <li>Your profile information</li>
      <li>All assessment results and responses</li>
      <li>Progress tracking data</li>
      <li>Performance analytics</li>
  </ul>
  
  <p>You have 7 days to cancel this request. After that, deletion is permanent.</p>
  
  @if (PendingDeletion is not null)
  {
      <div class="alert alert-warning">
          <p>Deletion scheduled for: @PendingDeletion.ScheduledFor</p>
          <button @onclick="CancelDeletion">Cancel Deletion</button>
      </div>
  }
  else
  {
      <div class="form-check">
          <input type="checkbox" @bind="ConfirmDelete" />
          <label>I understand this action is permanent</label>
      </div>
      
      <button @onclick="RequestDeletion" disabled="@(!ConfirmDelete)">
          Delete My Account
      </button>
  }
  ```

- **Acceptance:** Students can request deletion from UI
- **Dependencies:** Task 6

### Task 8: Build Data Deletion UI in Parent Portal

- **Description:** Parent interface to delete child's account
- **Files:**
  - `src/AcademicAssessment.Dashboard/Pages/ParentPortal/DeleteChildAccount.razor` (new)
- **Similar to Task 7** but with parent authentication and child selection
- **Acceptance:** Parents can request deletion for children
- **Dependencies:** Story 003 (COPPA), Task 6

### Task 9: Implement Deletion Confirmation Emails

- **Description:** Email notifications for deletion workflow
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Email/Templates/DeletionRequested.cshtml` (new)
  - `src/AcademicAssessment.Infrastructure/Email/Templates/DeletionCompleted.cshtml` (new)
  - `src/AcademicAssessment.Infrastructure/Email/Templates/DeletionCancelled.cshtml` (new)
- **Emails:**
  1. **Deletion Requested:** Sent immediately, includes cancellation link
  2. **Deletion Completed:** Sent after 7 days, confirms permanent deletion
  3. **Deletion Cancelled:** Sent if user cancels during grace period
- **Acceptance:** Emails sent at appropriate stages
- **Dependencies:** Task 4

### Task 10: Implement Audit Logging for Deletions

- **Description:** Comprehensive audit trail for compliance
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Audit/DeletionAuditLogger.cs` (new)
- **Logged Events:**
  - Deletion requested (user ID, timestamp, requester)
  - Deletion scheduled (scheduled date)
  - Deletion executed (entities deleted, record counts)
  - Deletion cancelled (cancellation reason)
  - Deletion failed (error details)
- **Retention:** 7 years for compliance
- **Acceptance:** All deletion events logged
- **Dependencies:** Task 4

### Task 11: Handle K-Anonymity After Deletion

- **Description:** Ensure aggregations remain valid after user deletion
- **Files:**
  - `src/AcademicAssessment.Analytics/Services/StudentAnalyticsService.cs`
- **Logic:**
  - Recalculate aggregations after deletion
  - Check if remaining group size >= k (5)
  - Suppress aggregations if group becomes too small
  - Notify admins if deletion affects analytics visibility
- **Acceptance:** Analytics remain privacy-compliant post-deletion
- **Dependencies:** Story 002 (K-Anonymity), Task 4

### Task 12: Write Unit Tests for Data Deletion Service

- **Description:** Comprehensive unit tests for deletion logic
- **Files:**
  - `tests/AcademicAssessment.Tests.Unit/Services/DataDeletionServiceTests.cs` (new)
- **Test Cases:**
  - Request deletion → Creates deletion job
  - Cancel deletion → Job status updated
  - Execute deletion → All entities removed
  - Grace period enforcement
  - Cascade deletion order (responses → assessments → student → user)
  - Transaction rollback on error
- **Acceptance:** 100% coverage for deletion service
- **Dependencies:** Task 4

### Task 13: Write Integration Tests for Deletion Workflow

- **Description:** End-to-end tests for complete deletion flow
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/DataDeletion/DeletionWorkflowTests.cs` (new)
- **Test Scenarios:**
  1. User requests deletion → Data deleted after 7 days
  2. User cancels deletion → Account reactivated
  3. Parent requests child deletion → Requires parental consent
  4. Deletion with active assessment → Assessment invalidated
  5. Deletion impact on class analytics
- **Acceptance:** All workflow scenarios tested
- **Dependencies:** Task 6

### Task 14: Update Documentation for GDPR Compliance

- **Description:** Document GDPR right to delete implementation
- **Files:**
  - `.github/specification/07-security-privacy.md`
  - `docs/compliance/GDPR_COMPLIANCE.md` (new)
  - `README.md`
- **Content:**

  ```markdown
  ## GDPR Right to Delete (Article 17)
  
  **Status:** ✅ Implemented (2025-10-25)
  
  EduMind.AI provides users with the right to request complete deletion of their personal data.
  
  ### Features
  - Self-service deletion requests
  - 7-day grace period for cancellation
  - Complete cascade deletion across all entities
  - Audit trail of deletions (anonymized)
  - Email confirmations at each stage
  - Parent portal for child account deletion
  
  ### Process
  1. User requests deletion (UI or API)
  2. 7-day grace period begins
  3. Background worker executes deletion
  4. Confirmation email sent
  5. Audit log updated
  
  ### Compliance
  - Deletion completed within 30 days (GDPR requirement)
  - Audit trail retained for 7 years
  - No PII retained after deletion
  - Anonymization for necessary records
  ```

- **Acceptance:** GDPR compliance documented
- **Dependencies:** Task 13

---

## Acceptance Criteria (Validation)

### Functional Testing

1. **User Self-Service Deletion:**
   - Log in as student
   - Navigate to Account Settings → Delete Account
   - Request deletion
   - Expected: Confirmation email sent, account inactive

2. **Grace Period Cancellation:**
   - Within 7 days, log in
   - Navigate to Account Settings
   - Click "Cancel Deletion"
   - Expected: Account reactivated, deletion cancelled

3. **Automated Deletion After Grace Period:**
   - Wait 7 days (or simulate with date change)
   - Background worker runs
   - Expected: All user data deleted, confirmation email sent

4. **Parent-Initiated Deletion:**
   - Log in to parent portal
   - Select child account
   - Request deletion
   - Expected: Same flow as user self-service

### Data Integrity Testing

- [ ] Query for deleted user → Returns null
- [ ] Query for deleted user's responses → Returns empty
- [ ] Query for deleted user's assessments → Returns empty
- [ ] Class analytics still function correctly
- [ ] No orphaned foreign key references

### Compliance Verification

- [ ] Deletion completes within 30 days (GDPR)
- [ ] Audit trail exists without PII
- [ ] Email confirmations sent at each stage
- [ ] Grace period honored (7 days)
- [ ] Anonymization preserves necessary records

---

## Context & References

### Documentation

- [Security & Privacy Specification](.github/specification/07-security-privacy.md)
- [Data Storage Strategy](.github/specification/05-data-storage.md)

### Compliance References

- [GDPR Article 17 (Right to Erasure)](https://gdpr-info.eu/art-17-gdpr/)
- [GDPR Recital 65](https://gdpr-info.eu/recitals/no-65/)

### Related Code

- `src/AcademicAssessment.Infrastructure/Repositories/` - Repository implementations
- `src/AcademicAssessment.Core/Entities/` - Domain entities

---

## Notes

- **Cascade Order:** Delete in reverse dependency order (responses → assessments → student → user)
- **Anonymization vs Deletion:** Some records (audit logs, parental consents) should be anonymized, not deleted
- **Grace Period:** 7 days allows users to change their mind
- **Background Processing:** Deletions run hourly to avoid blocking UI
- **Multi-Tenant:** Ensure deletion doesn't affect other tenants' data

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
