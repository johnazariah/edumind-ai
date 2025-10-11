using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Assessment entities with tenant filtering
/// </summary>
public sealed class AssessmentRepository : RepositoryBase<Assessment, Guid>, IAssessmentRepository
{
    public AssessmentRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(Assessment entity) => entity.Id;

    public Task<Result<IReadOnlyList<Assessment>>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.CourseId == courseId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetByTypeAsync(
        AssessmentType assessmentType,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.AssessmentType == assessmentType),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.Subject == subject && a.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.SchoolId == schoolId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetGlobalAssessmentsAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.SchoolId == null),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetAdaptiveAssessmentsAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.AssessmentType == AssessmentType.Adaptive),
            cancellationToken);

    public Task<Result<IReadOnlyList<Assessment>>> GetByTopicsAsync(
        IReadOnlyList<string> topics,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(a => a.Topics.Any(t => topics.Contains(t))),
            cancellationToken);
}
