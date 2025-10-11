using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Course entities
/// </summary>
public sealed class CourseRepository : RepositoryBase<Course, Guid>, ICourseRepository
{
    public CourseRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(Course entity) => entity.Id;

    public Task<Result<Course>> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(c => c.Code == code),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> GetBySubjectAsync(
        Subject subject,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.Subject == subject),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> GetByGradeLevelAsync(
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.Subject == subject && c.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> GetActiveCoursesAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.IsActive),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> GetByCourseAdminAsync(
        Guid courseAdminId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.CourseAdminId == courseAdminId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Course>>> SearchByTopicAsync(
        string topic,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.Topics.Contains(topic)),
            cancellationToken);
}
