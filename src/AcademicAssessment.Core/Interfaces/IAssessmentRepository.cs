using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for assessment entities with tenant filtering
/// </summary>
public interface IAssessmentRepository : IRepository<Assessment, Guid>
{
    /// <summary>
    /// Gets all assessments for a specific course
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessments by type
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetByTypeAsync(
        AssessmentType assessmentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessments by subject and grade level
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets school-specific assessments
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global assessments (available to all schools)
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetGlobalAssessmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets adaptive assessments
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetAdaptiveAssessmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessments covering specific topics
    /// </summary>
    Task<Result<IReadOnlyList<Assessment>>> GetByTopicsAsync(
        IReadOnlyList<string> topics,
        CancellationToken cancellationToken = default);
}
