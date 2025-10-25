# Analysis Story: Coding Standards and Guidelines

## Objective

Create a comprehensive `instructions.md` file that captures all coding standards, task guidelines, and style decisions made throughout the project.

## Context

The EduMind.AI project has evolved through 176+ commits with multiple developers and copilot sessions. Various coding decisions, architectural patterns, and style guidelines have been established but are scattered across:

- Commit messages
- Code comments
- Various documentation files in `docs/`
- Existing instruction files in `docs/instructions/`
- Pull request descriptions

## Task Instructions

### 1. Review Existing Instruction Documents

Search and analyze:

- `docs/instructions/**/*.md` - Any existing coding guidelines
- `docs/development/**/*.md` - Development practices
- `.github/copilot-instructions.md` - Copilot-specific guidance

### 2. Analyze Git Commit History

Review all 176 commits focusing on:

- Commit message patterns and conventions
- Fix commits that reveal coding standards (e.g., "Fix: Add quotes to prevent formatting")
- Feature commits showing architectural decisions
- Refactoring commits indicating code quality standards

Command to extract all commits:

```bash
git log --all --format="%H|%s|%b" > /tmp/commit-analysis.txt
```

### 3. Extract Coding Patterns from Codebase

Analyze key source files to identify patterns:

**C# / .NET Standards:**

- `src/**/*.cs` - Naming conventions, async patterns, error handling
- `src/**/Program.cs` - Configuration patterns, dependency injection
- Look for: namespace organization, class structure, method signatures

**Blazor / Frontend Standards:**

- `src/AcademicAssessment.StudentApp/Components/**/*.razor`
- Component structure, state management, event handling patterns

**API Standards:**

- `src/AcademicAssessment.Web/Controllers/*.cs`
- RESTful conventions, versioning, response patterns

**Infrastructure as Code:**

- `infra/**/*.bicep` - Resource naming, tagging conventions
- `src/infra/**/*.bicep` - Aspire-specific patterns

### 4. Document Testing Standards

Review:

- `tests/**/*.cs` - Test naming, organization, assertion patterns
- `coverlet.runsettings` - Coverage requirements
- Test project structures

### 5. Extract Configuration Standards

Analyze:

- `appsettings.json` patterns across projects
- Environment variable naming (found in `Program.cs` files)
- Connection string formats
- Logging configuration patterns

### 6. Identify Documentation Standards

Review:

- `docs/**/*.md` - Markdown formatting, structure patterns
- README files - Documentation hierarchy
- Code comment styles in source files

## Expected Output Structure

Create `.github/instructions.md` with these sections:

### 1. Code Organization

- Solution structure principles
- Project naming conventions
- Folder organization patterns
- Namespace conventions

### 2. C# Coding Standards

- Naming conventions (classes, methods, variables, constants)
- Async/await patterns
- Error handling and exceptions
- LINQ usage guidelines
- Dependency injection patterns

### 3. API Development Standards

- RESTful endpoint design
- Versioning strategy (API versioning observed in code)
- Request/response patterns
- Error response format
- Authentication/authorization patterns

### 4. Frontend Development (Blazor)

- Component organization
- State management
- Event handling patterns
- Navigation patterns (note recent HTML link conversion)
- Performance considerations (note HttpClient configuration fix)

### 5. Infrastructure as Code

- Bicep resource naming
- Parameter and variable conventions
- Module organization
- Tag standards
- Output definitions

### 6. Testing Standards

- Test project organization
- Test naming conventions
- Unit test patterns
- Integration test setup
- E2E test approaches
- Coverage requirements

### 7. Git Workflow

- Branch naming
- Commit message format
- Pull request standards
- Review process

### 8. Documentation Standards

- README structure
- API documentation
- Inline code documentation
- Architectural decision records
- Deployment documentation

### 9. Configuration Management

- Environment variables
- Secrets management
- Connection strings
- Feature flags

### 10. Copilot Collaboration Guidelines

- How to provide context
- Expected interaction patterns
- Code review expectations
- Documentation responsibilities

## Success Criteria

- All major coding patterns are documented with examples
- Standards are specific enough to be actionable
- Examples from actual codebase are included
- Guidelines are organized logically
- Document is comprehensive but concise (aim for 50-100 pages equivalent)
- Cross-references to related ADRs where applicable

## Notes

- Focus on established patterns, not aspirational ones
- Include "why" explanations where decisions were contentious
- Note any deviations or special cases
- Reference specific commits or files as evidence
- Flag areas where standards may need refinement
