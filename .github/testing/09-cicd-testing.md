# CI/CD Testing Guide

**Purpose:** Understand automated testing in GitHub Actions workflows.

**Audience:** Developers and DevOps engineers managing the CI/CD pipeline.

---

## 🎯 CI/CD Testing Overview

EduMind.AI uses **GitHub Actions** for continuous integration with automated testing at every commit and pull request.

**Testing Pipeline:**

```
Push/PR → Build → Unit Tests → Integration Tests → E2E Tests → Deploy
   ↓         ↓          ↓              ↓                ↓          ↓
 Trigger  Compile  Fast Tests   API+DB Tests   UI Workflows   Azure
 (1s)     (2-3m)   (10-20s)      (2-5m)         (5-10m)      (10m)
```

**Total CI Time:** ~15-20 minutes for full pipeline

---

## 🏗️ Workflow Architecture

### Reusable Workflows

We use reusable workflows to avoid duplication:

```
.github/workflows/
├── _reusable-dotnet-build.yml          # Build + unit/performance tests
├── _reusable-integration-tests.yml     # Integration tests with services
├── _reusable-code-quality.yml          # Linting and static analysis
├── _reusable-security-scan.yml         # Security scanning
├── ci.yml                              # Main CI workflow (calls others)
└── deploy-azure.yml                    # Deployment workflow
```

### Main CI Workflow

**`.github/workflows/ci.yml`:**

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      dotnet-version: '9.0.x'
  
  integration-tests:
    needs: build-and-test
    uses: ./.github/workflows/_reusable-integration-tests.yml
  
  code-quality:
    uses: ./.github/workflows/_reusable-code-quality.yml
  
  security-scan:
    uses: ./.github/workflows/_reusable-security-scan.yml
```

---

## 🧪 Build & Unit Tests Workflow

**`.github/workflows/_reusable-dotnet-build.yml`:**

```yaml
name: Build and Unit Tests

on:
  workflow_call:
    inputs:
      dotnet-version:
        required: false
        type: string
        default: '9.0.x'

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
        run: dotnet build --no-restore --configuration Release
      
      - name: Run unit tests
        run: |
          dotnet test tests/AcademicAssessment.Tests.Unit/ \
            --no-build \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --logger "trx;LogFileName=unit-test-results.trx"
      
      - name: Run performance tests
        run: |
          dotnet test tests/AcademicAssessment.Tests.Performance/ \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=perf-test-results.trx"
      
      - name: Publish test results
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: Unit Test Results
          path: '**/unit-test-results.trx'
          reporter: dotnet-trx
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./tests/**/coverage.cobertura.xml
          flags: unittests
```

**What This Does:**

1. ✅ Checks out code
2. ✅ Sets up .NET 9.0 SDK
3. ✅ Restores NuGet packages
4. ✅ Builds solution in Release mode
5. ✅ Runs unit tests with coverage
6. ✅ Runs performance tests
7. ✅ Publishes test results (visible in PR)
8. ✅ Uploads coverage to Codecov

**Execution Time:** ~3-5 minutes

---

## 🔌 Integration Tests Workflow

**`.github/workflows/_reusable-integration-tests.yml`:**

```yaml
name: Integration Tests

on:
  workflow_call:

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:17
        env:
          POSTGRES_DB: edumind_test
          POSTGRES_USER: edumind_test_user
          POSTGRES_PASSWORD: edumind_test_pass
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
      
      redis:
        image: redis:7
        ports:
          - 6379:6379
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Run database migrations
        env:
          ConnectionStrings__AcademicDatabase: "Host=localhost;Port=5432;Database=edumind_test;Username=edumind_test_user;Password=edumind_test_pass"
        run: |
          dotnet ef database update \
            --project src/AcademicAssessment.Infrastructure/ \
            --startup-project src/AcademicAssessment.Web/
      
      - name: Run integration tests
        env:
          ConnectionStrings__AcademicDatabase: "Host=localhost;Port=5432;Database=edumind_test;Username=edumind_test_user;Password=edumind_test_pass"
          ConnectionStrings__Redis: "localhost:6379"
        run: |
          dotnet test tests/AcademicAssessment.Tests.Integration/ \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=integration-test-results.trx"
      
      - name: Publish test results
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: Integration Test Results
          path: '**/integration-test-results.trx'
          reporter: dotnet-trx
