# EduMind.AI Implementation Plan

## Overview

This document outlines the detailed implementation plan for building the multi-tenant, role-based academic assessment system with six distinct user interfaces and comprehensive data isolation.

## Updated Solution Structure

### New Projects to Add

```
src/
├── AcademicAssessment.Core/              # ✅ EXISTS - Domain models
├── AcademicAssessment.Infrastructure/    # ✅ EXISTS - Data & services
├── AcademicAssessment.Agents/            # ✅ EXISTS - AI agents
├── AcademicAssessment.Orchestration/     # ✅ EXISTS - Coordination
├── AcademicAssessment.Analytics/         # ✅ EXISTS - Analytics
├── AcademicAssessment.Web/               # ✅ EXISTS - Unified API
├── AcademicAssessment.StudentApp/        # ✅ EXISTS - Student interface
├── AcademicAssessment.Dashboard/         # ⚠️  RENAME to ClassApp
├── AcademicAssessment.ClassApp/          # 🆕 NEW - Teacher interface
├── AcademicAssessment.SchoolAdminApp/    # 🆕 NEW - School admin
├── AcademicAssessment.CourseAdminApp/    # 🆕 NEW - Course admin
├── AcademicAssessment.BusinessAdminApp/  # 🆕 NEW - Business admin
├── AcademicAssessment.SysAdminApp/       # 🆕 NEW - System admin
└── AcademicAssessment.SharedUI/          # 🆕 NEW - Shared components
```

### Action Items

1. **Rename** `AcademicAssessment.Dashboard` → `AcademicAssessment.ClassApp`
2. **Create** 4 new Blazor Web Apps for admin interfaces
3. **Create** `AcademicAssessment.SharedUI` for reusable components

---

## Phase 1: Foundation - Multi-Tenant Core (Week 1)

### 1.1 Domain Models - Multi-Tenant Entities

**File**: `src/AcademicAssessment.Core/Models/`

#### User & Identity Models

```csharp
// User.cs
public record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required UserRole Role { get; init; }
    public Guid? SchoolId { get; init; }
    public IReadOnlyList<Guid> ClassIds { get; init; } = [];
    public IReadOnlyList<Guid> CourseIds { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool IsActive { get; init; } = true;
}

// UserRole.cs (enum)
public enum UserRole
{
    Student,
    Teacher,
    SchoolAdministrator,
    CourseAdministrator,
    BusinessAdministrator,
    SystemAdministrator
}
```

#### Tenant Models

```csharp
// School.cs
public record School
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string District { get; init; }
    public string? Address { get; init; }
    public required SubscriptionTier Tier { get; init; }
    public DateTime SubscriptionStart { get; init; }
    public DateTime SubscriptionEnd { get; init; }
    public int MaxStudents { get; init; }
    public int MaxTeachers { get; init; }
    public bool IsActive { get; init; } = true;
    public Dictionary<string, object> Settings { get; init; } = new();
}

// Class.cs
public record Class
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid SchoolId { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public Guid? PrimaryTeacherId { get; init; }
    public IReadOnlyList<Guid> StudentIds { get; init; } = [];
    public IReadOnlyList<Guid> TeacherIds { get; init; } = [];
    public int MaxCapacity { get; init; }
    public string? Schedule { get; init; }
    public bool IsActive { get; init; } = true;
}

// Course.cs
public record Course
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required string Version { get; init; }
    public Guid? CourseAdministratorId { get; init; }
    public IReadOnlyList<LearningObjective> Objectives { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsPublished { get; init; } = false;
}
```

#### Student Models

```csharp
// Student.cs
public record Student
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid SchoolId { get; init; }
    public required Guid ClassId { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required string StudentNumber { get; init; }
    public DateTime EnrollmentDate { get; init; }
    public Dictionary<Subject, StudentSubjectProfile> SubjectProfiles { get; init; } = new();
    public IReadOnlyList<Guid> AssessmentIds { get; init; } = [];
}

// StudentSubjectProfile.cs
public record StudentSubjectProfile
{
    public required Subject Subject { get; init; }
    public double CurrentAbilityEstimate { get; init; }
    public double Confidence { get; init; }
    public int AssessmentsTaken { get; init; }
    public Dictionary<string, MasteryLevel> ObjectiveMastery { get; init; } = new();
    public DateTime LastAssessmentDate { get; init; }
}
```

