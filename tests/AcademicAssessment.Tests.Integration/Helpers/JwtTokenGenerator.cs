using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcademicAssessment.Core.Enums;
using Microsoft.IdentityModel.Tokens;

namespace AcademicAssessment.Tests.Integration.Helpers;

/// <summary>
/// Helper class to generate test JWT tokens for integration testing
/// Simulates Azure AD B2C token structure
/// </summary>
public static class JwtTokenGenerator
{
    private const string TestIssuer = "https://edumindai.b2clogin.com/test-tenant-id/v2.0/";
    private const string TestAudience = "test-client-id";
    private const string TestSecret = "test-secret-key-that-is-at-least-32-characters-long-for-hs256";

    /// <summary>
    /// Generates a test JWT token with specified claims
    /// </summary>
    public static string GenerateToken(
        Guid userId,
        string email,
        string fullName,
        UserRole role,
        Guid? schoolId = null,
        IEnumerable<Guid>? classIds = null,
        int expirationMinutes = 60)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new("oid", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Role, role.ToString()),
            new("role", role.ToString())
        };

        // Add school ID if provided
        if (schoolId.HasValue)
        {
            claims.Add(new Claim("extension_SchoolId", schoolId.Value.ToString()));
            claims.Add(new Claim("schoolId", schoolId.Value.ToString()));
        }

        // Add class IDs if provided
        if (classIds != null && classIds.Any())
        {
            var classIdsString = string.Join(",", classIds.Select(c => c.ToString()));
            claims.Add(new Claim("extension_ClassIds", classIdsString));
            claims.Add(new Claim("classIds", classIdsString));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        // For expired tokens, set both notBefore and expires in the past
        var notBefore = expirationMinutes < 0 ? now.AddMinutes(expirationMinutes - 5) : now;
        var expires = now.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a token for a student user
    /// </summary>
    public static string GenerateStudentToken(
        Guid studentId,
        string email = "student@test.com",
        string name = "Test Student",
        Guid? schoolId = null,
        IEnumerable<Guid>? classIds = null)
    {
        return GenerateToken(studentId, email, name, UserRole.Student, schoolId, classIds);
    }

    /// <summary>
    /// Generates a token for a teacher user
    /// </summary>
    public static string GenerateTeacherToken(
        Guid teacherId,
        Guid schoolId,
        string email = "teacher@test.com",
        string name = "Test Teacher",
        IEnumerable<Guid>? classIds = null)
    {
        return GenerateToken(teacherId, email, name, UserRole.Teacher, schoolId, classIds);
    }

    /// <summary>
    /// Generates a token for a school admin user
    /// </summary>
    public static string GenerateSchoolAdminToken(
        Guid adminId,
        Guid schoolId,
        string email = "admin@test.com",
        string name = "Test Admin")
    {
        return GenerateToken(adminId, email, name, UserRole.SchoolAdmin, schoolId);
    }

    /// <summary>
    /// Generates a token for a system admin user
    /// </summary>
    public static string GenerateSystemAdminToken(
        Guid adminId,
        string email = "sysadmin@test.com",
        string name = "System Admin")
    {
        return GenerateToken(adminId, email, name, UserRole.SystemAdmin);
    }

    /// <summary>
    /// Generates a token for a course admin user
    /// </summary>
    public static string GenerateCourseAdminToken(
        Guid adminId,
        string email = "courseadmin@test.com",
        string name = "Course Admin",
        IEnumerable<Guid>? classIds = null)
    {
        return GenerateToken(adminId, email, name, UserRole.CourseAdmin, classIds: classIds);
    }

    /// <summary>
    /// Generates a token for a business admin user
    /// </summary>
    public static string GenerateBusinessAdminToken(
        Guid adminId,
        string email = "bizadmin@test.com",
        string name = "Business Admin")
    {
        return GenerateToken(adminId, email, name, UserRole.BusinessAdmin);
    }

    /// <summary>
    /// Generates an expired token for testing
    /// </summary>
    public static string GenerateExpiredToken(Guid userId)
    {
        return GenerateToken(
            userId,
            "expired@test.com",
            "Expired User",
            UserRole.Student,
            expirationMinutes: -10);
    }

    /// <summary>
    /// Gets the test issuer for configuring test authentication
    /// </summary>
    public static string GetTestIssuer() => TestIssuer;

    /// <summary>
    /// Gets the test audience for configuring test authentication
    /// </summary>
    public static string GetTestAudience() => TestAudience;

    /// <summary>
    /// Gets the test secret for configuring test authentication
    /// </summary>
    public static string GetTestSecret() => TestSecret;
}
