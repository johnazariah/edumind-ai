# Unit Testing Guide

**Purpose:** Write effective unit tests for EduMind.AI using established patterns.

**Audience:** All developers writing tests for domain logic, models, and services.

---

## 🎯 What to Unit Test

Unit tests verify **individual components in isolation** without external dependencies.

### Test These Components

✅ **Domain Models** - Business logic, computed properties, validation  
✅ **Repository Logic** - Using InMemory database for isolation  
✅ **Services** - Business rules and calculations  
✅ **Utilities** - Helper methods and extensions  
✅ **Validators** - Input validation logic  
✅ **Mappers** - DTO transformations

### Don't Unit Test These

❌ **Framework code** - EF Core internals, ASP.NET middleware  
❌ **Configuration** - appsettings.json parsing  
❌ **External APIs** - Use integration tests instead  
❌ **Database schema** - Use migration tests  
❌ **UI rendering** - Use E2E tests

---

## 📝 Test Structure

### File Organization

```
tests/AcademicAssessment.Tests.Unit/
├── Models/
│   ├── AssessmentTests.cs          # Assessment model tests
│   ├── QuestionTests.cs            # Question model tests
│   └── StudentAssessmentTests.cs   # StudentAssessment model tests
│
├── Repositories/
│   ├── AssessmentRepositoryTests.cs
│   └── StudentRepositoryTests.cs
│
├── Analytics/
│   └── ProgressCalculatorTests.cs
│
├── Common/
│   └── ResultTests.cs              # Result<T> utility tests
│
└── Orchestration/
    └── MultiAgentOrchestratorTests.cs
```

**Naming Convention:**

- File: `{ClassName}Tests.cs`
- Class: `public class {ClassName}Tests`
- Test: `[Fact] public void MethodName_Scenario_ExpectedBehavior()`

### Test Class Template

```csharp
using FluentAssertions;
using Xunit;

namespace AcademicAssessment.Tests.Unit.Models;

public class AssessmentTests
{
    #region Test Helpers
    
    /// <summary>
    /// Creates a valid test assessment with default values
    /// </summary>
    private static Assessment CreateTestAssessment() => new()
    {
        Id = Guid.NewGuid(),
        CourseId = Guid.NewGuid(),
        Title = "Test Assessment",
        Subject = "Mathematics",
        GradeLevel = "High School",
        Difficulty = "Intermediate",
        AssessmentType = AssessmentType.Diagnostic,
        EstimatedDurationMinutes = 45,
        PassingScore = 70.0,
        MaxAttempts = 3,
        QuestionIds = new List<Guid> { Guid.NewGuid() },
        Topics = new List<string> { "Algebra", "Geometry" },
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    
    #endregion
    
    #region Constructor Tests
    
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var title = "Algebra Final";
        var createdAt = DateTimeOffset.UtcNow;
        
        // Act
        var assessment = new Assessment
        {
            Id = id,
            CourseId = courseId,
            Title = title,
            CreatedAt = createdAt
            // ... other required properties
        };
        
        // Assert
        assessment.Id.Should().Be(id);
        assessment.CourseId.Should().Be(courseId);
        assessment.Title.Should().Be(title);
        assessment.CreatedAt.Should().Be(createdAt);
    }
    
    #endregion
    
    #region Computed Property Tests
    
    [Fact]
    public void QuestionCount_ReturnsNumberOfQuestions()
    {
        // Arrange
        var assessment = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            }
        };
        
        // Act
        var count = assessment.QuestionCount;
        
        // Assert
        count.Should().Be(3);
    }
    
    #endregion
}
```

---

## 🧪 AAA Pattern

### Arrange-Act-Assert

Every test should follow this structure with **explicit comments**:

```csharp
[Fact]
public void CalculateScore_AllCorrectAnswers_Returns100Percent()
{
    // Arrange - Set up test data and dependencies
    var totalQuestions = 10;
    var correctAnswers = 10;
    var calculator = new ScoreCalculator();
    
    // Act - Execute the method under test
    var score = calculator.Calculate(totalQuestions, correctAnswers);
    
    // Assert - Verify the expected outcome
    score.Should().Be(100.0);
}
```