#### Assessment Models

```csharp
// Assessment.cs
public record Assessment
{
    public required Guid Id { get; init; }
    public required Guid SchoolId { get; init; }     // Tenant isolation
    public required Guid ClassId { get; init; }      // Tenant isolation
    public required Guid CourseId { get; init; }
    public required AssessmentType Type { get; init; }
    public required string Title { get; init; }
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public int TimeLimit { get; init; }
    public Guid CreatedBy { get; init; }
    public IReadOnlyList<Question> Questions { get; init; } = [];
    public AssessmentStatus Status { get; init; }
}

// StudentAssessment.cs
public record StudentAssessment
{
    public required Guid Id { get; init; }
    public required Guid AssessmentId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid SchoolId { get; init; }     // Tenant isolation
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<StudentResponse> Responses { get; init; } = [];
    public double? Score { get; init; }
    public double? AbilityEstimate { get; init; }
    public AssessmentStatus Status { get; init; }
}

// Question.cs
public record Question
{
    public required Guid Id { get; init; }
    public required Guid CourseId { get; init; }
    public required QuestionType Type { get; init; }
    public required string Text { get; init; }
    public required DifficultyLevel Difficulty { get; init; }
    public double DifficultyParameter { get; init; }
    public IReadOnlyList<string> Options { get; init; } = [];
    public required string CorrectAnswer { get; init; }
    public string? Explanation { get; init; }
    public IReadOnlyList<string> LearningObjectiveIds { get; init; } = [];
    public int TimesUsed { get; init; }
    public double AverageCorrectRate { get; init; }
}

// StudentResponse.cs
public record StudentResponse
{
    public required Guid QuestionId { get; init; }
    public required string Answer { get; init; }
    public bool IsCorrect { get; init; }
    public double? PartialCredit { get; init; }
    public int TimeSpentSeconds { get; init; }
    public DateTime AnsweredAt { get; init; }
    public string? Feedback { get; init; }
}
```

### 1.2 Core Enums

**File**: `src/AcademicAssessment.Core/Enums/`

```csharp
// Subject.cs
public enum Subject
{
    Mathematics,
    Physics,
    Chemistry,
    Biology,
    English
}

// GradeLevel.cs
public enum GradeLevel
{
    Grade8 = 8,
    Grade9 = 9,
    Grade10 = 10,
    Grade11 = 11,
    Grade12 = 12
}

// AssessmentType.cs
public enum AssessmentType
{
    Diagnostic,
    Formative,
    Summative,
    Adaptive,
    Remedial,
    Challenge
}

// QuestionType.cs
public enum QuestionType
{
    MultipleChoice,
    TrueFalse,
    ShortAnswer,
    Essay,
    FillInBlank,
    Matching,
    Numerical
}

// DifficultyLevel.cs
public enum DifficultyLevel
{
    VeryEasy = 1,
    Easy = 2,
    Medium = 3,
    Hard = 4,
    VeryHard = 5
}

// AssessmentStatus.cs
public enum AssessmentStatus
{
    Draft,
    Scheduled,
    Active,
    Completed,
    Cancelled
}

// MasteryLevel.cs
public enum MasteryLevel
{
    NotStarted,
    Beginning,
    Developing,
    Proficient,
    Advanced,
    Mastered
}

// SubscriptionTier.cs
public enum SubscriptionTier
{
    Free,
    Basic,
    Standard,
    Premium,
    Enterprise
}
```

### 1.3 Functional Result Type

**File**: `src/AcademicAssessment.Core/Common/Result.cs`

