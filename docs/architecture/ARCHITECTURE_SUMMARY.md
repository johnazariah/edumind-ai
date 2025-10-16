# EduMind.AI - Architecture Summary for Development

## Executive Summary

You've asked for a comprehensive role-based access control (RBAC) system with **6 distinct user personas**, each with dedicated application interfaces. This document provides a high-level overview of the architecture, implementation strategy, and next steps.

---

## Six User Personas & Their Interfaces

### 1. 👨‍🎓 **Student** (Two Onboarding Models)

#### Model A: School-Based (B2B)

- **App**: `AcademicAssessment.StudentApp` ✅ EXISTS
- **Onboarding**: School administrator adds students
- **Database**: School's dedicated database
- **Features**: Full assessment suite, teacher feedback
- **Privacy**: Physical database isolation per school

#### Model B: Self-Service (B2C) 🆕 NEW

- **App**: Same `AcademicAssessment.StudentApp` ✅ EXISTS
- **Onboarding**: Self-signup like Duolingo (email/Google/Apple)
- **Database**: Shared "selfservice" virtual tenant
- **Features**: Core features + gamification (streaks, achievements, leaderboards)
- **Privacy**: Logical isolation, COPPA compliance for under-13
- **Pricing**: Freemium (free tier + premium upgrades)

**Common Functions:**

- Take assessments, view progress, receive recommendations
- **Data Access**: Own data only (no cross-student visibility)

See [SELF_SERVICE_ONBOARDING.md](SELF_SERVICE_ONBOARDING.md) for complete self-service architecture.

### 2. 👨‍🏫 **Teacher (Class Manager)**

- **App**: `AcademicAssessment.ClassApp` 🆕 NEW (rename from Dashboard)
- **Scope**: Assigned classes only
- **Key Functions**: Monitor class, grade assessments, provide feedback, track progress
- **Data Access**: Students in assigned classes only

### 3. 🏫 **School Administrator**

- **App**: `AcademicAssessment.SchoolAdminApp` 🆕 NEW
- **Scope**: Single school (all classes)
- **Key Functions**: School-wide analytics, teacher management, reports
- **Data Access**: All data within their school

### 4. 📚 **Course Administrator**

- **App**: `AcademicAssessment.CourseAdminApp` 🆕 NEW
- **Scope**: Specific courses/subjects (cross-school)
- **Key Functions**: Curriculum design, question banks, content management
- **Data Access**: Course content and anonymized performance data across schools

### 5. 💼 **Business Administrator**

- **App**: `AcademicAssessment.BusinessAdminApp` 🆕 NEW
- **Scope**: System-wide (multi-tenant management)
- **Key Functions**: Onboard schools, manage subscriptions, user provisioning
- **Data Access**: Schools, users, billing, subscriptions (no assessment content)

### 6. ⚙️ **System Administrator**

- **App**: `AcademicAssessment.SysAdminApp` 🆕 NEW
- **Scope**: Entire system (infrastructure)
- **Key Functions**: System health, infrastructure, LLM costs, security
- **Data Access**: All system data, logs, configurations

---

## Updated Solution Architecture

### Current State (8 projects)

```
✅ AcademicAssessment.Core              # Domain models
✅ AcademicAssessment.Infrastructure    # Data & services
✅ AcademicAssessment.Agents            # AI agents
✅ AcademicAssessment.Orchestration     # Coordination
✅ AcademicAssessment.Analytics         # Analytics
✅ AcademicAssessment.Web               # Web API
✅ AcademicAssessment.StudentApp        # Student interface
✅ AcademicAssessment.Dashboard         # To be renamed
```

### Target State (14 projects)

```
✅ AcademicAssessment.Core              # Domain models
✅ AcademicAssessment.Infrastructure    # Data & services
✅ AcademicAssessment.Agents            # AI agents
✅ AcademicAssessment.Orchestration     # Coordination
✅ AcademicAssessment.Analytics         # Analytics
✅ AcademicAssessment.Web               # Unified Web API (all endpoints)
✅ AcademicAssessment.StudentApp        # Student Blazor app
🔄 AcademicAssessment.ClassApp          # Teacher Blazor app (rename Dashboard)
🆕 AcademicAssessment.SchoolAdminApp    # School Admin Blazor app
🆕 AcademicAssessment.CourseAdminApp    # Course Admin Blazor app
🆕 AcademicAssessment.BusinessAdminApp  # Business Admin Blazor app
🆕 AcademicAssessment.SysAdminApp       # System Admin Blazor app
🆕 AcademicAssessment.SharedUI          # Shared UI components library
✅ Test projects (3)                    # Unit, Integration, Performance
```