```

**What This Does:**

1. ✅ Spins up PostgreSQL container
2. ✅ Spins up Redis container
3. ✅ Waits for services to be healthy
4. ✅ Runs database migrations
5. ✅ Runs integration tests with real services
6. ✅ Publishes test results

**Execution Time:** ~5-10 minutes

---

## 🎭 E2E Tests Workflow (Future)

**Planned `.github/workflows/_reusable-e2e-tests.yml`:**

```yaml
name: E2E Tests

on:
  workflow_call:

jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Start services
        run: docker-compose up -d
      
      - name: Wait for services
        run: |
          timeout 60 bash -c 'until docker-compose ps | grep healthy; do sleep 2; done'
      
      - name: Install Playwright
        run: |
          pwsh src/AcademicAssessment.Tests.UI/bin/Debug/net9.0/playwright.ps1 install --with-deps
      
      - name: Run E2E tests
        run: |
          dotnet test tests/AcademicAssessment.Tests.UI/ \
            --logger "trx;LogFileName=e2e-test-results.trx"
      
      - name: Upload screenshots
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: test-screenshots
          path: screenshots/
      
      - name: Upload videos
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: test-videos
          path: videos/
```

---

## 📊 Test Reporting

### dorny/test-reporter

Displays test results directly in PRs:

```yaml
- name: Publish test results
  if: always()  # Run even if tests fail
  uses: dorny/test-reporter@v1
  with:
    name: Unit Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
    fail-on-error: true
```

**Output in PR:**

```
✅ Unit Test Results
   487 passed, 0 failed, 0 skipped
   
❌ Integration Test Results
   285 passed, 2 failed, 0 skipped
   
   Failed Tests:
   - AssessmentController_GetById_NotFound
   - StudentRepository_Save_Concurrent
```

### Coverage Reporting

**Codecov Integration:**

```yaml
- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    files: ./tests/**/coverage.cobertura.xml
    flags: unittests
    fail_ci_if_error: false  # Don't fail build if upload fails
