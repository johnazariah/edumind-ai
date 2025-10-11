using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a class (group of students at the same grade level)
/// </summary>
public record Class
{
    /// <summary>
    /// Unique identifier for the class
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// School that owns this class
    /// </summary>
    public required Guid SchoolId { get; init; }

    /// <summary>
    /// Name of the class (e.g., "8th Grade Math - Section A")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short code for the class (e.g., "8M-A")
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Grade level for this class
    /// </summary>
    public required GradeLevel GradeLevel { get; init; }

    /// <summary>
    /// Subject being taught in this class
    /// </summary>
    public required Subject Subject { get; init; }

    /// <summary>
    /// Teacher IDs assigned to this class
    /// </summary>
    public IReadOnlyList<Guid> TeacherIds { get; init; } = [];

    /// <summary>
    /// Student IDs enrolled in this class
    /// </summary>
    public IReadOnlyList<Guid> StudentIds { get; init; } = [];

    /// <summary>
    /// Academic year (e.g., "2024-2025")
    /// </summary>
    public required string AcademicYear { get; init; }

    /// <summary>
    /// Whether the class is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// When the class was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the class was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Number of students enrolled in the class
    /// </summary>
    public int EnrollmentCount => StudentIds.Count;

    /// <summary>
    /// Whether the class has enough students for aggregate reporting (min 5 for k-anonymity)
    /// </summary>
    public bool SupportsAggregateReporting => EnrollmentCount >= 5;

    /// <summary>
    /// Creates a new class with updated properties
    /// </summary>
    public Class With(
        string? name = null,
        IReadOnlyList<Guid>? teacherIds = null,
        IReadOnlyList<Guid>? studentIds = null,
        bool? isActive = null) =>
        this with
        {
            Name = name ?? Name,
            TeacherIds = teacherIds ?? TeacherIds,
            StudentIds = studentIds ?? StudentIds,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Adds a student to the class
    /// </summary>
    public Class AddStudent(Guid studentId) =>
        StudentIds.Contains(studentId)
            ? this
            : this with
            {
                StudentIds = StudentIds.Append(studentId).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Removes a student from the class
    /// </summary>
    public Class RemoveStudent(Guid studentId) =>
        this with
        {
            StudentIds = StudentIds.Where(id => id != studentId).ToList().AsReadOnly(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Adds a teacher to the class
    /// </summary>
    public Class AddTeacher(Guid teacherId) =>
        TeacherIds.Contains(teacherId)
            ? this
            : this with
            {
                TeacherIds = TeacherIds.Append(teacherId).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Removes a teacher from the class
    /// </summary>
    public Class RemoveTeacher(Guid teacherId) =>
        this with
        {
            TeacherIds = TeacherIds.Where(id => id != teacherId).ToList().AsReadOnly(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
