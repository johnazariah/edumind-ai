# Student Self-Service Onboarding Architecture

## Overview

EduMind.AI supports **two distinct onboarding models** to serve different use cases:

1. **School-Based Model** (B2B) - Schools purchase subscriptions, administrators add students
2. **Self-Service Model** (B2C) - Individual students sign up directly, like Duolingo

This document focuses on the **self-service casual student onboarding** while maintaining the same privacy protections.

---

## Two-Tier Architecture

### Comparison: School vs. Self-Service

| Aspect | School-Based (B2B) | Self-Service (B2C) |
|--------|-------------------|-------------------|
| **Target User** | Schools with 100+ students | Individual learners |
| **Onboarding** | Admin adds students | Self-signup with email/social |
| **Payment** | School pays subscription | Free tier + premium upgrades |
| **Privacy Model** | Strict FERPA compliance | COPPA + parental consent |
| **Database** | Dedicated per school | Shared "self-service" tenant |
| **Features** | Full assessment suite | Core features + gamification |
| **Support** | Priority support | Community + self-help |
| **Data Isolation** | Physical (separate DB) | Logical (row-level security) |

---

## Self-Service User Journey

### Phase 1: Discovery & Signup

```
┌─────────────────────────────────────────────────────────────────┐
│                  Marketing Website                              │
│                  www.edumind.ai                                 │
│                                                                 │
│  "Learn smarter with AI-powered assessments"                   │
│  [Start Learning Free] [How It Works] [Pricing]                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Signup Options                                │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   Continue   │  │   Continue   │  │   Continue   │         │
│  │  with Google │  │  with Apple  │  │  with Email  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
│                                                                 │
│  "Already have an account? Sign In"                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               Profile Creation (Step 1 of 3)                    │
│                                                                 │
│  What's your name?                                             │
│  [First Name] [Last Name]                                      │
│                                                                 │
│  How old are you?                                              │
│  [Birthday Picker] or [Age Range Dropdown]                     │
│                                                                 │
│  What grade are you in?                                        │
│  ○ 8th Grade  ○ 9th Grade  ○ 10th Grade                       │
│  ○ 11th Grade ○ 12th Grade ○ Adult Learner                    │
│                                                                 │
│  [Next →]                                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            Subject Selection (Step 2 of 3)                      │
│                                                                 │
│  Which subjects are you interested in?                         │
│  (Select at least one)                                         │
│                                                                 │
│  ☑ Mathematics        ☐ Physics                               │
│  ☐ Chemistry          ☑ Biology                               │
│  ☐ English                                                     │
│                                                                 │
│  [← Back] [Next →]                                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│             Learning Goals (Step 3 of 3)                        │
│                                                                 │
│  What brings you here?                                         │
│  ○ Prepare for exams                                           │
│  ○ Improve my grades                                           │
│  ○ Learn at my own pace                                        │
│  ○ Challenge myself                                            │
│  ○ Just curious                                                │
│                                                                 │
│  How much time can you dedicate per week?                      │
│  ○ 15-30 minutes  ○ 30-60 minutes  ○ 1-2 hours  ○ 2+ hours   │
│                                                                 │
│  [← Back] [Start Learning! →]                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│           ✓ Account Created Successfully!                       │
│                                                                 │
│  Welcome to EduMind.AI! Let's get you started...              │
│  [Begin Assessment →]                                          │
└─────────────────────────────────────────────────────────────────┘
```

### Phase 2: Parental Consent (if under 13)

If the user indicates they are under 13 years old (COPPA compliance):

