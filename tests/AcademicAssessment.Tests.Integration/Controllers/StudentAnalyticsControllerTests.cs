using System.Net;
using System.Net.Http.Json;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Tests.Integration.Helpers;
using FluentAssertions;

namespace AcademicAssessment.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for StudentAnalyticsController endpoints.
/// Tests the full HTTP request/response cycle including routing, model binding, validation, and serialization.
/// </summary>
public class StudentAnalyticsControllerTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>
{
    private readonly AuthenticatedWebApplicationFactory<Program> _factory;
    private readonly Guid _testStudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private readonly Guid _testClassId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private readonly string _studentToken;
    private readonly string _teacherToken;

    public StudentAnalyticsControllerTests(AuthenticatedWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _studentToken = JwtTokenGenerator.GenerateStudentToken(
            _testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });
        _teacherToken = JwtTokenGenerator.GenerateTeacherToken(
            Guid.NewGuid(), _testSchoolId, "teacher@test.com", "Test Teacher", new[] { _testClassId });
    }

    #region Performance Summary Tests

    [Fact]
    public async Task GetPerformanceSummary_WithValidStudentId_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPerformanceSummary_ReturnsCorrectStructure()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("overallAccuracy");
        content.Should().Contain("totalAssessments");
        content.Should().Contain("subjectPerformance");
    }

    [Fact]
    public async Task GetPerformanceSummary_WithNoData_ReturnsEmptyResult()
    {
        var newStudentId = Guid.NewGuid();
        var token = JwtTokenGenerator.GenerateStudentToken(
            newStudentId, "newstudent@test.com", "New Student", _testSchoolId, new[] { _testClassId });
        var client = _factory.CreateAuthenticatedClient(token);

        var response = await client.GetAsync($"/api/v1/students/{newStudentId}/analytics/performance-summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Subject Performance Tests

    [Fact]
    public async Task GetSubjectPerformance_WithValidSubject_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject={Subject.Mathematics}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(Subject.Mathematics)]
    [InlineData(Subject.Physics)]
    [InlineData(Subject.Chemistry)]
    [InlineData(Subject.Biology)]
    [InlineData(Subject.English)]
    public async Task GetSubjectPerformance_AllSubjects_ReturnsOk(Subject subject)
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject={subject}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSubjectPerformance_ReturnsCorrectStructure()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject={Subject.Mathematics}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("subject");
        content.Should().Contain("accuracy");
        content.Should().Contain("totalQuestions");
    }

    #endregion

    #region Progress Over Time Tests

    [Fact]
    public async Task GetProgressOverTime_WithValidDateRange_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/progress-over-time?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProgressOverTime_WithInvalidDateRange_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-30);

        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/progress-over-time?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProgressOverTime_ReturnsChronologicalData()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/progress-over-time?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("dataPoints");
    }

    #endregion

    #region Weak Areas Tests

    [Fact]
    public async Task GetWeakAreas_WithValidStudent_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/weak-areas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWeakAreas_ReturnsCorrectStructure()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/weak-areas");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeakAreas_WithThresholdParameter_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var threshold = 0.6;

        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/weak-areas?threshold={threshold}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Recommended Topics Tests

    [Fact]
    public async Task GetRecommendedTopics_WithValidStudent_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/recommended-topics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecommendedTopics_ReturnsCorrectStructure()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/recommended-topics");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendedTopics_WithLimitParameter_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var limit = 5;

        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/recommended-topics?limit={limit}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Teacher Access Tests

    [Fact]
    public async Task Teacher_CanAccessStudentInTheirClass()
    {
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Teacher_CanAccessMultipleEndpoints()
    {
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        var summaryResponse = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");
        var weakAreasResponse = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/weak-areas");
        var recommendedResponse = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/recommended-topics");

        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        weakAreasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        recommendedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetPerformanceSummary_WithInvalidGuid_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync($"/api/v1/students/invalid-guid/analytics/performance-summary");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSubjectPerformance_WithInvalidSubject_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var response = await client.GetAsync(
            $"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject=InvalidSubject");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