---

## Multi-Tenant Data Isolation

### Tenant Hierarchy

```
System (Root)
└── Schools (Tenant Boundary)
    ├── Classes
    │   └── Students
    └── Teachers
```

### Key Isolation Mechanisms

1. **Row-Level Security (RLS)**
   - Every entity tagged with `SchoolId` and/or `ClassId`
   - Database-level filtering via Entity Framework query filters
   - Automatic tenant context injection

2. **Claims-Based Authorization**

   ```csharp
   public class UserClaims
   {
       public Guid UserId { get; set; }
       public UserRole Role { get; set; }
       public Guid? SchoolId { get; set; }      // Null for global admins
       public List<Guid>? ClassIds { get; set; } // For teachers
       public List<Guid>? CourseIds { get; set; }// For course admins
   }
   ```

3. **API Endpoint Segregation**

   ```
   /api/student/*          - Student endpoints
   /api/teacher/*          - Teacher/class endpoints
   /api/school-admin/*     - School administrator endpoints
   /api/course-admin/*     - Course administrator endpoints
   /api/business-admin/*   - Business administrator endpoints
   /api/system-admin/*     - System administrator endpoints
   ```

---

## Functional Programming Principles

Following your preferences for **small functions, functional constructs, composition, and idiomatic C#**:

### 1. Immutable Domain Models

```csharp
// Using records for immutability
public record Student
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid SchoolId { get; init; }
    public IReadOnlyList<Guid> ClassIds { get; init; } = [];
}
```

### 2. Railway-Oriented Programming

```csharp
// Result<T> monad for error handling
public abstract record Result<T>
{
    public record Success(T Value) : Result<T>;
    public record Failure(Error Error) : Result<T>;
}

// Functional composition
var result = await repository
    .GetStudentById(id)
    .MapAsync(student => EnrichWithProgress(student))
    .BindAsync(student => ValidateEligibility(student))
    .MapAsync(student => GenerateAssessment(student));
```

### 3. Small, Focused Functions

```csharp
// Pure functions with single responsibility
static DifficultyLevel CalculateDifficulty(double abilityEstimate) =>
    abilityEstimate switch
    {
        < 0.3 => DifficultyLevel.Easy,
        < 0.5 => DifficultyLevel.Medium,
        < 0.7 => DifficultyLevel.Hard,
        _ => DifficultyLevel.VeryHard
    };

// Function composition
static Func<Student, Task<Assessment>> CreateAssessment(
    IQuestionRepository questionRepo,
    IAdaptiveEngine engine) =>
    student => engine
        .EstimateAbility(student)
        .Map(CalculateDifficulty)
        .BindAsync(difficulty => questionRepo.GetQuestions(difficulty));
```

### 4. Higher-Order Functions

```csharp
// Generic repository operations
static async Task<Result<TOut>> WithTenantContext<TIn, TOut>(
    this Task<Result<TIn>> operation,
    ITenantContext context,
    Func<TIn, TOut> transform) =>
    await operation.MapAsync(value => 
        context.SchoolId == value.SchoolId 
            ? transform(value) 
            : throw new UnauthorizedAccessException());
```

### 5. LINQ Pipelines

```csharp
// Functional data transformation
var topPerformers = students
    .Where(s => s.SubjectProfiles.ContainsKey(Subject.Mathematics))
    .Select(s => (Student: s, Score: s.SubjectProfiles[Subject.Mathematics].CurrentAbilityEstimate))
    .OrderByDescending(x => x.Score)
    .Take(10)
    .ToList();
```

---

## Implementation Roadmap

### Phase 1: Foundation (Weeks 1-2) - **START HERE**

- [ ] Create multi-tenant domain models with immutable records
- [ ] Implement Result<T> monad for error handling
- [ ] Define core interfaces with functional signatures
- [ ] Set up database context with row-level security
- [ ] Create tenant context middleware
- [ ] Implement repository pattern with tenant isolation