```
┌─────────────────────────────────────────────────────────────────┐
│               Parental Consent Required                         │
│                                                                 │
│  We need your parent or guardian's permission to create        │
│  your account.                                                 │
│                                                                 │
│  Parent/Guardian Email:                                        │
│  [_______________________]                                     │
│                                                                 │
│  [Send Consent Request]                                        │
│                                                                 │
│  Your account will be activated once your parent approves.     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│          Email to Parent/Guardian                              │
│                                                                 │
│  Subject: Your child wants to use EduMind.AI                  │
│                                                                 │
│  Hi [Parent],                                                  │
│                                                                 │
│  Your child [Student Name] wants to create an account on      │
│  EduMind.AI to practice [subjects].                           │
│                                                                 │
│  Please review our Privacy Policy and click below to approve:  │
│                                                                 │
│  [Review & Approve Account]                                    │
│                                                                 │
│  What data we collect:                                         │
│  • Name, email, grade level                                    │
│  • Assessment responses and progress                           │
│  • Study time and activity logs                                │
│                                                                 │
│  What we DON'T do:                                            │
│  • Sell or share personal information                          │
│  • Show advertisements                                         │
│  • Share data with third parties                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Self-Service Data Model

### Virtual "Self-Service School" Tenant

Instead of creating a database per student (expensive), we create a **virtual school** for all self-service users:

```
┌──────────────────────────────────────────────────────────────┐
│           School Database: "edumind_selfservice"             │
│           (Shared by all self-service students)              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Virtual Classes (Auto-Generated)                      │ │
│  │                                                         │ │
│  │  • Class: "Grade 8 Mathematics"                        │ │
│  │    Students: [1,203 students]                          │ │
│  │                                                         │ │
│  │  • Class: "Grade 9 Physics"                            │ │
│  │    Students: [847 students]                            │ │
│  │                                                         │ │
│  │  • Class: "Grade 10 Chemistry"                         │ │
│  │    Students: [612 students]                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  Privacy: Students can only see their own data              │
│  No cross-student visibility                                │
│  Aggregate leaderboards anonymized                          │
└──────────────────────────────────────────────────────────────┘
```

### Domain Models for Self-Service

```csharp
/// <summary>
/// Self-service student account
/// </summary>
public record SelfServiceStudent : Student
{
    // Inherited from Student: Id, UserId, SchoolId, ClassId, GradeLevel
    
    public required SignupMethod SignupMethod { get; init; }  // Email, Google, Apple
    public required string? ExternalAuthId { get; init; }     // OAuth provider ID
    public required SubscriptionTier Tier { get; init; }      // Free, Premium
    public DateTime? PremiumExpiresAt { get; init; }
    
    // Parental consent (COPPA)
    public bool RequiresParentalConsent { get; init; }
    public string? ParentEmail { get; init; }
    public DateTime? ParentalConsentGrantedAt { get; init; }
    public bool IsAccountActive { get; init; }
    
    // Learning preferences
    public List<Subject> InterestedSubjects { get; init; } = [];
    public LearningGoal PrimaryGoal { get; init; }
    public TimeCommitment WeeklyCommitment { get; init; }
    
    // Gamification
    public int ExperiencePoints { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public DateTime? LastActivityDate { get; init; }
    public List<Achievement> Achievements { get; init; } = [];
}

public enum SignupMethod
{
    Email,
    Google,
    Apple,
    Microsoft
}

public enum LearningGoal
{
    PrepareForExams,
    ImproveGrades,
    LearnAtOwnPace,
    ChallengeSelf,
    JustCurious
}

public enum TimeCommitment
{
    Light,      // 15-30 min/week
    Moderate,   // 30-60 min/week
    Committed,  // 1-2 hours/week
    Intensive   // 2+ hours/week
}

public enum SubscriptionTier
{
    Free,           // Limited assessments per week
    Premium,        // Unlimited assessments
    PremiumPlus     // Unlimited + 1-on-1 tutoring
}

/// <summary>
/// Virtual class for self-service students
/// Auto-created based on grade level and subject
/// </summary>
public record VirtualClass : Class
{
    // Inherited: Id, Name, SchoolId, GradeLevel
    
    public required Subject Subject { get; init; }
    public bool IsVirtual { get; init; } = true;
    public bool AllowLeaderboard { get; init; } = true;
    public bool IsPublic { get; init; } = true;
    
