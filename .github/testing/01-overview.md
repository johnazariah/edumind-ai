# Testing Overview & Philosophy

**Purpose:** Understand EduMind.AI's testing strategy, architecture, and quality standards.

**Audience:** All developers working on the project.

---

## 🎯 Testing Strategy

### Our Testing Philosophy

EduMind.AI follows a **comprehensive, multi-layered testing approach** that balances:

- **Speed:** Fast feedback during development
- **Confidence:** High coverage of critical paths
- **Maintainability:** Tests that remain valuable over time
- **Pragmatism:** Right level of testing for each component

We believe:

✅ **Tests are documentation** - They show how code should be used  
✅ **Fast tests matter** - Quick feedback enables rapid iteration  
✅ **Integration tests catch real bugs** - Unit tests alone aren't sufficient  
✅ **Coverage is a guide, not a goal** - 100% coverage doesn't mean bug-free  
✅ **Flaky tests must be fixed or removed** - Unreliable tests erode confidence

---

## 🏗️ Test Architecture

### The Test Pyramid

We follow the test pyramid with adjusted proportions for our AI-enhanced system:

```
         /\           E2E & UI Tests
        /  \          • Full user workflows
       / 5% \         • Browser automation
      /______\        • ~5% of total tests
     /        \       
    / AI/Agent \      AI & Agent Tests
   /    15%     \     • Ollama integration
  /              \    • Question generation
 /________________\   • Multi-agent orchestration
/                  \  
/   Integration     \ Integration Tests
/       30%         \ • API endpoints
/____________________\ • Database operations
/                     \
/      Unit Tests      \
/         50%           \
/_______________________\

Total: ~1000+ tests across all layers
```

### Test Project Structure

```
tests/
├── AcademicAssessment.Tests.Unit/          [~500 tests, <10s runtime]
│   ├── Models/                             Domain model behavior
│   ├── Repositories/                       Repository logic (InMemory DB)
│   ├── Common/                             Utilities and helpers
│   ├── Analytics/                          Analytics calculations
│   └── Orchestration/                      Agent coordination logic
│
├── AcademicAssessment.Tests.Integration/   [~300 tests, <60s runtime]
│   ├── Controllers/                        API endpoint integration
│   ├── Database/                           Real PostgreSQL tests
│   └── Fixtures/                           Shared test infrastructure
│
├── AcademicAssessment.Tests.UI/            [~50 tests, <5min runtime]
│   └── Workflows/                          End-to-end user scenarios
│
├── AcademicAssessment.Tests.Performance/   [~20 tests, <10min runtime]
│   └── Load/                               Performance and stress tests
│
└── coverlet.runsettings                    Coverage configuration
```

---

## 📊 Testing Standards

### Coverage Goals

| Component | Target | Rationale |
|-----------|--------|-----------|
| **Domain Models** | 90%+ | Core business logic must be thoroughly tested |
| **Repositories** | 80%+ | Data access is critical for correctness |
| **API Controllers** | 80%+ | External contract must be reliable |
| **Services/Agents** | 75%+ | Business logic needs strong coverage |
| **Blazor Components** | 60%+ | UI logic harder to test, focus on critical paths |
| **Infrastructure** | 50%+ | Configuration and plumbing |
| **Overall Project** | 70%+ | Balanced coverage across all layers |

### Quality Gates

**Before Merging PR:**

- ✅ All tests pass locally
- ✅ All tests pass in CI/CD
- ✅ Code coverage maintained or improved
- ✅ No new flaky tests introduced
- ✅ Test execution time reasonable (<15min for full suite)

**Before Release:**

- ✅ All test suites green
- ✅ E2E smoke tests pass in staging
- ✅ Performance tests show no regression
- ✅ AI agent tests validate quality
- ✅ Manual smoke test checklist completed

---

## 🛠️ Testing Tools & Frameworks

### Primary Frameworks

#### xUnit (2.5.3 / 2.9.2)

