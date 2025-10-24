# Domain Model

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**System Version:** 0.2.0

---

## Table of Contents

1. [Overview](#overview)
2. [Core Entities](#core-entities)
3. [Entity Relationships](#entity-relationships)
4. [Aggregate Boundaries](#aggregate-boundaries)
5. [Enumerations](#enumerations)
6. [Business Rules](#business-rules)
7. [Domain Events](#domain-events)

---

## 1. Overview

The EduMind.AI domain model is designed using **Domain-Driven Design (DDD)** principles with a focus on:

- **Immutability:** All entities are C# records with init-only properties
- **Rich domain models:** Business logic encapsulated within entities
- **Functional patterns:** `With()` methods for updates, no setters
- **Railway-oriented programming:** `Result<T>` for error handling
- **Multi-tenancy:** SchoolId/ClassId embedded in entities for isolation

### Design Principles

- **Records over classes:** Immutable by default, value semantics
- **Required properties:** `required` keyword for mandatory fields
- **Computed properties:** Readonly properties derived from state
- **Pure methods:** `With()` methods return new instances
- **No anemic models:** Business logic lives in the domain

---

## 2. Core Entities

### 2.1 Student

Represents a learner in the system, supporting both school-based (B2B) and self-service (B2C) models.

```csharp
public record Student
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }  // References User table
    public Guid? SchoolId { get; init; }        // Null for self-service
    public IReadOnlyList<Guid> ClassIds { get; init; } = [];
    public required GradeLevel GradeLevel { get; init; }
    
    // COPPA Compliance
    public DateOnly? DateOfBirth { get; init; }
    public bool ParentalConsentGranted { get; init; }
    public string? ParentEmail { get; init; }
    
    // Self-Service (B2C) Features
    public SubscriptionTier SubscriptionTier { get; init; } = SubscriptionTier.Free;
    public DateTimeOffset? SubscriptionExpiresAt { get; init; }
    public int Level { get; init; } = 1;              // Gamification
    public int XpPoints { get; init; } = 0;
    public int DailyStreak { get; init; } = 0;
    public DateOnly? LastActivityDate { get; init; }
    
    // Audit
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public bool RequiresCoppaCompliance => ...;
    public bool IsSelfService => !SchoolId.HasValue;
}
```

**Key Methods:**
- `With(...)` - Update properties immutably
- `AddXp(points)` - Award XP and update level
- `UpdateStreak()` - Maintain daily streak tracking
- `EnrollInClass(classId)` - Add student to class
- `UnenrollFromClass(classId)` - Remove from class

**Business Rules:**
- Students under 13 require parental consent (COPPA)
- Self-service students have SchoolId = null
- XP to level: 100 XP per level
- Streak breaks if no activity for 24+ hours
- Free tier: 5 assessments/week limit

---

### 2.2 Assessment

Collection of questions organized into a complete assessment.

```csharp
public record Assessment
{
    public required Guid Id { get; init; }
    public required Guid CourseId { get; init; }
    public Guid? SchoolId { get; init; }          // Null for global
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required AssessmentType AssessmentType { get; init; }
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    
    public IReadOnlyList<string> Topics { get; init; } = [];
    public IReadOnlyList<Guid> QuestionIds { get; init; } = [];
    
    public required int TotalPoints { get; init; }
    public int? TimeLimitMinutes { get; init; }    // Null = untimed
    public int PassingScorePercentage { get; init; } = 70;
    public required bool IsActive { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public bool IsAdaptive => AssessmentType == AssessmentType.Adaptive;
    public int QuestionCount => QuestionIds.Count;
}
```

**Key Methods:**
- `With(...)` - Update assessment properties
- `AddQuestion(questionId)` - Append question to assessment
- `RemoveQuestion(questionId)` - Remove question
- `ReorderQuestions(newOrder)` - Change question sequence

**Business Rules:**
- Global assessments (SchoolId = null) available to all
- School-specific assessments only visible to that school
- Adaptive assessments use IRT algorithm for question selection
- Passing score default: 70%

---

### 2.3 Question

Individual assessment question with metadata for adaptive testing.

```csharp
public record Question
{
    public required Guid Id { get; init; }
    public required Guid CourseId { get; init; }
    public required string QuestionText { get; init; }
    public required QuestionType QuestionType { get; init; }
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required DifficultyLevel DifficultyLevel { get; init; }
    
    public IReadOnlyList<string> Topics { get; init; } = [];
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];
    
    public string? AnswerOptions { get; init; }      // JSON array
    public required string CorrectAnswer { get; init; }  // JSON
    public string? Explanation { get; init; }
    public required int Points { get; init; }
    
    // IRT Parameters (for adaptive testing)
    public double? IrtDiscrimination { get; init; }
    public double? IrtDifficulty { get; init; }
    public double? IrtGuessing { get; init; }
    
    // Flexible Content Organization
    public string? BoardName { get; init; }         // "CBSE", "ICSE", "IB"
    public string? ModuleName { get; init; }        // Topic grouping
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ...;
    
    // Quality Metrics
    public required bool IsActive { get; init; }
    public int TimesAnswered { get; init; } = 0;
    public int TimesCorrect { get; init; } = 0;
    public bool IsAiGenerated { get; init; } = false;
    public string? ContentHash { get; init; }       // Duplicate detection
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public double SuccessRate => TimesAnswered == 0 ? 0 : (double)TimesCorrect / TimesAnswered;
}
```

**Key Methods:**
- `With(...)` - Update question content
- `RecordAnswer(wasCorrect)` - Track usage statistics
- `UpdateIrtParameters(...)` - Calibrate IRT values

**Business Rules:**
- Answer format depends on QuestionType (JSON serialized)
- IRT parameters calibrated after sufficient response data (typically 100+ responses)
- ContentHash prevents duplicate questions
- Metadata allows board-specific extensions without schema changes

---

### 2.4 StudentAssessment

A student's attempt at taking an assessment.

```csharp
public record StudentAssessment
{
    public required Guid Id { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid AssessmentId { get; init; }
    public Guid? SchoolId { get; init; }
    public Guid? ClassId { get; init; }
    
    public required AssessmentStatus Status { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    
    public int? Score { get; init; }
    public required int MaxScore { get; init; }
    public bool? Passed { get; init; }
    
    public int CurrentQuestionIndex { get; init; } = 0;
    public double? EstimatedAbility { get; init; }  // IRT theta
    
    public int? TimeSpentSeconds { get; init; }
    public int CorrectAnswers { get; init; } = 0;
    public int IncorrectAnswers { get; init; } = 0;
    public int SkippedQuestions { get; init; } = 0;
    
    public string? Feedback { get; init; }
    public IReadOnlyList<string> Recommendations { get; init; } = [];
    public int XpEarned { get; init; } = 0;
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public double? PercentageScore => Score.HasValue ? (double)Score.Value / MaxScore * 100 : null;
}
```

**Key Methods:**
- `Start()` - Begin the assessment
- `Complete(...)` - Finalize with score and feedback
- `NextQuestion()` - Advance question index
- `RecordCorrect()`, `RecordIncorrect()`, `RecordSkipped()` - Track answers
- `UpdateAbility(newAbility)` - Update IRT theta estimate
- `Pause()`, `Resume()`, `Abandon()` - State transitions

**Business Rules:**
- Status transitions: NotStarted → InProgress → Completed/Abandoned
- Can pause and resume (except after completion)
- EstimatedAbility (theta) updated after each question in adaptive mode
- XP awarded on completion (score-based for self-service students)

---

### 2.5 StudentResponse

Individual answer to a specific question.

```csharp
public record StudentResponse
{
    public required Guid Id { get; init; }
    public required Guid StudentAssessmentId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid QuestionId { get; init; }
    public Guid? SchoolId { get; init; }
    
    public required string StudentAnswer { get; init; }  // JSON
    public required bool IsCorrect { get; init; }
    public required int PointsEarned { get; init; }
    public required int MaxPoints { get; init; }
    public int TimeSpentSeconds { get; init; } = 0;
    
    public required int QuestionOrder { get; init; }     // 0-indexed
    public double? AbilityAtTime { get; init; }          // IRT theta snapshot
    
    public string? Feedback { get; init; }               // Per-question feedback
    public string? AiExplanation { get; init; }          // AI-generated explanation
    
    public required DateTimeOffset SubmittedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    
    // Computed Properties
    public bool WasSkipped => string.IsNullOrWhiteSpace(StudentAnswer);
}
```

**Business Rules:**
- One response per question per assessment attempt
- StudentAnswer format depends on question type (JSON)
- Feedback generated by AI agents
- AbilityAtTime captures IRT theta at time of answer (for analysis)
- Time tracking for each individual question

---

### 2.6 School

Educational institution with dedicated database.

```csharp
public record School
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }      // Short identifier
    public required string Address { get; init; }
    public required string ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public required bool IsActive { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public string ConnectionStringKey => $"School-{Id}-ConnectionString";
    public string DatabaseName => $"edumind_school_{Code.ToLowerInvariant()}_{Id:N}";
}
```

**Business Rules:**
- Each school gets physically isolated database
- Code must be unique across system
- ConnectionString stored in Azure Key Vault
- Database name includes code and ID for traceability

---

### 2.7 Class

Group of students taught by one or more teachers.

```csharp
public record Class
{
    public required Guid Id { get; init; }
    public required Guid SchoolId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required Subject Subject { get; init; }
    
    public IReadOnlyList<Guid> TeacherIds { get; init; } = [];
    public IReadOnlyList<Guid> StudentIds { get; init; } = [];
    
    public required string AcademicYear { get; init; }  // "2024-2025"
    public required bool IsActive { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    // Computed Properties
    public int EnrollmentCount => StudentIds.Count;
    public bool SupportsAggregateReporting => EnrollmentCount >= 5;
}
```

**Key Methods:**
- `AddStudent(studentId)`, `RemoveStudent(studentId)` - Manage enrollment
- `AddTeacher(teacherId)`, `RemoveTeacher(teacherId)` - Manage assignments

**Business Rules:**
- Minimum 5 students required for aggregate reports (k-anonymity)
- Class belongs to exactly one school
- Multiple teachers can be assigned to a class

---

### 2.8 Course

Curriculum definition with learning objectives.

```csharp
public record Course
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required string Description { get; init; }
    
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];
    public IReadOnlyList<string> Topics { get; init; } = [];
    
    // Flexible Organization
    public string? BoardName { get; init; }
    public string? ModuleName { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ...;
    
    public required bool IsActive { get; init; }
    public Guid? CourseAdminId { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
```

**Business Rules:**
- Courses are global (not school-specific)
- Course administrators manage curriculum across schools
- Supports multiple educational boards without hierarchy requirements

---

### 2.9 User

Base user entity for all persona types.

```csharp
public record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required UserRole Role { get; init; }
    public Guid? SchoolId { get; init; }
    public required bool IsActive { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    
    public string? ExternalId { get; init; }  // Azure AD B2C subject
}
```

**Business Rules:**
- Email must be unique (enforced by database)
- Students reference User via UserId
- SchoolId null for global roles (Course Admin, Business Admin, Sys Admin)

---

## 3. Entity Relationships

### Relationship Diagram

```
School
  │
  ├──> Class (1:N)
  │      │
  │      └──> Student (M:N via ClassIds)
  │      └──> Teacher (M:N via TeacherIds)
  │
  └──> Student (1:N)
         │
         └──> StudentAssessment (1:N)
                │
                ├──> Assessment (N:1)
                │      │
                │      └──> Course (N:1)
                │            │
                │            └──> Question (1:N)
                │
                └──> StudentResponse (1:N)
                       │
                       └──> Question (N:1)

User
  │
  └──> Student (1:1)
  └──> Teacher (1:1) [future]
  └──> Admin (1:1) [future]
```

### Relationship Details

| Parent Entity | Child Entity | Relationship | Foreign Key | Notes |
|---------------|--------------|--------------|-------------|-------|
| School | Class | One-to-Many | Class.SchoolId | School owns classes |
| School | Student | One-to-Many | Student.SchoolId | Null for self-service |
| Class | Student | Many-to-Many | Student.ClassIds[] | Array of class IDs |
| Class | Teacher | Many-to-Many | Class.TeacherIds[] | Array of teacher IDs |
| User | Student | One-to-One | Student.UserId | Student profile |
| Course | Assessment | One-to-Many | Assessment.CourseId | Course contains assessments |
| Course | Question | One-to-Many | Question.CourseId | Course contains questions |
| Assessment | Question | Many-to-Many | Assessment.QuestionIds[] | Ordered array |
| Student | StudentAssessment | One-to-Many | StudentAssessment.StudentId | Student's attempts |
| Assessment | StudentAssessment | One-to-Many | StudentAssessment.AssessmentId | Assessment attempts |
| StudentAssessment | StudentResponse | One-to-Many | StudentResponse.StudentAssessmentId | Answers in attempt |
| Question | StudentResponse | One-to-Many | StudentResponse.QuestionId | Question answered |

---

## 4. Aggregate Boundaries

Following DDD principles, these are the aggregate roots and their consistency boundaries:

### Aggregate 1: School Aggregate
**Root:** School  
**Entities:** Class, Student (school-based)  
**Invariants:**
- School must exist before classes
- Students must be assigned to valid school
- Minimum 5 students per class for aggregate reporting

### Aggregate 2: Course Aggregate
**Root:** Course  
**Entities:** Question, Assessment  
**Invariants:**
- Assessments must reference valid course
- Questions must belong to course
- Assessment question IDs must reference existing questions

### Aggregate 3: Assessment Session Aggregate
**Root:** StudentAssessment  
**Entities:** StudentResponse  
**Invariants:**
- StudentAssessment must reference valid student and assessment
- All responses belong to single assessment attempt
- Response question order must be unique within attempt
- Score = sum of all response points

### Aggregate 4: Student Aggregate
**Root:** Student  
**Invariants:**
- COPPA compliance: Under-13 requires parental consent
- Free tier: Max 5 assessments per week
- Level calculation: XP / 100 + 1
- Streak breaks after 24 hours inactivity

### Transactional Boundaries

```
✅ Single transaction:
  - Create StudentAssessment
  - Update student assessment status
  - Add StudentResponse to same StudentAssessment
  
❌ Cross-aggregate (eventual consistency):
  - Student enrolls in Class (updates Student.ClassIds and Class.StudentIds separately)
  - Question statistics update after response (async)
  - XP award to Student after StudentAssessment complete (async)
```

---

## 5. Enumerations

### 5.1 Subject
```csharp
public enum Subject
{
    Mathematics = 0,
    Physics = 1,
    Chemistry = 2,
    Biology = 3,
    English = 4
}
```

### 5.2 GradeLevel
```csharp
public enum GradeLevel
{
    Grade6 = 6,   // Ages 11-12
    Grade7 = 7,   // Ages 12-13
    Grade8 = 8,   // Ages 13-14
    Grade9 = 9,   // Ages 14-15
    Grade10 = 10, // Ages 15-16
    Grade11 = 11, // Ages 16-17
    Grade12 = 12  // Ages 17-18
}
```

### 5.3 AssessmentType
```csharp
public enum AssessmentType
{
    Diagnostic = 0,  // Determine current skill level
    Formative = 1,   // During learning process
    Summative = 2,   // End of learning period
    Practice = 3,    // Skill reinforcement
    Adaptive = 4     // IRT-based difficulty adjustment
}
```

### 5.4 AssessmentStatus
```csharp
public enum AssessmentStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Abandoned = 3,
    Paused = 4,
    Grading = 5,   // Being scored
    Graded = 6     // Scoring complete
}
```

### 5.5 DifficultyLevel
```csharp
public enum DifficultyLevel
{
    VeryEasy = 0,  // Below grade level
    Easy = 1,      // Lower end of grade level
    Medium = 2,    // Appropriate for grade level
    Hard = 3,      // Upper end of grade level
    VeryHard = 4   // Above grade level
}
```

### 5.6 QuestionType
```csharp
public enum QuestionType
{
    MultipleChoice = 0,   // Single correct answer
    MultipleSelect = 1,   // Multiple correct answers
    TrueFalse = 2,
    ShortAnswer = 3,      // Text input
    Essay = 4,            // Long-form response
    MathExpression = 5,   // Mathematical notation
    FillInBlank = 6,
    Matching = 7          // Match items between lists
}
```

### 5.7 UserRole
```csharp
public enum UserRole
{
    Student = 0,
    Teacher = 1,
    SchoolAdmin = 2,
    CourseAdmin = 3,
    BusinessAdmin = 4,
    SystemAdmin = 5
}
```

### 5.8 SubscriptionTier (Self-Service)
```csharp
public enum SubscriptionTier
{
    Free = 0,      // 5 assessments/week
    Basic = 1,     // 20 assessments/week
    Premium = 2,   // Unlimited
    Enterprise = 3 // Custom features
}
```

### 5.9 MasteryLevel
```csharp
public enum MasteryLevel
{
    NotStarted = 0,
    Beginner = 1,
    Developing = 2,
    Proficient = 3,
    Advanced = 4,
    Expert = 5
}
```

---

## 6. Business Rules

### Multi-Tenancy Rules

1. **Physical Isolation (B2B Schools)**
   - Each school has dedicated PostgreSQL database
   - SchoolId present on all tenant-scoped entities
   - Query filters automatically apply SchoolId WHERE clause

2. **Logical Isolation (B2C Self-Service)**
   - SchoolId = null for self-service students
   - Stored in shared "selfservice" virtual tenant
   - Additional query filters for user isolation

3. **Data Visibility**
   - Students: Own data only
   - Teachers: Students in assigned classes
   - School Admins: All school data
   - Course Admins: Anonymized cross-school data (min 5 students)
   - Business/System Admins: All data

### Privacy Rules

1. **COPPA Compliance (Under 13)**
   - Date of birth required
   - Parental consent mandatory
   - Parent email captured
   - Restricted data collection

2. **K-Anonymity**
   - Aggregate reports require minimum 5 students
   - Class-level reports suppressed if < 5 students
   - Course admins see anonymized data only

3. **Data Retention**
   - Active student data: Indefinite
   - Abandoned assessments: 90 days
   - Inactive accounts: 2 years
   - Audit logs: 7 years

### Assessment Rules

1. **Adaptive Assessment (IRT)**
   - Starting theta: 0.0 (average ability)
   - Update theta after each question
   - Select next question with highest information at current theta
   - Minimum 5 questions per assessment
   - Stop criterion: Standard error < 0.3 OR 30 questions

2. **Scoring**
   - Points per question: 1-10 (defined in Question.Points)
   - Partial credit: Not supported (binary correct/incorrect)
   - Final score: Sum of PointsEarned across all responses
   - Passing: PercentageScore >= PassingScorePercentage

3. **Time Limits**
   - Assessment.TimeLimitMinutes = null: Untimed
   - Grace period: 5 minutes after time expires
   - Auto-submit on time expiration

### Gamification Rules (Self-Service)

1. **XP Awards**
   - Assessment completion: Score percentage × 100 XP
   - Perfect score bonus: +50 XP
   - Daily streak bonus: Streak count × 10 XP

2. **Level Calculation**
   - Level = (XP / 100) + 1
   - Level 1: 0-99 XP
   - Level 2: 100-199 XP
   - etc.

3. **Streak Maintenance**
   - Activity within 24 hours maintains streak
   - 24+ hours breaks streak (resets to 1)
   - Activity = submitting any assessment question

### Question Quality Rules

1. **IRT Calibration**
   - Requires 100+ responses for stable parameters
   - Recalibrate every 500 responses
   - Flag questions with poor fit (info < 0.1)

2. **Duplicate Detection**
   - ContentHash = SHA256(QuestionText + CorrectAnswer)
   - Block questions with matching hash
   - Allow deliberate duplicates via metadata flag

3. **Success Rate Thresholds**
   - Remove if < 10% success rate (too hard)
   - Review if > 95% success rate (too easy)
   - Optimal range: 50-80% success rate

---

## 7. Domain Events

Domain events for eventual consistency and integration:

### Student Events
- `StudentEnrolled(StudentId, SchoolId, GradeLevel)`
- `StudentProgressedToNextLevel(StudentId, NewLevel)`
- `StreakBroken(StudentId, PreviousStreak)`
- `SubscriptionExpiring(StudentId, ExpiresAt)`

### Assessment Events
- `AssessmentStarted(StudentAssessmentId, StudentId, AssessmentId)`
- `QuestionAnswered(StudentResponseId, IsCorrect, PointsEarned)`
- `AssessmentCompleted(StudentAssessmentId, FinalScore, Passed)`
- `AssessmentAbandoned(StudentAssessmentId)`

### Question Events
- `QuestionCreated(QuestionId, IsAiGenerated)`
- `QuestionCalibrated(QuestionId, IrtParameters)`
- `QuestionDeactivated(QuestionId, Reason)`

### School Events
- `SchoolOnboarded(SchoolId, DatabaseName)`
- `ClassCreated(ClassId, SchoolId, EnrollmentCount)`
- `ClassSizeBelowReportingThreshold(ClassId, EnrollmentCount)`

---

## Related Documentation

- **04-application-components.md** - How domain models are used in application layer
- **05-data-storage.md** - Database schema and EF Core mappings
- **07-security-privacy.md** - Privacy rules and compliance
- **.github/instructions.md** - Coding patterns for domain models

---

**Implementation Location:** `src/AcademicAssessment.Core/Models/`  
**Enumerations Location:** `src/AcademicAssessment.Core/Enums/`
