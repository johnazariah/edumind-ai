using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Provides contextual information about the current tenant (user and their access scope)
/// Extracted from authentication claims and used for row-level security
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current authenticated user ID
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Current user's role in the system
    /// </summary>
    UserRole Role { get; }

    /// <summary>
    /// School ID for school-based users (null for self-service or system admins)
    /// Used for physical database selection and row-level security
    /// </summary>
    Guid? SchoolId { get; }

    /// <summary>
    /// Class IDs the user has access to (for teachers and students)
    /// </summary>
    IReadOnlyList<Guid> ClassIds { get; }

    /// <summary>
    /// Whether this is a self-service (B2C) user
    /// </summary>
    bool IsSelfService => !SchoolId.HasValue;

    /// <summary>
    /// Whether this is a school-based (B2B) user
    /// </summary>
    bool IsSchoolBased => SchoolId.HasValue;

    /// <summary>
    /// Email of the current user
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Full name of the current user
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Checks if the user has access to a specific school
    /// </summary>
    bool HasAccessToSchool(Guid schoolId);

    /// <summary>
    /// Checks if the user has access to a specific class
    /// </summary>
    bool HasAccessToClass(Guid classId);

    /// <summary>
    /// Checks if the user has at least the specified role level
    /// </summary>
    bool HasRole(UserRole minimumRole);
}