**Why AAA?**

- **Readability:** Clear what's setup, execution, and verification
- **Maintainability:** Easy to modify any section
- **Debugging:** Quickly identify which phase failed

### Complex Arrange Example

```csharp
[Fact]
public void GetRecommendations_BasedOnPerformance_ReturnsAppropriate()
{
    // Arrange
    var student = new Student
    {
        Id = Guid.NewGuid(),
        Name = "Test Student"
    };
    
    var assessmentResults = new List<AssessmentResult>
    {
        new() { Score = 45.0, Topic = "Algebra" },
        new() { Score = 85.0, Topic = "Geometry" },
        new() { Score = 62.0, Topic = "Calculus" }
    };
    
    var recommendationService = new RecommendationService();
    
    // Act
    var recommendations = recommendationService.Generate(student, assessmentResults);
    
    // Assert
    recommendations.Should().HaveCount(2);  // Algebra and Calculus need work
    recommendations.Should().Contain(r => r.Topic == "Algebra")
        .Which.Priority.Should().Be(Priority.High);
}
```

---

## ✨ FluentAssertions

### Why FluentAssertions?

**Better Error Messages:**

```csharp
// Using Assert (poor error message)
Assert.Equal(5, list.Count);  
// Error: "Assert.Equal() Failure: Expected 5, Actual 3"

// Using FluentAssertions (descriptive error message)
list.Should().HaveCount(5);
// Error: "Expected list to have 5 item(s), but found 3."
```

**More Readable:**

```csharp
// Assert - procedural
Assert.NotNull(result);
Assert.True(result.IsSuccess);
Assert.Equal("Math", result.Value.Subject);

// FluentAssertions - fluent, natural language
result.Should().NotBeNull();
result.IsSuccess.Should().BeTrue();
result.Value.Subject.Should().Be("Math");
```

### Common Assertions

#### Value Assertions

```csharp
// Equality
value.Should().Be(42);
value.Should().NotBe(0);

// Nullability
obj.Should().BeNull();
obj.Should().NotBeNull();

// Boolean
condition.Should().BeTrue();
condition.Should().BeFalse();

// Numeric comparisons
score.Should().BeGreaterThan(70);
score.Should().BeLessThanOrEqualTo(100);
score.Should().BeInRange(0, 100);
```

#### String Assertions

```csharp
// Equality
name.Should().Be("Expected");
name.Should().BeEquivalentTo("EXPECTED");  // Case-insensitive

// Contains
text.Should().Contain("substring");
text.Should().NotContain("error");
text.Should().StartWith("Hello");
text.Should().EndWith("world");

// Empty/whitespace
str.Should().BeEmpty();
str.Should().BeNullOrEmpty();
str.Should().BeNullOrWhiteSpace();
```

#### Collection Assertions

```csharp
// Count
list.Should().HaveCount(5);
list.Should().NotBeEmpty();
list.Should().BeEmpty();

// Contains
list.Should().Contain(expectedItem);
list.Should().Contain(x => x.Id == expectedId);
list.Should().NotContain(x => x.IsDeleted);

// Equality
list.Should().Equal(expectedList);
list.Should().BeEquivalentTo(expectedList);  // Order-independent

// All/Any
list.Should().OnlyContain(x => x.IsValid);
list.Should().AllBe(x => x.Score > 0);
```

#### Object Assertions

```csharp
// Type checking
obj.Should().BeOfType<Assessment>();
obj.Should().BeAssignableTo<IAssessment>();

// Property assertions
assessment.Should().NotBeNull();
assessment.Should().Match<Assessment>(a => 
    a.Title == "Math" && 
    a.QuestionCount > 0);

// Equivalency (compare by values)
actual.Should().BeEquivalentTo(expected);
actual.Should().BeEquivalentTo(expected, options => 
    options.Excluding(a => a.Id));  // Exclude properties
```