Our test framework of choice for .NET.

```csharp
[Fact]  // Single test
public void SimpleTest() { }

[Theory]  // Parameterized test
[InlineData(1)]
[InlineData(2)]
public void ParameterizedTest(int value) { }
```

**Why xUnit?**

- Modern, actively maintained
- Excellent parallel execution
- Clean attribute syntax
- Strong community support

#### FluentAssertions (6.12.1 / 7.0.0)

Expressive, readable assertions.

```csharp
// Readable and descriptive
result.Should().NotBeNull();
list.Should().HaveCount(5)
    .And.Contain(x => x.Id == expectedId);
exception.Should().BeOfType<ArgumentNullException>()
    .Which.ParamName.Should().Be("studentId");

// Better error messages than Assert
// "Expected list to have 5 item(s), but found 3"
```

#### Moq (4.20.72)

Mocking framework for test doubles.

```csharp
var mockRepo = new Mock<IAssessmentRepository>();
mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
    .ReturnsAsync(new Assessment { /* ... */ });

// Verify method was called
mockRepo.Verify(r => r.SaveAsync(It.IsAny<Assessment>(), default), 
    Times.Once);
```

### Specialized Tools

#### Microsoft.AspNetCore.Mvc.Testing

WebApplicationFactory for integration testing APIs.

```csharp
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    
    public ApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }
}
```

#### Microsoft.Playwright (1.48.0)

Browser automation for E2E tests.

```csharp
await using var browser = await playwright.Chromium.LaunchAsync();
var page = await browser.NewPageAsync();
await page.GotoAsync("https://localhost:5049");
await page.ClickAsync("text=Start Assessment");
```

#### coverlet

Code coverage collection (.NET native).

```bash
dotnet test --collect:"XPlat Code Coverage"
# Generates coverage.cobertura.xml
```

---

## 🔄 Test Execution Model

### Local Development

**Fast Feedback Loop:**

```
Code Change
    ↓
Save File (Ctrl+S)
    ↓
IDE Auto-Runs Affected Tests (<1s)
    ↓
Green ✅ → Continue | Red ❌ → Fix
```

**Pre-Commit:**

```bash
# Run all unit tests (~10s)
dotnet test tests/AcademicAssessment.Tests.Unit/

# If touching API:
dotnet test tests/AcademicAssessment.Tests.Integration/
```

### CI/CD Pipeline

```
Push/PR → GitHub Actions
    ↓
┌─────────────────────────────────────┐
│ Build & Unit Tests (3-5 min)       │
│ • dotnet build                      │
│ • Unit tests in parallel            │
│ • Coverage collection               │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ Integration Tests (5-10 min)       │
│ • PostgreSQL container              │
│ • Redis container                   │
│ • Database migrations               │
│ • API integration tests             │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ E2E Tests (10-15 min)               │
│ • All services running              │
│ • Playwright browser tests          │
│ • Critical user workflows           │
└─────────────────────────────────────┘
    ↓
✅ All Green → Merge Allowed
❌ Any Red → Must Fix
```

---

## 📝 Testing Conventions

### Test Naming

**Pattern:** `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
// GOOD - Clear and descriptive
[Fact]
public void GetByIdAsync_ExistingId_ReturnsSuccess() { }

[Fact]
public void GetByIdAsync_NonExistentId_ReturnsNotFoundError() { }

[Fact]
public void CalculateScore_AllCorrectAnswers_Returns100Percent() { }

// AVOID - Ambiguous
[Fact]
public void TestGetById() { }

[Fact]
public void Test1() { }
```

### Test Structure (AAA Pattern)

**Arrange-Act-Assert** with explicit comments:

```csharp
[Fact]
public void Assessment_IsAdaptive_ReturnsTrueForAdaptiveType()
{
    // Arrange
    var assessment = new Assessment 
    { 
        Id = Guid.NewGuid(),
        AssessmentType = AssessmentType.Adaptive 
    };
    
    // Act
    var result = assessment.IsAdaptive;
    
    // Assert
    result.Should().BeTrue();
}
```

