using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for student entities with tenant filtering
/// </summary>
public interface IStudentRepository : IRepository<Student, Guid>
{
    /// <summary>
    /// Gets a student by user ID
    /// </summary>
    Task<Result<Student>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all students in a specific class
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetByClassIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all students in a specific school
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all self-service (B2C) students
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetSelfServiceStudentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets students by grade level
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetByGradeLevelAsync(
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets students by subscription tier (for self-service students)
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetBySubscriptionTierAsync(
        SubscriptionTier tier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets students whose subscriptions are expiring soon
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetExpiringSubscriptionsAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets students requiring COPPA parental consent
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetRequiringCoppaConsentAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top students by XP (leaderboard)
    /// </summary>
    Task<Result<IReadOnlyList<Student>>> GetTopByXpAsync(
        int count,
        CancellationToken cancellationToken = default);
}