#### Exception Assertions

```csharp
// Synchronous
Action act = () => service.Process(null);
act.Should().Throw<ArgumentNullException>();
act.Should().Throw<ArgumentNullException>()
    .WithMessage("*cannot be null*")
    .And.ParamName.Should().Be("input");

// Asynchronous
Func<Task> act = async () => await service.ProcessAsync(null);
await act.Should().ThrowAsync<ArgumentNullException>();
```

#### Result<T> Assertions (EduMind.AI Specific)

```csharp
// Success case
result.Should().BeOfType<Result<Assessment>.Success>();
var success = result.Should().BeOfType<Result<Assessment>.Success>().Subject;
success.Value.Should().NotBeNull();
success.Value.Title.Should().Be("Expected");

// Failure case
result.Should().BeOfType<Result<Assessment>.Failure>();
var failure = result.Should().BeOfType<Result<Assessment>.Failure>().Subject;
failure.Error.Code.Should().Be("NotFound");
failure.Error.Message.Should().Contain("not found");
```

---

## 🎭 Mocking with Moq

### When to Mock

Mock **external dependencies** to isolate the unit under test:

✅ Repositories  
✅ External services  
✅ HTTP clients  
✅ File system access  
✅ Time providers (DateTime.Now)

### Basic Mocking

```csharp
using Moq;

[Fact]
public async Task GetAssessmentSummary_ValidId_ReturnsSummary()
{
    // Arrange
    var mockRepo = new Mock<IAssessmentRepository>();
    var testAssessment = CreateTestAssessment();
    
    mockRepo
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync(new Result<Assessment>.Success(testAssessment));
    
    var service = new AssessmentService(mockRepo.Object);
    
    // Act
    var summary = await service.GetSummaryAsync(testAssessment.Id);
    
    // Assert
    summary.Should().NotBeNull();
    summary.Title.Should().Be(testAssessment.Title);
}
```

### Setup Patterns

```csharp
// Return specific value
mock.Setup(m => m.Method()).Returns(value);
mock.Setup(m => m.MethodAsync()).ReturnsAsync(value);

// Return different values on successive calls
mock.SetupSequence(m => m.Method())
    .Returns(first)
    .Returns(second)
    .Throws<Exception>();

// Match specific arguments
mock.Setup(m => m.Method(It.Is<int>(x => x > 0))).Returns(value);
mock.Setup(m => m.Method(It.IsAny<Guid>())).Returns(value);

// Throw exception
mock.Setup(m => m.Method()).Throws<InvalidOperationException>();
mock.Setup(m => m.Method()).Throws(new CustomException("message"));

// Callback (for side effects)
mock.Setup(m => m.Method(It.IsAny<string>()))
    .Callback<string>(s => Console.WriteLine(s))
    .Returns(value);
```

### Verification

```csharp
// Verify method was called
mockRepo.Verify(r => r.SaveAsync(It.IsAny<Assessment>(), default), Times.Once);
mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);

// Verify with specific arguments
mockRepo.Verify(r => r.SaveAsync(
    It.Is<Assessment>(a => a.Id == expectedId), 
    default));

// Verify all setups were called
mockRepo.VerifyAll();

// Verify no other calls were made
mockRepo.VerifyNoOtherCalls();
```

### Multiple Dependencies

```csharp
[Fact]
public async Task ComplexOperation_WithMultipleDependencies_Success()
{
    // Arrange
    var mockAssessmentRepo = new Mock<IAssessmentRepository>();
    var mockStudentRepo = new Mock<IStudentRepository>();
    var mockLogger = new Mock<ILogger<ServiceClass>>();
    
    // Setup mocks...
    mockAssessmentRepo
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync(new Result<Assessment>.Success(testAssessment));
    
    mockStudentRepo
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync(new Result<Student>.Success(testStudent));
    
    var service = new ServiceClass(
        mockAssessmentRepo.Object,
        mockStudentRepo.Object,
        mockLogger.Object);
    
    // Act
    var result = await service.ProcessAsync(assessmentId, studentId);
    
    // Assert
    result.Should().BeOfType<Result<ProcessedData>.Success>();
    mockAssessmentRepo.Verify(r => r.GetByIdAsync(assessmentId, default), Times.Once);
    mockStudentRepo.Verify(r => r.GetByIdAsync(studentId, default), Times.Once);
}
```

