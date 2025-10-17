# ADR: Reusable GitHub Actions Workflows

**Status:** ✅ Accepted  
**Date:** October 16, 2025  
**Authors:** Development Team  
**Deciders:** Technical Leadership  

## Context and Problem Statement

The EduMind.AI project has multiple GitHub Actions workflows (CI, deployment, Ollama integration) that contain significant duplication:

- **Build steps** repeated in 3+ workflows (Setup .NET, restore, build)
- **Test execution** duplicated across CI and deployment pipelines
- **Code quality checks** implemented differently in different places
- **No standardized security scanning** across the codebase
- **Maintenance burden** - updating build logic requires changes in multiple files

**Key Issues:**

1. ~200+ lines of duplicated build/test logic across workflows
2. Inconsistent .NET versions, configurations, and flags
3. No centralized code quality or security scanning
4. High risk of drift between workflows
5. Difficult to add new checks (must update N files)

## Decision Drivers

- **DRY Principle** - Don't Repeat Yourself
- **Consistency** - Same process everywhere
- **Maintainability** - Update once, apply everywhere
- **Security** - Standardized scanning and compliance checks
- **Developer Experience** - Clear, self-documenting pipelines
- **Scalability** - Easy to add new workflows or update existing ones

## Considered Options

### Option 1: Status Quo (No Change)

- **Pros:** No migration effort
- **Cons:** Continued duplication, inconsistency, maintenance burden

### Option 2: Composite Actions

- **Pros:** Can be used within jobs, portable
- **Cons:** Cannot define jobs/services, limited scope

### Option 3: Reusable Workflows ✅ **SELECTED**

- **Pros:** Full workflow/job reuse, supports services, highly configurable
- **Cons:** Slight learning curve, must be in same repo or public

### Option 4: External GitHub Actions Marketplace

- **Pros:** Community-maintained, pre-built
- **Cons:** Less control, potential security/maintenance issues, vendor lock-in

## Decision Outcome

**Chosen option:** **Reusable Workflows** (Option 3)

We will create 4 reusable workflows that encapsulate common CI/CD patterns:

1. **`_reusable-dotnet-build.yml`** - Build and unit testing
2. **`_reusable-code-quality.yml`** - Formatting and static analysis
3. **`_reusable-security-scan.yml`** - Security scanning (CodeQL, dependencies, secrets)
4. **`_reusable-integration-tests.yml`** - Integration testing with services

### Rationale

- **GitHub-native solution** - No external dependencies
- **Full workflow control** - Can define jobs, services, matrices
- **Highly configurable** - Rich input/output system
- **Transparent** - All logic visible in source control
- **Maintainable** - Update once, automatically applies everywhere

## Implementation Details

### 1. Reusable Workflow: .NET Build (`_reusable-dotnet-build.yml`)

**Purpose:** Standardized build and unit test execution

**Key Features:**

- Configurable .NET SDK version (default: 9.0.x)
- Optional Aspire workload installation
- Build configuration (Debug/Release)
- Unit test execution with `.trx` output
- Optional performance tests
- Optional code coverage (Codecov integration)
- Semantic versioning from git tags
- Artifact upload for test results

**Inputs:**

```yaml
dotnet-version: string (default: '9.0.x')
configuration: string (default: 'Release')
run-unit-tests: boolean (default: true)
run-performance-tests: boolean (default: false)
enable-code-coverage: boolean (default: false)
aspire-workload: boolean (default: true)
```

**Outputs:**

```yaml
build-version: string (semantic version)
```

**Usage Example:**

```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      configuration: Release
      run-unit-tests: true
      enable-code-coverage: true
```

### 2. Reusable Workflow: Code Quality (`_reusable-code-quality.yml`)

**Purpose:** Enforce code formatting and static analysis standards

**Key Features:**

- `dotnet format` verification (ensures consistent style)
- Roslyn analyzer execution (code quality rules)
- Optional fail-on-warnings mode
- Build log artifact upload for debugging

**Inputs:**

```yaml
dotnet-version: string (default: '9.0.x')
enable-format-check: boolean (default: true)
enable-analyzers: boolean (default: true)
fail-on-warnings: boolean (default: false)
```

**Standards Enforced:**

- EditorConfig rules (.editorconfig)
- StyleCop analyzers
- Microsoft.CodeAnalysis.NetAnalyzers
- Security analyzers
- Performance analyzers

### 3. Reusable Workflow: Security Scanning (`_reusable-security-scan.yml`)

**Purpose:** Comprehensive security scanning and vulnerability detection

**Key Features:**

#### CodeQL Analysis

- Static analysis for C# and JavaScript
- `security-extended` query suite
- `security-and-quality` query suite
- Results visible in GitHub Security tab
- SARIF output for integration with other tools

#### Dependency Scanning

- Scans all NuGet packages (direct + transitive)
- Identifies CVEs and security advisories
- Generates vulnerability report artifact
- Non-blocking (warnings only)

#### Secret Scanning

