# Story 010: Implement Dynamic Adaptive Testing

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Large (2-3 weeks)  
**Dependencies:** Story 009 (Question Authoring - need question pool with IRT parameters)


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/15

---

## Problem Statement

EduMind.AI currently uses **static assessments** where all students answer the same questions in the same order, regardless of ability level. This approach has significant drawbacks:

**Current Problems:**

- High-ability students waste time on easy questions
- Low-ability students get discouraged by too-hard questions
- Assessment length fixed (can't stop early when ability estimated)
- Inefficient: More questions needed for accurate ability estimate
- No real-time difficulty adjustment

**Adaptive Testing Advantages:**

- **Personalized:** Questions match student's current ability level
- **Efficient:** Fewer questions needed for same accuracy
- **Engaging:** Students challenged but not overwhelmed
- **Accurate:** Better ability estimation with targeted questions

**Business Impact:**

- Competitive advantage over static assessment platforms (Quizlet, Kahoot)
- Better learning outcomes → Higher student engagement
- Shorter assessments → More students complete
- Aligns with vision to compete with Duolingo (which uses adaptive algorithms)

---

## Goals & Success Criteria

### Functional Goals

1. **IRT-Based Question Selection**
   - Use Item Response Theory (IRT) to select next question
   - Match question difficulty to estimated student ability
   - Update ability estimate after each response

2. **Adaptive Stopping Rules**
   - Stop when ability estimated with sufficient confidence
   - Maximum question limit (prevent infinite assessments)
   - Minimum question requirement (ensure validity)

3. **Dynamic Difficulty Adjustment**
   - Start at medium difficulty (θ = 0)
   - Increase difficulty if student answers correctly
   - Decrease difficulty if student answers incorrectly
   - Target 50% success rate (optimal information zone)

### Non-Functional Goals

- **Fast:** Question selection in <100ms
- **Fair:** No bias against certain student groups
- **Transparent:** Students see progress bar based on confidence

### Success Criteria

- [ ] Adaptive assessments use 30% fewer questions than static for same accuracy
- [ ] Average assessment length: 10-15 questions (vs 20-25 static)
- [ ] Ability estimates within ±0.5θ of true ability (validated against static tests)
- [ ] Question selection algorithm runs in <100ms
- [ ] 80% of students report adaptive tests "feel personalized"

---

## Technical Approach

### Item Response Theory (IRT) Primer

**IRT Model:** 3-Parameter Logistic (3PL)

```
P(θ) = c + (1 - c) / (1 + exp(-a(θ - b)))

Where:
- θ (theta) = Student ability (standardized, mean=0, SD=1)
- a = Discrimination (how well question differentiates ability levels, 0-3)
- b = Difficulty (ability level where P=50%, range -3 to +3)
- c = Guessing (probability of correct guess, 0-0.5 for multiple choice)
- P(θ) = Probability student with ability θ answers correctly
```

**Example:**

- Easy question: b = -1.5 (most students answer correctly)
- Medium question: b = 0 (50% of average students correct)
- Hard question: b = +1.5 (only high-ability students correct)

**Fisher Information:** Measures how much a question reduces uncertainty about ability

```
I(θ) = a² * P'(θ)² / (P(θ) * (1 - P(θ)))

Highest information when:
- Question difficulty matches student ability (b ≈ θ)
- High discrimination (a > 1.5)
```

### Adaptive Algorithm

**Step 1: Initialize**

```
θ₀ = 0            # Start at average ability
SE₀ = 1.0         # Initial standard error (high uncertainty)
min_questions = 5
max_questions = 20
target_SE = 0.3   # Stop when standard error < 0.3
```

**Step 2: Select Next Question**

```python
def select_next_question(theta, answered_questions):
    # Get unanswered questions
    pool = get_unanswered_questions()
    
    # Calculate Fisher information for each question
    information = []
    for q in pool:
        info = fisher_information(theta, q.a, q.b, q.c)
        information.append((q, info))
    
    # Select question with maximum information
    best_question = max(information, key=lambda x: x[1])[0]
    return best_question
```

**Step 3: Update Ability Estimate (Maximum Likelihood)**

```python
def update_ability(theta, responses):
    # Use Newton-Raphson method
    for _ in range(10):  # Iterate until convergence
        L = sum_log_likelihood(theta, responses)
        dL = sum_log_likelihood_derivative(theta, responses)
        theta = theta - L / dL
        if abs(L) < 0.01:
            break
    return theta
```

**Step 4: Calculate Standard Error**

```python
def calculate_standard_error(theta, responses):
    I = sum(fisher_information(theta, r.a, r.b, r.c) for r in responses)
    SE = 1 / sqrt(I)
    return SE
```

**Step 5: Stopping Rule**

```python
def should_stop(n_questions, SE):
    if n_questions < min_questions:
        return False
    if n_questions >= max_questions:
        return True
    if SE < target_SE:
        return True
    return False
```

---

## Task Decomposition

### Task 1: Add IRT Parameters to Question Entity

- **Description:** Extend Question entity with IRT parameters
- **Files to Modify:**
  - `src/AcademicAssessment.Core/Entities/Question.cs`
- **New Properties:**

  ```csharp
  public sealed record Question
  {
      // ... existing properties ...
      
      // IRT Parameters
      public double Difficulty { get; init; }      // b parameter (-3 to +3)
      public double Discrimination { get; init; }  // a parameter (0 to 3)
      public double Guessing { get; init; }        // c parameter (0 to 0.5)
      
      public bool HasIrtParameters => Difficulty != 0 || Discrimination != 0;
  }
  ```

- **Migration:** Add columns to `questions` table
- **Acceptance:** Questions can store IRT parameters
- **Dependencies:** None

### Task 2: Create IRT Calculation Service

- **Description:** Implement IRT probability and information functions
- **Files to Create:**
  - `src/AcademicAssessment.Core/Services/IrtCalculationService.cs`
- **Methods:**

  ```csharp
  public interface IIrtCalculationService
  {
      // Calculate probability of correct response
      double CalculateProbability(double theta, double a, double b, double c);
      
      // Calculate Fisher information
      double CalculateFisherInformation(double theta, double a, double b, double c);
      
      // Calculate test information (sum of item information)
      double CalculateTestInformation(double theta, IEnumerable<QuestionResponse> responses);
      
      // Calculate standard error of measurement
      double CalculateStandardError(double theta, IEnumerable<QuestionResponse> responses);
  }
  ```

- **Implementation:** 3PL model formulas
- **Acceptance:** Unit tests validate IRT calculations
- **Dependencies:** Task 1

### Task 3: Create Ability Estimation Service

- **Description:** Estimate student ability (θ) using Maximum Likelihood
- **Files to Create:**
  - `src/AcademicAssessment.Core/Services/AbilityEstimationService.cs`
- **Algorithm:** Newton-Raphson method
- **Methods:**

  ```csharp
  public interface IAbilityEstimationService
  {
      // Estimate ability given responses
      Task<Result<AbilityEstimate>> EstimateAbilityAsync(
          IEnumerable<QuestionResponse> responses,
          CancellationToken cancellationToken = default);
      
      // Update ability after new response
      Task<Result<AbilityEstimate>> UpdateAbilityAsync(
          double currentTheta,
          IEnumerable<QuestionResponse> responses,
          CancellationToken cancellationToken = default);
  }
  
  public sealed record AbilityEstimate
  {
      public double Theta { get; init; }        // Estimated ability
      public double StandardError { get; init; } // Measurement error
      public double Information { get; init; }   // Total test information
      public int Iterations { get; init; }       // Convergence iterations
  }
  ```

- **Acceptance:** Ability estimated correctly (validated against known datasets)
- **Dependencies:** Task 2

### Task 4: Create Adaptive Question Selection Service

- **Description:** Select next question based on current ability estimate
- **Files to Create:**
  - `src/AcademicAssessment.Core/Services/AdaptiveQuestionSelectionService.cs`
- **Algorithm:** Maximum Fisher Information
- **Methods:**

  ```csharp
  public interface IAdaptiveQuestionSelectionService
  {
      // Select next question for student
      Task<Result<Question>> SelectNextQuestionAsync(
          Guid assessmentId,
          double currentTheta,
          IEnumerable<Guid> answeredQuestionIds,
          CancellationToken cancellationToken = default);
      
      // Check if assessment should stop
      Task<Result<bool>> ShouldStopAssessmentAsync(
          int questionCount,
          double standardError,
          AdaptiveStoppingRules rules,
          CancellationToken cancellationToken = default);
  }
  
  public sealed record AdaptiveStoppingRules
  {
      public int MinQuestions { get; init; } = 5;
      public int MaxQuestions { get; init; } = 20;
      public double TargetStandardError { get; init; } = 0.3;
  }
  ```

- **Acceptance:** Selects optimal questions based on ability
- **Dependencies:** Tasks 2-3

### Task 5: Add Adaptive Configuration to Assessment Entity

- **Description:** Allow assessments to be configured as adaptive
- **Files to Modify:**
  - `src/AcademicAssessment.Core/Entities/Assessment.cs`
- **New Properties:**

  ```csharp
  public sealed record Assessment
  {
      // ... existing properties ...
      
      public bool IsAdaptive { get; init; }
      public AdaptiveConfiguration? AdaptiveConfig { get; init; }
  }
  
  public sealed record AdaptiveConfiguration
  {
      public double InitialTheta { get; init; } = 0.0;
      public int MinQuestions { get; init; } = 5;
      public int MaxQuestions { get; init; } = 20;
      public double TargetStandardError { get; init; } = 0.3;
      public bool ShowProgressBar { get; init; } = true;
  }
  ```

- **Acceptance:** Assessments can be marked as adaptive
- **Dependencies:** None

### Task 6: Create Student Ability Tracking Table

- **Description:** Store historical ability estimates for each student
- **Files to Create:**
  - Migration: `add_student_ability_tracking_table`
- **Schema:**

  ```sql
  CREATE TABLE student_ability_tracking (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      student_id UUID NOT NULL REFERENCES students(id),
      assessment_id UUID NOT NULL REFERENCES assessments(id),
      subject VARCHAR(100) NOT NULL,
      
      theta DOUBLE PRECISION NOT NULL,          -- Estimated ability
      standard_error DOUBLE PRECISION NOT NULL, -- Measurement error
      information DOUBLE PRECISION NOT NULL,    -- Test information
      
      question_count INT NOT NULL,              -- Questions answered
      correct_count INT NOT NULL,               -- Correct answers
      
      estimated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      
      UNIQUE(student_id, assessment_id)
  );
  
  CREATE INDEX idx_student_ability_student ON student_ability_tracking(student_id);
  CREATE INDEX idx_student_ability_subject ON student_ability_tracking(subject);
  ```

- **Acceptance:** Student ability estimates persisted
- **Dependencies:** Task 5

### Task 7: Update Assessment Taking Service for Adaptive Mode

- **Description:** Modify assessment taking flow to support adaptive selection
- **Files to Modify:**
  - `src/AcademicAssessment.Core/Services/AssessmentTakingService.cs`
- **Changes:**

  ```csharp
  public async Task<Result<Question>> GetNextQuestionAsync(
      Guid assessmentId,
      Guid studentId,
      CancellationToken cancellationToken = default)
  {
      var assessment = await _assessmentRepo.GetByIdAsync(assessmentId, ct);
      
      if (!assessment.IsAdaptive)
      {
          // Static mode: Return next sequential question
          return GetNextStaticQuestion(assessmentId, studentId, ct);
      }
      else
      {
          // Adaptive mode: Use IRT selection
          var responses = await GetStudentResponses(assessmentId, studentId, ct);
          
          // Estimate current ability
          var abilityResult = await _abilityEstimation.EstimateAbilityAsync(responses, ct);
          
          // Select next question
          var answeredIds = responses.Select(r => r.QuestionId);
          return await _questionSelection.SelectNextQuestionAsync(
              assessmentId,
              abilityResult.Theta,
              answeredIds,
              ct);
      }
  }
  
  public async Task<Result<bool>> ShouldEndAssessmentAsync(
      Guid assessmentId,
      Guid studentId,
      CancellationToken cancellationToken = default)
  {
      var assessment = await _assessmentRepo.GetByIdAsync(assessmentId, ct);
      
      if (!assessment.IsAdaptive)
      {
          // Static: End when all questions answered
          return await AllQuestionsAnswered(assessmentId, studentId, ct);
      }
      else
      {
          // Adaptive: Check stopping rules
          var responses = await GetStudentResponses(assessmentId, studentId, ct);
          var ability = await _abilityEstimation.EstimateAbilityAsync(responses, ct);
          
          return await _questionSelection.ShouldStopAssessmentAsync(
              responses.Count(),
              ability.StandardError,
              assessment.AdaptiveConfig!,
              ct);
      }
  }
  ```

- **Acceptance:** Adaptive assessments work end-to-end
- **Dependencies:** Tasks 3-4

### Task 8: Update Assessment Results to Include Ability Estimate

- **Description:** Store and display ability estimate in results
- **Files to Modify:**
  - `src/AcademicAssessment.Core/Entities/AssessmentResult.cs`
- **New Properties:**

  ```csharp
  public sealed record AssessmentResult
  {
      // ... existing properties ...
      
      // Adaptive Testing Metrics
      public double? AbilityEstimate { get; init; }      // θ (theta)
      public double? StandardError { get; init; }        // SE(θ)
      public double? TestInformation { get; init; }      // I(θ)
      public bool WasAdaptive { get; init; }
  }
  ```

- **Acceptance:** Results show ability estimate for adaptive tests
- **Dependencies:** Task 7

### Task 9: Update Dashboard UI for Adaptive Assessment Creation

- **Description:** Allow teachers to configure assessments as adaptive
- **Files to Modify:**
  - `src/AcademicAssessment.Dashboard/Pages/Assessments/CreateAssessmentPage.razor`
- **UI Changes:**

  ```
  [x] Enable Adaptive Testing
  
  If enabled:
    Initial Ability:   [0.0] (slider -3 to +3)
    Min Questions:     [5]  (number input)
    Max Questions:     [20] (number input)
    Target Precision:  [0.3] (slider 0.1 to 0.5)
    [ ] Show Progress Bar
    
  Note: Requires question pool with IRT parameters
  ```

- **Acceptance:** Teachers can create adaptive assessments
- **Dependencies:** Task 5

### Task 10: Update Student UI to Show Adaptive Progress

- **Description:** Show confidence-based progress bar for adaptive tests
- **Files to Modify:**
  - `src/AcademicAssessment.StudentApp/Pages/AssessmentTaking.razor`
- **UI Changes:**

  ```
  Static Assessment:
    Progress: [======>           ] 7/15 questions
  
  Adaptive Assessment:
    Confidence: [=========>      ] 75% confident
    Questions: 7 answered (5-20 range)
  ```

- **Calculation:** `confidence = 1 - (SE / initial_SE)`
- **Acceptance:** Students see progress based on confidence
- **Dependencies:** Task 7

### Task 11: Create IRT Parameter Estimation Tool

- **Description:** Tool to estimate IRT parameters from historical data
- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Services/IrtParameterEstimationService.cs`
- **Purpose:** Estimate a, b, c parameters for questions without IRT data
- **Algorithm:** Marginal Maximum Likelihood (MML) or EM algorithm
- **Usage:** Run on existing questions with response data
- **Acceptance:** Reasonable IRT parameters estimated for all questions
- **Dependencies:** Tasks 1-2

### Task 12: Create Admin Tool to Bulk Update IRT Parameters

- **Description:** Dashboard page to run IRT calibration
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Pages/Admin/IrtCalibrationPage.razor`
- **Features:**
  - Show questions without IRT parameters
  - Button: "Estimate IRT Parameters"
  - Progress indicator
  - Results table: Question ID, estimated a, b, c
  - Bulk save button
- **Acceptance:** Admin can calibrate IRT parameters
- **Dependencies:** Task 11

### Task 13: Write Unit Tests for IRT Calculations

- **Description:** Validate IRT formulas against known results
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.Unit/Services/IrtCalculationServiceTests.cs`
- **Test Cases:**

  ```csharp
  [Theory]
  [InlineData(0, 1, 0, 0, 0.5)]    // θ=0, at difficulty, P=50%
  [InlineData(1, 1, 0, 0, 0.73)]   // θ=1, above difficulty, P=73%
  [InlineData(-1, 1, 0, 0, 0.27)]  // θ=-1, below difficulty, P=27%
  public void CalculateProbability_ReturnsExpected(
      double theta, double a, double b, double c, double expected)
  
  [Fact]
  public void FisherInformation_MaxAtDifficulty()
      // Information highest when θ ≈ b
  
  [Fact]
  public void AbilityEstimation_ConvergesToTrueAbility()
      // Given known responses, estimate θ correctly
  ```

- **Acceptance:** All tests pass with ±0.01 tolerance
- **Dependencies:** Tasks 2-3

### Task 14: Write Integration Tests for Adaptive Assessments

- **Description:** Test end-to-end adaptive assessment flow
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.Integration/AdaptiveTesting/AdaptiveAssessmentTests.cs`
- **Test Cases:**

  ```csharp
  [Fact]
  public async Task AdaptiveAssessment_SelectsEasierQuestionAfterIncorrect()
  
  [Fact]
  public async Task AdaptiveAssessment_SelectsHarderQuestionAfterCorrect()
  
  [Fact]
  public async Task AdaptiveAssessment_StopsAtTargetPrecision()
  
  [Fact]
  public async Task AdaptiveAssessment_StopsAtMaxQuestions()
  
  [Fact]
  public async Task AdaptiveAssessment_RequiresMinimumQuestions()
  ```

- **Acceptance:** All tests pass
- **Dependencies:** Task 7

### Task 15: Create Performance Comparison Report

- **Description:** Compare adaptive vs static assessment performance
- **Files to Create:**
  - `docs/research/ADAPTIVE_TESTING_VALIDATION.md`
- **Experiments:**
  1. **Question Count:** Adaptive vs Static for same accuracy
  2. **Accuracy:** Ability estimate error for different lengths
  3. **Student Satisfaction:** Survey after adaptive test
- **Metrics:**
  - Average questions: Adaptive vs Static
  - RMSE (ability estimate error)
  - Student ratings (5-point scale)
- **Acceptance:** Document shows adaptive testing advantages
- **Dependencies:** Task 14

---

## Acceptance Criteria

### Functional Requirements

- [ ] IRT parameters stored for all questions
- [ ] Ability estimation service works correctly
- [ ] Adaptive question selection uses maximum information
- [ ] Stopping rules enforced (min/max questions, target SE)
- [ ] Teachers can create adaptive assessments via Dashboard
- [ ] Students see confidence-based progress bar
- [ ] Ability estimate shown in results

### Performance Requirements

- [ ] Question selection in <100ms
- [ ] Ability estimation converges in <10 iterations

### Accuracy Requirements

- [ ] Adaptive tests use 30% fewer questions than static (same accuracy)
- [ ] Ability estimates within ±0.5θ of true ability

### User Experience Requirements

- [ ] 80% of students rate adaptive tests "feel personalized" (≥4/5)

---

## Context & References

### Documentation

- [System Specification - Adaptive Testing](.github/specification/04-detailed-design.md#adaptive-testing)

### Academic References

- Lord, F.M. (1980). *Applications of Item Response Theory to Practical Testing Problems*
- Embretson, S.E., & Reise, S.P. (2000). *Item Response Theory for Psychologists*
- Wainer, H. (2000). *Computerized Adaptive Testing: A Primer*

### External Libraries

- [CAT-SIM (Python IRT Library)](https://github.com/douglasrizzo/catsim)
- [mirt (R IRT Package)](https://github.com/philchalmers/mirt)

---

## Notes

- **IRT Calibration:** Requires historical response data (at least 200 responses per question)
- **Cold Start:** New questions start with default IRT parameters until calibrated
- **Fairness:** Monitor for bias (DIF - Differential Item Functioning) across demographics
- **Research Opportunity:** Publish paper on adaptive testing in K-12 education

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