---

## 🧩 Parameterized Tests

### Theory and InlineData

Test multiple scenarios with one test method:

```csharp
[Theory]
[InlineData(AssessmentType.Diagnostic, false)]
[InlineData(AssessmentType.Formative, false)]
[InlineData(AssessmentType.Summative, false)]
[InlineData(AssessmentType.Adaptive, true)]
public void IsAdaptive_VariesByType(AssessmentType type, bool expected)
{
    // Arrange
    var assessment = CreateTestAssessment() with { AssessmentType = type };
    
    // Act
    var result = assessment.IsAdaptive;
    
    // Assert
    result.Should().Be(expected);
}
```

### MemberData (Complex Data)

For complex test data:

```csharp
public class ScoreCalculatorTests
{
    public static IEnumerable<object[]> ScoreTestData =>
        new List<object[]>
        {
            new object[] { 10, 10, 100.0 },  // All correct
            new object[] { 10, 5, 50.0 },    // Half correct
            new object[] { 10, 0, 0.0 },     // None correct
            new object[] { 1, 1, 100.0 }     // Edge case
        };
    
    [Theory]
    [MemberData(nameof(ScoreTestData))]
    public void Calculate_VariousScenarios_ReturnsExpected(
        int total, int correct, double expected)
    {
        // Arrange
        var calculator = new ScoreCalculator();
        
        // Act
        var score = calculator.Calculate(total, correct);
        
        // Assert
        score.Should().Be(expected);
    }
}
```

### ClassData (Reusable Test Data)

```csharp
public class AssessmentTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Elementary", 30 };
        yield return new object[] { "Middle School", 45 };
        yield return new object[] { "High School", 60 };
        yield return new object[] { "College", 90 };
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(AssessmentTestData))]
public void EstimatedDuration_VariesByGradeLevel(
    string gradeLevel, int expectedMinutes)
{
    // Test implementation...
}
```

---

## 🗃️ Testing with InMemory Database

For repository tests, use EF Core InMemory provider:

```csharp
using Microsoft.EntityFrameworkCore;

public class AssessmentRepositoryTests
{
    private AcademicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AcademicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AcademicDbContext(options);
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingAssessment_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AssessmentRepository(context);
        
        var assessment = CreateTestAssessment();
        await context.Assessments.AddAsync(assessment);
        await context.SaveChangesAsync();
        
        // Act
        var result = await repository.GetByIdAsync(assessment.Id);
        
        // Assert
        result.Should().BeOfType<Result<Assessment>.Success>();
        var success = (Result<Assessment>.Success)result;
        success.Value.Id.Should().Be(assessment.Id);
    }
    
    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsFailure()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AssessmentRepository(context);
        
        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        
        // Assert
        result.Should().BeOfType<Result<Assessment>.Failure>();
        var failure = (Result<Assessment>.Failure)result;
        failure.Error.Code.Should().Be("NotFound");
    }
}
```

**Why InMemory for Unit Tests?**

- ✅ Fast (no I/O)
- ✅ Isolated (each test gets new database)
- ✅ No cleanup needed
- ✅ Tests actual repository logic

**When to Use Real Database:**

- Integration tests (see [04-integration-testing.md](./04-integration-testing.md))
- Testing migrations
- Testing database-specific features (e.g., full-text search)

---

## 📊 Test Organization with Regions

Group related tests for better readability:

