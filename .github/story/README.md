# Story Management - Copilot Agent Instructions

**Purpose:** Guide GitHub Copilot agents on how to approach, plan, and execute work from story specifications

**Last Updated:** 2025-10-25

---

## Overview

This directory contains **story specifications** - comprehensive blueprints for implementing features, fixing bugs, and improving the EduMind.AI platform. Each story is self-contained and provides everything an agent needs to successfully complete the work.

**Key Principle:** Stories are the **single source of truth**. This README provides meta-instructions on how to work with stories, not story details themselves.

---

## How to Work with Stories

### Step 1: Read the Story Specification

When assigned a story (e.g., Story 005), your first action is to read the complete `issue.md` file:

```bash
# Example: Working on Story 005 (Optimize Ollama LLM Performance)
Read: .github/story/p1-005-optimize-ollama-llm-performance/issue.md
```

**What you'll find:**

- **Problem Statement:** Why this work matters
- **Goals & Success Criteria:** What "done" looks like
- **Technical Approach:** Architecture and design decisions
- **Task Decomposition:** Step-by-step implementation tasks
- **Acceptance Criteria:** How to validate completion
- **Context & References:** Links to related docs

**Your job:** Understand the "why" before starting the "how"

---

### Step 2: Create Task Breakdown

Most stories include a **Task Decomposition** section with numbered tasks. Create a `tasks.md` file to track your progress:

```markdown
# Story 005: Optimize Ollama LLM Performance - Task Tracking

## Tasks

### Task 1: Create LLM Provider Abstraction
- **Status:** ✅ Complete
- **Files Modified:** 
  - `src/AcademicAssessment.Agents/Services/ILlmProvider.cs` (created)
- **Commit:** abc123def

### Task 2: Implement Ollama Provider
- **Status:** 🔄 In Progress
- **Current Step:** Writing unit tests
- **Blockers:** None

### Task 3: Implement Azure OpenAI Provider
- **Status:** ⏳ Not Started
- **Dependencies:** Task 1, Task 2
```

**Location:** Save as `.github/story/p1-005-optimize-ollama-llm-performance/tasks.md`

**Benefits:**

- Track progress incrementally
- Identify blockers early
- Provide status updates to users
- Resume work if interrupted

---

### Step 3: Work Incrementally

**Golden Rule:** Complete one task, commit, move to next

**Anti-Pattern:** Try to implement entire story in one massive commit

**Best Practice:**

1. **Read task description** - Understand what needs to be done
2. **Check dependencies** - Ensure prerequisite tasks are complete
3. **Implement changes** - Follow coding standards (`.github/coding-standards.md`)
4. **Write tests** - Cover new functionality (`.github/testing/`)
5. **Verify locally** - Run tests, check functionality
6. **Commit changes** - Clear, descriptive commit message
7. **Update task status** - Mark task complete in `tasks.md`
8. **Move to next task**

---

### Step 4: Follow Acceptance Criteria

Every story has **Acceptance Criteria** - a checklist that defines completion. Use this to validate your work:

```markdown
## Acceptance Criteria

- [ ] Azure OpenAI provider implemented
- [ ] Ollama provider refactored
- [ ] Provider selection via configuration
- [ ] All existing tests pass
- [ ] New tests written (80%+ coverage)
- [ ] Performance benchmark shows <5s inference
```

**Before marking a story complete:**

1. Check every box in acceptance criteria
2. Run full test suite
3. Verify in local environment
4. Update documentation if needed
5. Mark story as complete in `tasks.md`

---

## Story Structure

### Required Sections

Every story specification includes:

1. **Header**
   - Priority (P0/P1/P2)
   - Status (Ready/In Progress/Blocked/Done)
   - Effort estimate (Small/Medium/Large)
   - Dependencies (other stories required first)

2. **Problem Statement**
   - What's broken or missing?
   - Why does it matter?
   - Business impact

3. **Goals & Success Criteria**
   - What are we trying to achieve?
   - How do we measure success?

4. **Technical Approach**
   - Architecture decisions
   - Design patterns to use
   - Technology choices

5. **Task Decomposition**
   - Numbered, actionable tasks
   - Files to create/modify
   - Dependencies between tasks

6. **Acceptance Criteria**
   - Checklist of completion requirements
   - Testing requirements
   - Performance targets

7. **Context & References**
   - Links to related documentation
   - External references (libraries, papers, APIs)

### Optional Sections

- **Notes:** Additional context, risks, alternatives considered
- **Migration Strategy:** For breaking changes
- **Rollback Plan:** If deployment fails

---

## Story Priority System

