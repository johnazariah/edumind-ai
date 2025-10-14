using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Stub implementation of IStudentAssessmentRepository for development without database
/// </summary>
public class StubStudentAssessmentRepository : IStudentAssessmentRepository
{
    public Task<Result<StudentAssessment>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<StudentAssessment>(
            Error.NotFound("StudentAssessment", id)));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByAssessmentIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<StudentAssessment>> GetByStudentAndAssessmentAsync(
        Guid studentId,
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<StudentAssessment>(
            Error.NotFound("StudentAssessment", $"{studentId}/{assessmentId}")));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetInProgressByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetCompletedByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByStatusAsync(
        AssessmentStatus status,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByClassIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<double>> GetAverageScoreAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(0.0));
    }

    public Task<Result<double>> GetPassRateAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(0.0));
    }

    public Task<Result<StudentAssessment>> AddAsync(
        StudentAssessment entity,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<StudentAssessment>(
            Error.Validation("Stub repository does not support write operations")));
    }

    public Task<Result<StudentAssessment>> UpdateAsync(
        StudentAssessment entity,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<StudentAssessment>(
            Error.Validation("Stub repository does not support write operations")));
    }

    public Task<Result<Unit>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>(
            Error.Validation("Stub repository does not support write operations")));
    }

    public Task<Result<IReadOnlyList<StudentAssessment>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<StudentAssessment>>(
            Array.Empty<StudentAssessment>()));
    }

    public Task<Result<bool>> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(false));
    }

    public Task<Result<int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(0));
    }
}
