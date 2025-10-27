# Story 001: Fix Integration Test Serialization Bug

**Priority:** P0 - Critical Blocker  
**Status:** Ready for Implementation  
**Effort:** Small (4-8 hours)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/28

---

## Problem Statement

All integration tests are currently blocked by a .NET 9.0 WebApplicationFactory serialization bug that prevents API responses from being properly deserialized. This prevents us from validating API functionality and is a critical blocker for production deployment.

**Error:**

```
System.InvalidOperationException: The PipeWriter 'ResponseBodyPipeWriter' does not implement PipeWriter.UnflushedBytes
```

**GitHub Issue:** <https://github.com/dotnet/aspnetcore/issues/52187>

---

## Goals & Success Criteria

### Goals

- Unblock all integration tests in `tests/AcademicAssessment.Tests.Integration/`
- Validate all API endpoints function correctly
- Establish baseline test coverage for API layer

### Success Criteria

- [ ] All integration tests execute successfully
- [ ] No serialization errors in test output
- [ ] All 3 API controllers have passing integration tests (Assessment, Orchestration, StudentAnalytics)
- [ ] CI/CD pipeline integration tests pass

---

## Technical Approach

### Workaround: Newtonsoft.Json

Replace System.Text.Json with Newtonsoft.Json in API configuration to bypass the PipeWriter bug.

**Implementation:**

1. Add Newtonsoft.Json NuGet package to `AcademicAssessment.Web.csproj`
2. Configure controllers to use Newtonsoft.Json serializer
3. Verify all integration tests pass
4. Document temporary workaround nature (revert when framework bug is fixed)

### Files to Modify

- `src/AcademicAssessment.Web/Program.cs` - Configure Newtonsoft.Json
- `src/AcademicAssessment.Web/AcademicAssessment.Web.csproj` - Add package reference
- `tests/AcademicAssessment.Tests.Integration/README.md` - Document workaround

---

## Task Decomposition

Break this work into the following tasks in `tasks.md`:

### Task 1: Add Newtonsoft.Json Package

- **Description:** Add `Microsoft.AspNetCore.Mvc.NewtonsoftJson` NuGet package
- **Files:**
  - `src/AcademicAssessment.Web/AcademicAssessment.Web.csproj`
- **Acceptance:** Package reference added, project restores successfully
- **Dependencies:** None

### Task 2: Configure Newtonsoft.Json in API

- **Description:** Replace System.Text.Json with Newtonsoft.Json in Web API configuration
- **Files:**
  - `src/AcademicAssessment.Web/Program.cs`
- **Code Change:**

  ```csharp
  builder.Services.AddControllers()
      .AddNewtonsoftJson(options =>
      {
          options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
          options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
      });
  ```

- **Acceptance:** API starts successfully with Newtonsoft.Json
- **Dependencies:** Task 1

### Task 3: Run Integration Test Suite

- **Description:** Execute all integration tests and verify they pass
- **Command:** `dotnet test tests/AcademicAssessment.Tests.Integration/ --verbosity normal`
- **Acceptance:**
  - All tests execute without serialization errors
  - Test results show pass/fail status (not execution failure)
  - At least 80% of tests pass (some may fail due to business logic issues)
- **Dependencies:** Task 2

### Task 4: Fix Failing Tests (If Any)

- **Description:** Address any integration test failures revealed after unblocking
- **Files:** Test files in `tests/AcademicAssessment.Tests.Integration/`
- **Acceptance:** All integration tests pass
- **Dependencies:** Task 3

### Task 5: Document Workaround

- **Description:** Add documentation explaining temporary Newtonsoft.Json usage
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/README.md`
  - `.github/specification/09e-known-issues-limitations.md`
- **Content:**

  ```markdown
  ## Temporary Workaround: Newtonsoft.Json

  **Status:** Applied (2025-10-25)

  Using Newtonsoft.Json instead of System.Text.Json to work around .NET 9.0 
  WebApplicationFactory serialization bug (GitHub #52187).

  **Revert when:** .NET 9.0.1 or later fixes the PipeWriter bug
  ```

- **Acceptance:** Documentation updated, workaround clearly marked as temporary
- **Dependencies:** Task 4

### Task 6: Update CI/CD Pipeline

- **Description:** Ensure GitHub Actions workflow runs integration tests
- **Files:** `.github/workflows/*.yml` (if exists)
- **Acceptance:** CI/CD pipeline executes integration tests successfully
- **Dependencies:** Task 5

---

## Acceptance Criteria (Validation)

Before marking this story complete, verify:

1. **Test Execution:**

   ```bash
   dotnet test tests/AcademicAssessment.Tests.Integration/ --verbosity normal
   ```

   Expected: All tests pass, no serialization errors

2. **API Functionality:**
   - Assessment API endpoints respond correctly
   - Orchestration API endpoints respond correctly
   - Student Analytics API endpoints respond correctly

3. **Documentation:**
   - Workaround documented in code comments
   - Known issues updated with status
   - README updated with workaround explanation

---

## Context & References

### Documentation

- [Known Issues & Limitations](.github/specification/09e-known-issues-limitations.md)
- [Integration Testing Guide](.github/testing/04-integration-testing.md)

### External References

- [.NET 9.0 WebApplicationFactory Bug](https://github.com/dotnet/aspnetcore/issues/52187)
- [Newtonsoft.Json Documentation](https://www.newtonsoft.com/json/help/html/Introduction.htm)

### Related Code

- `tests/AcademicAssessment.Tests.Integration/Controllers/AssessmentControllerTests.cs`
- `tests/AcademicAssessment.Tests.Integration/Controllers/OrchestrationControllerTests.cs`
- `tests/AcademicAssessment.Tests.Integration/Controllers/StudentAnalyticsControllerTests.cs`

---

## Notes

- **Temporary Solution:** This is a workaround, not a permanent fix
- **Monitor Framework:** Watch for .NET 9.0 patches that fix the bug
- **Revert Later:** Plan to revert to System.Text.Json when bug is resolved
- **Performance:** Newtonsoft.Json is slightly slower than System.Text.Json but acceptable for API responses

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