### P0: Critical Blockers

**Definition:** Must fix before production launch (legal/compliance/core functionality)

**Examples:**

- Story 001: Fix integration test serialization bug
- Story 002: Implement k-anonymity privacy enforcement
- Story 003: Implement COPPA compliance
- Story 004: Implement GDPR data deletion

**Timeline:** Complete first (weeks 1-5)

### P1: Production Quality

**Definition:** Required for professional production deployment

**Examples:**

- Story 005: Optimize LLM performance
- Story 006: Student onboarding with Google OAuth
- Story 007: Multi-tenant physical isolation
- Story 008: Dedicated audit logging

**Timeline:** After P0 (weeks 6-11)

### P2: Enhancements

**Definition:** Improve scalability, observability, user experience

**Examples:**

- Story 010: Dynamic adaptive testing
- Story 011: Monitoring & alerting
- Story 014: E2E test automation
- Story 017: Mobile app investigation

**Timeline:** After P1 (weeks 12+)

---

## Story Workflow

### 1. Story Selection

**User assigns story:**
> "Work on Story 005: Optimize Ollama LLM Performance"

**Your first action:**

```bash
# Read the specification
cat .github/story/005/issue.md

# Check dependencies
grep "Dependencies:" .github/story/005/issue.md
```

### 2. Planning Phase

**Create task tracking file:**

```bash
# Copy task decomposition from story
cat .github/story/005/issue.md | grep -A 100 "## Task Decomposition" > .github/story/005/tasks.md

# Add status markers
# Edit to add: Status, Files Modified, Commits
```

**Review documentation:**

- Coding standards (`.github/coding-standards.md`)
- Testing guide (`.github/testing/`)
- Architecture docs (`docs/architecture/`)

### 3. Implementation Phase

**For each task:**

```bash
# 1. Mark task as in-progress
echo "Status: 🔄 In Progress" >> .github/story/005/tasks.md

# 2. Implement changes
# (follow task instructions)

# 3. Write tests
# (see .github/testing/ for patterns)

# 4. Run tests locally
dotnet test

# 5. Commit with clear message
git add .
git commit -m "feat: Implement Azure OpenAI provider (Story 005, Task 3)"

# 6. Mark task complete
echo "Status: ✅ Complete" >> .github/story/005/tasks.md
echo "Commit: $(git rev-parse --short HEAD)" >> .github/story/005/tasks.md
```

### 4. Validation Phase

**Before marking story complete:**

```bash
# 1. Run full test suite
dotnet test

# 2. Check acceptance criteria
cat .github/story/005/issue.md | grep -A 20 "## Acceptance Criteria"

# 3. Verify all boxes checked
# 4. Update story status
echo "Status: ✅ Complete" > .github/story/005/status.txt

# 5. Document completion
git tag "story-005-complete"
```

### 5. Documentation Phase

**Update relevant documentation:**

```bash
# If architecture changed
vim docs/architecture/ARCHITECTURE_SUMMARY.md

# If new operational procedures
vim docs/operations/MONITORING_RUNBOOK.md

# Commit documentation updates
git commit -m "docs: Update architecture for hybrid LLM strategy (Story 005)"
```

---

## Common Patterns

### Pattern 1: Database Schema Changes

**When:** Story requires new tables/columns

**Steps:**

1. Create Entity Framework migration
2. Update entity models
3. Update repository
4. Write integration tests
5. Document schema changes

**Example:** Story 002 (K-Anonymity) adds `student_anonymization` table

### Pattern 2: New API Endpoints

**When:** Story adds REST API functionality

**Steps:**

1. Create controller method
2. Implement service layer
3. Update repository if needed
4. Add authorization attributes
5. Write integration tests
6. Update OpenAPI spec

**Example:** Story 008 (Audit Logging) adds `/api/admin/audit-logs` endpoints

### Pattern 3: UI Components

**When:** Story requires new Blazor components

**Steps:**

1. Create component file (`.razor`)
2. Implement code-behind (`.razor.cs`)
3. Add to parent page
4. Write unit tests (bUnit)
5. Test responsive design

**Example:** Story 009 (Question Authoring) adds `MarkdownEditor.razor` component

### Pattern 4: Background Jobs

**When:** Story requires scheduled tasks

**Steps:**

1. Create `BackgroundService` class
2. Implement `ExecuteAsync` method
3. Register in DI container
4. Configure schedule
5. Add logging
6. Write integration tests

**Example:** Story 013 (Backup & DR) adds daily backup verification job

---

## Troubleshooting

### Issue: Tests Failing After Changes

**Diagnosis:**

```bash
dotnet test --verbosity detailed
# Look for specific test failures
```

**Common Causes:**

1. Breaking changes to interfaces
2. Missing test setup (database seeding)
3. Integration test serialization bug (Story 001)

**Solution:** See `.github/testing/12-troubleshooting.md`

### Issue: Story Blocked by Dependency

**Example:** Story 004 (GDPR Delete) depends on Story 002 (K-Anonymity)

**Action:**

1. Check if dependency story is complete
2. If not, work on dependency first
3. Update task status: "Status: ⏸️ Blocked (waiting for Story 002)"
4. Ask user if you should switch to dependency story

### Issue: Story Specification Unclear

**Example:** Task description missing key details

**Action:**

1. Ask clarifying questions
2. Document assumptions in `tasks.md`
3. Proceed with best interpretation
4. Flag for user review

---

## Best Practices

### Do's ✅

- **Read entire story** before starting implementation
- **Work incrementally** (one task at a time, frequent commits)
- **Write tests** for all new code (unit, integration, E2E)
- **Follow coding standards** (Railway-Oriented Programming, async/await, etc.)
- **Update documentation** when architecture changes
- **Ask questions** if story unclear
- **Track progress** in `tasks.md`

### Don'ts ❌

- **Don't skip acceptance criteria** - They define "done"
- **Don't make architectural decisions** not in story spec
- **Don't commit untested code** - Run tests first
- **Don't work on multiple stories** in parallel (context switching overhead)
- **Don't ignore dependencies** - Complete prerequisite stories first
- **Don't guess requirements** - Ask if unclear

---

## Quick Reference

### File Structure

```
.github/story/
├── README.md                                              # This file (meta-instructions)
├── p0-001-fix-integration-test-serialization-bug/
│   ├── issue.md                                           # Story specification (read this first!)
│   ├── tasks.md                                           # (Generated) Task tracking
│   └── notes.md                                           # (Optional) Implementation notes
├── p0-002-implement-k-anonymity-privacy-enforcement/
│   └── issue.md
├── p0-003-implement-coppa-compliance/
│   └── issue.md
├── p0-004-implement-gdpr-right-to-delete/
│   └── issue.md
...
```

**Folder Naming Convention:** `{priority}-{id}-{kebab-case-title}`

- **Priority:** `p0` (critical), `p1` (production quality), `p2` (enhancements)
- **ID:** Sequential 3-digit number (001-017)
- **Title:** Kebab-case short description

**Benefits:**

- Stories sorted by priority automatically
- Descriptive names make browsing easy
- No need to open files to understand what's inside

### Essential Commands

```bash
# List all stories
ls .github/story/*/issue.md

# Read story specification
cat .github/story/005/issue.md

# Check story dependencies
grep "Dependencies:" .github/story/*/issue.md

# Find stories by priority
grep "Priority: P0" .github/story/*/issue.md

# Check acceptance criteria
grep -A 10 "## Acceptance Criteria" .github/story/005/issue.md
```

### Key Documentation

- **Coding Standards:** `.github/coding-standards.md`
- **Testing Guide:** `.github/testing/README.md`
- **Architecture:** `docs/architecture/ARCHITECTURE_SUMMARY.md`
- **Deployment:** `.github/deployment/reference.md`

---

## Summary for Copilot Agents

**When user says: "Work on Story XXX"**

1. **Read** `.github/story/XXX/issue.md` completely
2. **Understand** problem, goals, technical approach
3. **Create** `tasks.md` file to track progress
4. **Implement** one task at a time, commit frequently
5. **Test** all changes (unit, integration, E2E)
6. **Validate** against acceptance criteria
7. **Document** changes if architecture impacted
8. **Mark complete** when all criteria met

**Remember:** Stories are self-contained. Everything you need is in `issue.md`. This README just teaches you the workflow.

---

## Next Steps

1. **Review & Prioritize:** Product owner reviews stories, confirms priorities
2. **Sprint Planning:** Break stories into 2-week sprints
3. **Start with Story 001:** Quick win to unblock testing
4. **Parallel Track P0:** Work on Stories 002-004 in parallel where possible
5. **Weekly Demos:** Show progress to stakeholders

---

## Document Maintenance

**Update Frequency:** Weekly during implementation  
**Owner:** Technical Lead  
**Last Updated:** 2025-10-25

**Story Status Legend:**

- ✅ Specification Complete
- 🔄 In Progress
- ⏸️ Blocked
- ✔️ Done
- 📋 Not Started

---

**Analysis Completed By:** GitHub Copilot  
**Analysis Date:** 2025-10-25  
**Version:** 1.0
