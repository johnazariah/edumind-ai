# Testing Strategy

**Created:** January 15, 2025  
**Status:** Active  
**Version:** 2.0 (Updated for OLLAMA Integration)

## Overview

The EduMind.AI project uses a multi-tiered testing strategy that balances speed, coverage, and practical CI/CD constraints. Tests are categorized by speed, dependencies, and when they run.

This document replaces the previous version to reflect the new OLLAMA integration and CI/CD pipeline separation.

## Table of Contents

1. [Test Categories](#test-categories)
2. [CI/CD Pipeline Architecture](#cicd-pipeline-architecture)
3. [Local Development Workflow](#local-development-workflow)
4. [Test Data Management](#test-data-management)
5. [Environment Variables](#environment-variables)
6. [Test Coverage Goals](#test-coverage-goals)
7. [Troubleshooting](#troubleshooting)
8. [Best Practices](#best-practices)
9. [Future Improvements](#future-improvements)

---

## Test Categories

### 1. Unit Tests ⚡

**Purpose**: Fast, isolated tests with no external dependencies

**Location**: `tests/AcademicAssessment.Tests.Unit/`

**Scope**:

- Services (Analytics, Orchestration, Agents)
- Repositories (with mocked DbContext)
- Utilities and helpers
- Domain models and validation logic

**Example**:

```csharp
[Fact]
public async Task CalculatePerformanceSummary_WithNoAssessments_ReturnsEmptySummary()
{
    // Arrange
    var mockRepo = new Mock<IStudentAssessmentRepository>();
    mockRepo.Setup(r => r.GetByStudentIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(Result<IReadOnlyList<StudentAssessment>>.Success(new List<StudentAssessment>()));
    
    var service = new StudentAnalyticsService(mockRepo.Object, ...);
    
    // Act
    var result = await service.GetStudentPerformanceSummaryAsync(studentId);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.TotalAssessmentsTaken.Should().Be(0);
}
```

### 2. Integration Tests

**Purpose**: Test complete request/response cycles with real infrastructure

**Location**: `tests/AcademicAssessment.Tests.Integration/`

**Scope**:

- API controllers with WebApplicationFactory
- Database operations with TestContainers
- Service integration with real dependencies
- Authentication/Authorization flows

**Example**:

```csharp
[Fact]
public async Task GetPerformanceSummary_WithValidStudentId_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync($"/api/v1/students/{studentId}/analytics/performance-summary");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<StudentPerformanceSummary>();
    content.StudentId.Should().Be(studentId);
}
```

### 3. Performance Tests

**Purpose**: Verify system performance under load

**Location**: `tests/AcademicAssessment.Tests.Performance/`

**Scope**:

- API endpoint response times
- Database query performance
- Concurrent user scenarios
- Memory and resource usage

**Tools**: BenchmarkDotNet, NBomber

### 4. End-to-End Tests

**Purpose**: Test complete user workflows

**Future Implementation**: Playwright or Selenium

**Scope**:

- Student assessment taking workflow
- Teacher dashboard interactions
- Admin configuration flows

---

## Testing Frameworks & Tools

### Core Testing Framework

- **xUnit**: Primary test runner
- **FluentAssertions**: Expressive assertions
- **Moq**: Mocking framework

### Integration Testing

- **WebApplicationFactory**: In-memory API testing
- **TestContainers**: Docker-based test dependencies
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for fast tests

### Code Coverage

- **Coverlet**: Code coverage collection
- **ReportGenerator**: HTML coverage reports

### Performance Testing

- **BenchmarkDotNet**: Microbenchmarks
- **NBomber**: Load testing

### CI/CD

- **GitHub Actions**: Automated workflows
- **dorny/test-reporter**: Test result visualization
- **marocchino/sticky-pull-request-comment**: PR coverage comments

---

## Test Organization

### Project Structure

```
tests/
├── AcademicAssessment.Tests.Unit/
│   ├── Agents/
│   │   ├── Mathematics/
│   │   ├── Physics/
│   │   └── Shared/
│   ├── Analytics/
│   │   └── StudentAnalyticsServiceTests.cs
│   ├── Orchestration/
│   │   └── AssessmentOrchestratorTests.cs
│   └── Infrastructure/
│       └── Repositories/
├── AcademicAssessment.Tests.Integration/
│   ├── Controllers/
│   │   └── StudentAnalyticsControllerTests.cs
│   ├── Services/
│   ├── Database/
│   └── TestFixtures/
│       └── WebApplicationFactory.cs
└── AcademicAssessment.Tests.Performance/
    ├── Benchmarks/
    └── LoadTests/
```

### Naming Conventions

- **Test Classes**: `{ClassUnderTest}Tests.cs`
- **Test Methods**: `{MethodUnderTest}_{Scenario}_{ExpectedBehavior}`

Examples:

- `GetPerformanceSummary_WithValidStudentId_ReturnsOk`
- `GetImprovementAreas_WithInvalidTopN_ReturnsBadRequest`
- `CalculateAbilityEstimate_WithNoResponses_ReturnsDefaultValue`

---

## CI/CD Integration

### GitHub Actions Workflow

Our CI pipeline runs on every push and pull request:

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
      - name: Setup .NET 8.0
      - name: Restore dependencies
      - name: Build solution
      - name: Run tests with coverage
      - name: Generate coverage report
      - name: Add coverage comment to PR
      - name: Publish test results
```

### Pipeline Stages

#### 1. Build Stage

- Restore NuGet packages
- Build solution in Release mode
- Verify no compilation errors

#### 2. Test Stage

- Run all unit tests
- Run all integration tests
- Collect code coverage
- Generate coverage reports

#### 3. Quality Stage

- Check code formatting (`dotnet format`)
- Run code analysis
- Enforce code style rules

#### 4. Report Stage

- Publish test results to GitHub Actions
- Generate HTML coverage report
- Add coverage summary to PR comments
- Upload artifacts (coverage reports, test results)

### Test Execution

```bash
# Run all tests
dotnet test EduMind.AI.sln --configuration Release

# Run with coverage
dotnet test EduMind.AI.sln \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Run specific test project
dotnet test tests/AcademicAssessment.Tests.Integration/
```

---

## Code Coverage

### Coverage Goals

- **Overall Target**: 80% code coverage
- **Critical Services**: 90% coverage
- **Controllers**: 85% coverage
- **Utilities**: 95% coverage

### Coverage Report

Coverage is automatically generated and published:

1. **HTML Report**: Uploaded as CI artifact
2. **PR Comment**: Summary added to pull requests
3. **Trend Analysis**: Track coverage over time

### View Coverage Locally

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html"

# Open in browser
open coverage-report/index.html
```

---

## Testing the Analytics API

### Manual Testing

#### 1. Local Development

Start the API:

```bash
dotnet run --project src/AcademicAssessment.Web/AcademicAssessment.Web.csproj
```

Access Swagger UI: `http://localhost:5103/swagger`

#### 2. Test Script

We provide an automated test script:

```bash
./test-analytics-api.sh
```

This script tests all 7 analytics endpoints and validates:

- ✅ HTTP response codes
- ✅ Response structure
- ✅ Validation logic
- ✅ Error handling

### Automated Testing

#### Integration Tests

Run the StudentAnalyticsController integration tests:

```bash
dotnet test tests/AcademicAssessment.Tests.Integration/Controllers/StudentAnalyticsControllerTests.cs
```

These tests cover:

- ✅ All 7 endpoints
- ✅ Valid and invalid inputs
- ✅ Authorization checks
- ✅ Query parameter validation
- ✅ Response content validation
- ✅ Performance benchmarks

#### CI/CD Testing

Tests run automatically on:

- Every commit to `main` or `develop`
- Every pull request
- Scheduled nightly builds (optional)

### API Test Coverage

| Endpoint | Unit Tests | Integration Tests | Manual Tests |
|----------|-----------|-------------------|--------------|
| `/performance-summary` | ✅ | ✅ | ✅ |
| `/subject-performance` | ✅ | ✅ | ✅ |
| `/learning-objectives` | ✅ | ✅ | ✅ |
| `/ability-estimates` | ✅ | ✅ | ✅ |
| `/improvement-areas` | ✅ | ✅ | ✅ |
| `/progress-timeline` | ✅ | ✅ | ✅ |
| `/peer-comparison` | ✅ | ✅ | ✅ |

---

## Best Practices

### 1. Test Independence

- Each test should run independently
- No shared state between tests
- Use test fixtures for setup/teardown

### 2. Fast Execution

- Unit tests should run in milliseconds
- Integration tests in seconds
- Use in-memory databases for speed

### 3. Readable Tests

- Use Arrange-Act-Assert pattern
- Descriptive test names
- Clear assertions with FluentAssertions

### 4. Realistic Test Data

- Use realistic student IDs, dates, scores
- Test boundary conditions
- Include edge cases

### 5. Maintainability

- Keep tests simple and focused
- Avoid test duplication
- Use test helpers and builders

### 6. Continuous Improvement

- Monitor test execution times
- Refactor slow tests
- Update tests with code changes

---

## Running Tests in Different Environments

### Local Development

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/AcademicAssessment.Tests.Unit/

# Run with filter
dotnet test --filter "Category=Analytics"

# Run with verbosity
dotnet test --verbosity detailed
```

### CI/CD (GitHub Actions)

Tests run automatically with:

- .NET 8.0 SDK
- Ubuntu Linux runner
- Parallel test execution
- Code coverage collection

### Docker

```bash
# Run tests in container
docker-compose -f docker-compose.test.yml up

# View test results
docker-compose -f docker-compose.test.yml logs test-runner
```

---

## Next Steps

### Short Term

1. ✅ Complete integration tests for StudentAnalyticsController
2. ⏳ Add unit tests for StudentAnalyticsService
3. ⏳ Implement TestContainers for database tests
4. ⏳ Add authentication/authorization tests

### Medium Term

1. Add performance benchmarks
2. Implement load testing with NBomber
3. Create test data seeding scripts
4. Add mutation testing

### Long Term

1. E2E tests with Playwright
2. Visual regression testing
3. Chaos engineering tests
4. Production monitoring integration

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq Quickstart](https://github.com/moq/moq4)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [TestContainers](https://dotnet.testcontainers.org/)
- [GitHub Actions for .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)

---

## Conclusion

This testing strategy ensures:

- ✅ **Quality**: Comprehensive test coverage across all layers
- ✅ **Speed**: Fast feedback loop for developers
- ✅ **Confidence**: Automated testing in CI/CD
- ✅ **Maintainability**: Well-organized, readable tests
- ✅ **Scalability**: Testing infrastructure grows with the project

All changes should include appropriate tests, and all tests must pass before merging to main.