    // Students are dynamically added when they select this subject
    // No teacher assignment needed
}

/// <summary>
/// Gamification achievement
/// </summary>
public record Achievement
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string IconUrl { get; init; }
    public required AchievementCategory Category { get; init; }
    public required int PointsAwarded { get; init; }
    public DateTime UnlockedAt { get; init; }
}

public enum AchievementCategory
{
    Streak,          // "7 Day Streak", "30 Day Streak"
    Mastery,         // "Math Master", "Physics Pro"
    Progress,        // "First Assessment", "100 Questions"
    Challenge,       // "Perfect Score", "Speed Demon"
    Social           // "Helpful Peer", "Top 10%"
}
```

---

## Self-Service Onboarding Implementation

### Step 1: Signup Endpoint

```csharp
/// <summary>
/// Self-service student signup request
/// </summary>
public record SelfServiceSignupRequest
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required int Age { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required List<Subject> InterestedSubjects { get; init; }
    public required LearningGoal PrimaryGoal { get; init; }
    public required TimeCommitment WeeklyCommitment { get; init; }
    public required SignupMethod SignupMethod { get; init; }
    public string? ExternalAuthId { get; init; }  // For OAuth
    public string? ParentEmail { get; init; }     // If age < 13
}

/// <summary>
/// Self-service signup endpoint
/// </summary>
[ApiController]
[Route("api/public/signup")]
public class SelfServiceSignupController : ControllerBase
{
    private readonly ISelfServiceOnboardingService _onboardingService;
    private readonly ILogger<SelfServiceSignupController> _logger;
    
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SignupAsync(
        [FromBody] SelfServiceSignupRequest request,
        CancellationToken ct)
    {
        // Validate request
        if (request.InterestedSubjects.Count == 0)
        {
            return BadRequest("At least one subject must be selected");
        }
        
        // Create account (may be pending if under 13)
        var result = await _onboardingService.CreateSelfServiceAccountAsync(
            request, ct);
        
        return result switch
        {
            Result<SelfServiceSignupResponse>.Success(var response) => 
                response.RequiresParentalConsent
                    ? Accepted(response) // 202: Pending parental consent
                    : Created($"/api/student/profile", response), // 201: Active
            
            Result<SelfServiceSignupResponse>.Failure(var error) =>
                error.Code switch
                {
                    "EMAIL_ALREADY_EXISTS" => Conflict(error.Message),
                    "INVALID_AGE" => BadRequest(error.Message),
                    _ => StatusCode(500, error.Message)
                },
            
            _ => StatusCode(500, "Unexpected error")
        };
    }
}
```

### Step 2: Onboarding Service

```csharp
public interface ISelfServiceOnboardingService
{
    Task<Result<SelfServiceSignupResponse>> CreateSelfServiceAccountAsync(
        SelfServiceSignupRequest request,
        CancellationToken ct = default);
    
    Task<Result<Unit>> SendParentalConsentRequestAsync(
        Guid studentId,
        string parentEmail,
        CancellationToken ct = default);
    
    Task<Result<Unit>> GrantParentalConsentAsync(
        Guid studentId,
        string consentToken,
        CancellationToken ct = default);
}

public class SelfServiceOnboardingService : ISelfServiceOnboardingService
{
    private const string SelfServiceSchoolId = "00000000-0000-0000-0000-000000000001";
    
    private readonly IUserRepository _userRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IVirtualClassService _virtualClassService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SelfServiceOnboardingService> _logger;
    