- **TruffleHog OSS:** High-entropy string detection
- **Gitleaks:** Pattern-based secret detection
- Scans full git history (not just diffs)
- Verified secrets fail the pipeline

**Inputs:**

```yaml
enable-codeql: boolean (default: true)
enable-dependency-scan: boolean (default: true)
enable-secret-scan: boolean (default: true)
codeql-languages: string (default: 'csharp,javascript')
```

**Security Standards:**

- OWASP Top 10
- CWE (Common Weakness Enumeration)
- SANS Top 25
- GitHub Security Best Practices

### 4. Reusable Workflow: Integration Tests (`_reusable-integration-tests.yml`)

**Purpose:** Run integration tests with required infrastructure

**Key Features:**

- **Service Containers:** PostgreSQL 16 + Redis 7
- Health checks for all services
- Automatic database migrations (EF Core)
- Configurable connection strings
- Support for both GitHub services and external infrastructure
- Test result publishing (dorny/test-reporter)
- `.trx` artifact upload

**Inputs:**

```yaml
dotnet-version: string (default: '9.0.x')
configuration: string (default: 'Release')
api-base-url: string (optional - for testing deployed environments)
use-github-services: boolean (default: true)
aspire-workload: boolean (default: true)
```

**Secrets:**

```yaml
postgres-connection: string (optional)
redis-connection: string (optional)
```

**Service Configuration:**

```yaml
services:
  postgres:
    image: postgres:16-alpine
    env: POSTGRES_DB=edumind_test, ...
    health-cmd: pg_isready
    
  redis:
    image: redis:7-alpine
    health-cmd: redis-cli ping
```

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Calling Workflows                       │
│  (ci.yml, deploy-azure-azd.yml, ollama-integration.yml)   │
└─────────┬───────────────────┬───────────────┬──────────────┘
          │                   │               │
          │ uses:             │ uses:         │ uses:
          ▼                   ▼               ▼
┌──────────────────┐  ┌──────────────┐  ┌──────────────────┐
│  Reusable Build  │  │ Code Quality │  │ Security Scan    │
│  _reusable-      │  │ _reusable-   │  │ _reusable-       │
│  dotnet-build    │  │ code-quality │  │ security-scan    │
└──────────────────┘  └──────────────┘  └──────────────────┘
         │
         │ uses:
         ▼
┌──────────────────────┐
│ Integration Tests    │
│ _reusable-           │
│ integration-tests    │
└──────────────────────┘
```

## Pipeline Refactoring Strategy

### Before (Duplicated)

**ci.yml (214 lines):**

```yaml
- Setup .NET
- Install Aspire
- Restore
- Build
- Test (unit)
- Test (performance)
- Test (integration)
- Code quality checks
```

**deploy-azure-azd.yml (159 lines):**

```yaml
- Setup .NET
- Install Aspire
- Restore
- Build
- Test (unit)
- Deploy
- Test (integration on deployed)
```

**ollama-integration.yml (170 lines):**

```yaml
- Setup .NET
- Install Aspire
- Restore
- Build
- Install Ollama
- Test (integration with Ollama)
```

**Total:** ~200+ lines of duplicated build/test logic

### After (Reusable)

**ci.yml (simplified):**

```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      run-unit-tests: true
      run-performance-tests: true
      enable-code-coverage: true

  code-quality:
    needs: build
    uses: ./.github/workflows/_reusable-code-quality.yml

  integration-tests:
    needs: build
    uses: ./.github/workflows/_reusable-integration-tests.yml

  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
```

**deploy-azure-azd.yml (simplified):**

```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      configuration: Release
      run-unit-tests: true

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Deploy with azd
        run: azd up --no-prompt

  integration-tests:
    needs: deploy
    uses: ./.github/workflows/_reusable-integration-tests.yml
    with:
      api-base-url: ${{ needs.deploy.outputs.url }}
      use-github-services: false
