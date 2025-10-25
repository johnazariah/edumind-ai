# TODO-002: Implement Submit API Endpoint

**Priority:** P1 - High  
**Area:** Backend / API  
**Estimated Effort:** Medium (3-4 hours)  
**Status:** Not Started

## Description

Create the submit endpoint that allows students to finalize and submit their completed assessments. This endpoint should trigger the orchestrator for assessment analysis and grading.

## Context

Students need to submit their completed assessments to:

- Finalize their answers (no further changes allowed)
- Trigger AI-powered grading and feedback generation
- Generate results for immediate or deferred display
- Update student progress records

Currently, `AssessmentController.cs` has a TODO comment (line 399) indicating this needs implementation. The frontend's `SubmitAssessmentAsync()` method is a placeholder.

Related files:

- Frontend: `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
- Backend: `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
- Orchestrator: `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`

## Technical Requirements

### API Endpoint

```csharp
POST /api/v1.0/Assessment/{assessmentId}/session/submit
```

**Request Body:**

```json
{
  "sessionId": "guid",
  "finalAnswers": {
    "questionId1": {
      "selectedOptions": ["A", "C"],
      "freeTextResponse": null,
      "timeSpent": 120
    },
    "questionId2": {
      "selectedOptions": [],
      "freeTextResponse": "Final answer text",
      "timeSpent": 245
    }
  },
  "totalTimeSpent": 1800,
  "completedAt": "2025-10-25T14:45:00Z"
}
```

**Response:**

```json
{
  "success": true,
  "sessionId": "guid",
  "submittedAt": "2025-10-25T14:45:00Z",
  "resultsUrl": "/assessment/results/{sessionId}",
  "message": "Assessment submitted successfully. Results are being processed."
}
```

### Backend Logic

1. **Validate submission:**
   - Verify assessment exists and is active
   - Verify session belongs to authenticated student
   - Check session hasn't already been submitted
   - Validate all required questions have answers

2. **Persist final answers:**
   - Save all answers to database via `IStudentProgressRepository`
   - Mark session as "Submitted" with timestamp
   - Lock session to prevent further changes

3. **Trigger orchestrator:**
   - Call `StudentProgressOrchestrator.AnalyzeAssessmentAsync()`
   - Queue assessment for grading and feedback generation
   - Use background job to avoid blocking HTTP response

4. **Return response:**
   - Return session ID for results retrieval
   - Provide results URL
   - Indicate processing status

## Acceptance Criteria

- [ ] `POST /api/v1.0/Assessment/{assessmentId}/session/submit` endpoint created
- [ ] Endpoint validates assessment ID, session ID, and ownership
- [ ] Endpoint checks all required questions are answered
- [ ] Final answers persisted to database atomically (transaction)
- [ ] Session status updated to "Submitted" with timestamp
- [ ] Session locked to prevent further modifications
- [ ] Orchestrator triggered for assessment analysis (async)
- [ ] Background job queued for grading (if needed)
- [ ] Response includes session ID and results URL
- [ ] Proper error handling for validation failures
- [ ] Proper error handling for database/orchestrator failures
- [ ] Idempotency: multiple submits return same result
- [ ] Unit tests for controller action (>80% coverage)
- [ ] Integration test for full submit flow
- [ ] API documentation updated (Swagger annotations)
- [ ] Frontend `SubmitAssessmentAsync()` wired to endpoint
- [ ] Success message and redirect to results page

## Dependencies

- **Required:**
  - TODO-001: Implement Save API Endpoint (should be completed first)
  - `IStudentProgressRepository` interface and implementation
  - `StudentProgressOrchestrator` interface and implementation

- **Optional:**
  - Background job system (Hangfire, Azure Functions) for async processing
  - SignalR for real-time results updates

## References

- **Files:**
  - `src/AcademicAssessment.Web/Controllers/AssessmentController.cs` (line 399)
  - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
  - `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`
  
- **Documentation:**
  - `docs/planning/NEXT_STEPS.md` (Priority 1, Section 2)
  - `.github/specification/11a-student-workflows.md`

- **Related TODOs:**
  - TODO-001: Implement Save API Endpoint (prerequisite)
  - TODO-003: Wire Up Frontend Save/Submit (depends on this)
  - TODO-004: Build Results Page (depends on this)

## Implementation Notes

1. **Idempotency:** Use idempotency key or check submission status before processing
2. **Transaction boundary:** Ensure answer persistence and session status update are atomic
3. **Async processing:** Use background job for expensive operations (AI grading)
4. **Timeout handling:** Set appropriate timeout for orchestrator calls
5. **Partial submission:** Consider allowing partial submissions (save draft, submit later)
6. **Audit trail:** Log all submission attempts with timestamps
7. **Rate limiting:** Prevent rapid resubmission attempts

## Orchestrator Integration

The orchestrator should be triggered with:

```csharp
await _orchestrator.AnalyzeAssessmentAsync(
    studentId: currentUserId,
    assessmentId: assessmentId,
    sessionId: sessionId,
    cancellationToken: cancellationToken
);
```

This will:

- Score multiple choice questions automatically
- Queue free response questions for AI grading
- Generate personalized feedback
- Update student mastery levels
- Recommend next steps

## Testing Strategy

**Unit Tests:**

- Validate request DTO
- Test authentication/authorization
- Mock repository and orchestrator calls
- Verify idempotency logic
- Test error scenarios

**Integration Tests:**

- Complete full assessment submission flow
- Verify answers persisted correctly
- Confirm session locked after submission
- Test double submission returns same result
- Verify orchestrator called with correct parameters
- Test submission with missing required answers (should fail)

**Manual Testing:**

- Complete assessment in UI
- Submit assessment
- Verify redirect to results page
- Check database for submitted status
- Verify orchestrator processing triggered
- Test submitting same session twice (should be idempotent)