```csharp
public class AssessmentTests
{
    #region Test Helpers
    
    private static Assessment CreateTestAssessment() { /* ... */ }
    private static Student CreateTestStudent() { /* ... */ }
    
    #endregion
    
    #region Constructor Tests
    
    [Fact]
    public void Constructor_SetsAllProperties() { /* ... */ }
    
    [Fact]
    public void Constructor_RequiredProperties_ThrowsWhenMissing() { /* ... */ }
    
    #endregion
    
    #region Computed Property Tests
    
    [Fact]
    public void QuestionCount_ReturnsNumberOfQuestions() { /* ... */ }
    
    [Fact]
    public void IsAdaptive_WhenAdaptiveType_ReturnsTrue() { /* ... */ }
    
    #endregion
    
    #region Business Logic Tests
    
    [Fact]
    public void CalculateProgress_HalfComplete_Returns50Percent() { /* ... */ }
    
    #endregion
}
```

---

## ⚠️ Common Pitfalls

### 1. Testing Implementation Details

❌ **Bad - Tests internal implementation:**

```csharp
[Fact]
public void Calculate_UsesSpecificAlgorithm()
{
    // Verifies HOW it works, not WHAT it does
    mockHelper.Verify(h => h.InternalMethod(), Times.Once);
}
```

✅ **Good - Tests behavior:**

```csharp
[Fact]
public void Calculate_ReturnsCorrectResult()
{
    // Verifies WHAT it does
    var result = calculator.Calculate(10, 5);
    result.Should().Be(50.0);
}
```

### 2. Shared Test State

❌ **Bad - Shared mutable state:**

```csharp
private Assessment sharedAssessment = CreateTestAssessment();

[Fact]
public void Test1() 
{
    sharedAssessment.Title = "Modified";  // Affects other tests!
}
```

✅ **Good - Each test creates own data:**

```csharp
[Fact]
public void Test1() 
{
    var assessment = CreateTestAssessment();
    assessment = assessment with { Title = "Modified" };  // Immutable
}
```

### 3. Testing Multiple Things

❌ **Bad - Too many assertions:**

```csharp
[Fact]
public void Assessment_VariousProperties()
{
    var assessment = CreateTestAssessment();
    assessment.Title.Should().NotBeEmpty();
    assessment.QuestionCount.Should().BeGreaterThan(0);
    assessment.IsAdaptive.Should().BeFalse();
    // ... 10 more assertions
}
```

✅ **Good - One behavior per test:**

```csharp
[Fact]
public void Assessment_Title_IsNotEmpty() 
{
    var assessment = CreateTestAssessment();
    assessment.Title.Should().NotBeEmpty();
}

[Fact]
public void Assessment_HasQuestions() 
{
    var assessment = CreateTestAssessment();
    assessment.QuestionCount.Should().BeGreaterThan(0);
}
```

### 4. Over-Mocking

❌ **Bad - Mocking value objects:**

```csharp
var mockAssessment = new Mock<Assessment>();  // Don't mock domain models!
mockAssessment.Setup(a => a.Title).Returns("Test");
```

✅ **Good - Use real objects:**

```csharp
var assessment = new Assessment { Title = "Test", /* ... */ };
```

---

## 🏃 Running Unit Tests

```bash
# All unit tests
dotnet test tests/AcademicAssessment.Tests.Unit/

# Specific test class
dotnet test --filter "FullyQualifiedName~AssessmentTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~Constructor_SetsAllProperties"

# With coverage
dotnet test tests/AcademicAssessment.Tests.Unit/ --collect:"XPlat Code Coverage"
```

---

## 📚 Real Example from Codebase

See **`tests/AcademicAssessment.Tests.Unit/Models/AssessmentTests.cs`** for complete example demonstrating:

- Test helper methods
- Region organization
- AAA pattern
- FluentAssertions usage
- Parameterized tests with [Theory]
- Comprehensive model testing

---

**Last Updated:** 2025-10-25  
**Related:** [Overview](./01-overview.md) | [Integration Testing](./04-integration-testing.md) | [Local Testing](./02-local-testing.md)
