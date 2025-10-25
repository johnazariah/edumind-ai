# Local Development Testing

**Purpose:** Set up and run tests during daily development workflow.

**Time:** 10-15 minutes initial setup, <1 minute per test run after that.

---

## 🎯 Quick Start

### First Time Setup

```bash
# 1. Ensure you have .NET 9.0 SDK installed
dotnet --version
# Should show: 9.0.x

# 2. Restore dependencies
dotnet restore

# 3. Build solution
dotnet build

# 4. Run unit tests (no dependencies needed)
dotnet test tests/AcademicAssessment.Tests.Unit/

# ✅ If tests pass, you're ready!
```

### Daily Workflow

```bash
# Before starting work - verify tests pass
dotnet test tests/AcademicAssessment.Tests.Unit/

# Make code changes...

# Run affected tests (IDE auto-runs)
# Or manually:
dotnet test --filter "FullyQualifiedName~YourClassName"

# Before commit - run all unit tests
dotnet test tests/AcademicAssessment.Tests.Unit/
```

---

## 🛠️ Development Environment Setup

### IDE Configuration

#### Visual Studio Code (Recommended)

**Extensions:**

1. **C# Dev Kit** (Microsoft) - Primary C# support
2. **C#** (Microsoft) - Language features
3. **.NET Core Test Explorer** (Jun Han) - Test runner UI
4. **Coverage Gutters** (ryanluker) - Inline coverage visualization

**Test Explorer:**

- Open Command Palette (Ctrl+Shift+P)
- Run "Test: Focus on Test Explorer View"
- Tests appear in sidebar
- Click ▶️ to run individual tests
- Right-click → "Debug Test" to debug

**Settings (`.vscode/settings.json`):**

```json
{
  "dotnet-test-explorer.testProjectPath": "tests/**/*.csproj",
  "dotnet-test-explorer.enableTelemetry": false,
  "omnisharp.enableEditorConfigSupport": true,
  "omnisharp.enableRoslynAnalyzers": true
}
```

#### Visual Studio 2022

**Test Explorer:**

- Menu: Test → Test Explorer (Ctrl+E, T)
- Auto-discovers tests when solution opens
- Click ▶️ to run, 🐞 to debug
- Group by: Project, Namespace, Class

**Live Unit Testing (Enterprise only):**

- Menu: Test → Live Unit Testing → Start
- Real-time test execution as you type
- Shows pass/fail in code gutter

#### JetBrains Rider

**Unit Test Sessions:**

- Right-click test file → "Run Unit Tests"
- Persistent test session window
- Excellent coverage visualization
- Built-in profiling

---

## 🏃 Running Tests

### All Tests (Full Suite)

```bash
# All test projects (~15 minutes)
dotnet test

# All with coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific configuration
dotnet test --configuration Release
```

### By Project

```bash
# Unit tests only (~10 seconds)
dotnet test tests/AcademicAssessment.Tests.Unit/

# Integration tests (~60 seconds, requires services)
dotnet test tests/AcademicAssessment.Tests.Integration/

# UI tests (~5 minutes, requires services + browser)
dotnet test tests/AcademicAssessment.Tests.UI/

# Performance tests (~10 minutes)
dotnet test tests/AcademicAssessment.Tests.Performance/
```

### By Filter

```bash
# Run tests matching class name
dotnet test --filter "FullyQualifiedName~AssessmentTests"

# Run tests matching method name
dotnet test --filter "FullyQualifiedName~GetById"

# Run single specific test
dotnet test --filter "FullyQualifiedName=AcademicAssessment.Tests.Unit.Models.AssessmentTests.Constructor_SetsAllProperties"

# Run by category (if using [Trait] attributes)
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

### With Verbosity

```bash
# Quiet output (just pass/fail)
dotnet test --verbosity quiet

# Normal output (default)
dotnet test --verbosity normal

# Detailed output (includes test names)
dotnet test --verbosity detailed

# Diagnostic output (full logs)
dotnet test --verbosity diagnostic
```

---

## 🔍 Test Output & Logs

### Console Output

```bash
dotnet test tests/AcademicAssessment.Tests.Unit/ --verbosity normal
```

**Example Output:**

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   487, Skipped:     0, Total:   487, Duration: 9 s
```

