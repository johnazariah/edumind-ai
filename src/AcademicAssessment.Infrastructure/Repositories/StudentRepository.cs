using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Student entities with tenant filtering
/// </summary>
public sealed class StudentRepository : RepositoryBase<Student, Guid>, IStudentRepository
{
    public StudentRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(Student entity) => entity.Id;

    public Task<Result<Student>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(s => s.UserId == userId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetByClassIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.ClassIds.Contains(classId)),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.SchoolId == schoolId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetSelfServiceStudentsAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.SchoolId == null),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetByGradeLevelAsync(
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetBySubscriptionTierAsync(
        SubscriptionTier tier,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.SubscriptionTier == tier && s.SchoolId == null),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetExpiringSubscriptionsAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default)
    {
        var expirationDate = DateTimeOffset.UtcNow.AddDays(daysUntilExpiration);
        return FindManyAsync(
            query => query.Where(s =>
                s.SchoolId == null &&
                s.SubscriptionExpiresAt.HasValue &&
                s.SubscriptionExpiresAt <= expirationDate &&
                s.SubscriptionExpiresAt >= DateTimeOffset.UtcNow),
            cancellationToken);
    }

    public Task<Result<IReadOnlyList<Student>>> GetRequiringCoppaConsentAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s =>
                s.DateOfBirth.HasValue &&
                !s.ParentalConsentGranted &&
                s.DateOfBirth.Value.AddYears(13) > DateOnly.FromDateTime(DateTime.UtcNow)),
            cancellationToken);

    public Task<Result<IReadOnlyList<Student>>> GetTopByXpAsync(
        int count,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.SchoolId == null)
                          .OrderByDescending(s => s.XpPoints)
                          .Take(count),
            cancellationToken);
}
