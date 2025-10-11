using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;

namespace AcademicAssessment.Infrastructure.Context;

/// <summary>
/// Implementation of tenant context that holds current user and tenant information
/// Populated by middleware from authentication claims
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid UserId { get; init; }
    public UserRole Role { get; init; }
    public Guid? SchoolId { get; init; }
    public IReadOnlyList<Guid> ClassIds { get; init; } = [];
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Checks if the user has access to a specific school
    /// </summary>
    public bool HasAccessToSchool(Guid schoolId) =>
        Role >= UserRole.SystemAdmin || SchoolId == schoolId;

    /// <summary>
    /// Checks if the user has access to a specific class
    /// </summary>
    public bool HasAccessToClass(Guid classId) =>
        Role >= UserRole.SystemAdmin || ClassIds.Contains(classId);

    /// <summary>
    /// Checks if the user has at least the specified role level
    /// </summary>
    public bool HasRole(UserRole minimumRole) =>
        Role >= minimumRole;

    /// <summary>
    /// Creates a system admin context (for background operations)
    /// </summary>
    public static TenantContext CreateSystemContext() =>
        new()
        {
            UserId = Guid.Empty,
            Role = UserRole.SystemAdmin,
            SchoolId = null,
            ClassIds = [],
            Email = "system@edumind.ai",
            FullName = "System"
        };
}
