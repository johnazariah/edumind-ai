# EduMind.AI Coding Standards

**Version:** 1.0.0  
**Last Updated:** October 25, 2025

This document defines coding standards, conventions, and best practices for EduMind.AI development.

---

## Table of Contents

1. [C# Coding Standards](#c-coding-standards)
2. [API Development](#api-development)
3. [Blazor Development](#blazor-development)
4. [Infrastructure as Code](#infrastructure-as-code)
5. [Testing Standards](#testing-standards)
6. [Git Workflow](#git-workflow)
7. [Documentation Standards](#documentation-standards)

---

## 1. C# Coding Standards

### 1.1 General Principles

- **Language Version:** C# 13 (latest)
- **Target Framework:** .NET 9.0
- **Nullable Reference Types:** Enabled globally
- **Implicit Usings:** Enabled
- **File-Scoped Namespaces:** Required
- **Records over Classes:** Prefer immutable records for domain models

### 1.2 Naming Conventions

```csharp
// Interfaces: I prefix + PascalCase
public interface IAssessmentRepository { }
public interface IStudentService { }

// Classes: PascalCase
public class AssessmentController { }
public class StudentRepository { }

// Methods: PascalCase
public async Task<Result<Assessment>> GetByIdAsync(Guid id) { }

// Parameters: camelCase
public void ProcessAssessment(Guid assessmentId, string studentName) { }

// Private fields: camelCase (no underscore prefix)
private readonly ILogger logger;
private readonly HttpClient httpClient;

// Constants: PascalCase
public const int DefaultTimeoutSeconds = 30;

// Enums: PascalCase (singular)
public enum AssessmentType { Diagnostic, Formative, Summative, Adaptive }
public enum GradeLevel { Elementary, MiddleSchool, HighSchool, College }
```

### 1.3 Code Style

#### Immutable Domain Models

```csharp
// Use records for immutability and value semantics
public record Assessment
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required AssessmentType AssessmentType { get; init; }
    public IReadOnlyList<Guid> QuestionIds { get; init; } = [];
    
    // Computed properties
    public int QuestionCount => QuestionIds.Count;
    public bool IsAdaptive => AssessmentType == AssessmentType.Adaptive;
}
```

#### Required Properties

```csharp
// Use 'required' keyword for mandatory properties
public record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    
    // Optional properties without 'required'
    public Guid? SchoolId { get; init; }
    public string? ExternalId { get; init; }
}
```

#### Collection Expressions

```csharp
// Use collection expressions (C# 12+)
public IReadOnlyList<string> Topics { get; init; } = [];
public string[] AllowedOrigins { get; set; } = [];

// Collection initialization
var assessments = new List<Assessment>
{
    new() { Id = guid1, Title = "Math" },
    new() { Id = guid2, Title = "Science" }
};
```

### 1.4 Async/Await Patterns

```csharp
// All I/O operations must be async
public async Task<Result<Assessment>> GetByIdAsync(
    Guid id, 
    CancellationToken cancellationToken = default)
{
    var assessment = await dbContext.Assessments
        .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        
    return assessment is null 
        ? new Error("NotFound", $"Assessment {id} not found")
        : assessment;
}

// Async method naming: always suffix with 'Async'
// Always accept CancellationToken as last parameter
// Always use ConfigureAwait(false) in library code (not UI)
```

### 1.5 Error Handling

#### Railway-Oriented Programming with Result<T>

```csharp
// Result type for functional error handling
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(Error Error) : Result<T>;
    
    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
}

public sealed record Error(
    string Code,
    string Message,
    Exception? Exception = null);

// Usage in repositories
public async Task<Result<Student>> GetByIdAsync(Guid id)
{
    try
    {
        var student = await dbContext.Students.FindAsync(id);
        
        return student is null
            ? new Error("NotFound", $"Student {id} not found")
            : student;  // Implicit conversion
    }
    catch (Exception ex)
    {
        return new Error("DatabaseError", "Failed to retrieve student", ex);
    }
}

// Pattern matching consumption
var result = await repository.GetByIdAsync(studentId);
return result switch
{
    Result<Student>.Success s => Ok(s.Value),
    Result<Student>.Failure f => Problem(f.Error.Message),
    _ => StatusCode(500)
};
```

### 1.6 LINQ and Query Patterns

```csharp
// Prefer method syntax over query syntax
var activeAssessments = assessments
    .Where(a => a.IsActive)
    .OrderBy(a => a.Title)
    .Take(10)
    .ToList();

// Use async LINQ with EF Core
var students = await dbContext.Students
    .Where(s => s.GradeLevel == gradeLevel)
    .Include(s => s.Classes)
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

### 1.7 XML Documentation

```csharp
/// <summary>
/// Gets all assessments for a specific course
/// </summary>
/// <param name="courseId">The unique identifier of the course</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result containing list of assessments or error</returns>
Task<Result<IReadOnlyList<Assessment>>> GetByCourseIdAsync(
    Guid courseId,
    CancellationToken cancellationToken = default);
```

**Rules:**

- All public APIs must have XML docs
- Use `<summary>`, `<param>`, `<returns>` tags
- Document exceptions with `<exception>` tag
- Warning 1591 (missing docs) suppressed in Directory.Build.props

---

## 2. API Development

### 2.1 Controller Structure

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AcademicAssessment.Web.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentRepository repository;
    private readonly ILogger<AssessmentController> logger;

    public AssessmentController(
        IAssessmentRepository repository,
        ILogger<AssessmentController> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssessments(
        CancellationToken cancellationToken)
    {
        var result = await repository.GetAllAsync(cancellationToken);
        return result switch
        {
            Result<IReadOnlyList<Assessment>>.Success s => Ok(s.Value),
            Result<IReadOnlyList<Assessment>>.Failure f => Problem(f.Error.Message),
            _ => StatusCode(500)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAssessment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await repository.GetByIdAsync(id, cancellationToken);
        return result switch
        {
            Result<Assessment>.Success s => Ok(s.Value),
            Result<Assessment>.Failure f when f.Error.Code == "NotFound" => NotFound(),
            Result<Assessment>.Failure f => Problem(f.Error.Message),
            _ => StatusCode(500)
        };
    }
}
```

### 2.2 API Versioning

- **Asp.Versioning** NuGet package
- URL-based versioning: `/api/v1/assessment`
- Swagger UI per version

```csharp
// In Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### 2.3 DTO Patterns

```csharp
namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Data transfer object for assessment summary
/// </summary>
public record AssessmentSummary
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Subject { get; init; }
    public required string Difficulty { get; init; }
    public required int EstimatedDurationMinutes { get; init; }
    public required int QuestionCount { get; init; }
    public double? ProgressPercentage { get; init; }
    public bool IsInProgress { get; init; }
    public DateTimeOffset? LastAttemptedAt { get; init; }
}
```

---

## 3. Blazor Development

### 3.1 Component Structure

```csharp
@page "/assessments"
@inject IHttpClientFactory HttpClientFactory
@inject NavigationManager Navigation
@inject ILogger<AssessmentDashboard> Logger

<PageTitle>Assessments - EduMind.AI</PageTitle>

<div class="container-fluid">
    <h1 class="display-4 mb-4">📚 Your Assessments</h1>
    
    @if (assessments is null)
    {
        <p><em>Loading assessments...</em></p>
    }
    else if (!assessments.Any())
    {
        <p>No assessments available.</p>
    }
    else
    {
        <div class="row">
            @foreach (var assessment in assessments)
            {
                <AssessmentCard Assessment="@assessment" />
            }
        </div>
    }
</div>

@code {
    private List<AssessmentSummary>? assessments;

    protected override async Task OnInitializedAsync()
    {
        await LoadAssessmentsAsync();
    }

    private async Task LoadAssessmentsAsync()
    {
        try
        {
            using var client = HttpClientFactory.CreateClient("ApiClient");
            assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>(
                "api/v1/assessment");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load assessments");
        }
    }
}
```

### 3.2 HTTP Client Configuration

**IMPORTANT:** Use named clients via factory pattern to avoid performance issues.

```csharp
// In Program.cs - CORRECT pattern
builder.Services.AddHttpClient("ApiClient", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] 
        ?? "http://localhost:5103";
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// In components - inject factory and create named client
@inject IHttpClientFactory HttpClientFactory

@code {
    private async Task CallApiAsync()
    {
        using var client = HttpClientFactory.CreateClient("ApiClient");
        var data = await client.GetFromJsonAsync<DataType>("api/endpoint");
    }
}
```

**ANTI-PATTERN (causes 15+ second page loads):**

```csharp
// DO NOT DO THIS - causes HttpClient disposal issues
builder.Services.AddHttpClient<HttpClient>();
```

### 3.3 Navigation Patterns

**Known Issue:** Blazor Server SignalR sometimes fails to handle `@onclick` events.

**Workaround:** Use HTML anchor links for critical navigation:

```razor
@* PREFERRED - Reliable navigation *@
<a href="/assessment/@AssessmentId/session" class="btn btn-primary">
    Start Assessment
</a>

@* AVOID - May not work reliably in Blazor Server *@
<button @onclick="NavigateToSession" class="btn btn-primary">
    Start Assessment
</button>
```

---

## 4. Infrastructure as Code

### 4.1 Bicep Standards

```bicep
targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@metadata({
  azd: {
    type: 'generate'
    config: { length: 22, noSpecial: true }
  }
})
@secure()
param cache_password string

var tags = {
  'azd-env-name': environmentName
  'managed-by': 'azd'
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}
```

### 4.2 Naming Conventions

```bicep
// Resource group
name: 'rg-${environmentName}'

// Container Apps Environment
name: 'cae-${environmentName}'

// Container Apps
name: 'ca-webapi-${environmentName}'
name: 'ca-studentapp-${environmentName}'

// PostgreSQL
name: 'psql-${environmentName}-${uniqueSuffix}'

// Redis Cache
name: 'redis-${environmentName}'

// Log Analytics
name: 'log-${environmentName}'
```

---

## 5. Testing Standards

See comprehensive testing documentation in `.github/testing/`:

- **[Testing Overview](.github/testing/01-overview.md)** - Philosophy and architecture
- **[Unit Testing](.github/testing/03-unit-testing.md)** - AAA pattern, FluentAssertions, Moq
- **[Integration Testing](.github/testing/04-integration-testing.md)** - API and database tests
- **[E2E Testing](.github/testing/05-e2e-testing.md)** - Playwright browser automation
- **[AI Testing](.github/testing/06-ai-agent-testing.md)** - Ollama and agent validation

### 5.1 Key Principles

- **Test Naming:** `MethodName_StateUnderTest_ExpectedBehavior`
- **AAA Pattern:** Arrange-Act-Assert with explicit comments
- **FluentAssertions:** Use for readable, expressive assertions
- **Coverage Goals:** 70%+ overall, 90%+ for critical paths

---

## 6. Git Workflow

### 6.1 Commit Message Format

```
# Feature work
Add multi-agent orchestration for assessment feedback
Implement adaptive question selection algorithm

# Fixes
Fix: Use environment-specific resource group in workflow
Fix: Add quotes to template variable to prevent formatting issues

# Configuration changes
Configure PostgreSQL to work with Azure Files permissions
Migrate to Azure Database for PostgreSQL Flexible Server

# Documentation
Document deployment status and template substitution issue
Update architecture docs with RBAC design

# Work in progress
WIP: Fix Student App HTTP client and navigation issues

# Deployment/Infrastructure
Prepare for clean deployment in Australia East
Generate Bicep infrastructure to exit azd limited mode
```

**Conventions:**

- Present tense ("Add feature" not "Added feature")
- Imperative mood ("Fix bug" not "Fixes bug")
- Prefix `Fix:` for bug fixes
- Prefix `WIP:` for unfinished work
- Keep under 72 characters when possible
- Add detailed body for complex changes

---

## 7. Documentation Standards

### 7.1 README Files

- **Root README.md** - Project overview, getting started
- **docs/** - Architecture, deployment, planning docs
- Use Markdown with proper heading hierarchy
- Include code examples in fenced blocks with language tags

### 7.2 Code Comments

```csharp
// Single-line comments for explaining WHY, not WHAT
// Avoid obvious comments

// GOOD - explains reasoning
// Using named client to avoid HttpClient disposal issues that cause 15s delays
using var client = HttpClientFactory.CreateClient("ApiClient");

// BAD - states the obvious
// Create HTTP client
using var client = new HttpClient();
```

---

## Appendix: Quick Reference

### Common Patterns

#### Creating a New Domain Model

```csharp
using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a course offering within a school
/// </summary>
public record Course
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public Guid? SchoolId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

#### Creating a New Repository Interface

```csharp
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for course management
/// </summary>
public interface ICourseRepository : IRepository<Course, Guid>
{
    /// <summary>
    /// Gets all courses for a specific school
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);
}
```

---

**Last Updated:** 2025-10-25  
**Related:** [copilot-instructions.md](.github/copilot-instructions.md) | [Testing Guide](.github/testing/README.md)
