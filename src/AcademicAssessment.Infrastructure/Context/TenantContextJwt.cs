using System.Security.Claims;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AcademicAssessment.Infrastructure.Context;

/// <summary>
/// Production tenant context implementation that extracts tenant information from JWT claims
/// Implements row-level security by providing authenticated user and tenant context
/// </summary>
public sealed class TenantContextJwt : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? _user;

    public TenantContextJwt(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _user ??= _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? User?.FindFirst("sub")
                ?? User?.FindFirst("oid");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new InvalidOperationException("User ID claim not found or invalid in JWT token");
            }

            return userId;
        }
    }

    public string Email
    {
        get
        {
            var emailClaim = User?.FindFirst(ClaimTypes.Email)
                ?? User?.FindFirst("email")
                ?? User?.FindFirst("preferred_username");

            return emailClaim?.Value
                ?? throw new InvalidOperationException("Email claim not found in JWT token");
        }
    }

    public string FullName
    {
        get
        {
            var nameClaim = User?.FindFirst(ClaimTypes.Name)
                ?? User?.FindFirst("name");

            return nameClaim?.Value ?? Email; // Fallback to email if name not provided
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role)
                ?? User?.FindFirst("role")
                ?? User?.FindFirst("roles");

            if (roleClaim == null)
            {
                throw new InvalidOperationException("Role claim not found in JWT token");
            }

            return Enum.TryParse<UserRole>(roleClaim.Value, true, out var role)
                ? role
                : throw new InvalidOperationException($"Invalid role value in JWT token: {roleClaim.Value}");
        }
    }

    public Guid? SchoolId
    {
        get
        {
            var schoolIdClaim = User?.FindFirst("schoolId")
                ?? User?.FindFirst("school_id")
                ?? User?.FindFirst("extension_SchoolId");

            if (schoolIdClaim == null || string.IsNullOrWhiteSpace(schoolIdClaim.Value))
            {
                return null; // SystemAdmin and BusinessAdmin may not have a school
            }

            return Guid.TryParse(schoolIdClaim.Value, out var schoolId)
                ? schoolId
                : throw new InvalidOperationException($"Invalid school ID in JWT token: {schoolIdClaim.Value}");
        }
    }

    public IReadOnlyList<Guid> ClassIds
    {
        get
        {
            var classIdsClaim = User?.FindFirst("classIds")
                ?? User?.FindFirst("class_ids")
                ?? User?.FindFirst("extension_ClassIds");

            if (classIdsClaim == null || string.IsNullOrWhiteSpace(classIdsClaim.Value))
            {
                return Array.Empty<Guid>();
            }

            // Parse comma-separated GUIDs
            try
            {
                return classIdsClaim.Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Guid.Parse)
                    .ToList()
                    .AsReadOnly();
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Invalid class IDs format in JWT token: {classIdsClaim.Value}", ex);
            }
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasRole(UserRole minimumRole)
    {
        // Role hierarchy: SystemAdmin > BusinessAdmin > SchoolAdmin > CourseAdmin > Teacher > Student
        var roleHierarchy = new Dictionary<UserRole, int>
        {
            { UserRole.Student, 1 },
            { UserRole.Teacher, 2 },
            { UserRole.CourseAdmin, 3 },
            { UserRole.SchoolAdmin, 4 },
            { UserRole.BusinessAdmin, 5 },
            { UserRole.SystemAdmin, 6 }
        };

        return roleHierarchy.TryGetValue(Role, out var userLevel)
            && roleHierarchy.TryGetValue(minimumRole, out var requiredLevel)
            && userLevel >= requiredLevel;
    }

    public bool HasAccessToSchool(Guid schoolId)
    {
        return Role switch
        {
            UserRole.SystemAdmin => true,
            UserRole.BusinessAdmin => true,
            UserRole.SchoolAdmin => SchoolId == schoolId,
            UserRole.Teacher => SchoolId == schoolId,
            UserRole.Student => SchoolId == schoolId,
            UserRole.CourseAdmin => true, // Can see all schools they administer
            _ => false
        };
    }

    public bool HasAccessToClass(Guid classId)
    {
        return Role switch
        {
            UserRole.SystemAdmin => true,
            UserRole.BusinessAdmin => true,
            UserRole.SchoolAdmin => true, // Can see all classes in their school
            UserRole.CourseAdmin => true, // Can see all classes they administer
            UserRole.Teacher => ClassIds.Contains(classId),
            UserRole.Student => ClassIds.Contains(classId),
            _ => false
        };
    }

    public bool CanAccessStudent(Guid studentId)
    {
        return Role switch
        {
            UserRole.SystemAdmin => true,
            UserRole.BusinessAdmin => true,
            UserRole.Student => UserId == studentId, // Students can only see their own data
            UserRole.Teacher => true, // Teachers can see their students (filtered by SchoolId in queries)
            UserRole.SchoolAdmin => true, // School admins can see all students in their school
            UserRole.CourseAdmin => true, // Course admins can see students in their courses
            _ => false
        };
    }

    public IEnumerable<string> GetAllClaims()
    {
        return User?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? Enumerable.Empty<string>();
    }
}
