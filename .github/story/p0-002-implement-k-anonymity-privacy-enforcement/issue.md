# Story 002: Implement K-Anonymity Privacy Enforcement

**Priority:** P0 - Critical Blocker  
**Status:** Ready for Implementation  
**Effort:** Medium (2-3 days)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/5

---

## Problem Statement

The system currently lacks enforcement of k-anonymity (minimum group size) for analytics aggregations, creating a **FERPA compliance risk**. Without k-anonymity, small class sizes could allow re-identification of individual students through aggregated data.

**Compliance Risk:**

- FERPA prohibits disclosure of personally identifiable information
- Small aggregations (e.g., class of 3 students) can reveal individual performance
- Current implementation documents k=5 requirement but doesn't enforce it

**Reference:** `.github/specification/07-security-privacy.md` - K-Anonymity Aggregation

---

## Goals & Success Criteria

### Goals

- Enforce minimum group size (k=5) for all analytics aggregations
- Prevent accidental PII disclosure through small groups
- Achieve FERPA compliance for student data privacy
- Provide clear messaging when aggregations cannot be displayed

### Success Criteria

- [ ] Middleware enforces k=5 minimum for all analytics endpoints
- [ ] Analytics API returns appropriate error when group size < 5
- [ ] Dashboard UI displays privacy-friendly message for small classes
- [ ] Student identities cannot be inferred from aggregated data
- [ ] Privacy enforcement is tested with edge cases

---

## Technical Approach

### Architecture

Implement privacy enforcement at multiple layers:

1. **Database Layer:** PostgreSQL functions enforce minimum group size
2. **Application Layer:** Service-level validation before aggregation
3. **API Layer:** Middleware validates aggregation requests
4. **UI Layer:** Dashboard displays privacy messages

### K-Anonymity Rules

```csharp
public class KAnonymityConfig
{
    public const int MinimumGroupSize = 5; // k=5 for FERPA compliance
    
    // Aggregations allowed only when:
    // - Student count >= MinimumGroupSize
    // - No subgroup < MinimumGroupSize after filtering
}
```

### Implementation Strategy

**Phase 1: Database Functions**

- Create PostgreSQL function to count distinct students before aggregation
- Modify analytics queries to use k-anonymity checks

**Phase 2: Service Layer**

- Add `KAnonymityValidator` service
- Validate all analytics requests before execution
- Return `Result<T>` with privacy error when validation fails

**Phase 3: API Middleware**

- Create `KAnonymityMiddleware` for analytics endpoints
- Intercept requests to `/api/analytics/**`
- Return HTTP 403 with privacy message when k < 5

**Phase 4: UI Updates**

- Display privacy message in Dashboard when data suppressed
- Show "Insufficient data for privacy" instead of empty charts

---

## Task Decomposition

### Task 1: Create K-Anonymity Domain Models

- **Description:** Define domain models for privacy enforcement
- **Files:**
  - `src/AcademicAssessment.Core/Privacy/KAnonymityConfig.cs` (new)
  - `src/AcademicAssessment.Core/Privacy/KAnonymityValidationResult.cs` (new)
  - `src/AcademicAssessment.Core/Errors/PrivacyError.cs` (new)
- **Code:**

  ```csharp
  public record KAnonymityConfig(int MinimumGroupSize = 5);
  
  public record KAnonymityValidationResult(
      bool IsValid,
      int ActualGroupSize,
      int RequiredGroupSize,
      string? Message);
  
  public static class PrivacyError
  {
      public static Error InsufficientGroupSize(int actual, int required) =>
          new("Privacy.InsufficientGroupSize", 
              $"Cannot display aggregation. Group size {actual} is below privacy threshold {required}.");
  }
  ```

- **Acceptance:** Models compiled, referenced in Core project
- **Dependencies:** None

### Task 2: Create K-Anonymity Validator Service

- **Description:** Build service to validate group sizes before aggregation
- **Files:**
  - `src/AcademicAssessment.Core/Services/IKAnonymityValidator.cs` (new)
  - `src/AcademicAssessment.Analytics/Services/KAnonymityValidator.cs` (new)
- **Code:**

  ```csharp
  public interface IKAnonymityValidator
  {
      Task<Result<KAnonymityValidationResult>> ValidateGroupSizeAsync(
          Guid? schoolId, 
          Guid? classId, 
          CancellationToken cancellationToken);
  }
  
  public class KAnonymityValidator : IKAnonymityValidator
  {
      private readonly IStudentRepository _studentRepository;
      private readonly KAnonymityConfig _config;
      
      public async Task<Result<KAnonymityValidationResult>> ValidateGroupSizeAsync(
          Guid? schoolId, 
          Guid? classId, 
          CancellationToken cancellationToken)
      {
          // Query database for student count
          var count = await _studentRepository.CountAsync(schoolId, classId, cancellationToken);
          
          // Validate against k-anonymity threshold
          var isValid = count >= _config.MinimumGroupSize;
          
          return new KAnonymityValidationResult(
              IsValid: isValid,
              ActualGroupSize: count,
              RequiredGroupSize: _config.MinimumGroupSize,
              Message: isValid ? null : $"Group size too small for privacy (requires {_config.MinimumGroupSize})");
      }
  }
  ```