```

**Coverage Badge in README:**

```markdown
[![codecov](https://codecov.io/gh/yourusername/edumind-ai/branch/main/graph/badge.svg)](https://codecov.io/gh/yourusername/edumind-ai)
```

---

## 🔧 Environment Variables & Secrets

### Public Environment Variables

Set in workflow files:

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  ASPNETCORE_ENVIRONMENT: 'Testing'
  ConnectionStrings__AcademicDatabase: 'Host=localhost;Port=5432;...'
```

### Secrets

Store sensitive data in GitHub Secrets:

```yaml
steps:
  - name: Deploy
    env:
      AZURE_CREDENTIALS: ${{ secrets.AZURE_CREDENTIALS }}
      DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
    run: ./deploy.sh
```

**Setting Secrets:**

1. GitHub Repo → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Add name and value

---

## ⚡ Optimizing CI/CD Performance

### 1. Parallel Job Execution

```yaml
jobs:
  unit-tests:
    # Runs immediately
  
  integration-tests:
    needs: build  # Waits for build
  
  code-quality:
    # Runs immediately (parallel to tests)
```

### 2. Caching Dependencies

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'
    cache: true  # Cache NuGet packages
```

### 3. Only Run Affected Tests

```yaml
- name: Detect changes
  uses: dorny/paths-filter@v2
  id: changes
  with:
    filters: |
      src:
        - 'src/**'
      tests:
        - 'tests/**'

- name: Run tests
  if: steps.changes.outputs.src == 'true'
  run: dotnet test
```

### 4. Skip CI for Docs

```yaml
on:
  push:
    branches: [main]
    paths-ignore:
      - 'docs/**'
      - '*.md'
      - '.github/deployment/**'
```

---

## 🚫 Handling Flaky Tests

### Retry Failed Tests

```yaml
- name: Run tests with retry
  uses: nick-fields/retry@v2
  with:
    timeout_minutes: 10
    max_attempts: 3
    command: dotnet test tests/AcademicAssessment.Tests.UI/
```

### Mark Known Flaky Tests

```csharp
[Fact(Skip = "Flaky - see issue #123")]
public async Task FlakyTest() { /*...*/ }
```

### Isolate Flaky Tests

```yaml
# Run stable tests first
- name: Run stable tests
  run: dotnet test --filter "Category!=Flaky"

# Run flaky tests separately (don't fail build)
- name: Run flaky tests
  continue-on-error: true
  run: dotnet test --filter "Category=Flaky"
```

---

## 🐛 Debugging CI Failures

### 1. Access Build Logs

GitHub Actions → Your workflow run → Click failing job → View logs

### 2. Enable Debug Logging

Set secret `ACTIONS_STEP_DEBUG` to `true`

Or in workflow:

```yaml
- name: Debug step
  run: echo "::debug::This is a debug message"
```

### 3. SSH into Runner (Advanced)

```yaml
- name: Setup tmate session
  if: failure()
  uses: mxschmitt/action-tmate@v3
```

### 4. Reproduce Locally

```bash
# Use act to run GitHub Actions locally
act -j build-and-test
```

---

## ✅ PR Status Checks

### Required Checks Before Merge

In GitHub Repo Settings → Branches → Branch protection rules:

✅ Build & Unit Tests must pass  
✅ Integration Tests must pass  
✅ Code Quality checks must pass  
✅ Coverage must not decrease  
⚠️ E2E Tests (optional - can be flaky)

### Status Check Configuration

```yaml
name: CI Status Check

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  test-all:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run all tests
        run: dotnet test --configuration Release
      
      - name: Block merge if tests fail
        if: failure()
        run: exit 1
```

---

## 📈 CI/CD Metrics

### Track Pipeline Performance

**Metrics to Monitor:**

- **Build time** - Should stay under 5 minutes
- **Test time** - Unit tests <30s, integration <5m, E2E <10m
- **Flaky test rate** - Should be <1%
- **PR merge time** - From open to merged
- **Failure rate** - % of pipelines that fail

### GitHub Actions Usage

View in: Repo → Insights → Actions

- Total workflow runs
- Success vs failure rate
- Average run time
- Billable minutes used

---

## 🔄 Continuous Deployment

### Deploy on Main Branch

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]
  workflow_dispatch:  # Manual trigger

jobs:
  test:
    uses: ./.github/workflows/_reusable-integration-tests.yml
  
  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy with azd
        run: |
          azd auth login --client-id ${{ secrets.AZURE_CLIENT_ID }} \
                         --tenant-id ${{ secrets.AZURE_TENANT_ID }}
          azd deploy
```

---

## 🎯 Best Practices

### 1. Fast Feedback First

```yaml
jobs:
  fast-tests:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test tests/Unit/  # 20 seconds
  
  slow-tests:
    needs: fast-tests  # Only if fast tests pass
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test tests/Integration/  # 5 minutes
```

### 2. Fail Fast

```yaml
strategy:
  fail-fast: true  # Stop other jobs if one fails
  matrix:
    dotnet-version: ['8.0.x', '9.0.x']
```

### 3. Clear Job Names

```yaml
jobs:
  build-and-unit-tests:
    name: "Build + Unit Tests (.NET ${{ matrix.dotnet-version }})"
```

### 4. Timeout Protection

```yaml
jobs:
  test:
    timeout-minutes: 15  # Kill if exceeds
```

---

## 📚 Workflow Examples

### Matrix Testing (Multiple Versions)

```yaml
strategy:
  matrix:
    dotnet-version: ['8.0.x', '9.0.x']
    os: [ubuntu-latest, windows-latest]

runs-on: ${{ matrix.os }}
steps:
  - uses: actions/setup-dotnet@v4
    with:
      dotnet-version: ${{ matrix.dotnet-version }}
```

### Conditional Steps

```yaml
- name: Run integration tests
  if: github.event_name == 'pull_request'
  run: dotnet test tests/Integration/

- name: Deploy
  if: github.ref == 'refs/heads/main'
  run: ./deploy.sh
```

---

## 🐛 Common CI Issues

### Issue: Tests Pass Locally, Fail in CI

**Causes:**
- Environment differences (timezone, culture)
- Race conditions (parallel test execution)
- Missing environment variables
- File path differences (Windows vs Linux)

**Solutions:**

```csharp
// Use UTC dates in tests
var date = DateTimeOffset.UtcNow;

// Use Path.Combine for cross-platform paths
var path = Path.Combine("data", "file.txt");

// Set culture explicitly
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
```

### Issue: Integration Tests Timeout

**Solution:** Increase health check timeouts

```yaml
services:
  postgres:
    options: >-
      --health-interval 10s
      --health-timeout 5s
      --health-retries 10  # Increase retries
```

---

**Last Updated:** 2025-10-25  
**Related:** [Coverage](./08-coverage.md) | [Integration Testing](./04-integration-testing.md) | [Troubleshooting](./12-troubleshooting.md)
