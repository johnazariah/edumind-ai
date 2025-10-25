# EduMind.AI Testing Guide

Comprehensive testing documentation for the EduMind.AI Academic Assessment Multi-Agent System.

---

## 📚 Documentation Structure

This testing guide is organized into focused documents covering all aspects of testing:

### Core Testing Guides

1. **[Overview & Philosophy](./01-overview.md)** ⭐  
   Testing strategy, test pyramid, coverage goals, and quality standards

2. **[Local Development Testing](./02-local-testing.md)** ⭐  
   Running tests during daily development, IDE integration, debugging

3. **[Unit Testing](./03-unit-testing.md)**  
   Unit test structure, conventions, mocking, and best practices

4. **[Integration Testing](./04-integration-testing.md)**  
   API integration tests, database testing, WebApplicationFactory patterns

5. **[End-to-End Testing](./05-e2e-testing.md)**  
   UI testing with Playwright, browser automation, user workflows

6. **[AI/Agent Testing](./06-ai-agent-testing.md)**  
   Ollama integration, question generation, feedback validation

7. **[Test Data Management](./07-test-data.md)**  
   Seed data, fixtures, builders, and data cleanup strategies

8. **[Test Coverage](./08-coverage.md)**  
   Coverage reports, analysis, and improvement strategies

9. **[CI/CD Testing](./09-cicd-testing.md)**  
   Automated testing in GitHub Actions, test reports, flaky tests

10. **[Manual Testing](./10-manual-testing.md)**  
    Smoke tests, UAT procedures, exploratory testing

11. **[Cloud Testing](./11-cloud-testing.md)**  
    Azure test environments, production smoke tests

12. **[Troubleshooting](./12-troubleshooting.md)**  
    Debugging failed tests, test isolation, common issues

---

## 🎯 Quick Start

### New to the Project?

1. Read **[Overview & Philosophy](./01-overview.md)** to understand our testing approach
2. Follow **[Local Development Testing](./02-local-testing.md)** to set up your environment
3. Review **[Unit Testing](./03-unit-testing.md)** to learn conventions

### Need to Run Tests?

**Unit Tests (Fast):**

```bash
dotnet test tests/AcademicAssessment.Tests.Unit/
```

**Integration Tests (Require Services):**

```bash
# Start dependencies first (PostgreSQL, Redis)
dotnet test tests/AcademicAssessment.Tests.Integration/
```

**All Tests:**

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Writing a New Feature?

1. Start with **unit tests** for core logic
2. Add **integration tests** for API endpoints
3. Add **E2E tests** for critical user workflows
4. Ensure **coverage** meets targets (70%+ overall)

---

## 📊 Current Test Status

**Test Projects:**

- `AcademicAssessment.Tests.Unit` - Unit tests for all layers
- `AcademicAssessment.Tests.Integration` - API and database integration tests
- `AcademicAssessment.Tests.UI` - End-to-end UI tests with Playwright
- `AcademicAssessment.Tests.Performance` - Load and performance tests

**Testing Frameworks:**

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **Playwright** - Browser automation
- **coverlet** - Code coverage

**Test Execution:**

- **Local:** Run via dotnet CLI or IDE test explorer
- **CI/CD:** Automated in GitHub Actions on every push/PR
- **Coverage:** Cobertura and OpenCover formats

---

## 🔍 Finding What You Need

### By Task

| Task | Document |
|------|----------|
| Set up testing locally | [02-local-testing.md](./02-local-testing.md) |
| Write a unit test | [03-unit-testing.md](./03-unit-testing.md) |
| Test an API endpoint | [04-integration-testing.md](./04-integration-testing.md) |
| Test UI workflows | [05-e2e-testing.md](./05-e2e-testing.md) |
| Test AI agents | [06-ai-agent-testing.md](./06-ai-agent-testing.md) |
| Manage test data | [07-test-data.md](./07-test-data.md) |
| Improve coverage | [08-coverage.md](./08-coverage.md) |
| Fix failing tests | [12-troubleshooting.md](./12-troubleshooting.md) |

### By Test Type

| Test Type | Scope | Speed | Dependencies | Document |
|-----------|-------|-------|--------------|----------|
| **Unit** | Single class/method | Milliseconds | None | [03-unit-testing.md](./03-unit-testing.md) |
| **Integration** | Multiple components | Seconds | DB, Redis | [04-integration-testing.md](./04-integration-testing.md) |
| **E2E/UI** | Full user workflow | Seconds-Minutes | All services + browser | [05-e2e-testing.md](./05-e2e-testing.md) |
| **AI/Agent** | LLM integration | Minutes | Ollama | [06-ai-agent-testing.md](./06-ai-agent-testing.md) |
| **Performance** | Load/stress | Minutes | All services | Documented in performance project |

### By Component