### Phase 2: Core APIs (Week 3)

- [ ] Implement authentication & authorization policies
- [ ] Create role-based API controllers
- [ ] Set up SignalR hubs for real-time updates
- [ ] Add API versioning and documentation

### Phase 3: Student & Teacher (Weeks 4-6)

- [ ] Student assessment taking interface
- [ ] Teacher class monitoring dashboard
- [ ] Real-time progress tracking
- [ ] Grading interface

### Phase 4: School Administration (Weeks 7-8)

- [ ] School admin dashboard
- [ ] School-wide analytics
- [ ] Teacher management
- [ ] Reporting tools

### Phase 5: Course Management (Weeks 9-10)

- [ ] Course admin interface
- [ ] Question bank management
- [ ] Curriculum design tools
- [ ] LLM integration for content generation

### Phase 6: Business & System Admin (Weeks 11-13)

- [ ] Business admin dashboard
- [ ] School onboarding wizard
- [ ] System admin console
- [ ] Monitoring and logging

### Phase 7: Polish & Deploy (Weeks 14-16)

- [ ] Shared UI component library
- [ ] Performance optimization
- [ ] Security audit
- [ ] Production deployment

---

## Key Architectural Decisions

### ✅ Single Unified API

- **Decision**: One `AcademicAssessment.Web` API with role-based controllers
- **Rationale**: Easier maintenance, shared infrastructure, consistent authentication
- **Alternative**: Separate APIs per role (rejected due to complexity)

### ✅ Separate Blazor Apps

- **Decision**: 6 distinct Blazor applications for each persona
- **Rationale**:
  - Clear separation of concerns
  - Independent deployment and scaling
  - Optimized UX per persona
  - Better security boundaries
- **Alternative**: Single app with role-based routing (rejected due to complexity)

### ✅ Row-Level Security in EF Core

- **Decision**: Automatic query filters based on tenant context
- **Rationale**:
  - Impossible to bypass (enforced at data layer)
  - No manual filtering in every query
  - Consistent across all operations
- **Alternative**: Manual filtering (rejected due to error-prone nature)

### ✅ Functional Programming Style

- **Decision**: Immutable records, Result<T>, functional composition
- **Rationale**:
  - Type safety
  - Easier testing
  - Better composability
  - Explicit error handling
- **Alternative**: Traditional OOP (rejected per your preference)

---

## Next Steps - What Would You Like to Start With?

### Option A: Core Domain Models ⭐ RECOMMENDED

Start with the foundation - create immutable domain models, enums, and Result<T> type.
**Time**: 4-6 hours
**Files**: 15-20 model files in `AcademicAssessment.Core`

### Option B: Multi-Tenant Infrastructure

Set up database context, tenant middleware, and repository pattern.
**Time**: 6-8 hours
**Dependencies**: Requires domain models first

### Option C: Authentication & Authorization

Implement role-based policies and API security.
**Time**: 4-5 hours
**Dependencies**: Requires domain models first

### Option D: Single Vertical Slice

Implement one complete user journey end-to-end (e.g., student taking an assessment).
**Time**: 12-16 hours
**Dependencies**: Requires all of the above

---

## Documentation References

1. **`RBAC_ARCHITECTURE.md`** - Complete role definitions and access control matrix
2. **`IMPLEMENTATION_PLAN.md`** - Detailed code examples and implementation steps
3. **`CONTEXT.md`** - Original project context and requirements
4. **`copilot-instructions.md`** - Comprehensive implementation guide
5. **`TASK_JOURNAL.md`** - Development progress tracking

---

## Questions for You

1. **Start Point**: Which phase/option would you like to begin with?
2. **Priorities**: Are there specific personas (e.g., Student, Teacher) you want to prioritize?
3. **Scope**: Should we focus on one subject (e.g., Mathematics) first, then expand?
4. **Testing**: Do you want unit tests written alongside each component, or after?
5. **LLM Integration**: Should we start with mock LLM services, or integrate Azure OpenAI early?

---

*Ready to start building! Let me know which direction you'd like to take first.* 🚀

---

*Last Updated: October 11, 2025*
