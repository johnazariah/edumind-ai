# Task Tracker: Story 019 - Student App UI/UX Overhaul

**Story:** p0-019-student-app-ui-overhaul
**Priority:** P0
**Created:** 2025-10-25
**Status:** In Progress

---

## Current Task List

### Phase 1: Critical Fixes

- [x] Task 1: Blazor Interactivity Audit
  - Status: Completed
  - Notes: All interactive pages were scanned for `@onclick`, `@onchange`, `@bind`. Missing `@rendermode InteractiveServer` directives were added during Story 018 validation. See commits related to AssessmentSession/AssessmentDashboard/AssessmentDetail/AssessmentResults.

- [in-progress] Task 2: Fix Math Notation Rendering (KaTeX)
  - Status: In Progress
  - Work done:
    - Updated `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js` to fallback to `document.body` when no element passed.
    - Added ElementReference and markup changes in `AssessmentSession.razor` and `AssessmentSession.razor.cs` to render KaTeX only inside the question container.
  - Files changed:
    - `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js`
    - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor`
    - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
  - Acceptance: Inline and display math render correctly; math re-renders when navigating between questions.

- [ ] Task 3: Improve Mock Data Quality
  - Status: Not Started
  - Plan: Add `MockDataGenerator.cs` and update `src/AcademicAssessment.Web/Controllers/AssessmentController.cs` to serve subject-specific question banks (Mathematics, Chemistry, Biology).
  - Files to add/modify:
    - `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
    - `src/AcademicAssessment.Web/Services/MockDataGenerator.cs` (new)

### Phase 2: Complete Workflows

- [ ] Task 4: Implement Answer Review Page
  - Status: Not Started
  - Plan: Implement `AssessmentReview.razor` and server endpoint `GET /api/v1.0/Assessment/{assessmentId}/session/{sessionId}/review` to return questions, student answers, correct answers and explanations.
  - Files to add:
    - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentReview.razor`
    - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentReview.razor.cs`
    - Web API controller changes in `src/AcademicAssessment.Web`

- [ ] Task 5: UI Polish & Loading States
  - Status: Not Started
  - Plan: Add `LoadingSpinner.razor`, ensure buttons are disabled during async ops, add consistent button styles, and user-friendly error messages.
  - Files to modify: components and pages across StudentApp (AssessmentSession, AssessmentDetail, AssessmentDashboard, etc.)

- [ ] Task 6: End-to-End Workflow Testing (Playwright)
  - Status: Ready / Draft added
  - Work done:
    - Playwright suite added under `tests/e2e` with config and a sample `assessment.spec.ts` that exercises the full workflow (start → answer → submit → results).
    - Browsers installed in the dev container.
  - Files added:
    - `tests/e2e/package.json`
    - `tests/e2e/playwright.config.ts`
    - `tests/e2e/tests/assessment.spec.ts`
    - `tests/e2e/README.md`
  - How to run locally:

    ```bash
    # from repo root
    ./scripts/start-aspire.sh   # ensure Aspire and services are running
    cd tests/e2e
    npm install
    npm run install-browsers   # already run in dev container once
    npm test
    ```

  - Acceptance: Playwright E2E validates full assessment workflow across subjects and question types.

---

## Work Log / Commits

- 2025-10-25: Completed Story 018 (Aspire deployment parity) and added Story 019 skeleton.
- 2025-10-25: Added KaTeX fallback in `assessment-ui.js` and wired question container ElementReference in AssessmentSession to enable scoped rendering.
- 2025-10-25: Added Playwright E2E scaffold and sample test under `tests/e2e` and installed browsers.

---

## Notes & Next Actions

1. Task 2 (KaTeX) - Finish by adding optional `math-renderer.js` wrapper or expand `assessment-ui.js` with initialization promise to avoid race conditions on first render. Add a small sample math question in mock data for deterministic E2E tests.
2. Task 3 (Mock Data) - Implement `MockDataGenerator` to make Playwright tests deterministic and reduce flakiness.
3. Task 4 (Review) - Implement API endpoint and StudentApp page; wire navigation from Results to Review.
4. Task 6 (E2E) - Expand the Playwright suite to assert KaTeX rendering (check for `.katex` nodes), mark-for-review flows, and multi-question navigation.

---

## Change History

- This file was generated / updated by the agent on 2025-10-25 to persist Story 019 task state and context.