```

**Estimated Reduction:** ~60% less code, clearer intent

## Benefits

### 1. Consistency

- ✅ Same .NET version, build flags, and configurations everywhere
- ✅ Predictable behavior across all pipelines
- ✅ Uniform test execution and reporting

### 2. Maintainability

- ✅ Update build process once → applies to all workflows
- ✅ Single source of truth for each concern
- ✅ Clear separation of responsibilities

### 3. Security

- ✅ Standardized security scanning (CodeQL, secrets, dependencies)
- ✅ Enforced code quality checks
- ✅ Audit trail in GitHub Security tab
- ✅ Compliance-ready (SOC 2, ISO 27001)

### 4. Developer Experience

- ✅ Self-documenting workflow files
- ✅ Easy to understand pipeline structure
- ✅ Comprehensive README documentation
- ✅ Clear input/output contracts

### 5. Scalability

- ✅ Easy to add new workflows (just reference reusables)
- ✅ Easy to add new checks (update reusable once)
- ✅ Supports versioning (can pin to specific versions)

### 6. Cost Optimization

- ✅ Faster builds (less duplication)
- ✅ Efficient caching strategies
- ✅ Parallel job execution
- ✅ Reduced GitHub Actions minutes

## Consequences

### Positive

1. **Reduced Code Duplication:** ~60% reduction in workflow YAML
2. **Improved Consistency:** Same process everywhere
3. **Faster Iteration:** Update once, apply everywhere
4. **Better Security Posture:** Standardized scanning
5. **Clearer Intent:** Workflows are self-documenting
6. **Easier Onboarding:** New devs understand pipeline structure quickly

### Negative (Mitigations)

1. **Learning Curve:** Team must understand reusable workflows
   - **Mitigation:** Comprehensive README.md documentation
   - **Mitigation:** Examples in all workflows

2. **Indirection:** Logic not directly visible in calling workflow
   - **Mitigation:** Clear naming conventions (`_reusable-*`)
   - **Mitigation:** Detailed input/output documentation
   - **Mitigation:** Clickable links in GitHub Actions UI

3. **Versioning Complexity:** Breaking changes affect all callers
   - **Mitigation:** Use semantic versioning (tag reusables)
   - **Mitigation:** Thorough testing before merging changes
   - **Mitigation:** Backward compatibility where possible

## Compliance and Standards

This architecture supports:

- ✅ **SOC 2 Type II:** Automated security scanning, audit logs
- ✅ **ISO 27001:** Security controls, vulnerability management
- ✅ **GDPR:** Secret scanning prevents data leaks
- ✅ **OWASP:** CodeQL covers OWASP Top 10
- ✅ **CIS Benchmarks:** Dependency scanning, least privilege

## Migration Plan

### Phase 1: Create Reusable Workflows ✅ **COMPLETE**

- [x] Create `_reusable-dotnet-build.yml`
- [x] Create `_reusable-code-quality.yml`
- [x] Create `_reusable-security-scan.yml`
- [x] Create `_reusable-integration-tests.yml`
- [x] Create documentation (README.md, this ADR)

### Phase 2: Refactor Existing Workflows (In Progress)

- [ ] Refactor `ci.yml` to use reusables
- [ ] Refactor `deploy-azure-azd.yml` to use reusables
- [ ] Refactor `ollama-integration.yml` to use reusables
- [ ] Test all workflows in feature branch
- [ ] Merge to main after validation

### Phase 3: Enable Security Features (Future)

- [ ] Enable CodeQL on pull requests
- [ ] Configure Dependabot for automated dependency updates
- [ ] Set up GitHub Advanced Security (if available)
- [ ] Integrate with external security tools (Snyk, etc.)

### Phase 4: Optimization (Future)

- [ ] Implement build caching strategies
- [ ] Add matrix builds for multi-platform testing
- [ ] Optimize for GitHub Actions billing
- [ ] Add performance benchmarking

## Monitoring and Metrics

**Success Metrics:**

- Lines of YAML reduced: Target ~60%
- Build consistency: 100% same process
- Security coverage: 100% of PRs scanned
- Time to add new workflow: <15 minutes (vs. 1+ hour)
- Developer satisfaction: Survey after 3 months

**Monitoring:**

- GitHub Actions usage dashboard
- Security alert trends (GitHub Security tab)
- Build time trends
- Test pass rate trends
- Workflow failure analysis

## Related Documents

- [GitHub Actions: Reusing Workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)
- [Azure Deployment Strategy](./AZURE_DEPLOYMENT_STRATEGY.md)
- [Aspire Migration Log](./ASPIRE_MIGRATION_LOG.md)
- [Authentication Setup](./AUTHENTICATION_SETUP.md)
- [Reusable Workflows README](../../.github/workflows/README.md)

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-16 | 1.0 | Initial ADR documenting reusable workflow strategy | Development Team |

## Appendix: Example Usage Patterns

### Pattern 1: Basic CI Pipeline

```yaml
name: CI
on: [push, pull_request]

jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml

  code-quality:
    needs: build
    uses: ./.github/workflows/_reusable-code-quality.yml

  integration-tests:
    needs: build
    uses: ./.github/workflows/_reusable-integration-tests.yml

  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
```

### Pattern 2: Deployment Pipeline

```yaml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      configuration: Release
      enable-code-coverage: true

  deploy-dev:
    needs: build
    environment: dev
    runs-on: ubuntu-latest
    steps:
      - uses: Azure/login@v2
      - run: azd up --environment dev

  test-deployed:
    needs: deploy-dev
    uses: ./.github/workflows/_reusable-integration-tests.yml
    with:
      api-base-url: https://dev.edumind.ai
      use-github-services: false
```

### Pattern 3: Security-Only Scan

```yaml
name: Security Scan
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM daily

jobs:
  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
    with:
      enable-codeql: true
      enable-dependency-scan: true
      enable-secret-scan: true
```

## Conclusion

The adoption of reusable workflows represents a significant improvement in our CI/CD infrastructure. By eliminating duplication, standardizing processes, and improving security scanning, we create a more maintainable, reliable, and scalable software engineering platform.

This decision aligns with industry best practices and positions EduMind.AI for future growth while maintaining high code quality and security standards.
