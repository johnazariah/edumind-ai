using System.Security.Claims;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AcademicAssessment.Infrastructure.Middleware;

/// <summary>
/// Middleware to extract tenant context from authentication claims
/// Sets up ITenantContext for dependency injection in downstream components
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // If already populated (e.g., by tests), skip
        if (tenantContext.UserId != Guid.Empty)
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            PopulateTenantContext(user, tenantContext);
        }

        await _next(context);
    }

    private static void PopulateTenantContext(ClaimsPrincipal user, ITenantContext tenantContext)
    {
        // Extract claims
        var userId = GetGuidClaim(user, "sub") ?? GetGuidClaim(user, ClaimTypes.NameIdentifier);
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var fullName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var roleStr = user.FindFirst(ClaimTypes.Role)?.Value;
        var schoolIdStr = user.FindFirst("school_id")?.Value;
        var classIdsStr = user.FindAll("class_id").Select(c => c.Value).ToList();

        // Parse role
        var role = Enum.TryParse<UserRole>(roleStr, true, out var parsedRole)
            ? parsedRole
            : UserRole.Student;

        // Parse school ID
        var schoolId = Guid.TryParse(schoolIdStr, out var parsedSchoolId)
            ? parsedSchoolId
            : (Guid?)null;

        // Parse class IDs
        var classIds = classIdsStr
            .Select(id => Guid.TryParse(id, out var parsed) ? (Guid?)parsed : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList()
            .AsReadOnly();

        // Update tenant context (assuming mutable implementation for middleware)
        if (tenantContext is Context.TenantContext mutableContext)
        {
            // Use reflection or make TenantContext mutable for middleware
            // For now, we'll need to use a scoped service instead
        }
    }

    private static Guid? GetGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}

/// <summary>
/// Scoped service that holds tenant context for the current request
/// </summary>
public sealed class ScopedTenantContext : ITenantContext
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public Guid? SchoolId { get; set; }
    public IReadOnlyList<Guid> ClassIds { get; set; } = [];
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public bool HasAccessToSchool(Guid schoolId) =>
        Role >= UserRole.SystemAdmin || SchoolId == schoolId;

    public bool HasAccessToClass(Guid classId) =>
        Role >= UserRole.SystemAdmin || ClassIds.Contains(classId);

    public bool HasRole(UserRole minimumRole) =>
        Role >= minimumRole;
}

/// <summary>
/// Extension methods for registering tenant context middleware
/// </summary>
public static class TenantContextMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder) =>
        builder.UseMiddleware<TenantContextMiddleware>();
}
