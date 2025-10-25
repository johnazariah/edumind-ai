# TODO-001: Implement Save API Endpoint

**Priority:** P1 - High  
**Area:** Backend / API  
**Estimated Effort:** Medium (3-4 hours)  
**Status:** Not Started

## Description

Create the save endpoint that allows students to persist their assessment progress during an active session. Currently, the Student App UI has placeholder save methods that need to be wired to actual backend persistence.

## Context

The Student App's `AssessmentSession.razor` component implements autosave functionality that calls `SaveProgressAsync()`, but this method currently only saves to browser local storage. We need proper backend persistence to:

- Prevent data loss on browser crashes or network issues
- Allow students to resume assessments across devices
- Enable teachers/admins to monitor in-progress assessments
- Provide audit trail for assessment integrity

Related files:

- Frontend: `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
- Backend: `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
- Repository: `src/AcademicAssessment.Infrastructure/Repositories/StudentProgressRepository.cs`

## Technical Requirements

### API Endpoint

```csharp
POST /api/v1.0/Assessment/{assessmentId}/session/save
```

**Request Body:**

```json
{
  "sessionId": "guid",
  "answers": {
    "questionId1": {
      "selectedOptions": ["A", "C"],
      "freeTextResponse": null,
      "markedForReview": false,
      "timeSpent": 120
    },
    "questionId2": {
      "selectedOptions": [],
      "freeTextResponse": "Sample answer text",
      "markedForReview": true,
      "timeSpent": 45
    }
  },
  "currentQuestionIndex": 5,
  "timeRemaining": 2400
}
```

**Response:**

```json
{
  "success": true,
  "lastSavedAt": "2025-10-25T14:30:00Z",
  "message": "Progress saved successfully"
}
```

### Database Persistence

Use `IStudentProgressRepository` to persist:

- Answer selections (multiple choice, checkboxes)
- Free text responses (short answer, essay)
- Question metadata (marked for review, time spent)
- Session state (current question, time remaining)

Consider database schema:

- Update existing `StudentResponse` table or
- Create new `SessionState` table for in-progress state

## Acceptance Criteria

- [ ] `POST /api/v1.0/Assessment/{assessmentId}/session/save` endpoint created
- [ ] Endpoint validates assessment ID and session ID
- [ ] Endpoint authenticates student and validates ownership
- [ ] Answer data persisted to database via `IStudentProgressRepository`
- [ ] Response includes last saved timestamp
- [ ] Proper error handling for validation failures
- [ ] Proper error handling for database failures
- [ ] Transaction support to ensure atomic saves
- [ ] Unit tests for controller action (>80% coverage)
- [ ] Integration test for full save flow
- [ ] API documentation updated (Swagger annotations)
- [ ] Frontend `SaveProgressAsync()` updated to call endpoint
- [ ] Toast notification shows save success/failure in UI

## Dependencies

- Frontend placeholder method: `AssessmentSession.razor.cs:SaveProgressAsync()`
- Backend controller: `AssessmentController.cs` (line 363 has TODO comment)
- Repository interface: `IStudentProgressRepository`

## References

- **Files:**
  - `src/AcademicAssessment.Web/Controllers/AssessmentController.cs` (line 363)
  - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
  - `src/AcademicAssessment.Infrastructure/Repositories/StudentProgressRepository.cs`
  
- **Documentation:**
  - `docs/planning/NEXT_STEPS.md` (Priority 1, Section 1)
  - `.github/specification/11a-student-workflows.md`

- **Related TODOs:**
  - TODO-002: Implement Submit API Endpoint (depends on this)
  - TODO-003: Wire Up Frontend Save/Submit (depends on this)

## Implementation Notes

1. Consider batch saving to reduce database load (save every 30 seconds or on question navigation)
2. Add optimistic concurrency handling (version checks)
3. Consider compression for large text responses
4. Add rate limiting to prevent abuse (max 1 save per 5 seconds per session)
5. Log save operations for audit trail
6. Consider background job for final persistence after autosave

## Testing Strategy

**Unit Tests:**

- Validate request DTO mapping
- Test authentication/authorization checks
- Mock repository calls
- Verify error responses

**Integration Tests:**

- Create test assessment session
- Save progress multiple times
- Verify data persistence in database
- Test concurrent save attempts
- Test save with invalid session ID

**Manual Testing:**

- Start assessment in browser
- Answer several questions
- Verify autosave triggers
- Check database for saved state
- Refresh browser and verify state restored
