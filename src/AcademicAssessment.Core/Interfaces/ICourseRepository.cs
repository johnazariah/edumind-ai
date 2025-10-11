using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for course entities
/// </summary>
public interface ICourseRepository : IRepository<Course, Guid>
{
    /// <summary>
    /// Gets a course by its code
    /// </summary>
    Task<Result<Course>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all courses for a specific subject
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetBySubjectAsync(
        Subject subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all courses for a specific grade level
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetByGradeLevelAsync(
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets courses by subject and grade level
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active courses
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetActiveCoursesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets courses managed by a specific course administrator
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> GetByCourseAdminAsync(
        Guid courseAdminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches courses by topic
    /// </summary>
    Task<Result<IReadOnlyList<Course>>> SearchByTopicAsync(
        string topic,
        CancellationToken cancellationToken = default);
}
