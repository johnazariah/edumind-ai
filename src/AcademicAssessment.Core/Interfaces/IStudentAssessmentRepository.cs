using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for student assessment attempts with tenant filtering
/// </summary>
public interface IStudentAssessmentRepository : IRepository<StudentAssessment, Guid>
{
    /// <summary>
    /// Gets all assessment attempts for a specific student
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all attempts for a specific assessment
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetByAssessmentIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a student's attempt for a specific assessment
    /// </summary>
    Task<Result<StudentAssessment>> GetByStudentAndAssessmentAsync(
        Guid studentId,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all in-progress assessments for a student
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetInProgressByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all completed assessments for a student
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetCompletedByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessment attempts by status
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetByStatusAsync(
        AssessmentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all assessment attempts for a class
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetByClassIdAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessment attempts within a date range
    /// </summary>
    Task<Result<IReadOnlyList<StudentAssessment>>> GetByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average score for an assessment (privacy-preserving - min 5 students)
    /// </summary>
    Task<Result<double>> GetAverageScoreAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pass rate for an assessment (privacy-preserving - min 5 students)
    /// </summary>
    Task<Result<double>> GetPassRateAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}
