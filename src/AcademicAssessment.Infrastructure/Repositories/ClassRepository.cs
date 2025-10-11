using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Class entities with tenant filtering
/// </summary>
public sealed class ClassRepository : RepositoryBase<Class, Guid>, IClassRepository
{
    public ClassRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(Class entity) => entity.Id;

    public Task<Result<IReadOnlyList<Class>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.SchoolId == schoolId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Class>>> GetByTeacherIdAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.TeacherIds.Contains(teacherId)),
            cancellationToken);

    public Task<Result<IReadOnlyList<Class>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.StudentIds.Contains(studentId)),
            cancellationToken);

    public Task<Result<IReadOnlyList<Class>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.Subject == subject && c.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Class>>> GetByAcademicYearAsync(
        string academicYear,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.AcademicYear == academicYear),
            cancellationToken);

    public Task<Result<Class>> GetBySchoolAndCodeAsync(
        Guid schoolId,
        string code,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(c => c.SchoolId == schoolId && c.Code == code),
            cancellationToken);

    public Task<Result<IReadOnlyList<Class>>> GetClassesWithAggregateReportingAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(c => c.StudentIds.Count >= 5),
            cancellationToken);
}
