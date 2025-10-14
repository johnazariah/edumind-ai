using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Development-only implementation of ITenantContext
/// Provides a default tenant context for testing without authentication
/// </summary>
public class TenantContextDevelopment : ITenantContext
{
    /// <summary>
    /// Default development user ID
    /// </summary>
    public Guid UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Default role for development (SystemAdmin for unrestricted access)
    /// </summary>
    public UserRole Role => UserRole.SystemAdmin;

    /// <summary>
    /// No school restriction in development
    /// </summary>
    public Guid? SchoolId => null;

    /// <summary>
    /// No class restrictions in development
    /// </summary>
    public IReadOnlyList<Guid> ClassIds => Array.Empty<Guid>();

    /// <summary>
    /// Development email
    /// </summary>
    public string Email => "dev@edumind.ai";

    /// <summary>
    /// Development user full name
    /// </summary>
    public string FullName => "Development User";

    /// <summary>
    /// System admin has access to all schools
    /// </summary>
    public bool HasAccessToSchool(Guid schoolId) => true;

    /// <summary>
    /// System admin has access to all classes
    /// </summary>
    public bool HasAccessToClass(Guid classId) => true;

    /// <summary>
    /// System admin has all roles
    /// </summary>
    public bool HasRole(UserRole minimumRole) => true;
}