- **Acceptance:** Validator compiles, unit tests pass
- **Dependencies:** Task 1

### Task 3: Add Database K-Anonymity Function

- **Description:** Create PostgreSQL function to enforce k-anonymity at database level
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Data/Migrations/AddKAnonymityFunction.cs` (new migration)
- **SQL:**

  ```sql
  CREATE OR REPLACE FUNCTION check_k_anonymity(
      p_school_id UUID,
      p_class_id UUID,
      p_min_group_size INT DEFAULT 5
  ) RETURNS BOOLEAN AS $$
  DECLARE
      student_count INT;
  BEGIN
      SELECT COUNT(DISTINCT s.id)
      INTO student_count
      FROM students s
      WHERE (p_school_id IS NULL OR s.school_id = p_school_id)
        AND (p_class_id IS NULL OR s.class_id = p_class_id);
      
      RETURN student_count >= p_min_group_size;
  END;
  $$ LANGUAGE plpgsql;
  ```

- **Acceptance:** Migration applies successfully, function callable
- **Dependencies:** Task 1

### Task 4: Update StudentAnalyticsService with K-Anonymity

- **Description:** Modify analytics service to use k-anonymity validation
- **Files:**
  - `src/AcademicAssessment.Analytics/Services/StudentAnalyticsService.cs`
- **Changes:**

  ```csharp
  public async Task<Result<PerformanceMetrics>> GetPerformanceMetricsAsync(
      Guid studentId, 
      CancellationToken cancellationToken)
  {
      // Get student's school/class
      var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
      
      // Validate k-anonymity before aggregation
      var validation = await _kAnonymityValidator.ValidateGroupSizeAsync(
          student.SchoolId, 
          student.ClassId, 
          cancellationToken);
      
      if (!validation.Value.IsValid)
          return Result<PerformanceMetrics>.Failure(
              PrivacyError.InsufficientGroupSize(
                  validation.Value.ActualGroupSize, 
                  validation.Value.RequiredGroupSize));
      
      // Proceed with aggregation
      // ...existing code...
  }
  ```

- **Acceptance:** All analytics methods use k-anonymity validation
- **Dependencies:** Task 2

### Task 5: Create K-Anonymity Middleware

- **Description:** Add middleware to enforce k-anonymity on analytics API endpoints
- **Files:**
  - `src/AcademicAssessment.Web/Middleware/KAnonymityMiddleware.cs` (new)
  - `src/AcademicAssessment.Web/Program.cs`
- **Code:**

  ```csharp
  public class KAnonymityMiddleware
  {
      private readonly RequestDelegate _next;
      private readonly ILogger<KAnonymityMiddleware> _logger;
      
      public async Task InvokeAsync(HttpContext context, IKAnonymityValidator validator)
      {
          // Only intercept analytics endpoints
          if (!context.Request.Path.StartsWithSegments("/api/analytics"))
          {
              await _next(context);
              return;
          }
          
          // Extract school/class from query or claims
          var schoolId = context.User.FindFirst("school_id")?.Value;
          var classId = context.Request.Query["classId"].FirstOrDefault();
          
          // Validate k-anonymity
          var result = await validator.ValidateGroupSizeAsync(
              schoolId != null ? Guid.Parse(schoolId) : null,
              classId != null ? Guid.Parse(classId) : null,
              context.RequestAborted);
          
          if (!result.Value.IsValid)
          {
              context.Response.StatusCode = 403; // Forbidden
              await context.Response.WriteAsJsonAsync(new
              {
                  error = "Privacy protection active",
                  message = "Insufficient data to display without compromising privacy",
                  requiredGroupSize = result.Value.RequiredGroupSize
              });
              return;
          }
          
          await _next(context);
      }
  }
  ```

- **Acceptance:** Middleware intercepts analytics requests, enforces k=5
- **Dependencies:** Task 2

### Task 6: Update Dashboard UI for Privacy Messages

- **Description:** Display user-friendly messages when k-anonymity blocks data
- **Files:**
  - `src/AcademicAssessment.Dashboard/Components/AnalyticsDashboard.razor`
  - `src/AcademicAssessment.Dashboard/Components/PrivacyMessageComponent.razor` (new)
- **UI:**

  ```razor
  @if (AnalyticsResult.IsFailure && AnalyticsResult.Error.Code == "Privacy.InsufficientGroupSize")
  {
      <div class="alert alert-info" role="alert">
          <i class="bi bi-shield-lock"></i>
          <strong>Privacy Protected</strong>
          <p>This class has too few students to display aggregated data while protecting individual privacy.</p>
          <p><small>We require at least 5 students to show class analytics.</small></p>
      </div>
  }
  else if (AnalyticsResult.IsSuccess)
  {
      <!-- Display analytics charts -->
  }
  ```

- **Acceptance:** Dashboard shows privacy message for small classes
- **Dependencies:** Task 5

### Task 7: Write Unit Tests for K-Anonymity

- **Description:** Comprehensive unit tests for k-anonymity validation
- **Files:**
  - `tests/AcademicAssessment.Tests.Unit/Privacy/KAnonymityValidatorTests.cs` (new)
- **Test Cases:**
  - Group size = 5 → Valid
  - Group size = 4 → Invalid
  - Group size = 10 → Valid
  - Null school/class → Validates all students
  - Empty class → Invalid
- **Acceptance:** 100% code coverage for k-anonymity logic
- **Dependencies:** Task 2

### Task 8: Write Integration Tests for Privacy Enforcement

- **Description:** Integration tests for analytics API with k-anonymity
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/Privacy/KAnonymityIntegrationTests.cs` (new)
- **Test Cases:**
  - Request analytics for class with 3 students → HTTP 403
  - Request analytics for class with 5 students → HTTP 200
  - Request analytics for class with 10 students → HTTP 200
  - Verify error message format