    public async Task<Result<SelfServiceSignupResponse>> CreateSelfServiceAccountAsync(
        SelfServiceSignupRequest request,
        CancellationToken ct)
    {
        try
        {
            // 1. Check if email already exists
            var existingUser = await _userRepo.GetByEmailAsync(request.Email, ct);
            if (existingUser is Result<User>.Success)
            {
                return new Error("EMAIL_ALREADY_EXISTS", 
                    "An account with this email already exists");
            }
            
            // 2. Determine if parental consent required (COPPA)
            var requiresConsent = request.Age < 13;
            var isActive = !requiresConsent;
            
            // 3. Create user account
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.Student,
                SchoolId = Guid.Parse(SelfServiceSchoolId),
                CreatedAt = DateTime.UtcNow,
                IsActive = isActive
            };
            
            await _userRepo.AddAsync(user, ct);
            
            // 4. Get or create virtual classes for selected subjects
            var classIds = new List<Guid>();
            foreach (var subject in request.InterestedSubjects)
            {
                var virtualClass = await _virtualClassService
                    .GetOrCreateVirtualClassAsync(
                        request.GradeLevel, 
                        subject, 
                        ct);
                
                if (virtualClass is Result<VirtualClass>.Success(var cls))
                {
                    classIds.Add(cls.Id);
                }
            }
            
            // 5. Create student profile
            var student = new SelfServiceStudent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SchoolId = Guid.Parse(SelfServiceSchoolId),
                ClassId = classIds.FirstOrDefault(), // Primary class
                GradeLevel = request.GradeLevel,
                StudentNumber = GenerateStudentNumber(),
                EnrollmentDate = DateTime.UtcNow,
                SignupMethod = request.SignupMethod,
                ExternalAuthId = request.ExternalAuthId,
                Tier = SubscriptionTier.Free,
                RequiresParentalConsent = requiresConsent,
                ParentEmail = request.ParentEmail,
                IsAccountActive = isActive,
                InterestedSubjects = request.InterestedSubjects,
                PrimaryGoal = request.PrimaryGoal,
                WeeklyCommitment = request.WeeklyCommitment,
                ExperiencePoints = 0,
                CurrentStreak = 0,
                LongestStreak = 0,
                Achievements = []
            };
            
            await _studentRepo.AddAsync(student, ct);
            
            // 6. If requires consent, send email to parent
            if (requiresConsent && !string.IsNullOrEmpty(request.ParentEmail))
            {
                await SendParentalConsentRequestAsync(
                    student.Id, 
                    request.ParentEmail, 
                    ct);
            }
            
            // 7. Send welcome email to student
            await _emailService.SendWelcomeEmailAsync(
                student.Id, 
                request.Email, 
                request.FirstName, 
                ct);
            
            _logger.LogInformation(
                "Self-service account created: {StudentId}, Email: {Email}, Requires Consent: {RequiresConsent}",
                student.Id, request.Email, requiresConsent);
            
            return new SelfServiceSignupResponse
            {
                StudentId = student.Id,
                UserId = userId,
                RequiresParentalConsent = requiresConsent,
                IsActive = isActive,
                Message = requiresConsent
                    ? "Account created! Please check your parent's email for approval."
                    : "Welcome to EduMind.AI! Your account is ready."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create self-service account");
            return new Error("SIGNUP_FAILED", 
                "Failed to create account. Please try again.", ex);
        }
    }
    
    private static string GenerateStudentNumber() =>
        $"SS-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
}
```

### Step 3: Virtual Class Management

```csharp
public interface IVirtualClassService
{
    Task<Result<VirtualClass>> GetOrCreateVirtualClassAsync(
        GradeLevel grade,
        Subject subject,
        CancellationToken ct = default);
    
    Task<Result<Unit>> AddStudentToVirtualClassAsync(
        Guid studentId,
        Guid classId,
        CancellationToken ct = default);
}

public class VirtualClassService : IVirtualClassService
{
    private readonly IClassRepository _classRepo;
    private readonly ILogger<VirtualClassService> _logger;
    