```csharp
/// <summary>
/// Railway-oriented programming result type for functional error handling
/// </summary>
public abstract record Result<T>
{
    public record Success(T Value) : Result<T>;
    public record Failure(Error Error) : Result<T>;
    
    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
    
    public static implicit operator Result<T>(T value) => new Success(value);
    public static implicit operator Result<T>(Error error) => new Failure(error);
}

public record Error(string Code, string Message, Exception? Exception = null);

// Extension methods for functional composition
public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper) =>
        result switch
        {
            Result<TIn>.Success(var value) => mapper(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException()
        };
    
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper) =>
        result switch
        {
            Result<TIn>.Success(var value) => await mapper(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException()
        };
    
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder) =>
        result switch
        {
            Result<TIn>.Success(var value) => binder(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException()
        };
    
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder) =>
        result switch
        {
            Result<TIn>.Success(var value) => await binder(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException()
        };
    
    public static T Match<T>(
        this Result<T> result,
        Func<T, T> onSuccess,
        Func<Error, T> onFailure) =>
        result switch
        {
            Result<T>.Success(var value) => onSuccess(value),
            Result<T>.Failure(var error) => onFailure(error),
            _ => throw new InvalidOperationException()
        };
}
```

### 1.4 Core Interfaces

**File**: `src/AcademicAssessment.Core/Interfaces/`

```csharp
// ITenantContext.cs
public interface ITenantContext
{
    Guid? SchoolId { get; }
    IReadOnlyList<Guid> ClassIds { get; }
    UserRole Role { get; }
    Guid UserId { get; }
}

// IRepository.cs - Generic repository with tenant isolation
public interface IRepository<T> where T : class
{
    Task<Result<T>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<T>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<T>> AddAsync(T entity, CancellationToken ct = default);
    Task<Result<T>> UpdateAsync(T entity, CancellationToken ct = default);
    Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct = default);
}

// Specific repositories
public interface IStudentRepository : IRepository<Student>
{
    Task<Result<IReadOnlyList<Student>>> GetByClassIdAsync(
        Guid classId, 
        CancellationToken ct = default);
    
    Task<Result<IReadOnlyList<Student>>> GetBySchoolIdAsync(
        Guid schoolId, 
        CancellationToken ct = default);
}

public interface IAssessmentRepository : IRepository<Assessment>
{
    Task<Result<IReadOnlyList<Assessment>>> GetByClassIdAsync(
        Guid classId, 
        CancellationToken ct = default);
    
    Task<Result<IReadOnlyList<Assessment>>> GetActiveAssessmentsAsync(
        CancellationToken ct = default);
}

public interface IQuestionRepository : IRepository<Question>
{
    Task<Result<IReadOnlyList<Question>>> GetByCourseIdAsync(
        Guid courseId, 
        CancellationToken ct = default);
    
    Task<Result<IReadOnlyList<Question>>> GetByDifficultyAsync(
        Subject subject,
        DifficultyLevel difficulty,
        int count,
        CancellationToken ct = default);
}
```

---

## Phase 2: Infrastructure - Database & Multi-Tenancy (Week 1-2)

### 2.1 Database Context with Row-Level Security

**File**: `src/AcademicAssessment.Infrastructure/Data/AcademicContext.cs`

```csharp
public class AcademicContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    
    public AcademicContext(
        DbContextOptions<AcademicContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<StudentAssessment> StudentAssessments => Set<StudentAssessment>();
    public DbSet<Question> Questions => Set<Question>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply tenant filters globally
        ApplyTenantFilters(modelBuilder);
        
        // Configure relationships
        ConfigureRelationships(modelBuilder);
        
        // Configure indexes
        ConfigureIndexes(modelBuilder);
    }
    
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // Students filtered by school
        modelBuilder.Entity<Student>()
            .HasQueryFilter(s => 
                _tenantContext.SchoolId == null || 
                s.SchoolId == _tenantContext.SchoolId);
        
        // Assessments filtered by school and class
        modelBuilder.Entity<Assessment>()
            .HasQueryFilter(a => 
                _tenantContext.SchoolId == null || 
                a.SchoolId == _tenantContext.SchoolId);
        
        // Classes filtered by school
        modelBuilder.Entity<Class>()
            .HasQueryFilter(c => 
                _tenantContext.SchoolId == null || 
                c.SchoolId == _tenantContext.SchoolId);
    }
    
    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // Student -> School (required)
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.SchoolId);
        
        // Student -> Class (required)
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.ClassId);
        
        // Assessment -> School (required, tenant isolation)
        modelBuilder.Entity<Assessment>()
            .HasIndex(a => a.SchoolId);
        
        // Assessment -> Class (required, tenant isolation)
        modelBuilder.Entity<Assessment>()
            .HasIndex(a => a.ClassId);
    }
    
    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes for common queries
        modelBuilder.Entity<Student>()
            .HasIndex(s => new { s.SchoolId, s.ClassId });
        
        modelBuilder.Entity<Assessment>()
            .HasIndex(a => new { a.SchoolId, a.ClassId, a.Status });
        
        modelBuilder.Entity<Question>()
            .HasIndex(q => new { q.CourseId, q.Difficulty });
    }
}
```

