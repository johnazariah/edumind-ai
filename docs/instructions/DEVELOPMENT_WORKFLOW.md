# Development Workflow Guide

**Purpose:** Standard development practices, protocols, and hygiene for the EduMind.AI project  
**Audience:** All developers contributing to the codebase  
**Status:** Living document - update as workflows evolve

---

## Table of Contents

1. [Branch Naming Conventions](#branch-naming-conventions)
2. [Commit Message Standards](#commit-message-standards)
3. [Pull Request Process](#pull-request-process)
4. [Testing Requirements](#testing-requirements)
5. [Documentation Requirements](#documentation-requirements)
6. [Code Hygiene Checklist](#code-hygiene-checklist)
7. [Sprint Workflow](#sprint-workflow)

---

## Branch Naming Conventions

### Format

```
<type>/<short-description>
```

### Branch Types

- **`feature/`** - New features or enhancements
  - Example: `feature/orchestrator-decision-making`
  - Example: `feature/student-assessment-ui`

- **`bugfix/`** - Bug fixes
  - Example: `bugfix/circuit-breaker-timeout`
  - Example: `bugfix/routing-statistics-null-ref`

- **`docs/`** - Documentation only changes
  - Example: `docs/api-documentation`
  - Example: `docs/architecture-diagrams`

- **`refactor/`** - Code refactoring (no feature change)
  - Example: `refactor/repository-pattern`
  - Example: `refactor/clean-architecture`

- **`test/`** - Adding or fixing tests
  - Example: `test/integration-tests`
  - Example: `test/orchestrator-unit-tests`

- **`chore/`** - Maintenance, dependency updates, tooling
  - Example: `chore/update-dependencies`
  - Example: `chore/ci-pipeline-improvements`

### Best Practices

✅ **DO:**
- Use lowercase with hyphens
- Keep descriptions short and descriptive (2-4 words)
- Delete branches after merging

❌ **DON'T:**
- Use spaces or underscores
- Include issue numbers in branch name (put in commit message instead)
- Create branches from outdated main

### Creating a New Branch

```bash
# Ensure main is up to date
git checkout main
git pull origin main

# Create and switch to new branch
git checkout -b feature/your-feature-name

# Push branch to remote
git push -u origin feature/your-feature-name
```

---

## Commit Message Standards

### Format: Conventional Commits

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Commit Types

- **`feat:`** - New feature
- **`fix:`** - Bug fix
- **`docs:`** - Documentation changes
- **`style:`** - Code style changes (formatting, no logic change)
- **`refactor:`** - Code refactoring (no feature/bug change)
- **`test:`** - Adding or updating tests
- **`chore:`** - Maintenance, dependencies, tooling
- **`perf:`** - Performance improvements
- **`ci:`** - CI/CD pipeline changes
- **`build:`** - Build system or dependency changes

### Scope (Optional)

Indicates which part of codebase is affected:
- `orchestrator` - Orchestration layer
- `agents` - Agent implementations
- `analytics` - Analytics service
- `infrastructure` - Data layer
- `web` - Web API
- `dashboard` - Dashboard UI
- `tests` - Test suite

### Examples

**Good Commit Messages:**

```bash
feat(orchestrator): add circuit breaker pattern to task routing

Implements circuit breaker with 3-failure threshold and 5-minute timeout.
Includes exponential backoff retry logic.

Closes #42
```

```bash
fix(agents): resolve null reference in MathAgent question generation

The LLM service was returning null when rate limited, causing crashes.
Added proper error handling and fallback logic.
```

```bash
docs: add Week 1 Day 4 state persistence summary

Comprehensive documentation of database entities, repositories,
and state service implementation.
```

```bash
test(orchestrator): add unit tests for workflow execution

- Test dependency resolution
- Test parallel step execution
- Test retry logic with exponential backoff

Coverage increased from 85% to 92%
```

**Bad Commit Messages:**

```bash
❌ "Fixed stuff"
❌ "WIP"
❌ "Updated files"
❌ "Changes"
❌ "asdf"
```

### Commit Checklist

Before committing, ensure:

- [ ] Code compiles without errors
- [ ] All tests pass locally
- [ ] New code has appropriate tests
- [ ] Documentation updated (if needed)
- [ ] No debugging code (console.log, commented code)
- [ ] Commit message follows conventions

### Committing Changes

```bash
# Stage specific files
git add src/path/to/file.cs

# Or stage all changes (use carefully)
git add -A

# Commit with message
git commit -m "feat(orchestrator): add workflow state persistence"

# Push to remote
git push origin feature/your-branch-name
```

---

## Pull Request Process

### Before Creating a PR

1. **Ensure branch is up to date with main:**
   ```bash
   git checkout main
   git pull origin main
   git checkout your-branch
   git merge main
   # Resolve any conflicts
   ```

2. **Run full test suite:**
   ```bash
   dotnet build
   dotnet test
   ```

3. **Update documentation:**
   - Update TASK_JOURNAL.md with milestone
   - Create/update relevant technical docs
   - Update README.md if needed

### Creating the PR

Use GitHub CLI or web interface:

```bash
# Using GitHub CLI
gh pr create --title "feat: Complete orchestrator decision-making (Week 1 Day 1)" \
             --body "Implements intelligent agent selection with 4-factor priority scoring, IRT difficulty adjustment, and learning path optimization.

## Changes
- Added SelectAgent() method with scoring algorithm
- Implemented AdjustQuestionDifficulty() using IRT
- Added SequenceTopicsByPrerequisites() for learning paths
- Created 15 unit tests with 100% coverage

## Testing
- All 378 unit tests passing
- Build clean with no warnings

## Documentation
- Added WEEK1_DAY1_SUMMARY.md
- Updated TASK_JOURNAL.md

Closes #1" \
             --base main
```

### PR Title Format

```
<type>: <brief description> (<sprint reference>)
```

Examples:
- `feat: Complete orchestrator decision-making (Week 1 Day 1)`
- `fix: Resolve circuit breaker timeout issue`
- `docs: Consolidate and reorganize documentation`

### PR Description Template

```markdown
## Summary
Brief description of what this PR accomplishes.

## Changes
- Bullet point list of key changes
- Focus on "what" and "why"

## Testing
- Test results (# passing/failing)
- Coverage metrics (if applicable)
- Manual testing performed

## Documentation
- List of documentation files updated
- Link to relevant docs

## Related Issues
Closes #123
Relates to #456
```

### PR Review Checklist

**For PR Author:**
- [ ] All tests passing
- [ ] Code follows project conventions
- [ ] No merge conflicts
- [ ] Documentation updated
- [ ] TASK_JOURNAL.md updated
- [ ] Meaningful commit messages

**For Reviewers:**
- [ ] Code is understandable
- [ ] Tests cover new functionality
- [ ] No obvious bugs or security issues
- [ ] Documentation is clear
- [ ] Follows architectural patterns

### After PR Approval

```bash
# Squash and merge (preferred for feature branches)
gh pr merge --squash

# Delete branch after merge
git branch -d feature/your-branch-name
git push origin --delete feature/your-branch-name
```

---

## Testing Requirements

### Before Every Commit

```bash
# Build the solution
dotnet build

# Run unit tests
dotnet test tests/AcademicAssessment.Tests.Unit/

# Check for errors
dotnet build --no-incremental
```

### Test Coverage Requirements

- **Unit Tests:** Minimum 80% coverage for new code
- **Integration Tests:** Required for API endpoints and database operations
- **Critical Paths:** 100% coverage for orchestrator, routing, workflows

### Running Tests Locally

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/AcademicAssessment.Tests.Unit/

# Run with detailed output
dotnet test --verbosity detailed

# Run with coverage (if configured)
dotnet test /p:CollectCoverage=true
```

### Test Categories

1. **Unit Tests** (Fast, no dependencies)
   - Business logic
   - Calculations and algorithms
   - Model validation

2. **Integration Tests** (Slower, with dependencies)
   - API endpoints
   - Database operations
   - Agent communication

3. **End-to-End Tests** (Slowest, full system)
   - User workflows
   - Multi-agent scenarios
   - Real LLM integration

---

## Documentation Requirements

### When to Update Documentation

**Always update documentation when:**
- Adding new features or APIs
- Changing architecture or design patterns
- Completing sprint tasks or milestones
- Fixing bugs that reveal design issues
- Adding new dependencies or tools

### Required Documentation Updates

1. **TASK_JOURNAL.md** (MANDATORY)
   - Update after EVERY significant change
   - Add milestone entry with:
     - What was done
     - Files changed
     - Test results
     - Next steps

2. **Sprint Summaries** (For sprint work)
   - Create WEEKx_DAYy_SUMMARY.md for each day
   - Include:
     - Implementation details
     - Code metrics
     - Testing results
     - Lessons learned

3. **Technical Documentation** (As needed)
   - API changes → Update API_TESTING_GUIDE.md
   - Architecture changes → Update ARCHITECTURE_SUMMARY.md
   - New integrations → Create integration guide

### Documentation in Code

```csharp
/// <summary>
/// Brief description of what this method does.
/// </summary>
/// <param name="parameter">Parameter description</param>
/// <returns>Return value description</returns>
/// <remarks>
/// Additional context, algorithms used, or important notes.
/// </remarks>
public async Task<Result> MethodName(Parameter parameter)
{
    // Implementation
}
```

---

## Code Hygiene Checklist

### Before Committing

- [ ] **No commented-out code** (use git history instead)
- [ ] **No debug statements** (Console.WriteLine, Debug.Print)
- [ ] **No hardcoded values** (use configuration)
- [ ] **No TODOs** (create issues instead or fix immediately)
- [ ] **Consistent formatting** (use dotnet format)
- [ ] **Meaningful variable names** (no `temp`, `x`, `data`)
- [ ] **No unused imports** (remove with IDE)

### Code Style

```bash
# Format code automatically
dotnet format

# Check for formatting issues
dotnet format --verify-no-changes
```

### Code Review Self-Checklist

Before requesting review:
- [ ] Is this the simplest solution?
- [ ] Are all edge cases handled?
- [ ] Is error handling appropriate?
- [ ] Are there security concerns?
- [ ] Is it testable?
- [ ] Will this scale?
- [ ] Is it documented?

---

## Sprint Workflow

### Daily Workflow

1. **Start of Day:**
   ```bash
   # Pull latest changes
   git checkout main
   git pull origin main
   
   # Check current sprint status
   cat docs/planning/TASK_JOURNAL.md | head -100
   
   # Review sprint roadmap
   cat docs/planning/SPRINT_ROADMAP.md
   ```

2. **During Development:**
   - Work in small, focused commits
   - Run tests frequently
   - Update TASK_JOURNAL.md as you progress
   - Ask questions in PR comments if uncertain

3. **End of Day:**
   - Commit all work (even if WIP)
   - Update TASK_JOURNAL.md with progress
   - Create daily summary (if completing sprint task)
   - Push to remote for backup

### Sprint Task Workflow

For each sprint task (e.g., Week 1 Day 1):

1. **Planning Phase:**
   - Read sprint task description
   - Break down into subtasks
   - Estimate time and complexity

2. **Implementation Phase:**
   - Create feature branch
   - Implement functionality
   - Write tests as you go
   - Commit frequently with good messages

3. **Testing Phase:**
   - Run full test suite
   - Verify build is clean
   - Check code coverage
   - Manual testing if needed

4. **Documentation Phase:**
   - Create daily summary document
   - Update TASK_JOURNAL.md
   - Update technical docs
   - Document any decisions made

5. **Review Phase:**
   - Self-review code
   - Create PR with detailed description
   - Address review comments
   - Merge after approval

### Week Completion Workflow

At end of each week:

1. **Create Comprehensive PR:**
   - Include all week's work
   - Link all daily summaries
   - Highlight key achievements
   - Document any blockers

2. **Update Planning Documents:**
   - Mark week complete in SPRINT_ROADMAP.md
   - Update SPRINT_EXECUTIVE_SUMMARY.md
   - Add comprehensive entry to TASK_JOURNAL.md

3. **Retrospective:**
   - What went well?
   - What could improve?
   - Any process changes needed?

---

## Quick Reference

### Essential Commands

```bash
# Create new branch
git checkout -b feature/my-feature

# Stage and commit
git add -A
git commit -m "feat(scope): description"

# Push changes
git push origin feature/my-feature

# Create PR
gh pr create --fill

# Run tests
dotnet test

# Format code
dotnet format

# Build solution
dotnet build
```

### Key Documentation Files

- **Current Work:** `docs/planning/TASK_JOURNAL.md`
- **Sprint Plan:** `docs/planning/SPRINT_ROADMAP.md`
- **Copilot Context:** `docs/instructions/copilot-instructions.md`
- **Architecture:** `docs/architecture/ARCHITECTURE_SUMMARY.md`
- **Testing:** `docs/development/testing/TESTING_STRATEGY.md`

### Getting Help

1. Check TASK_JOURNAL.md for recent context
2. Review sprint roadmap for current priorities
3. Check KNOWN_ISSUES.md for common problems
4. Ask in PR comments for code-specific questions
5. Update documentation when you find answers

---

## Version History

- **v1.0** (October 16, 2025) - Initial workflow guide created during documentation consolidation