    public async Task<Result<VirtualClass>> GetOrCreateVirtualClassAsync(
        GradeLevel grade,
        Subject subject,
        CancellationToken ct)
    {
        // Try to find existing virtual class
        var className = $"Grade {(int)grade} {subject}";
        var existingClass = await _classRepo.GetByNameAsync(
            SelfServiceSchoolId, 
            className, 
            ct);
        
        if (existingClass is Result<Class>.Success(var cls) && 
            cls is VirtualClass virtualClass)
        {
            return virtualClass;
        }
        
        // Create new virtual class
        var newClass = new VirtualClass
        {
            Id = Guid.NewGuid(),
            Name = className,
            SchoolId = Guid.Parse(SelfServiceSchoolId),
            GradeLevel = grade,
            Subject = subject,
            IsVirtual = true,
            AllowLeaderboard = true,
            IsPublic = true,
            MaxCapacity = 100000, // No limit for virtual classes
            IsActive = true,
            StudentIds = []
        };
        
        await _classRepo.AddAsync(newClass, ct);
        
        _logger.LogInformation(
            "Created virtual class: {ClassName} ({Subject}, Grade {Grade})",
            className, subject, grade);
        
        return newClass;
    }
}
```

---

## Privacy Considerations for Self-Service

### 1. COPPA Compliance (Under 13)

**Requirement:** Children under 13 require verifiable parental consent.

**Implementation:**
- Age verification during signup
- Parent email required for under-13 users
- Account inactive until parent approves
- Double opt-in: parent must click email link and review privacy policy

### 2. Data Minimization

Collect only what's necessary:
- ✅ Name, email, grade level (required for service)
- ✅ Assessment responses (core functionality)
- ✅ Progress tracking (personalization)
- ❌ Address, phone number (not collected)
- ❌ Payment info for free tier (not collected)

### 3. Self-Service Privacy Model

**Differences from School Model:**

| Aspect | School Model | Self-Service Model |
|--------|-------------|-------------------|
| **Database** | Dedicated per school | Shared "selfservice" DB |
| **Isolation** | Physical | Logical (row-level) |
| **Visibility** | Teachers see class | No cross-student visibility |
| **Aggregation** | Class/school reports | Anonymized leaderboards only |
| **Consent** | School admin manages | Individual/parental consent |

### 4. Anonymized Leaderboards

Self-service students can see anonymized leaderboards:

```csharp
public record AnonymizedLeaderboardEntry
{
    public required string AnonymousName { get; init; }  // "Student #42"
    public required int Rank { get; init; }
    public required int ExperiencePoints { get; init; }
    public required List<Achievement> PublicAchievements { get; init; }
    
    // NO: Real name, email, detailed progress
}

public async Task<Result<List<AnonymizedLeaderboardEntry>>> GetLeaderboardAsync(
    GradeLevel grade,
    Subject subject,
    CancellationToken ct)
{
    var students = await _studentRepo.GetTopPerformersAsync(
        grade, subject, limit: 100, ct);
    
    return students
        .Select((s, index) => new AnonymizedLeaderboardEntry
        {
            AnonymousName = $"Student #{index + 1}",
            Rank = index + 1,
            ExperiencePoints = s.ExperiencePoints,
            PublicAchievements = s.Achievements
                .Where(a => a.IsPublic)
                .ToList()
        })
        .ToList();
}
```

---

## Freemium Model

### Free Tier (B2C)

**Included:**
- ✅ 5 assessments per week
- ✅ Core subjects (all 5)
- ✅ Basic progress tracking
- ✅ Anonymized leaderboards
- ✅ Achievement system
- ✅ Mobile app access

**Limited:**
- ❌ No detailed analytics
- ❌ No personalized study plans
- ❌ No priority support
- ❌ Ads displayed (optional)

### Premium Tier ($9.99/month)

**Everything in Free, plus:**
- ✅ Unlimited assessments
- ✅ Detailed analytics and insights
- ✅ Personalized study plans
- ✅ Ad-free experience
- ✅ Priority support
- ✅ Export progress reports

### Premium Plus Tier ($24.99/month)

**Everything in Premium, plus:**
- ✅ 1-on-1 virtual tutoring (4 hours/month)
- ✅ Live homework help
- ✅ College prep resources
- ✅ Parent dashboard access

---

## Migration Path: Self-Service → School

If a self-service student's school adopts EduMind.AI:

```csharp
public interface IStudentMigrationService
{
    /// <summary>
    /// Migrate self-service student to school account
    /// </summary>
    Task<Result<Unit>> MigrateToSchoolAccountAsync(
        Guid selfServiceStudentId,
        Guid targetSchoolId,
        Guid targetClassId,
        CancellationToken ct = default);
}

