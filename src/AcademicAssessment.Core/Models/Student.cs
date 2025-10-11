using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a student in the system
/// Can be either school-based (B2B) or self-service (B2C)
/// </summary>
public record Student
{
    /// <summary>
    /// Unique identifier for the student
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User ID (references User table)
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// School ID for school-based students (null for self-service)
    /// </summary>
    public Guid? SchoolId { get; init; }

    /// <summary>
    /// Class IDs the student is enrolled in
    /// </summary>
    public IReadOnlyList<Guid> ClassIds { get; init; } = [];

    /// <summary>
    /// Grade level of the student
    /// </summary>
    public required GradeLevel GradeLevel { get; init; }

    /// <summary>
    /// Date of birth (for age verification and COPPA compliance)
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Whether the student is under 13 and requires COPPA compliance
    /// </summary>
    public bool RequiresCoppaCompliance =>
        DateOfBirth.HasValue &&
        DateOfBirth.Value.AddYears(13) > DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Parental consent status for students under 13
    /// </summary>
    public bool ParentalConsentGranted { get; init; }

    /// <summary>
    /// Email of parent/guardian (for COPPA compliance)
    /// </summary>
    public string? ParentEmail { get; init; }

    /// <summary>
    /// Whether this is a self-service (B2C) student
    /// </summary>
    public bool IsSelfService => !SchoolId.HasValue;

    /// <summary>
    /// Subscription tier for self-service students
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; init; } = SubscriptionTier.Free;

    /// <summary>
    /// When the subscription expires (for self-service students)
    /// </summary>
    public DateTimeOffset? SubscriptionExpiresAt { get; init; }

    /// <summary>
    /// Gamification level (for self-service students)
    /// </summary>
    public int Level { get; init; } = 1;

    /// <summary>
    /// Gamification XP points (for self-service students)
    /// </summary>
    public int XpPoints { get; init; } = 0;

    /// <summary>
    /// Daily streak count (for engagement)
    /// </summary>
    public int DailyStreak { get; init; } = 0;

    /// <summary>
    /// Last activity date (for streak tracking)
    /// </summary>
    public DateOnly? LastActivityDate { get; init; }

    /// <summary>
    /// When the student account was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the student record was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a new student with updated properties
    /// </summary>
    public Student With(
        GradeLevel? gradeLevel = null,
        IReadOnlyList<Guid>? classIds = null,
        SubscriptionTier? subscriptionTier = null,
        int? level = null,
        int? xpPoints = null,
        int? dailyStreak = null,
        DateOnly? lastActivityDate = null) =>
        this with
        {
            GradeLevel = gradeLevel ?? GradeLevel,
            ClassIds = classIds ?? ClassIds,
            SubscriptionTier = subscriptionTier ?? SubscriptionTier,
            Level = level ?? Level,
            XpPoints = xpPoints ?? XpPoints,
            DailyStreak = dailyStreak ?? DailyStreak,
            LastActivityDate = lastActivityDate ?? LastActivityDate,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Adds XP points and updates level if necessary
    /// </summary>
    public Student AddXp(int points)
    {
        var newXp = XpPoints + points;
        var newLevel = CalculateLevel(newXp);

        return this with
        {
            XpPoints = newXp,
            Level = newLevel,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates daily streak if student is active today
    /// </summary>
    public Student UpdateStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (LastActivityDate == today)
        {
            return this; // Already updated today
        }

        var yesterday = today.AddDays(-1);
        var newStreak = LastActivityDate == yesterday ? DailyStreak + 1 : 1;

        return this with
        {
            DailyStreak = newStreak,
            LastActivityDate = today,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Enrolls student in a class
    /// </summary>
    public Student EnrollInClass(Guid classId) =>
        ClassIds.Contains(classId)
            ? this
            : this with
            {
                ClassIds = ClassIds.Append(classId).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Removes student from a class
    /// </summary>
    public Student UnenrollFromClass(Guid classId) =>
        this with
        {
            ClassIds = ClassIds.Where(id => id != classId).ToList().AsReadOnly(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Calculates level based on XP (100 XP per level)
    /// </summary>
    private static int CalculateLevel(int xp) => (xp / 100) + 1;
}
