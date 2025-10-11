using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for class entities with tenant filtering
/// </summary>
public interface IClassRepository : IRepository<Class, Guid>
{
    /// <summary>
    /// Gets all classes for a specific school
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all classes taught by a specific teacher
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetByTeacherIdAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all classes a student is enrolled in
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets classes by subject and grade level
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets classes by academic year
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetByAcademicYearAsync(
        string academicYear,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a class by its code within a school
    /// </summary>
    Task<Result<Class>> GetBySchoolAndCodeAsync(
        Guid schoolId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets classes that support aggregate reporting (min 5 students)
    /// </summary>
    Task<Result<IReadOnlyList<Class>>> GetClassesWithAggregateReportingAsync(
        CancellationToken cancellationToken = default);
}
