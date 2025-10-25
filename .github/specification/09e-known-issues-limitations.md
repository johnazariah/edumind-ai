# 09e. Known Issues and Limitations

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**Status:** Active Tracking

---

## Table of Contents

1. [Overview](#overview)
2. [Critical Blockers](#critical-blockers)
3. [Known Limitations](#known-limitations)
4. [Technical Debt](#technical-debt)
5. [Planned Improvements](#planned-improvements)
6. [Workarounds](#workarounds)

---

## Overview

This document tracks known issues, bugs, limitations, and technical debt across the EduMind.AI platform. It serves as a single source of truth for understanding system constraints and planned improvements.

### Related Documents

- [09a-core-assessment-features.md](09a-core-assessment-features.md) - Assessment features
- [09b-agent-orchestration-features.md](09b-agent-orchestration-features.md) - Orchestration features
- [09c-user-interface-features.md](09c-user-interface-features.md) - UI features
- [09d-analytics-reporting-features.md](09d-analytics-reporting-features.md) - Analytics features

---

## Critical Blockers

### .NET 9.0 WebApplicationFactory Serialization Bug

**Status:** 🔴 **BLOCKED - Awaiting Framework Fix**  
**Severity:** HIGH - Blocks all integration tests  
**Affected Version:** .NET 9.0.10  
**GitHub Issue:** <https://github.com/dotnet/aspnetcore/issues/52187>

#### Summary

Integration tests using `WebApplicationFactory<Program>` fail with serialization errors when controllers return responses. The error occurs in the test host's `PipeWriter` implementation.

#### Error Message

```text
System.InvalidOperationException: The PipeWriter 'ResponseBodyPipeWriter' does not implement PipeWriter.UnflushedBytes
   at System.Text.Json.ThrowHelper.ThrowInvalidOperationException_PipeWriterDoesNotImplementUnflushedBytes
   at System.Text.Json.Serialization.Metadata.JsonTypeInfo`1.SerializeAsync(PipeWriter pipeWriter, ...)
   at Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter.WriteResponseBodyAsync(...)
```

#### Impact

- ✅ **Authentication:** Working correctly - tests authenticate and reach controllers
- ✅ **Authorization:** All policies configured and working  
- ✅ **Business Logic:** Controller actions execute successfully
- ❌ **Response Serialization:** Fails when writing JSON response back to test client

#### Attempted Workarounds (All Failed)

1. **DefaultBufferSize = 4096** in `Program.cs` AddControllers().AddJsonOptions()
   - Result: Same error, no improvement

2. **DefaultBufferSize = 1** in test factory Configure JsonOptions
   - Result: Same error, no improvement

3. **PostConfigure JwtBearerOptions** instead of Configure
   - Result: Fixed auth but not serialization

#### Test Evidence

Test logs show successful authentication but serialization failure:

```text
[INF] User 00000000-0000-0000-0000-000000000001 requesting performance summary
[INF] Getting performance summary for student 00000000-0000-0000-0000-000000000001
[ERR] HTTP GET /api/v1/students/.../performance-summary responded 500
```

The controller receives the request, processes it, but fails when returning the response.

#### Possible Solutions

**Option 1: Use Newtonsoft.Json for Tests (RECOMMENDED)**

Add to `Program.cs`:

```csharp
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // NuGet: Microsoft.AspNetCore.Mvc.NewtonsoftJson
```

This bypasses System.Text.Json and the PipeWriter issue.

**Option 2: Test Against Real Aspire Instance**

Instead of using `WebApplicationFactory`, run tests against the actual Aspire-hosted API:

- Start Aspire: `dotnet run --project src/EduMind.AppHost`
- Update test configuration to use live endpoint
- Trade-off: Slower tests, requires running services

**Option 3: Wait for .NET Framework Update**

Monitor the GitHub issue and update to .NET 9.0.11+ when available.

**Option 4: Downgrade to .NET 8.0**

Revert to .NET 8.0 LTS where this issue does not exist.

#### Current Status

- Integration test configuration is **complete and correct**
- Authentication and authorization are **fully working**
- Blocked by framework-level bug in .NET 9 test host
- Awaiting framework fix or implementing Newtonsoft.Json workaround

#### Related Files

- `tests/AcademicAssessment.Tests.Integration/Helpers/AuthenticatedWebApplicationFactory.cs`
- `src/AcademicAssessment.Web/Program.cs` (lines 167-173)
- All test classes in `AcademicAssessment.Tests.Integration`

#### Last Updated

January 2025 - .NET 9.0.10 with Aspire 9.5.1

---

## Known Limitations

### Azure Deployment Issues

#### Template Variable Substitution Failure

**Status:** ⚠️ **Workaround Available**  
**Severity:** MEDIUM - Affects Azure deployment only  
**Date Identified:** January 24, 2025

**Problem:**

Azure Container Apps deployment fails because template variables are not substituted in `webapi.tmpl.yaml`:

- `{{ .Env.POSTGRES_HOST }}` remains literal instead of FQDN
- `{{ .Env.POSTGRES_PASSWORD }}` not substituted
- Results in connection string: `postgres` instead of actual Azure PostgreSQL FQDN

**Impact:**

- Local deployment: ✅ Works perfectly
- Azure deployment: ❌ Health check fails ("Unhealthy")
- Root cause confirmed: Template substitution, NOT application code

**Workaround:**

Manual secret injection script available:

```bash
./scripts/inject-azure-secrets.sh rg-staging "<postgres-password>"
```

This directly sets connection strings as Container App secrets, bypassing template substitution.

**Permanent Fix:**

Planned migration away from `azd` templates to direct Bicep secret references. See `docs/deployment/AZURE_WORKAROUND_PLAN.md` for details.

**Files:**

- `src/infra/webapi.tmpl.yaml` (problematic template)
- `scripts/inject-azure-secrets.sh` (workaround script)
- `docs/deployment/AZURE_WORKAROUND_PLAN.md` (detailed analysis)

### LLM Performance

#### OLLAMA Response Time

**Status:** ⚠️ **Known Limitation - By Design**  
**Severity:** LOW - Expected for local LLM

**Issue:**

OLLAMA llama3.2:3b evaluation takes 20-25 seconds per question response.

**Impact:**

- Slower than Azure OpenAI (which would be <5 seconds)
- Acceptable for development and testing
- May not scale for real-time production use with hundreds of concurrent students

**Context:**

This is expected behavior for local LLM inference on CPU. Trade-off for zero-cost operation.

**Solutions:**

1. **Use GPU:** NVIDIA GPU can reduce to ~2-5 seconds
2. **Migrate to Azure OpenAI:** <5 seconds response time (costs ~$0.01/evaluation)
3. **Hybrid Approach:** OLLAMA for dev/test, Azure OpenAI for production

**Configuration:** Already supports provider switching via `appsettings.json`

**Files:**

- `docs/archive/historical/OLLAMA_EVALUATION.md` - Performance analysis
- `src/AcademicAssessment.Infrastructure/ExternalServices/OllamaService.cs`

### Assessment Features

#### Static Assessment Structure

**Status:** ⚠️ **Current Limitation**  
**Severity:** MEDIUM - Limits adaptive testing

**Issue:**

Assessments have pre-assigned questions in fixed order. Cannot dynamically select questions based on student responses during assessment.

**Impact:**

- IRT parameters exist but not used for real-time adaptation
- Computerized Adaptive Testing (CAT) not possible with current structure
- Students receive same question set regardless of ability

**Planned Solution:**

Week 3-4: Implement dynamic question selection:

- Store question pool per assessment (not specific questions)
- Select next question based on previous response
- Update AssessmentSession to support dynamic questions

**Files:**

- `src/AcademicAssessment.Core/Entities/Assessment.cs`
- `assessment_questions` table schema needs redesign

#### No Question Authoring UI

**Status:** 📋 **Not Implemented**  
**Severity:** MEDIUM - Manual database editing required

**Issue:**

No user interface for creating/editing questions. Must edit database directly or use SQL scripts.

**Impact:**

- Teachers cannot create their own questions
- Content updates require developer intervention
- Barrier to adoption for schools

**Planned Solution:**

Week 4: Build question authoring interface with WYSIWYG editor, math input, and preview.

**Files:** Not yet implemented

### Privacy Features

#### K-Anonymity Not Enforced

**Status:** ⚠️ **Critical Privacy Gap**  
**Severity:** HIGH - Compliance risk

**Issue:**

Class analytics do not enforce minimum 5 students (k-anonymity). Can potentially expose individual student data through small group analysis.

**Impact:**

- FERPA compliance risk
- Privacy concerns for small classes
- Could re-identify students through correlation

**Current Mitigation:**

Individual student analytics only accessible by that student or authorized teachers.

**Planned Solution:**

Week 4: Implement k-anonymity middleware:

```csharp
public class KAnonymityMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAggregateQuery(context.Request.Path))
        {
            var groupSize = await GetGroupSize(context);
            if (groupSize < 5)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(
                    "Insufficient students for privacy-preserving analytics");
                return;
            }
        }
        await _next(context);
    }
}
```

**Files:**

- Planned: `src/AcademicAssessment.Web/Middleware/KAnonymityMiddleware.cs`
- Spec: `docs/architecture/PRIVACY_AND_SECURITY.md`

### Orchestration

#### Incomplete Progress Analysis

**Status:** ⚠️ **Stub Implementation**  
**Severity:** MEDIUM - Core feature incomplete

**Issue:**

Three orchestrator methods have TODO comments and stub implementations:

1. `AnalyzeProgressAsync()` - Line 428
2. `RecommendStudyPathAsync()` - Line 452
3. `ScheduleNextAssessmentAsync()` - Line 476

**Impact:**

- Core orchestration features not available
- Cannot automatically recommend study paths
- No intelligent assessment scheduling

**Current Behavior:**

Methods throw `NotImplementedException` or return placeholder data.

**Planned Solution:**

Week 3: Implement comprehensive progress analysis using ML.NET or rule-based algorithms.

**Files:**

- `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`

---

## Technical Debt

### TODO Comments in Code

**Status:** Tracked  
**Count:** 19 instances

#### High Priority TODOs

**1. Azure OpenAI Integration (Line 437, Program.cs)**

```csharp
// TODO: Implement when Azure.AI.OpenAI SDK v2.0 is stable
```

**Impact:** Cannot use Azure OpenAI as LLM provider  
**Effort:** 2-4 hours  
**Priority:** MEDIUM

**2. Save/Submit Database Persistence (AssessmentController.cs)**

```csharp
// Line 363: TODO: Persist to database via IStudentProgressRepository
// Line 399: TODO: Persist to database and trigger orchestrator
// Line 430: TODO: Retrieve actual results from database
```

**Impact:** Save/submit use mock data, not persisted  
**Effort:** 4-8 hours  
**Priority:** HIGH

**3. Dead Letter Queue (StudentProgressOrchestrator.cs, Line 810)**

```csharp
// TODO: Move to dead letter queue for manual intervention
```

**Impact:** Failed tasks not tracked for recovery  
**Effort:** 3-5 hours  
**Priority:** MEDIUM

#### Medium Priority TODOs

**4. Health Checking Implementation (Line 940, StudentProgressOrchestrator.cs)**

```csharp
// TODO: Implement actual health checking
```

**Impact:** Agent health not accurately monitored  
**Effort:** 2-3 hours

**5. Historical Success Rate Tracking (Line 983, StudentProgressOrchestrator.cs)**

```csharp
// TODO: Track historical success rate per agent
```

**Impact:** No long-term agent performance metrics  
**Effort:** 3-4 hours

**6. Answer Review Navigation (AssessmentResults.razor.cs, Line 107)**

```csharp
// TODO: Navigate to answer review page once implemented
```

**Impact:** Cannot review answers after submission  
**Effort:** 8-12 hours (requires full review page)

**7. State Persistence (AssessmentDetail.razor.cs, Line 53)**

```csharp
// TODO: Persist the student's current state once backend endpoints are available
```

**Impact:** Assessment start not tracked  
**Effort:** 1-2 hours

#### Low Priority TODOs

**8. Proper Logging (AssessmentDashboard.razor.cs, Line 44)**

```csharp
// TODO: Add proper logging
```

**Impact:** Limited error diagnostics  
**Effort:** 1 hour

### Stub Implementations

#### Stub LLM Service

**Status:** ✅ **Intentional - For Testing**

`StubLLMService` provides mock responses for testing without LLM dependency.

**Purpose:** CI/CD pipelines, unit tests  
**Not a bug:** This is by design

**Files:**

- `src/AcademicAssessment.Infrastructure/ExternalServices/StubLLMService.cs`

#### Stub Repositories (Legacy)

**Status:** ⚠️ **Partially Removed**

Some stub repositories may still exist from early development:

- `StubRepositoryBase.cs`
- `StubStudentAssessmentRepository.cs`
- `StubStudentResponseRepository.cs`
- `StubQuestionRepository.cs`
- `StubAssessmentRepository.cs`

**Impact:** May cause confusion about which repositories are active

**Action:** Audit and remove unused stub repositories

### Code Quality Issues

#### Large Methods

**Issue:** `StudentProgressOrchestrator.cs` has methods exceeding 100 lines

**Examples:**

- `ExecuteWorkflowAsync()` - ~200 lines
- `RouteTaskWithFallback()` - ~150 lines

**Impact:** Harder to maintain and test

**Solution:** Refactor into smaller, focused methods

#### Missing Unit Tests

**Current Test Coverage:** 99.2% (378/381 tests passing)

**Missing Coverage:**

- 3 skipped tests (specific edge cases)
- Some error handling paths
- New UI components (Week 2)

**Action:** Increase coverage to >95% including UI components

#### Insufficient XML Documentation

**Issue:** Many public methods lack XML documentation comments

**Impact:** IntelliSense not helpful, harder for team to understand APIs

**Solution:** Add `/// <summary>` tags to all public interfaces and methods

---

## Planned Improvements

### Performance Optimizations

#### Database Query Optimization

**Status:** Planned for Week 4

**Improvements:**

1. **Add Indexes:**
   - `student_assessments.student_id` (already exists)
   - `student_responses.student_assessment_id` (add)
   - `questions.subject, questions.difficulty` (composite index)

2. **Query Caching:**
   - Cache frequently accessed assessments in Redis
   - Cache question banks per subject
   - TTL: 15 minutes

3. **Batch Loading:**
   - Already implemented in orchestrator
   - Extend to other repositories

#### OLLAMA Performance

**Status:** Research in Progress

**Options:**

1. **Upgrade to larger model:** llama3.2:7b or 13b (better accuracy, slower)
2. **Quantization:** Use INT8 quantization for faster inference
3. **GPU Acceleration:** Leverage NVIDIA GPU if available
4. **Model Warm-up:** Keep model in memory to avoid cold starts

### Feature Enhancements

#### Real-Time Adaptive Testing

**Status:** Planned for Week 3-4

**Goal:** Select next question based on previous responses

**Approach:**

- Estimate student ability (theta) after each question
- Select question with optimal difficulty (theta ± 0.5)
- Stop when estimate converges (SE < 0.3)

#### Advanced Analytics

**Status:** Planned for Week 5

**Features:**

- Predictive analytics (identify at-risk students)
- Learning path optimization
- Curriculum effectiveness analysis
- ROI metrics

#### Accessibility Compliance

**Status:** Planned for Week 3, Days 15-16

**Goal:** WCAG 2.1 AA compliance

**Checklist:**

- [ ] Keyboard navigation for all interactive elements
- [ ] ARIA labels on components
- [ ] High contrast mode
- [ ] Screen reader testing (NVDA, JAWS)
- [ ] Color contrast ratios >4.5:1

---

## Workarounds

### Azure Deployment Workaround

**Issue:** Template variable substitution fails

**Workaround:**

```bash
# 1. Get PostgreSQL password from Azure Key Vault or deployment output
POSTGRES_PASS="your-secure-password"

# 2. Run injection script
./scripts/inject-azure-secrets.sh rg-staging "$POSTGRES_PASS"

# 3. Wait for container restart (~30 seconds)
sleep 30

# 4. Verify health
curl https://webapi.kindplant-6461f562.australiaeast.azurecontainerapps.io/health
# Expected: "Healthy"
```

**Permanent Fix:** Planned for Week 6

### Integration Test Workaround

**Issue:** .NET 9.0 serialization bug

**Workaround Option 1 (Recommended):**

Add Newtonsoft.Json to `Program.cs`:

```csharp
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });
```

**Workaround Option 2:**

Test against live Aspire instance instead of `WebApplicationFactory`:

```bash
# Terminal 1: Start Aspire
dotnet run --project src/EduMind.AppHost

# Terminal 2: Run integration tests against localhost:5103
dotnet test tests/AcademicAssessment.Tests.Integration \
    --settings tests/live-aspire.runsettings
```

### OLLAMA Slow Response Workaround

**Issue:** 20-25 seconds per evaluation

**Workaround:**

Use exact match fallback by configuring stub LLM:

```json
{
  "LLM": {
    "Provider": "Stub"
  }
}
```

This bypasses OLLAMA entirely for faster testing.

**Trade-off:** No semantic evaluation, exact string matching only

---

## Issue Tracking

### How to Report Issues

1. **Create GitHub Issue:** <https://github.com/johnazariah/edumind-ai/issues>
2. **Include:**
   - Clear title
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details (OS, .NET version, browser)
   - Logs or error messages
3. **Label appropriately:** bug, enhancement, question, etc.

### Priority Levels

- **P0 (Critical):** Blocks development or deployment
- **P1 (High):** Major feature impact, affects users
- **P2 (Medium):** Moderate impact, workaround available
- **P3 (Low):** Minor issues, cosmetic bugs

### Current P0 Issues

1. **.NET 9.0 Integration Test Serialization** - Blocked, awaiting framework fix
2. **K-Anonymity Missing** - Privacy compliance risk

### Current P1 Issues

1. **Azure Deployment Template Substitution** - Workaround available
2. **Save/Submit Database Persistence** - Mock data only
3. **Question Authoring UI Missing** - Blocks teacher adoption

---

## Related Documentation

- **[09a-core-assessment-features.md](09a-core-assessment-features.md)** - Assessment features
- **[09b-agent-orchestration-features.md](09b-agent-orchestration-features.md)** - Orchestration features
- **[09c-user-interface-features.md](09c-user-interface-features.md)** - UI features
- **[09d-analytics-reporting-features.md](09d-analytics-reporting-features.md)** - Analytics features
- **[07-security-privacy.md](07-security-privacy.md)** - Privacy compliance
- **docs/development/KNOWN_ISSUES.md** - Original issue tracking document

---

**Document Status:** Active Tracking  
**Last Review:** October 24, 2025  
**Next Review:** Weekly during active development