- **Core Models:** [03-unit-testing.md](./03-unit-testing.md)
- **Repositories:** [03-unit-testing.md](./03-unit-testing.md) + [04-integration-testing.md](./04-integration-testing.md)
- **API Controllers:** [04-integration-testing.md](./04-integration-testing.md)
- **Blazor Pages:** [05-e2e-testing.md](./05-e2e-testing.md)
- **AI Agents:** [06-ai-agent-testing.md](./06-ai-agent-testing.md)
- **Orchestration:** [06-ai-agent-testing.md](./06-ai-agent-testing.md)

---

## 🏗️ Testing Architecture

### Test Pyramid

```
       /\        E2E Tests (UI, workflows)
      /  \       ~10% of tests - slow, high value
     /____\      
    /      \     Integration Tests (API, DB)
   /        \    ~30% of tests - medium speed
  /__________\   
 /            \  Unit Tests (models, logic)
/______________\ ~60% of tests - fast, comprehensive
```

### Test Organization

```
tests/
├── AcademicAssessment.Tests.Unit/
│   ├── Models/              # Domain model tests
│   ├── Repositories/        # Repository logic tests (with InMemory DB)
│   ├── Common/              # Common utilities tests
│   ├── Analytics/           # Analytics service tests
│   └── Orchestration/       # Multi-agent orchestration tests
│
├── AcademicAssessment.Tests.Integration/
│   ├── Controllers/         # API endpoint tests
│   ├── Database/            # Database integration tests
│   └── Fixtures/            # Test fixtures and factories
│
├── AcademicAssessment.Tests.UI/
│   └── BasicFunctionalityTests.cs  # End-to-end workflow tests
│
├── AcademicAssessment.Tests.Performance/
│   └── (Performance and load tests)
│
├── coverlet.runsettings     # Coverage configuration
└── test-*.sh                # Test helper scripts
```

---

## 💡 Testing Principles

### 1. **Test Independence**

Each test must run independently - no shared state between tests.

```csharp
// GOOD - Each test creates its own data
[Fact]
public void Test1() {
    var assessment = CreateTestAssessment();
    // ...
}

[Fact]
public void Test2() {
    var assessment = CreateTestAssessment();
    // ...
}
```

### 2. **Arrange-Act-Assert (AAA)**

Structure tests with clear sections:

```csharp
[Fact]
public void GetById_ExistingId_ReturnsAssessment()
{
    // Arrange
    var repository = CreateRepository();
    var id = Guid.NewGuid();
    
    // Act
    var result = await repository.GetByIdAsync(id);
    
    // Assert
    result.Should().NotBeNull();
}
```

### 3. **FluentAssertions**

Use FluentAssertions for readable, expressive assertions:

```csharp
// GOOD
result.Should().NotBeNull();
list.Should().HaveCount(5);
exception.Should().BeOfType<ArgumentNullException>();

// AVOID
Assert.NotNull(result);
Assert.Equal(5, list.Count);
```

### 4. **Test Naming**

Use descriptive names following pattern: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
[Fact]
public void GetByIdAsync_ExistingId_ReturnsSuccess() { }

[Fact]
public void GetByIdAsync_NonExistentId_ReturnsNotFoundError() { }
```

### 5. **Coverage Goals**

- **Overall:** 70%+ code coverage
- **Critical paths:** 90%+ coverage required
- **New code:** Must include tests before PR approval

---

## 🚀 Common Commands

### Run All Tests

```bash
# All test projects
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific configuration
dotnet test --configuration Release
```

### Run Specific Test Project

```bash
# Unit tests only
dotnet test tests/AcademicAssessment.Tests.Unit/

# Integration tests only
dotnet test tests/AcademicAssessment.Tests.Integration/

# UI tests only
dotnet test tests/AcademicAssessment.Tests.UI/
```

### Run Specific Test

```bash
# By fully qualified name
dotnet test --filter "FullyQualifiedName~AssessmentTests"

# By test name pattern
dotnet test --filter "DisplayName~GetById"

# By category/trait
dotnet test --filter "Category=Unit"
```

### View Coverage

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Coverage files generated in:
# tests/*/TestResults/*/coverage.cobertura.xml
```

---

## 📞 Getting Help

### Test Failures

1. Check **[12-troubleshooting.md](./12-troubleshooting.md)** for common issues
2. Review test logs in console or CI/CD output
3. Debug locally following **[02-local-testing.md](./02-local-testing.md)**

### Coverage Issues

1. Review **[08-coverage.md](./08-coverage.md)** for analysis strategies
2. Identify untested code paths
3. Add tests following component-specific guides

### CI/CD Test Failures

1. Check GitHub Actions logs
2. Review **[09-cicd-testing.md](./09-cicd-testing.md)** for CI-specific issues
3. Reproduce locally before fixing

---

## 🔄 Document Maintenance

**Last Updated:** 2025-10-25  
**Version:** 1.0.0

These testing guides are living documents. Update them when:

- New testing patterns emerge
- Test frameworks are upgraded
- Coverage requirements change
- New test types are added
- Best practices evolve

---

**Ready to test?** Start with **[01-overview.md](./01-overview.md)** for the full context, or jump to **[02-local-testing.md](./02-local-testing.md)** to run tests immediately.
