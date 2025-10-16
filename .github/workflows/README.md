# Reusable GitHub Actions Workflows

This directory contains reusable workflows that provide consistent build, test, and security scanning capabilities across all pipelines.

## 📋 Available Workflows

### 1. `_reusable-dotnet-build.yml`
**Purpose:** Build .NET solution and run unit/performance tests

**Inputs:**
- `dotnet-version` (string, default: `9.0.x`) - .NET SDK version
- `configuration` (string, default: `Release`) - Build configuration
- `run-unit-tests` (boolean, default: `true`) - Run unit tests
- `run-performance-tests` (boolean, default: `false`) - Run performance tests
- `enable-code-coverage` (boolean, default: `false`) - Collect code coverage
- `aspire-workload` (boolean, default: `true`) - Install Aspire workload

**Outputs:**
- `build-version` - Semantic version of the build

**Example Usage:**
```yaml
jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      dotnet-version: '9.0.x'
      configuration: Release
      run-unit-tests: true
      enable-code-coverage: true
```

### 2. `_reusable-code-quality.yml`
**Purpose:** Code formatting, linting, and static analysis

**Inputs:**
- `dotnet-version` (string, default: `9.0.x`) - .NET SDK version
- `enable-format-check` (boolean, default: `true`) - Check code formatting
- `enable-analyzers` (boolean, default: `true`) - Run static analyzers
- `fail-on-warnings` (boolean, default: `false`) - Fail on analyzer warnings

**Example Usage:**
```yaml
jobs:
  code-quality:
    uses: ./.github/workflows/_reusable-code-quality.yml
    with:
      enable-format-check: true
      enable-analyzers: true
      fail-on-warnings: false
```

### 3. `_reusable-security-scan.yml`
**Purpose:** Security scanning (CodeQL, dependency vulnerabilities, secrets)

**Inputs:**
- `enable-codeql` (boolean, default: `true`) - Run CodeQL analysis
- `enable-dependency-scan` (boolean, default: `true`) - Scan for vulnerable dependencies
- `enable-secret-scan` (boolean, default: `true`) - Scan for secrets
- `codeql-languages` (string, default: `csharp,javascript`) - Languages for CodeQL

**Example Usage:**
```yaml
jobs:
  security:
    uses: ./.github/workflows/_reusable-security-scan.yml
    with:
      enable-codeql: true
      enable-dependency-scan: true
      enable-secret-scan: true
```

### 4. `_reusable-integration-tests.yml`
**Purpose:** Run integration tests with PostgreSQL and Redis

**Inputs:**
- `dotnet-version` (string, default: `9.0.x`) - .NET SDK version
- `configuration` (string, default: `Release`) - Build configuration
- `api-base-url` (string, optional) - API URL for testing deployed environments
- `use-github-services` (boolean, default: `true`) - Use GitHub Actions service containers
- `aspire-workload` (boolean, default: `true`) - Install Aspire workload

**Secrets:**
- `postgres-connection` (optional) - PostgreSQL connection string
- `redis-connection` (optional) - Redis connection string

**Example Usage:**
```yaml
jobs:
  integration-tests:
    uses: ./.github/workflows/_reusable-integration-tests.yml
    with:
      use-github-services: true
      aspire-workload: true
```

## 🎯 Benefits

### Consistency
- Same build/test/scan process everywhere
- Standardized tool versions and configurations
- Predictable behavior across environments

### Maintainability
- Update once, apply everywhere
- No duplication across workflow files
- Clear separation of concerns

### Visibility
- Centralized workflow logic
- Easy to audit and understand
- Self-documenting with detailed inputs/outputs

### Flexibility
- Highly configurable via inputs
- Support both service containers and external services
- Can be composed in different ways

## 📖 How to Use in Your Workflows

### Basic CI Pipeline
```yaml
name: CI

on: [push, pull_request]

jobs:
  build:
    uses: ./.github/workflows/_reusable-dotnet-build.yml
    with:
      run-unit-tests: true
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

### Deployment Pipeline
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
      run-unit-tests: true

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure
        run: azd up --no-prompt

  integration-tests:
    needs: deploy
    uses: ./.github/workflows/_reusable-integration-tests.yml
    with:
      api-base-url: https://myapp.azurewebsites.net
      use-github-services: false
    secrets:
      postgres-connection: ${{ secrets.AZURE_POSTGRES_CONNECTION }}
      redis-connection: ${{ secrets.AZURE_REDIS_CONNECTION }}
```

## 🔒 Security Scanning Features

### CodeQL
- Analyzes C# and JavaScript code
- Detects security vulnerabilities and code quality issues
- Uses `security-extended` and `security-and-quality` query suites
- Results visible in GitHub Security tab

### Dependency Scanning
- Scans all NuGet packages (direct and transitive)
- Identifies packages with known vulnerabilities
- Generates report artifact
- Non-blocking (warnings only)

### Secret Scanning
- **TruffleHog:** Scans for leaked secrets in git history
- **Gitleaks:** Additional secret detection
- Checks full git history
- Fails pipeline if verified secrets found

## 📊 Test Reporting

All test workflows automatically:
- Generate `.trx` test result files
- Upload results as artifacts (30-day retention)
- Publish results to GitHub Actions UI via `dorny/test-reporter`
- Support code coverage collection (optional)
- Integrate with Codecov (optional)

## 🛠️ Maintenance

### Updating a Reusable Workflow
1. Edit the reusable workflow file
2. Test changes in a feature branch
3. Merge to `main` once validated
4. All workflows using it will automatically use the new version

### Best Practices
- ✅ Keep workflows focused (single responsibility)
- ✅ Make inputs optional with sensible defaults
- ✅ Document all inputs and outputs
- ✅ Use semantic versioning for breaking changes
- ✅ Test changes thoroughly before merging

### Versioning
- Reusable workflows are referenced by branch/tag/SHA
- For stability, consider tagging versions: `v1`, `v1.0.0`, etc.
- Example: `uses: ./.github/workflows/_reusable-build.yml@v1`

## 📚 Resources

- [GitHub Actions: Reusing workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)
- [Workflow syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
- [CodeQL documentation](https://codeql.github.com/docs/)
- [Dependabot alerts](https://docs.github.com/en/code-security/dependabot)
