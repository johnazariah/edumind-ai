# ADR-040: Reusable GitHub Actions Workflows

**Status:** ✅ Accepted  
**Date:** October 16, 2025  
**Context:** CI/CD Pipeline Standardization

## Context

The project had multiple GitHub Actions workflows with significant duplication:

- **Build steps** repeated in 3+ workflows (setup .NET, restore, build)
- **Test execution** duplicated across CI and deployment pipelines  
- **Code quality checks** implemented differently in different places
- **No standardized security scanning** across the codebase
- **Maintenance burden**: Updating build logic required changes in multiple files

**Problems**:

- ~200+ lines of duplicated YAML across workflows
- Inconsistent .NET versions, configurations, and flags
- No centralized security scanning (CodeQL, dependency check, secret scanning)
- Risk of drift between workflows (one updated, others forgotten)
- Difficult to add new checks (must update N files)

## Decision

Created **4 reusable workflows** that encapsulate common CI/CD patterns:

1. **`_reusable-dotnet-build.yml`** - Build and unit testing
2. **`_reusable-code-quality.yml`** - Formatting and static analysis
3. **`_reusable-security-scan.yml`** - Security scanning (CodeQL, dependencies, secrets)
4. **`_reusable-integration-tests.yml`** - Integration testing with services

## Rationale

1. **DRY Principle**: Define once, use everywhere
2. **Consistency**: Same process across all workflows
3. **Maintainability**: Update once, automatically applies everywhere
4. **Security**: Standardized scanning and compliance checks
5. **Developer Experience**: Clear, self-documenting pipelines
6. **Scalability**: Easy to add new workflows or update existing

## Consequences

### Positive

- **80% code reduction**: Main CI workflow reduced from 150+ lines to ~30 lines
- **Consistency**: All workflows use exact same build/test process
- **Easy updates**: Change one file, all workflows benefit
- **Better security**: CodeQL, secret scanning, dependency check on all PRs
- **Clear ownership**: Reusable workflows have single purpose

### Negative

- **Learning curve**: Developers must understand reusable workflow syntax
- **Indirection**: One level of indirection when debugging workflows
- **Debugging**: Harder to debug (need to open reusable workflow file)
- **Versioning**: Must careful with reusable workflow changes (affects all callers)

### Risks Mitigated

- Prefix reusable workflows with `_` to indicate they're not standalone
- Document inputs/outputs clearly in each reusable workflow
- Use matrix builds in reusable workflows for extensibility
- Provide default values for all optional inputs

## Implementation

**Reusable Workflow 1: .NET Build** (_reusable-dotnet-build.yml):

```yaml
name: .NET Build and Test

on:
  workflow_call:
    inputs:
      dotnet-version:
        type: string
        default: '9.0.x'
      configuration:
        type: string
        default: 'Release'
      run-unit-tests:
        type: boolean
        default: true
      enable-code-coverage:
        type: boolean
        default: false
    outputs:
      build-version:
        value: ${{ jobs.build.outputs.version }}

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration ${{ inputs.configuration }} --no-restore
      
      - name: Run unit tests
        if: inputs.run-unit-tests
        run: dotnet test --no-build --verbosity normal
      
      - name: Generate code coverage
        if: inputs.enable-code-coverage
        run: |
          dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report
      
      - name: Upload coverage to Codecov
        if: inputs.enable-code-coverage
        uses: codecov/codecov-action@v3
```

**Reusable Workflow 2: Code Quality** (_reusable-code-quality.yml):

```yaml
name: Code Quality Checks

on:
  workflow_call:
    inputs:
      enable-format-check:
        type: boolean
        default: true
      enable-analyzers:
        type: boolean
        default: true
      fail-on-warnings:
        type: boolean
        default: false

jobs:
  quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Check code formatting
        if: inputs.enable-format-check
        run: dotnet format --verify-no-changes --verbosity diagnostic
      
      - name: Build with analyzers
        if: inputs.enable-analyzers
        run: |
          dotnet build \
            /p:TreatWarningsAsErrors=${{ inputs.fail-on-warnings }} \
            /p:EnforceCodeStyleInBuild=true \
            /p:EnableNETAnalyzers=true
```

**Reusable Workflow 3: Security Scan** (_reusable-security-scan.yml):

```yaml
name: Security Scanning

on:
  workflow_call:
    inputs:
      run-codeql:
        type: boolean
        default: true
      run-dependency-check:
        type: boolean
        default: true
      run-secret-scan:
        type: boolean
        default: true

jobs:
  codeql:
    if: inputs.run-codeql
    runs-on: ubuntu-latest
    permissions:
      security-events: write
    steps:
      - uses: actions/checkout@v4
      
      - name: Initialize CodeQL
        uses: github/codeql-action/init@v2
        with:
          languages: csharp
      
      - name: Autobuild
        uses: github/codeql-action/autobuild@v2
      
      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v2
  
  dependency-check:
    if: inputs.run-dependency-check
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Check for vulnerable dependencies
        run: |
          dotnet list package --vulnerable --include-transitive
          dotnet list package --deprecated
  
  secret-scan:
    if: inputs.run-secret-scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Scan for secrets
        uses: trufflesecurity/trufflehog@main
        with:
          path: ./
```

**Using Reusable Workflows** (Main CI):

```yaml
name: CI Pipeline

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      configuration: Release
      run-unit-tests: true
      enable-code-coverage: true
  
  quality:
    uses: ./.github/workflows/_reusable-code-quality.yml
    with:
      enable-format-check: true
      enable-analyzers: true
  
  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
    with:
      run-codeql: true
      run-dependency-check: true
      run-secret-scan: true
  
  integration-tests:
    needs: [build]
    uses: ./.github/workflows/_reusable-integration-tests.yml
    with:
      run-ollama-tests: true
```

## Benefits Realized

**Before** (150 lines in ci.yml):

```yaml
- Setup .NET (10 lines)
- Restore dependencies (5 lines)
- Build (5 lines)
- Run tests (10 lines)
- Code coverage (15 lines)
- Format check (10 lines)
- CodeQL (20 lines)
- Deploy (30 lines)
# Total: ~150 lines, duplicated in 3 workflows = 450 lines
```

**After** (30 lines in ci.yml):

```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
  quality:
    uses: ./.github/workflows/_reusable-code-quality.yml
  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
# Total: ~30 lines, reusable workflows = 400 lines (but shared)
# Effective reduction: 450 → 30 lines in consuming workflows (93% reduction)
```

## Standards Enforced

**Code Quality**:

- `dotnet format` verification (EditorConfig rules)
- Roslyn analyzers (StyleCop, code analysis)
- Security analyzers

**Security**:

- CodeQL static analysis (SAST)
- Dependency vulnerability scanning
- Secret scanning (prevent credential leaks)

**Testing**:

- Unit tests on every PR
- Integration tests with Ollama
- Code coverage tracking (Codecov)

## Alternative Considered: Composite Actions

**Rejected because:**

- Cannot define jobs/services (only steps)
- Limited reusability (step-level only)
- No matrix build support
- Less powerful than reusable workflows

## Related Decisions

- ADR-041: Semantic Commit Messages (trigger workflows)
- ADR-050: xUnit Test Framework
- ADR-033: Client-Secret Authentication (deployment workflows)

## References

- `.github/workflows/_reusable-*.yml` - Reusable workflow definitions
- `.github/workflows/ci.yml` - Main CI using reusable workflows
- `.github/workflows/deploy.yml` - Deployment using reusable workflows
- Commit: `33922da` - "feat: add reusable workflow infrastructure for robust software engineering"
- docs/deployment/ADR-REUSABLE-WORKFLOWS.md