### TRX Logging (Detailed Results)

```bash
# Generate TRX log file
dotnet test --logger "trx;LogFileName=test-results.trx"

# View results in:
# tests/AcademicAssessment.Tests.Unit/TestResults/test-results.trx
```

### HTML Reports (Human-Readable)

```bash
# Install report generator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open in browser
# coverage-report/index.html
```

---

## 🐛 Debugging Tests

### Visual Studio Code

**Method 1: Debug Test Explorer**

1. Open Test Explorer (Ctrl+Shift+P → "Test: Focus on Test Explorer")
2. Find your test
3. Right-click → "Debug Test"
4. Breakpoints will be hit

**Method 2: Debug Configuration**

Add to `.vscode/launch.json`:

```json
{
  "name": ".NET Core Test",
  "type": "coreclr",
  "request": "launch",
  "program": "dotnet",
  "args": [
    "test",
    "${workspaceFolder}/tests/AcademicAssessment.Tests.Unit/",
    "--filter",
    "FullyQualifiedName~AssessmentTests"
  ],
  "cwd": "${workspaceFolder}",
  "console": "internalConsole",
  "stopAtEntry": false
}
```

Press F5 to debug.

### Visual Studio 2022

1. Set breakpoint in test
2. Right-click test in Test Explorer → "Debug"
3. Or: Debug → Start Debugging (F5) with test file open

### Rider

1. Set breakpoint in test
2. Click 🐞 icon next to test method
3. Or: Right-click test → "Debug 'TestName'"

### Command Line Debugging

```bash
# Run specific test with diagnostic output
dotnet test --filter "FullyQualifiedName~YourTest" --verbosity diagnostic

# Set environment variable for more logging
export DOTNET_CLI_UI_LANGUAGE=en
export COREHOST_TRACE=1
dotnet test
```

---

## ⚡ Fast Feedback Loop

### Watch Mode (Continuous Testing)

```bash
# Run tests automatically on file change
dotnet watch test --project tests/AcademicAssessment.Tests.Unit/

# Output:
# dotnet watch ⌚ Started
# [Tests run on every save]
# dotnet watch ⌚ File changed: src/AcademicAssessment.Core/Models/Assessment.cs
# [Tests automatically re-run]
```

**When to Use:**

- ✅ During TDD workflow
- ✅ Refactoring existing code
- ✅ Working on specific component

**When NOT to Use:**

- ❌ Running full test suite (too slow)
- ❌ Integration tests (need service restarts)

### IDE Auto-Run

**VS Code with C# Dev Kit:**

- Tests automatically run on save
- Results show in Test Explorer
- Failed tests highlighted in code

**Visual Studio Live Unit Testing:**

- Real-time test execution
- Pass/fail indicators in gutter
- Enterprise edition only

**Rider Continuous Testing:**

- Tools → Unit Tests → Enable Continuous Testing
- Runs affected tests on save
- Shows coverage inline

---

## 📦 Dependencies for Integration Tests

Integration tests require external services. Use Docker Compose:

### Start Services

```bash
# Start PostgreSQL + Redis
docker-compose up -d

# Verify services are running
docker-compose ps

# Output:
# NAME                 STATUS
# edumind-postgresql   Up
# edumind-redis        Up
```

### Connection Strings

Services available at:

- **PostgreSQL:** `localhost:5432`
  - Database: `edumind_test`
  - User: `edumind_test_user`
  - Password: `edumind_test_pass`

- **Redis:** `localhost:6379`
  - No password (dev only)

### Run Integration Tests

```bash
# Now integration tests will pass
dotnet test tests/AcademicAssessment.Tests.Integration/
```

### Stop Services

```bash
# Stop but keep data
docker-compose stop

# Stop and remove data
docker-compose down -v
```

---

## 🧪 Test Data

### Seed Data for Testing

Use the provided seed script:

```bash
# Seed database with test data
./scripts/seed-demo-data.sh

# Or manually:
psql -h localhost -U edumind_test_user -d edumind_test -f scripts/seed-demo-data.sql
```

### InMemory Database (Unit Tests)

Unit tests use Entity Framework InMemory provider:

```csharp
// No external dependencies needed
var options = new DbContextOptionsBuilder<AcademicDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

using var context = new AcademicDbContext(options);
// Use context in tests
```

---

## 📊 Coverage in IDE

### Visual Studio Code (Coverage Gutters)

1. Install "Coverage Gutters" extension
2. Run tests with coverage:

   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

3. Command Palette → "Coverage Gutters: Display Coverage"
4. Green/red bars in gutter show covered/uncovered lines

### Visual Studio 2022

1. Test → Analyze Code Coverage for All Tests
2. Coverage results in Coverage Results window
3. Double-click to see line-by-line coverage
4. Export to XML/HTML for analysis

### Rider

1. Run tests with coverage (right-click → "Cover Unit Tests")
2. Coverage window shows percentages
3. File highlights covered/uncovered code
4. Export reports in multiple formats

---

## 🔧 Troubleshooting

### Tests Won't Run

**Problem:** `dotnet test` does nothing or errors

**Solutions:**

```bash
# 1. Clean and rebuild
dotnet clean
dotnet build

# 2. Restore packages
dotnet restore

# 3. Check .NET SDK version
dotnet --version
# Should be 9.0.x

# 4. Verify test project builds
dotnet build tests/AcademicAssessment.Tests.Unit/
```

### Tests Fail Locally But Pass in CI

**Problem:** Environment differences

**Check:**

1. **Connection strings:** Are you using correct local values?
2. **Dependencies:** Are PostgreSQL/Redis running?
3. **Data state:** Does database have conflicting data?
4. **Time zones:** Some tests may be timezone-sensitive
5. **.NET version:** CI uses .NET 9.0, do you?

**Solution:**

```bash
# Use exact CI environment
docker-compose up -d  # Start services
dotnet test --configuration Release  # Same as CI
```

### Integration Tests Fail

**Problem:** `Unable to connect to database`

**Solution:**

```bash
# 1. Check services are running
docker-compose ps

# 2. If not, start them
docker-compose up -d

# 3. Wait for PostgreSQL to be ready (~5 seconds)
docker-compose logs postgresql

# 4. Verify connection
psql -h localhost -U edumind_test_user -d edumind_test -c "SELECT 1;"
```

### Slow Test Execution

**Problem:** Tests take too long

**Check:**

```bash
# 1. Are you running integration tests by accident?
dotnet test tests/AcademicAssessment.Tests.Unit/  # Should be fast

# 2. Profile slow tests
dotnet test --logger "console;verbosity=detailed" | grep "ms]"
```

**Solutions:**

- Run only unit tests during development
- Use `--filter` to run subset
- Check for N+1 queries in integration tests
- Use InMemory database instead of real PostgreSQL

### Coverage Not Generating

**Problem:** `--collect:"XPlat Code Coverage"` produces no output

**Solution:**

```bash
# 1. Ensure coverlet.collector is installed
grep coverlet tests/AcademicAssessment.Tests.Unit/*.csproj

# 2. Run with explicit runsettings
dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings

# 3. Check TestResults directory
find tests -name "coverage.cobertura.xml"
```

---

## ✅ Pre-Commit Checklist

Before committing code:

```bash
# 1. Run unit tests (fast)
dotnet test tests/AcademicAssessment.Tests.Unit/
# ✅ All pass

# 2. If changed API, run integration tests
dotnet test tests/AcademicAssessment.Tests.Integration/
# ✅ All pass

# 3. Check code coverage (optional but recommended)
dotnet test --collect:"XPlat Code Coverage"
# ✅ Coverage maintained or improved

# 4. Format code
dotnet format
# ✅ No changes or committed

# 5. Build full solution
dotnet build
# ✅ No errors

# Ready to commit! 🚀
```

---

## 📚 Next Steps

- **Learn unit testing patterns:** [03-unit-testing.md](./03-unit-testing.md)
- **Understand integration testing:** [04-integration-testing.md](./04-integration-testing.md)
- **Debug failing tests:** [12-troubleshooting.md](./12-troubleshooting.md)

---

**Last Updated:** 2025-10-25  
**Related:** [Overview](./01-overview.md) | [Unit Testing](./03-unit-testing.md) | [Troubleshooting](./12-troubleshooting.md)
