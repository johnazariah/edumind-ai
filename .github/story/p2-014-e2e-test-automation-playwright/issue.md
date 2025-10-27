# Story 014: E2E Test Automation with Playwright

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Large (2 weeks)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/19

---

## Problem Statement

Only unit and integration tests exist. No end-to-end tests for critical user workflows. Cannot catch UI bugs or integration issues before production.

**Current State:**

- No automated UI testing
- Manual QA for every release (slow, error-prone)
- Regressions slip through to production
- Cannot confidently deploy on Friday

**Business Impact:** Production bugs impact users, manual testing bottleneck slows releases.

---

## Goals & Success Criteria

1. **Playwright E2E tests** for 10 critical workflows
2. **Run in CI/CD** pipeline (block merges if tests fail)
3. **Multiple browsers** (Chrome, Firefox, Safari/WebKit)
4. **Visual regression testing** (screenshot comparison)
5. **80%+ coverage** of critical paths

**Success Criteria:**

- [ ] 10 critical workflows automated
- [ ] Tests run in GitHub Actions on every PR
- [ ] Visual regression detection working
- [ ] Test execution <10 minutes
- [ ] Zero flaky tests

---

## Technical Approach

### Playwright Setup

**Project Structure:**

```
tests/
└── AcademicAssessment.Tests.E2E/
    ├── Pages/              # Page Object Models
    ├── Tests/              # Test specs
    ├── Fixtures/           # Test data
    └── playwright.config.ts
```

**Browsers:** Chromium, Firefox, WebKit (parallel execution)

### Critical Workflows to Test

1. **Student Onboarding** (Story 006)
   - Google OAuth sign-in
   - Complete 7-step wizard
   - Land on dashboard with profile

2. **Take Assessment**
   - Select assessment
   - Answer 5 questions (different types)
   - Submit assessment
   - View results

3. **Teacher Create Question** (Story 009)
   - Create multiple choice question
   - Add KaTeX equation
   - Save and preview

4. **View Analytics** (existing feature)
   - Navigate to analytics
   - Filter by date range
   - Download CSV report

5. **COPPA Parental Consent** (Story 003)
   - Student <13 registers
   - Parent receives email
   - Parent approves
   - Student account activated

6. **GDPR Data Deletion** (Story 004)
   - Request data deletion
   - 7-day grace period
   - Confirm deletion
   - Account removed

7. **Adaptive Assessment** (Story 010)
   - Start adaptive test
   - Questions adjust to ability
   - See confidence progress bar
   - Complete assessment

8. **Mobile Responsive UI**
   - Resize to tablet/mobile
   - All pages usable
   - Touch interactions work

9. **Error Handling**
   - API timeout
   - Network offline
   - Invalid input
   - Graceful error messages

10. **Admin Tenant Management** (Story 007)
    - Create new tenant
    - Configure settings
    - Pause tenant
    - Delete tenant

---

## Task Decomposition

### Task 1: Set Up Playwright Project

- **Install:** `npm install -D @playwright/test`
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.E2E/playwright.config.ts`
  - `tests/AcademicAssessment.Tests.E2E/package.json`
- **Config:** Browsers, base URL, timeouts, screenshots
- **Acceptance:** Playwright runs simple test

### Task 2: Create Page Object Models

- **Files to Create:**
  - `LoginPage.ts`
  - `DashboardPage.ts`
  - `AssessmentTakingPage.ts`
  - `QuestionEditorPage.ts`
  - `AnalyticsPage.ts`
- **Pattern:** Encapsulate page interactions
- **Acceptance:** POM classes reusable across tests

### Task 3: Write E2E Tests for 10 Workflows

- **Files to Create:** 10 test spec files (one per workflow)
- **Example:**

  ```typescript
  test('Student completes onboarding', async ({ page }) => {
    await loginPage.loginWithGoogle();
    await onboardingWizard.enterName('John Doe');
    await onboardingWizard.selectGrade('10');
    await onboardingWizard.complete();
    expect(await dashboard.isVisible()).toBe(true);
  });
  ```

- **Acceptance:** All 10 workflows pass

### Task 4: Add Visual Regression Testing

- **Tool:** Playwright's `expect(page).toHaveScreenshot()`
- **Capture:** Baseline screenshots for key pages
- **Compare:** Fail if UI changes unexpectedly
- **Acceptance:** Visual regression tests pass

### Task 5: Integrate with GitHub Actions

- **Files to Create:**
  - `.github/workflows/e2e-tests.yml`
- **Trigger:** On pull request, before merge
- **Steps:**
  1. Start application (Docker Compose)
  2. Run Playwright tests
  3. Upload test results/screenshots
  4. Block merge if tests fail
- **Acceptance:** Tests run automatically in CI

### Task 6: Add Test Data Fixtures

- **Files to Create:**
  - `tests/AcademicAssessment.Tests.E2E/Fixtures/students.json`
  - `tests/AcademicAssessment.Tests.E2E/Fixtures/questions.json`
- **Seed:** Database with test data before each test
- **Cleanup:** Reset database after each test
- **Acceptance:** Tests isolated and repeatable

### Task 7: Handle Authentication in Tests

- **Challenge:** Google OAuth doesn't work in automated tests
- **Solution:** Mock OAuth or use test accounts
- **Implementation:** Playwright auth state persistence
- **Acceptance:** Tests can authenticate without manual intervention

### Task 8: Optimize Test Performance

- **Parallel Execution:** Run tests across multiple workers
- **Selective Tests:** Run smoke tests on every PR, full suite nightly
- **Timeouts:** Aggressive timeouts (30s max per test)
- **Acceptance:** Full test suite runs in <10 minutes

### Task 9: Document E2E Testing Guide

- **Files to Create:**
  - `tests/AcademicAssessment.Tests.E2E/README.md`
- **Content:**
  - How to run tests locally
  - How to add new tests
  - Troubleshooting flaky tests
  - Best practices
- **Acceptance:** New developers can run and write tests

---

## Acceptance Criteria

- [ ] 10 critical workflows automated with Playwright
- [ ] Tests run in GitHub Actions on every PR
- [ ] Visual regression tests configured
- [ ] Test execution <10 minutes
- [ ] Zero flaky tests (3 consecutive runs pass)
- [ ] 80%+ coverage of critical user paths
- [ ] Tests block merges if failing

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot
