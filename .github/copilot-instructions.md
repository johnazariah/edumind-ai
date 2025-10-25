# GitHub Copilot Agent Instructions

**Version:** 1.0.0  
**Last Updated:** October 25, 2025

This document provides instructions for GitHub Copilot agents working on the EduMind.AI Academic Assessment Multi-Agent System.

---

## Table of Contents

1. [Development Workflow](#development-workflow)
2. [Spec-Driven Development](#spec-driven-development)
3. [Documentation References](#documentation-references)
4. [Task Execution Guidelines](#task-execution-guidelines)

---

## 1. Development Workflow

### 1.1 Spec-Driven Development Model

**All development work follows a specification-first approach:**

1. **Specification Creation:** Every feature, bug fix, or enhancement starts with a specification document
2. **Story Organization:** Work is organized into stories in `.github/story/{story-id}/`
3. **Task Decomposition:** Specifications guide agents to break work into trackable tasks
4. **Iterative Execution:** Tasks are completed incrementally with frequent commits
5. **Continuous Documentation:** Work artifacts and decisions are captured

### 1.2 Story Structure

```
.github/story/
├── 001/
│   ├── issue.md          # Main specification
│   ├── tasks.md          # (Generated) Task breakdown
│   └── notes.md          # (Optional) Implementation notes
├── 002/
│   └── issue.md
└── 003/
    └── issue.md
```

### 1.3 Your Role as a Copilot Agent

When you receive a request to work on EduMind.AI:

1. **Check for active story:** Look for `.github/story/{current-id}/issue.md`
2. **Read the specification:** Understand context, goals, constraints
3. **Follow task instructions:** The spec tells you how to decompose work
4. **Execute incrementally:** Complete one task, commit, move to next
5. **Update documentation:** Keep specs and notes current as work progresses

---

## 2. Spec-Driven Development

### 2.1 What is a Specification?

A specification (`issue.md`) is a **comprehensive blueprint** that contains:

- **Problem Statement:** What problem are we solving and why?
- **Goals & Success Criteria:** What does "done" look like?
- **Technical Approach:** Architecture, design decisions, constraints
- **Task Decomposition Instructions:** How to break this into implementable tasks
- **Acceptance Criteria:** How to validate the work is complete
- **Context & Dependencies:** Links to related docs, APIs, requirements

### 2.2 Reading a Specification

When you read a spec, extract:

1. **The "Why":** Business context and user value
2. **The "What":** Functional and technical requirements
3. **The "How":** Architectural approach and design patterns
4. **The "Tasks":** Specific instructions on breaking down work
5. **The "Done":** Definition of complete and acceptance tests

### 2.3 Creating Tasks from Specifications

The specification will instruct you on task creation. Typically:

```markdown
## Task Decomposition

Create the following tasks in order:

### Task 1: [Brief Title]
- **Description:** What needs to be done
- **Files to modify:** List of files
- **Acceptance:** How to verify completion
- **Dependencies:** Previous tasks required

### Task 2: [Brief Title]
...
```

**Your job:** Create a `tasks.md` file with this breakdown, then work through tasks sequentially.

### 2.4 Task Execution Pattern

For each task:

1. **Read task description:** Understand what's required
2. **Check dependencies:** Ensure previous tasks complete
3. **Implement changes:** Follow coding standards and patterns
4. **Write tests:** Cover new functionality
5. **Verify locally:** Run tests, check functionality
6. **Commit changes:** Clear, descriptive commit message
7. **Update task status:** Mark as complete in tasks.md
8. **Move to next task**

---

## 3. Documentation References

### 3.1 System Understanding

For comprehensive system understanding, refer to:

- **[System Specification](.github/specification/README.md)** - Complete system architecture and requirements
- **[Architecture Summary](docs/architecture/ARCHITECTURE_SUMMARY.md)** - High-level system design
- **[Solution Structure](docs/architecture/SOLUTION_STRUCTURE.md)** - Project organization

### 3.2 Development Standards

For coding conventions and best practices:

- **[Coding Standards](.github/coding-standards.md)** - C#, API, Blazor, Infrastructure patterns
- **[Testing Guide](.github/testing/README.md)** - Comprehensive testing documentation

### 3.3 Operational Guides

For deployment and operations:

- **[Deployment Reference](.github/deployment/reference.md)** - Technical deployment documentation
- **[Deployment Playbooks](.github/deployment/playbook/README.md)** - Scenario-based operational guides

### 3.4 When to Consult Documentation

| Situation | Reference Document |
|-----------|-------------------|
| Understanding system architecture | `.github/specification/02-system-architecture.md` |
| Learning domain models | `.github/specification/03-domain-model.md` |
| Writing C# code | `.github/coding-standards.md` |
| Creating tests | `.github/testing/03-unit-testing.md` |
| API development | `.github/coding-standards.md#api-development` |
| Blazor components | `.github/coding-standards.md#blazor-development` |
| Deployment issues | `.github/deployment/reference.md` |
| Security/privacy requirements | `.github/specification/07-security-privacy.md` |

**Important:** Always consult documentation before implementing. Don't guess patterns or conventions.

---

## 4. Task Execution Guidelines

### 4.1 Code Quality Standards

All code must follow:

- **[Coding Standards](.github/coding-standards.md)** for language-specific conventions
- **Railway-Oriented Programming** with `Result<T>` for error handling
- **Async/await** patterns with CancellationToken support
- **Immutable records** for domain models
- **Required properties** for mandatory fields
- **XML documentation** for public APIs

### 4.2 Testing Requirements

All new code requires tests (see [Testing Guide](.github/testing/README.md)):

- **Unit tests** for business logic (70%+ coverage)
- **Integration tests** for API endpoints
- **E2E tests** for critical user workflows
- **AI tests** for agent functionality (use stubs for speed)

### 4.3 Git Workflow

**Commit Conventions:**

```
Present tense, imperative mood:
✅ "Add user authentication endpoint"
✅ "Fix: Resolve HttpClient disposal issue"
✅ "docs: Update API documentation"

❌ "Added authentication" (past tense)
❌ "Fixing bug" (continuous tense)
```

**Commit Frequency:**

- Commit after completing each task
- Small, focused commits are better than large ones
- Each commit should pass tests

### 4.4 Communication

When working on a story:

- **Ask clarifying questions** if specifications are unclear
- **Document decisions** in story notes.md
- **Report blockers** immediately
- **Suggest improvements** to specifications if you identify issues

### 4.5 Error Handling

If you encounter issues:

1. **Check documentation first:** Most patterns are documented
2. **Review existing code:** Look for similar implementations
3. **Consult troubleshooting guides:** `.github/testing/12-troubleshooting.md`
4. **Ask for guidance:** If truly blocked, ask user

---

## 5. Project Context

### 5.1 Technology Stack

- **.NET 9.0** with C# 13
- **Blazor Server** (interactive UI)
- **ASP.NET Core Web API** (RESTful services)
- **PostgreSQL 17** (primary data store)
- **Redis 7** (caching)
- **Ollama** (local LLM - llama3.2:3b)
- **Semantic Kernel** (AI orchestration)
- **.NET Aspire** (cloud-native orchestration)
- **Azure Container Apps** (deployment)

### 5.2 System Purpose

EduMind.AI is a multi-tenant academic assessment platform that:

- Generates adaptive assessments using AI agents
- Provides personalized feedback
- Tracks student progress with analytics
- Supports B2B (schools) and B2C (individuals)
- Multi-agent AI system for question generation and feedback
- Adaptive assessments that adjust to student performance
- Real-time analytics and progress tracking
- Multi-tenant architecture with RBAC
- Comprehensive privacy controls (COPPA, FERPA, GDPR compliant)

---

**For detailed information, always consult the comprehensive documentation in:**

- `.github/specification/` - Complete system requirements
- `.github/coding-standards.md` - Development standards
- `.github/testing/` - Testing strategies and guides
- `.github/deployment/` - Operational procedures
- `docs/architecture/` - Architectural decisions

**Last Updated:** 2025-10-25  
**Version:** 1.0.0