public class StudentMigrationService : IStudentMigrationService
{
    public async Task<Result<Unit>> MigrateToSchoolAccountAsync(
        Guid selfServiceStudentId,
        Guid targetSchoolId,
        Guid targetClassId,
        CancellationToken ct)
    {
        // 1. Load student from self-service DB
        var student = await _studentRepo.GetByIdAsync(selfServiceStudentId, ct);
        
        // 2. Export all historical data (assessments, progress)
        var historicalData = await ExportStudentDataAsync(selfServiceStudentId, ct);
        
        // 3. Create new student in target school's database
        var newStudent = student with
        {
            Id = Guid.NewGuid(), // New ID in school database
            SchoolId = targetSchoolId,
            ClassId = targetClassId,
            EnrollmentDate = DateTime.UtcNow
        };
        
        // Switch to target school database context
        await using var schoolContext = await _dbContextFactory
            .CreateDbContextForSchoolAsync(targetSchoolId, ct);
        
        // 4. Import student and historical data
        await schoolContext.Students.AddAsync(newStudent, ct);
        await ImportHistoricalDataAsync(newStudent.Id, historicalData, ct);
        await schoolContext.SaveChangesAsync(ct);
        
        // 5. Mark old account as migrated (keep for audit trail)
        await MarkAccountAsMigratedAsync(selfServiceStudentId, targetSchoolId, ct);
        
        // 6. Update user to point to new school
        await _userRepo.UpdateSchoolAsync(
            student.UserId, 
            targetSchoolId, 
            ct);
        
        _logger.LogInformation(
            "Migrated self-service student {StudentId} to school {SchoolId}",
            selfServiceStudentId, targetSchoolId);
        
        return Unit.Value;
    }
}
```

---

## Summary: Dual Model Architecture

### School-Based (B2B)
- **Target:** Schools with 100+ students
- **Privacy:** Physical database per school (strictest)
- **Features:** Full suite, teacher tools, admin dashboards
- **Payment:** School pays subscription
- **Support:** Priority, dedicated

### Self-Service (B2C)
- **Target:** Individual learners
- **Privacy:** Logical isolation in shared DB (still secure)
- **Features:** Core features + gamification
- **Payment:** Freemium (free + premium upgrades)
- **Support:** Self-help + community

### Both Models:
- ✅ FERPA/COPPA/GDPR compliant
- ✅ Comprehensive audit logging
- ✅ Right to be forgotten
- ✅ No data sharing with third parties
- ✅ Same AI-powered assessment engine
- ✅ Migration path between models

---

## Implementation Priority

### Phase 1: Core Self-Service (Week 1-2)
- [ ] Self-service signup endpoint
- [ ] OAuth integration (Google, Apple)
- [ ] Virtual class management
- [ ] COPPA parental consent flow

### Phase 2: Gamification (Week 3)
- [ ] Achievement system
- [ ] Streak tracking
- [ ] Experience points
- [ ] Anonymized leaderboards

### Phase 3: Freemium (Week 4)
- [ ] Free tier limits (5 assessments/week)
- [ ] Premium upgrade flow
- [ ] Payment integration (Stripe)
- [ ] Premium feature gates

### Phase 4: Migration (Week 5)
- [ ] Self-service → School migration tool
- [ ] Data export/import
- [ ] Parent dashboard (Premium Plus)

---

*Last Updated: October 11, 2025*