### Test Organization

Use `#region` to group related tests:

```csharp
public class AssessmentTests
{
    #region Test Helpers
    
    private static Assessment CreateTestAssessment() => new() 
    { 
        // ... 
    };
    
    #endregion
    
    #region Constructor Tests
    
    [Fact]
    public void Constructor_SetsAllProperties() { }
    
    #endregion
    
    #region Computed Property Tests
    
    [Fact]
    public void IsAdaptive_WhenAdaptiveType_ReturnsTrue() { }
    
    #endregion
}
```

---

## 🎓 Test-Driven Development (TDD)

While not strictly enforced, TDD is encouraged for:

- **New features** - Write test first to clarify requirements
- **Bug fixes** - Write failing test, then fix
- **Refactoring** - Tests provide safety net

### TDD Cycle

```
Red → Green → Refactor
 ↓      ↓       ↓
Write  Make    Improve
Test   It      Code
       Pass
```

**Example TDD Workflow:**

```csharp
// 1. RED - Write failing test
[Fact]
public void CalculateProgress_CompletedAssessment_Returns100()
{
    var progress = ProgressCalculator.Calculate(totalQuestions: 10, answered: 10);
    progress.Should().Be(100.0);
}
// Test fails - method doesn't exist yet

// 2. GREEN - Implement minimum code to pass
public static double Calculate(int totalQuestions, int answered)
{
    return answered == totalQuestions ? 100.0 : 0.0;  // Simplest solution
}
// Test passes

// 3. REFACTOR - Improve implementation
public static double Calculate(int totalQuestions, int answered)
{
    if (totalQuestions <= 0) throw new ArgumentException(nameof(totalQuestions));
    return (answered / (double)totalQuestions) * 100.0;  // Proper calculation
}
// Test still passes, code is better
```

---

## 🔬 Test Categories

### Unit Tests (50% of suite)

**Scope:** Single class or method in isolation

**Characteristics:**

- ⚡ Fast (<1ms per test)
- 🔒 Isolated (no external dependencies)
- 🎯 Focused (one behavior per test)
- 📊 High volume (many tests)

**What to Test:**

- Domain model behavior
- Business logic calculations
- Validation rules
- Algorithm correctness
- Edge cases and error handling

**Example:**

```csharp
[Fact]
public void Assessment_QuestionCount_ReturnsNumberOfQuestions()
{
    // Arrange
    var assessment = new Assessment 
    { 
        QuestionIds = new[] { Guid.NewGuid(), Guid.NewGuid() } 
    };
    
    // Act
    var count = assessment.QuestionCount;
    
    // Assert
    count.Should().Be(2);
}
```

### Integration Tests (30% of suite)

**Scope:** Multiple components working together

**Characteristics:**

- 🐌 Slower (100ms-1s per test)
- 🔗 Connected (uses real database, Redis)
- 🎯 Broader (tests interactions)
- 📊 Moderate volume

**What to Test:**

- API endpoints end-to-end
- Database operations
- Repository implementations
- Service integrations
- Error handling across layers

**Example:**

```csharp
[Fact]
public async Task GetAssessment_ExistingId_ReturnsOkWithData()
{
    // Arrange
    var client = factory.CreateClient();
    var id = await SeedTestAssessmentAsync();
    
    // Act
    var response = await client.GetAsync($"/api/v1/assessment/{id}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var assessment = await response.Content.ReadFromJsonAsync<Assessment>();
    assessment.Should().NotBeNull();
}
```

### E2E/UI Tests (5% of suite)

**Scope:** Complete user workflows

**Characteristics:**

- 🐢 Slow (10s-60s per test)
- 🌐 Full stack (all services + browser)
- 🎯 Widest (critical paths only)
- 📊 Low volume (expensive)

**What to Test:**

