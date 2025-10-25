# Story 006: Student Onboarding with Google OAuth

**Priority:** P1 - Production Quality  
**Status:** Ready for Implementation  
**Effort:** Medium (1-2 weeks)  
**Dependencies:** Story 003 (COPPA Compliance - for age verification)

---

## Problem Statement

While Azure AD B2C with Google OAuth is technically configured, the **student onboarding experience is incomplete and unpolished**. First-time users signing up with Google face:

- No guided onboarding flow after authentication
- Missing profile completion steps (grade, school, interests)
- No welcome tutorial or product orientation
- Unclear error messages for edge cases
- No age verification integration (COPPA requirement)
- Poor mobile responsiveness on signup
- No analytics tracking for conversion optimization

**Current State:**

- Google OAuth redirects to Azure AD B2C → Returns JWT token → User lands on empty dashboard
- No profile setup, no welcome experience, no guidance

**User Impact:**

- High drop-off rate after signup (users don't understand next steps)
- Confusion about how to find/start assessments
- Missing critical profile data (grade level, school) blocks proper assessment delivery
- Poor first impression hurts retention

---

## Goals & Success Criteria

### Goals

- Create delightful first-time user experience
- Collect required profile information (grade, school, date of birth)
- Integrate COPPA age verification for users <13
- Provide welcome tutorial explaining key features
- Ensure mobile-responsive onboarding flow
- Track onboarding funnel metrics for optimization
- Handle edge cases gracefully (duplicate accounts, errors)

### Success Criteria

- [ ] 90%+ of new Google sign-ups complete profile setup
- [ ] Average onboarding time <3 minutes
- [ ] Mobile-friendly onboarding (responsive design)
- [ ] Age verification triggers parental consent flow (<13 users)
- [ ] Clear error messages with resolution steps
- [ ] Welcome tutorial completion rate >70%
- [ ] Analytics tracking for each onboarding step
- [ ] Zero orphaned accounts (incomplete profiles)

---

## Technical Approach

### Onboarding Flow Architecture

```
Google Sign-In Flow:
1. User clicks "Sign in with Google"
2. Azure AD B2C redirects to Google OAuth
3. User authorizes EduMind.AI
4. Google returns to Azure AD B2C with profile (email, name, photo)
5. Azure AD B2C issues JWT token with claims
6. StudentApp receives token and user claims
7. Check if profile complete:
   - If YES → Redirect to dashboard
   - If NO → Redirect to onboarding wizard

Onboarding Wizard Steps:
Step 1: Welcome & Name Confirmation
Step 2: Date of Birth (triggers COPPA if <13)
Step 3: Grade Level Selection
Step 4: School Selection (search or create)
Step 5: Interests & Goals (optional)
Step 6: Welcome Tutorial (product tour)
Step 7: Dashboard redirect

If Age <13:
- Pause onboarding after Step 2
- Request parent email
- Trigger Story 003 parental consent flow
- Account inactive until consent granted
```

### UI Components

**New Blazor Components:**

- `OnboardingWizard.razor` - Multi-step form container
- `WelcomeStep.razor` - Welcome message + name confirmation
- `DateOfBirthStep.razor` - Age verification
- `GradeLevelStep.razor` - Grade selection (6-12)
- `SchoolSelectionStep.razor` - Search/select school
- `InterestsStep.razor` - Subject interests (optional)
- `WelcomeTutorial.razor` - Interactive product tour
- `OnboardingProgress.razor` - Step indicator

**Reusable Components:**

- `StepIndicator.razor` - Visual progress bar
- `SchoolAutocomplete.razor` - Typeahead search for schools
- `GradeSelector.razor` - Grade level dropdown with icons

---

## Task Decomposition

### Task 1: Create Onboarding Domain Models

- **Description:** Define domain entities for tracking onboarding progress
- **Files:**
  - `src/AcademicAssessment.Core/Entities/OnboardingSession.cs` (new)
  - `src/AcademicAssessment.Core/Enums/OnboardingStep.cs` (new)
  - `src/AcademicAssessment.Core/Enums/OnboardingStatus.cs` (new)
- **Code:**

  ```csharp
  public record OnboardingSession
  {
      public Guid Id { get; init; }
      public Guid UserId { get; init; }
      public OnboardingStatus Status { get; init; }
      public OnboardingStep CurrentStep { get; init; }
      public DateTime StartedAt { get; init; }
      public DateTime? CompletedAt { get; init; }
      public Dictionary<string, string>? StepData { get; init; } // JSON storage
      public string? ReferralSource { get; init; }
  }
  
  public enum OnboardingStep
  {
      Welcome = 1,
      DateOfBirth = 2,
      GradeLevel = 3,
      SchoolSelection = 4,
      Interests = 5,
      Tutorial = 6,
      Complete = 7
  }
  
  public enum OnboardingStatus
  {
      InProgress,
      PendingParentalConsent,
      Completed,
      Abandoned
  }
  ```

- **Acceptance:** Models compiled, follow domain-driven design patterns
- **Dependencies:** None

### Task 2: Create Database Migration for Onboarding Tracking

- **Description:** Add onboarding_sessions table for funnel analytics
- **Files:**
  - `src/AcademicAssessment.Infrastructure/Data/Migrations/AddOnboardingTracking.cs` (new)
- **SQL:**

  ```sql
  CREATE TABLE onboarding_sessions (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
      status VARCHAR(30) NOT NULL DEFAULT 'InProgress',
      current_step VARCHAR(30) NOT NULL,
      started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      completed_at TIMESTAMP,
      step_data JSONB,
      referral_source VARCHAR(100),
      CONSTRAINT chk_onboarding_status CHECK (status IN ('InProgress', 'PendingParentalConsent', 'Completed', 'Abandoned'))
  );
  
  CREATE INDEX idx_onboarding_sessions_user ON onboarding_sessions(user_id);
  CREATE INDEX idx_onboarding_sessions_status ON onboarding_sessions(status);
  
  -- Add profile completion flag to students table
  ALTER TABLE students ADD COLUMN profile_completed BOOLEAN DEFAULT FALSE;
  ALTER TABLE students ADD COLUMN onboarding_completed_at TIMESTAMP;
  ```

- **Acceptance:** Migration applies successfully, indexes created
- **Dependencies:** Task 1

### Task 3: Implement OnboardingService

- **Description:** Business logic for onboarding workflow management
- **Files:**
  - `src/AcademicAssessment.Core/Services/IOnboardingService.cs` (new)
  - `src/AcademicAssessment.Infrastructure/Services/OnboardingService.cs` (new)
- **Methods:**

  ```csharp
  public interface IOnboardingService
  {
      /// <summary>
      /// Start onboarding session for new user
      /// </summary>
      Task<Result<OnboardingSession>> StartOnboardingAsync(Guid userId, string? referralSource, CancellationToken cancellationToken);
      
      /// <summary>
      /// Check if user needs onboarding
      /// </summary>
      Task<Result<bool>> RequiresOnboardingAsync(Guid userId, CancellationToken cancellationToken);
      
      /// <summary>
      /// Save progress for current step
      /// </summary>
      Task<Result> SaveStepDataAsync(Guid sessionId, OnboardingStep step, Dictionary<string, string> data, CancellationToken cancellationToken);
      
      /// <summary>
      /// Advance to next step
      /// </summary>
      Task<Result<OnboardingStep>> AdvanceToNextStepAsync(Guid sessionId, CancellationToken cancellationToken);
      
      /// <summary>
      /// Complete onboarding and activate account
      /// </summary>
      Task<Result> CompleteOnboardingAsync(Guid sessionId, CancellationToken cancellationToken);
      
      /// <summary>
      /// Get onboarding analytics (admin only)
      /// </summary>
      Task<Result<OnboardingMetrics>> GetOnboardingMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
  }
  ```

- **Key Logic:**
  - Check profile_completed flag on Students table
  - If false → Start onboarding session
  - Track step progression in onboarding_sessions
  - Trigger COPPA flow if age <13
  - Mark profile_completed = true on completion
- **Acceptance:** Service implements full onboarding lifecycle
- **Dependencies:** Task 2

### Task 4: Build OnboardingWizard Component (Container)

- **Description:** Main wizard container with step routing
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/OnboardingWizard.razor` (new)
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/OnboardingWizard.razor.cs` (new)
- **Features:**
  - Multi-step form with navigation
  - Step validation before advancing
  - Progress indicator (visual stepper)
  - Back button (with data preservation)
  - Auto-save on step completion
  - Mobile-responsive layout
- **Razor:**

  ```razor
  @page "/onboarding"
  @attribute [Authorize]
  @inject IOnboardingService OnboardingService
  @inject NavigationManager Navigation
  
  <div class="onboarding-container">
      <div class="onboarding-header">
          <StepIndicator CurrentStep="@currentStep" TotalSteps="7" />
      </div>
      
      <div class="onboarding-content">
          @switch (currentStep)
          {
              case OnboardingStep.Welcome:
                  <WelcomeStep OnNext="HandleWelcomeNext" />
                  break;
              case OnboardingStep.DateOfBirth:
                  <DateOfBirthStep OnNext="HandleDateOfBirthNext" OnBack="GoBack" />
                  break;
              case OnboardingStep.GradeLevel:
                  <GradeLevelStep OnNext="HandleGradeLevelNext" OnBack="GoBack" />
                  break;
              case OnboardingStep.SchoolSelection:
                  <SchoolSelectionStep OnNext="HandleSchoolSelectionNext" OnBack="GoBack" />
                  break;
              case OnboardingStep.Interests:
                  <InterestsStep OnNext="HandleInterestsNext" OnBack="GoBack" OnSkip="SkipInterests" />
                  break;
              case OnboardingStep.Tutorial:
                  <WelcomeTutorial OnComplete="HandleTutorialComplete" OnSkip="SkipTutorial" />
                  break;
          }
      </div>
  </div>
  ```

- **Acceptance:** Wizard navigates between steps, preserves data
- **Dependencies:** Task 3

### Task 5: Build WelcomeStep Component

- **Description:** Welcome message + name confirmation
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/WelcomeStep.razor` (new)
- **UI:**

  ```razor
  <div class="welcome-step">
      <img src="/images/logo-large.svg" alt="EduMind.AI" class="logo" />
      <h1>Welcome to EduMind.AI!</h1>
      <p class="subtitle">Your personalized learning companion</p>
      
      <div class="name-confirmation">
          <label>Is this your name?</label>
          <input type="text" @bind="studentName" class="form-control" />
          <small class="text-muted">We got this from your Google account</small>
      </div>
      
      <button @onclick="OnNext" class="btn btn-primary btn-lg">
          Let's Get Started →
      </button>
  </div>
  ```

- **Acceptance:** Displays Google name, allows editing, advances to next step
- **Dependencies:** Task 4

### Task 6: Build DateOfBirthStep Component with Age Verification

- **Description:** Date of birth input with COPPA age check
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/DateOfBirthStep.razor` (new)
- **Integration:**
  - Use `AgeVerificationService` from Story 003
  - If age <13 → Redirect to parental consent flow
  - If age ≥13 → Continue to next step
- **UI:**

  ```razor
  <div class="dob-step">
      <h2>When's your birthday?</h2>
      <p>We need this to provide age-appropriate content</p>
      
      <div class="date-picker">
          <select @bind="birthMonth">
              <option value="">Month</option>
              <option value="1">January</option>
              <!-- ... -->
          </select>
          <select @bind="birthDay">
              <option value="">Day</option>
              <!-- 1-31 -->
          </select>
          <select @bind="birthYear">
              <option value="">Year</option>
              <!-- 1995-2018 -->
          </select>
      </div>
      
      @if (showCoppaMessage)
      {
          <div class="alert alert-info">
              <p>Since you're under 13, we'll need your parent's permission to continue.</p>
          </div>
      }
      
      <div class="button-group">
          <button @onclick="OnBack" class="btn btn-secondary">← Back</button>
          <button @onclick="HandleNext" class="btn btn-primary">Continue →</button>
      </div>
  </div>
  ```

- **Logic:**

  ```csharp
  private async Task HandleNext()
  {
      var dateOfBirth = new DateOnly(birthYear, birthMonth, birthDay);
      var age = ageVerificationService.CalculateAge(dateOfBirth);
      
      await OnboardingService.SaveStepDataAsync(sessionId, OnboardingStep.DateOfBirth, 
          new Dictionary<string, string> { ["dateOfBirth"] = dateOfBirth.ToString() });
      
      if (age < 13)
      {
          // Trigger COPPA parental consent flow
          Navigation.NavigateTo("/onboarding/parental-consent-required");
      }
      else
      {
          await OnNext.InvokeAsync();
      }
  }
  ```

- **Acceptance:** Age verification works, COPPA flow triggered correctly
- **Dependencies:** Task 4, Story 003 (COPPA)

### Task 7: Build GradeLevelStep Component

- **Description:** Grade level selection (6-12)
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/GradeLevelStep.razor` (new)
- **UI:**

  ```razor
  <div class="grade-step">
      <h2>What grade are you in?</h2>
      <p>This helps us show you the right content</p>
      
      <div class="grade-selector">
          @foreach (var grade in grades)
          {
              <div class="grade-card @(selectedGrade == grade ? "selected" : "")"
                   @onclick="() => SelectGrade(grade)">
                  <div class="grade-icon">@GetGradeIcon(grade)</div>
                  <div class="grade-label">@grade</div>
              </div>
          }
      </div>
      
      <div class="button-group">
          <button @onclick="OnBack" class="btn btn-secondary">← Back</button>
          <button @onclick="HandleNext" class="btn btn-primary" disabled="@(selectedGrade == null)">
              Continue →
          </button>
      </div>
  </div>
  ```

- **Validation:** Grade must be selected before advancing
- **Acceptance:** Grade selection works, visual feedback on selection
- **Dependencies:** Task 4

### Task 8: Build SchoolSelectionStep Component with Search

- **Description:** Search and select school with autocomplete
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/SchoolSelectionStep.razor` (new)
  - `src/AcademicAssessment.Web/Controllers/SchoolsController.cs` (add search endpoint)
- **Features:**
  - Typeahead search (min 3 characters)
  - Display school name, city, state
  - "My school isn't listed" option
  - Create new school request (admin approval required)
- **API:**

  ```csharp
  [HttpGet("api/schools/search")]
  public async Task<IActionResult> SearchSchools([FromQuery] string query, CancellationToken cancellationToken)
  {
      var schools = await schoolRepository.SearchAsync(query, limit: 10, cancellationToken);
      return Ok(schools);
  }
  ```

- **UI:**

  ```razor
  <div class="school-step">
      <h2>Which school do you attend?</h2>
      
      <div class="school-search">
          <input type="text" 
                 @bind-value="searchQuery" 
                 @bind-value:event="oninput"
                 @onkeyup="HandleSearchInput"
                 placeholder="Start typing your school name..."
                 class="form-control" />
          
          @if (searchResults.Any())
          {
              <ul class="search-results">
                  @foreach (var school in searchResults)
                  {
                      <li @onclick="() => SelectSchool(school)">
                          <strong>@school.Name</strong>
                          <small>@school.City, @school.State</small>
                      </li>
                  }
              </ul>
          }
      </div>
      
      <button @onclick="ShowSchoolNotListed" class="btn btn-link">
          My school isn't listed
      </button>
      
      <div class="button-group">
          <button @onclick="OnBack" class="btn btn-secondary">← Back</button>
          <button @onclick="HandleNext" class="btn btn-primary" disabled="@(selectedSchool == null)">
              Continue →
          </button>
      </div>
  </div>
  ```

- **Acceptance:** Search works, school selection persists, handles "not listed"
- **Dependencies:** Task 4

### Task 9: Build InterestsStep Component (Optional)

- **Description:** Subject interests and learning goals (skippable)
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/InterestsStep.razor` (new)
- **UI:**

  ```razor
  <div class="interests-step">
      <h2>What subjects interest you?</h2>
      <p class="text-muted">Optional - helps us personalize your experience</p>
      
      <div class="subject-grid">
          @foreach (var subject in subjects)
          {
              <div class="subject-card @(selectedSubjects.Contains(subject) ? "selected" : "")"
                   @onclick="() => ToggleSubject(subject)">
                  <i class="@GetSubjectIcon(subject)"></i>
                  <span>@subject</span>
              </div>
          }
      </div>
      
      <div class="goals-section">
          <label>What do you want to achieve? (optional)</label>
          <textarea @bind="goals" placeholder="E.g., improve math grades, prepare for SAT..." rows="3"></textarea>
      </div>
      
      <div class="button-group">
          <button @onclick="OnSkip" class="btn btn-link">Skip for now</button>
          <button @onclick="OnBack" class="btn btn-secondary">← Back</button>
          <button @onclick="HandleNext" class="btn btn-primary">Continue →</button>
      </div>
  </div>
  ```

- **Acceptance:** Interests saved, skip button works, optional field behavior
- **Dependencies:** Task 4

### Task 10: Build WelcomeTutorial Component (Product Tour)

- **Description:** Interactive tutorial highlighting key features
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Onboarding/WelcomeTutorial.razor` (new)
- **Tutorial Steps:**
  1. "Find assessments on your dashboard"
  2. "Track your progress over time"
  3. "Get instant AI-powered feedback"
  4. "See where you can improve"
- **UI Library:** Use [Shepherd.js](https://shepherdjs.dev/) or custom modal
- **Features:**
  - Step-by-step overlay
  - Highlight dashboard elements
  - Skip tutorial option
  - Completion tracking
- **Acceptance:** Tutorial displays correctly, can skip, completion tracked
- **Dependencies:** Task 4

### Task 11: Implement Onboarding Analytics Tracking

- **Description:** Track funnel metrics for optimization
- **Files:**
  - `src/AcademicAssessment.StudentApp/Services/OnboardingAnalytics.cs` (new)
- **Events to Track:**
  - `onboarding_started` (source: google_oauth)
  - `onboarding_step_completed` (step: welcome, dob, grade, etc.)
  - `onboarding_step_abandoned` (last_step: X)
  - `onboarding_completed` (duration_seconds: X)
  - `coppa_consent_required` (age: X)
- **Implementation:**

  ```csharp
  public class OnboardingAnalytics
  {
      private readonly ILogger<OnboardingAnalytics> _logger;
      
      public void TrackStepCompleted(Guid userId, OnboardingStep step, TimeSpan duration)
      {
          _logger.LogInformation(
              "Onboarding step completed: UserId={UserId}, Step={Step}, Duration={Duration}s",
              userId, step, duration.TotalSeconds);
          
          // TODO: Send to Application Insights as custom event
      }
      
      public async Task<OnboardingMetrics> GetFunnelMetrics(DateTime startDate, DateTime endDate)
      {
          // Query onboarding_sessions table
          // Calculate:
          // - Total started
          // - Completion rate per step
          // - Average time per step
          // - Drop-off points
          // - COPPA percentage
      }
  }
  ```

- **Acceptance:** Events logged, queryable in Application Insights
- **Dependencies:** Task 3

### Task 12: Add Authentication Redirect Logic

- **Description:** Detect incomplete profiles and redirect to onboarding
- **Files:**
  - `src/AcademicAssessment.StudentApp/Pages/Index.razor` (modify)
  - `src/AcademicAssessment.StudentApp/Authentication/OnboardingRedirectMiddleware.cs` (new)
- **Logic:**

  ```csharp
  protected override async Task OnInitializedAsync()
  {
      var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
      var user = authState.User;
      
      if (user.Identity?.IsAuthenticated == true)
      {
          var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
          var requiresOnboarding = await OnboardingService.RequiresOnboardingAsync(userId);
          
          if (requiresOnboarding.Value)
          {
              Navigation.NavigateTo("/onboarding");
          }
          else
          {
              // Normal dashboard flow
          }
      }
  }
  ```

- **Acceptance:** New users redirected to onboarding, returning users see dashboard
- **Dependencies:** Task 3, Task 4

### Task 13: Style Onboarding Components (Mobile-Responsive)

- **Description:** CSS styling for professional, mobile-friendly onboarding
- **Files:**
  - `src/AcademicAssessment.StudentApp/wwwroot/css/onboarding.css` (new)
- **Design Requirements:**
  - Responsive breakpoints (mobile, tablet, desktop)
  - Clean, modern aesthetic
  - Consistent with Dashboard design system
  - Accessible (WCAG 2.1 AA)
  - Smooth transitions between steps
  - Loading states for API calls
- **Key Styles:**

  ```css
  .onboarding-container {
      max-width: 600px;
      margin: 0 auto;
      padding: 2rem;
  }
  
  .step-indicator {
      display: flex;
      justify-content: space-between;
      margin-bottom: 2rem;
  }
  
  .grade-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 1.5rem;
      border: 2px solid #e0e0e0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.3s ease;
  }
  
  .grade-card.selected {
      border-color: #1976d2;
      background-color: #e3f2fd;
  }
  
  @media (max-width: 768px) {
      .onboarding-container {
          padding: 1rem;
      }
      
      .grade-selector {
          grid-template-columns: repeat(2, 1fr);
      }
  }
  ```

- **Acceptance:** UI looks professional, works on mobile/tablet/desktop
- **Dependencies:** Task 4-10

### Task 14: Write Unit Tests for OnboardingService

- **Description:** Test onboarding business logic
- **Files:**
  - `tests/AcademicAssessment.Tests.Unit/Services/OnboardingServiceTests.cs` (new)
- **Test Cases:**
  - Start onboarding → Creates session
  - Requires onboarding → Returns true for incomplete profiles
  - Save step data → Updates session JSONB
  - Advance step → Increments current_step
  - Complete onboarding → Sets profile_completed = true
  - Age <13 → Triggers COPPA flow
- **Acceptance:** 90%+ coverage for OnboardingService
- **Dependencies:** Task 3

### Task 15: Write Integration Tests for Onboarding Flow

- **Description:** End-to-end onboarding workflow tests
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/Onboarding/OnboardingWorkflowTests.cs` (new)
- **Test Scenarios:**
  1. New Google user → Redirected to onboarding
  2. Complete all steps → Profile marked complete
  3. User <13 → COPPA consent flow triggered
  4. Skip optional steps → Still completes onboarding
  5. Abandon onboarding → Session marked abandoned
  6. Resume onboarding → Picks up where left off
- **Acceptance:** All critical paths tested
- **Dependencies:** Task 12

### Task 16: Update Documentation

- **Description:** Document onboarding implementation
- **Files:**
  - `.github/specification/11a-student-workflows.md` (update)
  - `docs/onboarding/STUDENT_ONBOARDING.md` (new)
  - `README.md` (update features list)
- **Content:**

  ```markdown
  ## Student Onboarding Flow
  
  **Status:** ✅ Implemented (2025-10-25)
  
  New students signing up with Google OAuth experience a guided 7-step onboarding:
  
  1. Welcome & name confirmation
  2. Date of birth (age verification)
  3. Grade level selection
  4. School selection (with search)
  5. Interests & goals (optional)
  6. Welcome tutorial (product tour)
  7. Dashboard activation
  
  ### Features
  - Mobile-responsive design
  - COPPA integration (parental consent for <13)
  - Auto-save progress
  - Funnel analytics tracking
  - Skip optional steps
  - Resume capability
  
  ### Metrics
  - Average completion time: 2.5 minutes
  - Completion rate: 92%
  - Drop-off analysis in Application Insights
  ```

- **Acceptance:** Onboarding documented, examples provided
- **Dependencies:** Task 15

---

## Acceptance Criteria (Validation)

### Functional Testing

1. **New Google User Flow:**
   - Sign in with Google (new account)
   - Expected: Redirect to onboarding wizard
   - Complete all steps
   - Expected: Land on dashboard with complete profile

2. **Under-13 User (COPPA):**
   - Enter date of birth showing age 12
   - Expected: COPPA message displayed
   - Expected: Parental consent email sent
   - Expected: Account inactive until consent

3. **Mobile Responsiveness:**
   - Open onboarding on iPhone Safari
   - Expected: UI scales correctly
   - Expected: All buttons clickable
   - Expected: No horizontal scrolling

4. **Resume Onboarding:**
   - Start onboarding, complete 3 steps
   - Close browser
   - Sign in again
   - Expected: Resume at step 4

### UX Testing

- [ ] Onboarding feels natural and engaging
- [ ] Error messages are clear and helpful
- [ ] Loading states show during API calls
- [ ] Back button preserves entered data
- [ ] Skip button clearly labeled on optional steps
- [ ] Tutorial can be dismissed or skipped

### Analytics Verification

- [ ] All onboarding events logged
- [ ] Funnel visualization in Application Insights
- [ ] Drop-off analysis by step
- [ ] Average completion time tracked
- [ ] COPPA trigger rate tracked

---

## Context & References

### Documentation

- [Student Workflows](.github/specification/11a-student-workflows.md)
- [Authentication Setup](docs/deployment/AUTHENTICATION_SETUP.md)
- [COPPA Compliance](Story 003)

### Design References

- [Duolingo Onboarding](https://www.duolingo.com) - Gamified, step-by-step
- [Khan Academy Sign-Up](https://www.khanacademy.org) - Education-focused
- [Quizlet Onboarding](https://quizlet.com) - Student-friendly

### Related Code

- `src/AcademicAssessment.StudentApp/Pages/Index.razor` - Landing page
- Azure AD B2C configuration (existing)

---

## Notes

- **First Impressions Matter:** Onboarding is the first user interaction—must be polished
- **Mobile-First:** Many students access from phones—design for mobile first
- **COPPA Integration:** Age verification must seamlessly trigger parental consent
- **Analytics are Key:** Track funnel to optimize conversion rates
- **A/B Testing:** Consider future A/B tests on step order, copy, visuals

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
