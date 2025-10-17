# Week 1 Day 1 - Kickoff Checklist

**Date:** October 16, 2025  
**Focus:** Complete Orchestrator Logic - Day 1  
**Goal:** Implement orchestrator decision-making algorithm

## Morning Setup (30 minutes)

### ✅ Environment Verification

- [ ] Aspire running successfully

  ```bash
  dotnet run --project src/EduMind.AppHost --launch-profile https
  ```

  - Dashboard: <https://localhost:17126>
  - Web API: <https://localhost:5001>

- [ ] Integration tests passing (40/59)

  ```bash
  dotnet test tests/AcademicAssessment.Tests.Integration
  ```

- [ ] Git status clean

  ```bash
  git status
  git checkout -b feature/orchestrator-logic
  ```

### ✅ Project Board Setup

- [ ] Create GitHub Project (if not done)

  ```bash
  gh project create --owner johnazariah --title "EduMind AI - 6 Week Sprint"
  ```

- [ ] Create Week 1 issues

  ```bash
  # Use scripts/create-sprint-issues.sh or create manually
  ```

- [ ] Move Task 1.1 to "In Progress"

  ```bash
  gh issue list --repo johnazariah/edumind-ai --label "week-1"
  ```

## Main Work: Task 1.1 - Orchestrator Decision-Making (5-6 hours)

### Current State Analysis

**File:** `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`

Review current implementation:

```bash
# Open the file and review
code src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs

# Check existing methods
grep -n "async Task" src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs
```

### Implementation Tasks

#### 1. Agent Selection Algorithm (2 hours)

- [ ] Review student performance data structure

  ```bash
  code src/AcademicAssessment.Core/Models/StudentPerformance.cs
  ```

- [ ] Implement `SelectOptimalAgent()` method

  ```csharp
  private async Task<string> SelectOptimalAgent(
      Guid studentId,
      Subject subject,
      StudentPerformance performance)
  {
      // Logic:
      // 1. Identify weakest subject area
      // 2. Match to appropriate subject agent
      // 3. Consider agent availability
      // 4. Return agent ID
  }
  ```

- [ ] Add performance-based scoring
  - Weight recent performance higher
  - Consider error patterns
  - Account for time spent per question

- [ ] Write unit tests

  ```bash
  # Create test file if not exists
  code tests/AcademicAssessment.Orchestration.Tests/StudentProgressOrchestratorTests.cs
  ```

#### 2. Difficulty Adjustment Logic (2 hours)

- [ ] Implement `AdjustDifficulty()` method

  ```csharp
  private double AdjustDifficulty(
      StudentPerformance performance,
      double currentDifficulty)
  {
      // Use IRT (Item Response Theory) principles
      // - If accuracy > 80%: increase difficulty by 0.2
      // - If accuracy < 50%: decrease difficulty by 0.2
      // - If 50-80%: slight adjustments based on velocity
  }
  ```

- [ ] Add ability estimate tracking
  - Implement theta (θ) estimation
  - Track per subject
  - Update after each assessment

- [ ] Implement adaptive question selection
  - Match question difficulty to ability estimate
  - Optimize information gain
  - Balance subject coverage

- [ ] Write unit tests for edge cases
  - New student (no history)
  - Perfect performance
  - Poor performance
  - Inconsistent performance

#### 3. Learning Path Optimization (1-2 hours)

- [ ] Implement `OptimizeLearningPath()` method

  ```csharp
  private async Task<LearningPath> OptimizeLearningPath(
      Guid studentId,
      StudentPerformance performance)
  {
      // 1. Identify knowledge gaps
      // 2. Determine prerequisite topics
      // 3. Create ordered learning sequence
      // 4. Estimate time to mastery
  }
  ```

- [ ] Add prerequisite topic mapping
  - Load curriculum dependencies
  - Build topic graph
  - Use topological sort for ordering

- [ ] Implement mastery prediction
  - Estimate questions needed for mastery
  - Predict time to proficiency
  - Calculate confidence intervals

- [ ] Write integration test
  - Test with sample student data
  - Verify learning path makes sense
  - Check prerequisite ordering

### Code Review Checklist

Before committing, verify:

- [ ] All methods have XML documentation comments
- [ ] Logging added at key decision points
- [ ] Error handling for edge cases
- [ ] No hardcoded values (use configuration)
- [ ] Unit tests written and passing
- [ ] Code follows C# conventions
- [ ] No compiler warnings
- [ ] Performance considerations documented

### Testing Commands

```bash
# Run orchestrator-specific tests
dotnet test tests/AcademicAssessment.Orchestration.Tests/ --filter "FullyQualifiedName~StudentProgressOrchestrator"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Check for code smells
dotnet format --verify-no-changes
```

## End of Day Activities (30 minutes)

### ✅ Commit Work

```bash
# Review changes
git status
git diff

# Commit with descriptive message
git add src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs
git add tests/AcademicAssessment.Orchestration.Tests/StudentProgressOrchestratorTests.cs
git commit -m "feat: Implement orchestrator decision-making algorithm

- Add agent selection based on student performance
- Implement difficulty adjustment using IRT principles
- Add learning path optimization with prerequisite handling
- Include unit tests with >80% coverage

Refs #<issue-number>"

# Push to remote
git push origin feature/orchestrator-logic
```

### ✅ Update Progress

```bash
# Update issue with progress
gh issue comment <issue-number> --body "✅ Day 1 Progress:
- Agent selection algorithm implemented
- Difficulty adjustment logic complete
- Learning path optimization done
- Unit tests passing

Ready for code review tomorrow."

# If complete, move to "In Review"
gh issue edit <issue-number> --remove-label "in-progress" --add-label "in-review"
```

### ✅ Prepare for Tomorrow

- [ ] Review Task 1.2: Complete task routing implementation
- [ ] Identify any blockers or dependencies
- [ ] Schedule code review if needed
- [ ] Update sprint board

## Standup Notes Template

**What I did today:**

- Implemented orchestrator decision-making algorithm
- Added agent selection based on performance metrics
- Implemented IRT-based difficulty adjustment
- Created learning path optimization with prerequisites

**What I'll do tomorrow:**

- Task 1.2: Complete task routing implementation
- Implement RouteTaskToAgent() method
- Add capability matching and fallback logic

**Blockers:**

- None / [List any blockers]

## Useful Commands

```bash
# Quick test of orchestrator
dotnet run --project src/AcademicAssessment.Web
curl -k https://localhost:5001/health

# Check Aspire dashboard
open https://localhost:17126

# View logs
docker logs <container-id>

# Check database
dotnet ef database update --project src/AcademicAssessment.Infrastructure
```

## Resources

- **Roadmap:** `/docs/SPRINT_ROADMAP.md`
- **Test Status:** `/docs/TEST_STATUS.md`
- **Architecture:** `/docs/ARCHITECTURE_SUMMARY.md`
- **IRT Theory:** <https://en.wikipedia.org/wiki/Item_response_theory>
- **Adaptive Testing:** <https://en.wikipedia.org/wiki/Computerized_adaptive_testing>

---

**Let's build! 🚀**

**Expected Time:** 6-7 hours + meetings  
**Complexity:** High (core algorithm)  
**Risk:** Medium (requires understanding of IRT)  
**Impact:** Critical (foundation for intelligent assessment)