- Critical user journeys
- Multi-page workflows
- Authentication flows
- Error scenarios users see
- Cross-browser compatibility

**Example:**

```csharp
[Fact]
public async Task StudentTakesAssessment_EndToEnd_Success()
{
    // Student logs in, selects assessment, answers questions, submits, views results
    await page.GotoAsync("https://localhost:5049");
    await page.ClickAsync("text=Start Assessment");
    // ... multiple steps ...
    await page.ClickAsync("text=Submit");
    await Expect(page.Locator(".success-message")).ToBeVisibleAsync();
}
```

### AI/Agent Tests (15% of suite)

**Scope:** LLM integration and agent behavior

**Characteristics:**

- 🐌 Very slow (10s-120s per test)
- 🤖 Requires Ollama
- 🎯 Quality validation
- 📊 Focused volume

**What to Test:**

- Question generation quality
- Feedback relevance
- Multi-agent orchestration
- Prompt effectiveness
- Response consistency

**Example:**

```bash
# Test Ollama integration
./tests/test-ollama-integration.sh

# Expected output:
# ✅ Ollama service is running
# ✅ Model llama3.2:3b is available
# ✅ Question generation works
```

---

## 📈 Test Metrics

### Key Metrics We Track

1. **Test Count:** ~1000+ tests total
2. **Execution Time:** <15 minutes full suite
3. **Code Coverage:** 70%+ overall
4. **Flaky Rate:** <1% (tests that randomly fail)
5. **PR Block Rate:** % of PRs blocked by test failures

### Coverage Analysis

Generated in CI/CD and viewable locally:

```bash
dotnet test --collect:"XPlat Code Coverage"

# Coverage report in:
# tests/*/TestResults/*/coverage.cobertura.xml
```

**Coverage Quality Over Quantity:**

- ✅ 70% coverage of critical code > 90% coverage of trivial code
- ✅ Test complex business logic thoroughly
- ✅ Don't test framework code (EF, ASP.NET internals)
- ✅ Focus on scenarios that can realistically break

---

## 🚦 Test Health

### Healthy Test Characteristics

- **Fast:** Unit tests <1ms, integration <1s, E2E <60s
- **Reliable:** Pass consistently (not flaky)
- **Isolated:** No dependencies on other tests
- **Readable:** Clear what's being tested
- **Maintainable:** Easy to update when code changes
- **Valuable:** Would catch real bugs

### Unhealthy Test Smells

❌ **Flaky tests** - Pass/fail randomly  
❌ **Slow tests** - Take too long to execute  
❌ **Brittle tests** - Break on unrelated changes  
❌ **Unclear tests** - Hard to understand what they test  
❌ **Redundant tests** - Multiple tests for same behavior  
❌ **Testing implementation** - Tests internal details, not behavior

### Test Debt Management

**Regular Maintenance:**

- Review flaky tests monthly
- Remove redundant tests
- Update tests when requirements change
- Refactor tests for clarity
- Keep test execution time under control

---

## 🎯 Next Steps

### For New Developers

1. **Read this overview** ✅ You're here!
2. **Set up local testing:** [02-local-testing.md](./02-local-testing.md)
3. **Learn unit testing:** [03-unit-testing.md](./03-unit-testing.md)
4. **Write your first test** using the patterns you've learned

### For Experienced Developers

- **Review specific guides** for your work:
  - API changes → [04-integration-testing.md](./04-integration-testing.md)
  - UI features → [05-e2e-testing.md](./05-e2e-testing.md)
  - AI agents → [06-ai-agent-testing.md](./06-ai-agent-testing.md)
- **Check coverage:** [08-coverage.md](./08-coverage.md)
- **Optimize CI/CD:** [09-cicd-testing.md](./09-cicd-testing.md)

---

**Last Updated:** 2025-10-25  
**Related:** [README](./README.md) | [Local Testing](./02-local-testing.md) | [Unit Testing](./03-unit-testing.md)