- **Acceptance:** All integration tests pass
- **Dependencies:** Task 5

### Task 9: Update Documentation

- **Description:** Document k-anonymity implementation and compliance
- **Files:**
  - `.github/specification/07-security-privacy.md`
  - `docs/architecture/PRIVACY_AND_SECURITY.md`
  - `README.md`
- **Content:**

  ```markdown
  ## K-Anonymity Privacy Protection

  **Status:** ✅ Implemented

  EduMind.AI enforces k-anonymity (k=5) for all analytics aggregations to protect 
  student privacy and maintain FERPA compliance.

  - **Minimum Group Size:** 5 students
  - **Enforcement:** Database, service, API, and UI layers
  - **User Experience:** Clear privacy messages when data cannot be displayed
  - **Compliance:** FERPA, COPPA, GDPR compliant
  ```

- **Acceptance:** Documentation updated, status marked as implemented
- **Dependencies:** Task 8

---

## Acceptance Criteria (Validation)

### Functional Testing

1. **Small Class (3 students):**
   - Navigate to Dashboard → Analytics
   - Expected: "Privacy Protected" message displayed
   - Expected: No charts or aggregated data shown

2. **Medium Class (5 students):**
   - Navigate to Dashboard → Analytics
   - Expected: Full analytics dashboard displayed
   - Expected: Charts show aggregated data

3. **API Direct Test:**

   ```bash
   curl -X GET "https://localhost:7001/api/analytics/performance?classId=<small-class-id>" \
     -H "Authorization: Bearer <token>"
   ```

   Expected: HTTP 403 with privacy message

### Compliance Verification

- [ ] No individual student identifiable from aggregations
- [ ] Privacy message explains why data not shown
- [ ] k=5 enforced at all layers (database, service, API, UI)
- [ ] Edge cases handled (null class, empty class, deleted students)

### Documentation

- [ ] Privacy implementation documented
- [ ] Compliance status updated to "Implemented"
- [ ] API documentation includes privacy constraints

---

## Context & References

### Documentation

- [Security & Privacy Specification](.github/specification/07-security-privacy.md)
- [Privacy Executive Summary](docs/architecture/PRIVACY_EXECUTIVE_SUMMARY.md)
- [Analytics Features](.github/specification/09d-analytics-reporting-features.md)

### Compliance References

- [FERPA Privacy Rule](https://www2.ed.gov/policy/gen/guid/fpco/ferpa/index.html)
- [K-Anonymity Research](https://en.wikipedia.org/wiki/K-anonymity)

### Related Code

- `src/AcademicAssessment.Analytics/Services/StudentAnalyticsService.cs`
- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`

---

## Notes

- **Privacy-First Design:** Better to show nothing than risk PII disclosure
- **User Communication:** Clear messaging helps users understand privacy protection
- **Future Enhancement:** Consider l-diversity for additional privacy protection
- **Configuration:** k=5 is configurable via `appsettings.json` if needed

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