### 2.2 Tenant Context Middleware

**File**: `src/AcademicAssessment.Infrastructure/Middleware/TenantContextMiddleware.cs`

```csharp
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext)
    {
        // Extract claims from authenticated user
        var user = context.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var schoolId = user.FindFirst("SchoolId")?.Value;
            var classIds = user.FindAll("ClassId")
                .Select(c => Guid.Parse(c.Value))
                .ToList();
            var role = Enum.Parse<UserRole>(
                user.FindFirst(ClaimTypes.Role)?.Value ?? "Student");
            var userId = Guid.Parse(
                user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            // Set tenant context for this request
            if (tenantContext is TenantContext mutableContext)
            {
                mutableContext.SchoolId = schoolId != null 
                    ? Guid.Parse(schoolId) 
                    : null;
                mutableContext.ClassIds = classIds;
                mutableContext.Role = role;
                mutableContext.UserId = userId;
            }
        }
        
        await _next(context);
    }
}

public class TenantContext : ITenantContext
{
    public Guid? SchoolId { get; set; }
    public IReadOnlyList<Guid> ClassIds { get; set; } = [];
    public UserRole Role { get; set; }
    public Guid UserId { get; set; }
}
```

---

## Phase 3: Web API - Role-Based Controllers (Week 2-3)

### 3.1 Authorization Policies

**File**: `src/AcademicAssessment.Web/Authorization/AuthorizationPolicies.cs`

```csharp
public static class AuthorizationPolicies
{
    public const string StudentOnly = "StudentOnly";
    public const string TeacherOnly = "TeacherOnly";
    public const string SchoolAdminOnly = "SchoolAdminOnly";
    public const string CourseAdminOnly = "CourseAdminOnly";
    public const string BusinessAdminOnly = "BusinessAdminOnly";
    public const string SystemAdminOnly = "SystemAdminOnly";
    
    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(StudentOnly, policy =>
            policy.RequireRole(nameof(UserRole.Student)));
        
        options.AddPolicy(TeacherOnly, policy =>
            policy.RequireRole(nameof(UserRole.Teacher)));
        
        options.AddPolicy(SchoolAdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.SchoolAdministrator)));
        
        options.AddPolicy(CourseAdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.CourseAdministrator)));
        
        options.AddPolicy(BusinessAdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.BusinessAdministrator)));
        
        options.AddPolicy(SystemAdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.SystemAdministrator)));
    }
}
```

---

## Implementation Order

### ✅ Week 1: Foundation
1. Create domain models with immutable records
2. Create enums and Result type
3. Define core interfaces
4. Set up database context with tenant filters

### ✅ Week 2: Infrastructure
1. Implement repository pattern with tenant isolation
2. Create tenant context middleware
3. Set up authentication and authorization
4. Database migrations

### ✅ Week 3-4: Student & Teacher Apps
1. Student assessment interface
2. Teacher class monitoring dashboard
3. Real-time SignalR hubs

### ✅ Week 5-6: School Administration
1. School admin dashboard
2. School-wide analytics

### ✅ Week 7-8: Course Management
1. Course admin interface
2. Question bank management

### ✅ Week 9-10: Business Administration
1. Business admin dashboard
2. School onboarding

### ✅ Week 11-12: System Administration
1. System admin console
2. Monitoring and logging

---

*Last Updated: October 11, 2025*
