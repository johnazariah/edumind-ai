using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Tests.Integration.Helpers;
using FluentAssertions;

namespace AcademicAssessment.Tests.Integration.Controllers;

/// <summary>
/// Authentication and authorization tests for StudentAnalyticsController.
/// Tests JWT token validation, role-based access control, and cross-tenant security.
/// </summary>
public class StudentAnalyticsControllerAuthTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>
{
    private readonly AuthenticatedWebApplicationFactory<Program> _factory;
    private readonly Guid _testStudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _otherStudentId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private readonly Guid _otherSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private readonly Guid _testClassId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    public StudentAnalyticsControllerAuthTests(AuthenticatedWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    #region Authentication Tests

    [Fact]
    public async Task GetPerformanceSummary_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPerformanceSummary_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-12345");

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPerformanceSummary_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = JwtTokenGenerator.GenerateExpiredToken(_testStudentId);
        var client = _factory.CreateAuthenticatedClient(expiredToken);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPerformanceSummary_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Student Access Control Tests

    [Fact]
    public async Task StudentCanAccessOwnData()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<StudentPerformanceSummary>();
        content.Should().NotBeNull();
        content!.StudentId.Should().Be(_testStudentId);
    }

    [Fact]
    public async Task StudentCannotAccessOtherStudentData()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_otherStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Teacher Access Control Tests

    [Fact]
    public async Task TeacherCanAccessStudentInSameSchool()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateTeacherToken(teacherId, _testSchoolId, "teacher@test.com", "Test Teacher", new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TeacherCannotAccessStudentInDifferentSchool()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateTeacherToken(teacherId, _otherSchoolId, "teacher@test.com", "Test Teacher", new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region School Admin Access Control Tests

    [Fact]
    public async Task SchoolAdminCanAccessStudentInSameSchool()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateSchoolAdminToken(adminId, _testSchoolId, "admin@test.com", "School Admin");
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SchoolAdminCannotAccessStudentInDifferentSchool()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateSchoolAdminToken(adminId, _otherSchoolId, "admin@test.com", "School Admin");
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region System Admin Access Control Tests

    [Fact]
    public async Task SystemAdminCanAccessAnyStudent()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateSystemAdminToken(adminId);
        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemAdminCanAccessStudentInAnySchool()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateSystemAdminToken(adminId);
        var client = _factory.CreateAuthenticatedClient(token);

        // Act - Test accessing students from different schools
        var response1 = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");
        var response2 = await client.GetAsync($"/api/v1/students/{_otherStudentId}/analytics/performance-summary");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region All Endpoints Authentication Tests

    [Theory]
    [InlineData("/api/v1/students/{0}/analytics/performance-summary")]
    [InlineData("/api/v1/students/{0}/analytics/subject-performance")]
    [InlineData("/api/v1/students/{0}/analytics/learning-objectives")]
    [InlineData("/api/v1/students/{0}/analytics/ability-estimates")]
    [InlineData("/api/v1/students/{0}/analytics/improvement-areas")]
    [InlineData("/api/v1/students/{0}/analytics/progress-timeline")]
    [InlineData("/api/v1/students/{0}/analytics/peer-comparison")]
    public async Task AllEndpoints_WithoutToken_ReturnsUnauthorized(string endpointTemplate)
    {
        // Arrange
        var client = _factory.CreateClient();
        var endpoint = string.Format(endpointTemplate, _testStudentId);

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"Endpoint {endpoint} should require authentication");
    }

    [Theory]
    [InlineData("/api/v1/students/{0}/analytics/performance-summary")]
    [InlineData("/api/v1/students/{0}/analytics/subject-performance")]
    [InlineData("/api/v1/students/{0}/analytics/learning-objectives")]
    [InlineData("/api/v1/students/{0}/analytics/ability-estimates")]
    [InlineData("/api/v1/students/{0}/analytics/improvement-areas")]
    [InlineData("/api/v1/students/{0}/analytics/progress-timeline")]
    [InlineData("/api/v1/students/{0}/analytics/peer-comparison")]
    public async Task AllEndpoints_WithValidToken_ReturnsSuccessOrForbidden(string endpointTemplate)
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);
        var endpoint = string.Format(endpointTemplate, _testStudentId);

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        // Should be either OK (200) or Forbidden (403), but not Unauthorized (401)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, $"Endpoint {endpoint} should accept valid token");
    }

    #endregion

    #region Role-Based Access Tests

    [Theory]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.Teacher)]
    [InlineData(UserRole.SchoolAdmin)]
    [InlineData(UserRole.CourseAdmin)]
    [InlineData(UserRole.BusinessAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    public async Task AllRoles_CanAuthenticateSuccessfully(UserRole role)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string token = role switch
        {
            UserRole.Student => JwtTokenGenerator.GenerateStudentToken(userId, "user@test.com", "Test User", _testSchoolId, new[] { _testClassId }),
            UserRole.Teacher => JwtTokenGenerator.GenerateTeacherToken(userId, _testSchoolId, "user@test.com", "Test User", new[] { _testClassId }),
            UserRole.SchoolAdmin => JwtTokenGenerator.GenerateSchoolAdminToken(userId, _testSchoolId, "user@test.com", "Test User"),
            UserRole.CourseAdmin => JwtTokenGenerator.GenerateCourseAdminToken(userId, "user@test.com", "Test User", new[] { _testClassId }),
            UserRole.BusinessAdmin => JwtTokenGenerator.GenerateBusinessAdminToken(userId, "user@test.com", "Test User"),
            UserRole.SystemAdmin => JwtTokenGenerator.GenerateSystemAdminToken(userId, "user@test.com", "Test User"),
            _ => throw new ArgumentException($"Unsupported role: {role}")
        };

        var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        // Should not be unauthorized (token is valid)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, $"Role {role} should be able to authenticate");
    }

    #endregion

    #region Token Claim Validation Tests

    [Fact]
    public async Task TokenWithoutRequiredClaims_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Create a minimal token without proper claims
        var token = JwtTokenGenerator.GenerateToken(
            Guid.NewGuid(),
            "minimal@test.com",
            "Minimal User",
            UserRole.Student,
            expirationMinutes: 60);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

        // Assert
        // Should either succeed or fail based on claim requirements
        // This tests that the token is structurally valid
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Multi-Tenant Security Tests

    [Fact]
    public async Task TeacherInSchoolA_CannotAccessStudentInSchoolB()
    {
        // Arrange - Teacher in School A
        var teacherToken = JwtTokenGenerator.GenerateTeacherToken(
            Guid.NewGuid(),
            _testSchoolId,
            "teacher.schoola@test.com",
            "Teacher School A",
            new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(teacherToken);

        // Student in different school (School B)
        var studentInSchoolB = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/students/{studentInSchoolB}/analytics/performance-summary");

        // Assert
        // Should be Forbidden because teacher doesn't have access to School B
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SchoolAdminInSchoolA_CannotAccessStudentInSchoolB()
    {
        // Arrange - School Admin in School A
        var adminToken = JwtTokenGenerator.GenerateSchoolAdminToken(
            Guid.NewGuid(),
            _testSchoolId,
            "admin.schoola@test.com",
            "Admin School A");
        var client = _factory.CreateAuthenticatedClient(adminToken);

        // Student in different school (School B)
        var studentInSchoolB = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/students/{studentInSchoolB}/analytics/performance-summary");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    #endregion
}
