using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for StudentAssessment entities
/// Includes privacy-preserving aggregate methods
/// </summary>
public sealed class StudentAssessmentRepository :
    RepositoryBase<StudentAssessment, Guid>,
    IStudentAssessmentRepository
{
    public StudentAssessmentRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(StudentAssessment entity) => entity.Id;

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa => sa.StudentId == studentId)
                          .OrderByDescending(sa => sa.CreatedAt),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByAssessmentIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa => sa.AssessmentId == assessmentId),
            cancellationToken);

    public Task<Result<StudentAssessment>> GetByStudentAndAssessmentAsync(
        Guid studentId,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(sa => sa.StudentId == studentId && sa.AssessmentId == assessmentId),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetInProgressByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa =>
                sa.StudentId == studentId &&
                (sa.Status == AssessmentStatus.InProgress || sa.Status == AssessmentStatus.Paused)),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetCompletedByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa =>
                sa.StudentId == studentId &&
                sa.Status == AssessmentStatus.Completed)
                          .OrderByDescending(sa => sa.CompletedAt),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByStatusAsync(
        AssessmentStatus status,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa => sa.Status == status),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByClassIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa => sa.ClassId == classId),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sa =>
                sa.CompletedAt.HasValue &&
                sa.CompletedAt >= startDate &&
                sa.CompletedAt <= endDate),
            cancellationToken);

    /// <summary>
    /// Gets average score with k-anonymity check (min 5 students)
    /// </summary>
    public async Task<Result<double>> GetAverageScoreAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var completedAssessments = await DbSet
                .Where(sa =>
                    sa.AssessmentId == assessmentId &&
                    sa.Status == AssessmentStatus.Completed &&
                    sa.Score.HasValue)
                .ToListAsync(cancellationToken);

            // K-anonymity check: minimum 5 students
            if (completedAssessments.Count < 5)
            {
                return Error.Forbidden("Insufficient data to provide aggregate statistics (minimum 5 students required)");
            }

            var average = completedAssessments.Average(sa => sa.Score!.Value);
            return new Result<double>.Success(average);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    /// <summary>
    /// Gets pass rate with k-anonymity check (min 5 students)
    /// </summary>
    public async Task<Result<double>> GetPassRateAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var completedAssessments = await DbSet
                .Where(sa =>
                    sa.AssessmentId == assessmentId &&
                    sa.Status == AssessmentStatus.Completed &&
                    sa.Passed.HasValue)
                .ToListAsync(cancellationToken);

            // K-anonymity check: minimum 5 students
            if (completedAssessments.Count < 5)
            {
                return Error.Forbidden("Insufficient data to provide aggregate statistics (minimum 5 students required)");
            }

            var passRate = completedAssessments.Count(sa => sa.Passed!.Value) /
                          (double)completedAssessments.Count * 100;

            return new Result<double>.Success(passRate);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }
}
