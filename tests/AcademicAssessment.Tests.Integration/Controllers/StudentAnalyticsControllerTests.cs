using System.Net;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Json;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Tests.Integration.Helpers;
using AcademicAssessment.Tests.Integration.Helpers;
using FluentAssertions;
using FluentAssertions;

namespace AcademicAssessment.Tests.Integration.Controllers;

using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.Extensions.DependencyInjection;

/// <summary>using Moq;

/// Functional integration tests for StudentAnalyticsController endpoints.

/// Tests the full HTTP request/response cycle including routing, model binding, validation, and serialization with JWT authentication.namespace AcademicAssessment.Tests.Integration.Controllers;

/// </summary>

public class StudentAnalyticsControllerTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>/// <summary>

{/// Integration tests for StudentAnalyticsController endpoints.

    private readonly AuthenticatedWebApplicationFactory<Program> _factory;/// Tests the full HTTP request/response cycle including routing, model binding, validation, serialization, and JWT authentication.

    private readonly Guid _testStudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");/// </summary>

    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010"); public class StudentAnalyticsControllerTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>

    private readonly Guid _testClassId = Guid.Parse("00000000-0000-0000-0000-000000000020");{

    private readonly string _studentToken; private readonly AuthenticatedWebApplicationFactory<Program> _factory;

    private readonly Guid _testStudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public StudentAnalyticsControllerTests(AuthenticatedWebApplicationFactory<Program> factory)    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    {    private readonly Guid _testClassId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    _factory = factory;    private readonly string _studentToken;

    private readonly string _teacherToken;

    // Pre-generate token for common test scenarios    private readonly string _systemAdminToken;

    _studentToken = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId
});

    }    public StudentAnalyticsControllerTests(AuthenticatedWebApplicationFactory<Program> factory)

{

    #region Performance Summary Tests        _factory = factory;



    [Fact]        // Pre-generate tokens for common test scenarios

    public async Task GetPerformanceSummary_WithValidStudentId_ReturnsOk()        _studentToken = JwtTokenGenerator.GenerateStudentToken(_testStudentId, "student@test.com", "Test Student", _testSchoolId, new[] { _testClassId });

    {
        _teacherToken = JwtTokenGenerator.GenerateTeacherToken(Guid.NewGuid(), _testSchoolId, "teacher@test.com", "Test Teacher", new[] { _testClassId });

        // Arrange        _systemAdminToken = JwtTokenGenerator.GenerateSystemAdminToken(Guid.NewGuid());

        var client = _factory.CreateAuthenticatedClient(_studentToken);
    }



    // Act    #region Performance Summary Tests

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

    [Fact]

    // Assert    public async Task GetPerformanceSummary_WithValidStudentId_ReturnsOk()

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    {

        var content = await response.Content.ReadFromJsonAsync<StudentPerformanceSummary>();        // Arrange

        content.Should().NotBeNull(); var client = _factory.CreateAuthenticatedClient(_studentToken);

        content!.StudentId.Should().Be(_testStudentId);

    }        // Act

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

    [Fact]

    public async Task GetPerformanceSummary_WithInvalidStudentId_ReturnsNotFound()        // Assert

    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Arrange        var content = await response.Content.ReadFromJsonAsync<StudentPerformanceSummary>();

        var client = _factory.CreateAuthenticatedClient(_studentToken); content.Should().NotBeNull();

        content!.StudentId.Should().Be(_testStudentId);

        // Act    }

        var response = await client.GetAsync("/api/v1/students/invalid-guid/analytics/performance-summary");

        [Fact]

        // Assert    public async Task GetPerformanceSummary_WithInvalidStudentId_ReturnsNotFound()

        // ASP.NET Core routing returns 404 when GUID format is invalid (route doesn't match)    {

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);        // Act

    }
    var response = await _client.GetAsync("/api/v1/students/invalid-guid/analytics/performance-summary");



    [Fact]        // Assert

    public async Task GetPerformanceSummary_WithNonexistentStudentId_ReturnsForbiddenOrNotFound()        // ASP.NET Core routing returns 404 when GUID format is invalid (route doesn't match)

    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Arrange    }

        var nonexistentId = Guid.NewGuid();

        var client = _factory.CreateAuthenticatedClient(_studentToken); [Fact]

        public async Task GetPerformanceSummary_WithNonexistentStudentId_ReturnsOkWithEmptyData()

        // Act    {

        var response = await client.GetAsync($"/api/v1/students/{nonexistentId}/analytics/performance-summary");        // Arrange

        var nonexistentId = Guid.NewGuid();

        // Assert

        // Either forbidden (no access) or not found        // Act

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound); var response = await _client.GetAsync($"/api/v1/students/{nonexistentId}/analytics/performance-summary");

    }

    // Assert

    #endregion        // Stub repository returns empty data (not NotFound) - this is expected behavior in development

    // In production with real database, this would return NotFound

    #region Subject Performance Tests        response.StatusCode.Should().Be(HttpStatusCode.OK);

    var content = await response.Content.ReadFromJsonAsync<StudentPerformanceSummary>();

    [Fact] content.Should().NotBeNull();

    public async Task GetSubjectPerformance_WithoutSubjectFilter_ReturnsOk()        content!.StudentId.Should().Be(nonexistentId);

{ }

// Arrange

var client = _factory.CreateAuthenticatedClient(_studentToken);    #endregion



        // Act    #region Subject Performance Tests

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance");

[Fact]

// Assert    public async Task GetSubjectPerformance_WithoutSubjectFilter_ReturnsOk()

response.StatusCode.Should().Be(HttpStatusCode.OK);
{

    var content = await response.Content.ReadFromJsonAsync<SubjectPerformance>();        // Act

    content.Should().NotBeNull(); var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance");

}

// Assert

[Fact] response.StatusCode.Should().Be(HttpStatusCode.OK);

public async Task GetSubjectPerformance_WithValidSubject_ReturnsOk()        var content = await response.Content.ReadFromJsonAsync<SubjectPerformance>();

{
    content.Should().NotBeNull();

    // Arrange    }

    var client = _factory.CreateAuthenticatedClient(_studentToken);

    [Fact]

    // Act    public async Task GetSubjectPerformance_WithValidSubject_ReturnsOk()

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject=0");
    {

        // Act

        // Assert        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<SubjectPerformance>();        // Assert

        content.Should().NotBeNull(); response.StatusCode.Should().Be(HttpStatusCode.OK);

        content!.Subject.Should().Be(Subject.Mathematics); var content = await response.Content.ReadFromJsonAsync<SubjectPerformance>();

    }
    content.Should().NotBeNull();

    content!.Subject.Should().Be(Subject.Mathematics);

    [Fact]    }

public async Task GetSubjectPerformance_WithInvalidSubject_ReturnsBadRequest()

{
    [Fact]

    // Arrange    public async Task GetSubjectPerformance_WithInvalidSubject_ReturnsBadRequest()

    var client = _factory.CreateAuthenticatedClient(_studentToken);
    {

        // Act

        // Act        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject=999");

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/subject-performance?subject=999");

        // Assert

        // Assert        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

}

#endregion

#endregion

#region Learning Objectives Tests

#region Learning Objectives Tests

[Fact]

[Fact] public async Task GetLearningObjectiveMastery_WithoutSubjectFilter_ReturnsOk()

    public async Task GetLearningObjectiveMastery_WithoutSubjectFilter_ReturnsOk()
{

    {        // Act

        // Arrange        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/learning-objectives");

        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Assert

        // Act        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/learning-objectives"); var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<LearningObjectiveMastery>>();

        content.Should().NotBeNull();

        // Assert    }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<LearningObjectiveMastery>>(); [Fact]

        content.Should().NotBeNull();    public async Task GetLearningObjectiveMastery_WithValidSubject_ReturnsOk()

    }    {

    // Act

    [Fact] var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/learning-objectives?subject=1");

    public async Task GetLearningObjectiveMastery_WithValidSubject_ReturnsOk()

{        // Assert

    // Arrange        response.StatusCode.Should().Be(HttpStatusCode.OK);

    var client = _factory.CreateAuthenticatedClient(_studentToken); var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<LearningObjectiveMastery>>();

    content.Should().NotBeNull();

    // Act    }

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/learning-objectives?subject=1");

    #endregion

    // Assert

    response.StatusCode.Should().Be(HttpStatusCode.OK);    #region Ability Estimates Tests

        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<LearningObjectiveMastery>>();

    content.Should().NotBeNull(); [Fact]

    }
public async Task GetAbilityEstimates_WithValidStudentId_ReturnsOk()

{

    #endregion        // Act

    var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/ability-estimates");

    #region Ability Estimates Tests

    // Assert

    [Fact] response.StatusCode.Should().Be(HttpStatusCode.OK);

    public async Task GetAbilityEstimates_WithValidStudentId_ReturnsOk()        var content = await response.Content.ReadFromJsonAsync<Dictionary<Subject, double>>();

{
    content.Should().NotBeNull();

    // Arrange    }

    var client = _factory.CreateAuthenticatedClient(_studentToken);

    #endregion

    // Act

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/ability-estimates");    #region Improvement Areas Tests



        // Assert    [Fact]

        response.StatusCode.Should().Be(HttpStatusCode.OK);    public async Task GetImprovementAreas_WithDefaultTopN_ReturnsOk()

        var content = await response.Content.ReadFromJsonAsync<Dictionary<Subject, double>>();
{

    content.Should().NotBeNull();        // Act

}
var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas");



#endregion        // Assert

response.StatusCode.Should().Be(HttpStatusCode.OK);

#region Improvement Areas Tests        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<ImprovementArea>>();

content.Should().NotBeNull();

[Fact]    }

    public async Task GetImprovementAreas_WithDefaultTopN_ReturnsOk()

{
    [Fact]

    // Arrange    public async Task GetImprovementAreas_WithValidTopN_ReturnsOk()

    var client = _factory.CreateAuthenticatedClient(_studentToken);
    {

        // Act

        // Act        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas?topN=5");

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas");

        // Assert

        // Assert        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.StatusCode.Should().Be(HttpStatusCode.OK); var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<ImprovementArea>>();

        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<ImprovementArea>>(); content.Should().NotBeNull();

        content.Should().NotBeNull();
    }

}

[Theory]

[Fact][InlineData(0)]

public async Task GetImprovementAreas_WithValidTopN_ReturnsOk()    [InlineData(-1)]

{
    [InlineData(21)]

    // Arrange    [InlineData(100)]

    var client = _factory.CreateAuthenticatedClient(_studentToken);    public async Task GetImprovementAreas_WithInvalidTopN_ReturnsBadRequest(int topN)

{

    // Act        // Act

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas?topN=5"); var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas?topN={topN}");



    // Assert        // Assert

    response.StatusCode.Should().Be(HttpStatusCode.OK); response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<ImprovementArea>>(); var content = await response.Content.ReadAsStringAsync();

    content.Should().NotBeNull(); content.Should().Contain("topN must be between 1 and 20");

}    }



    [Theory]    #endregion

    [InlineData(0)]

[InlineData(-1)]    #region Progress Timeline Tests

    [InlineData(21)]

[InlineData(100)][Fact]

public async Task GetImprovementAreas_WithInvalidTopN_ReturnsBadRequest(int topN)    public async Task GetProgressTimeline_WithoutDateRange_ReturnsOk()

{
    {

        // Arrange        // Act

        var client = _factory.CreateAuthenticatedClient(_studentToken); var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/progress-timeline");



        // Act        // Assert

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/improvement-areas?topN={topN}"); response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ProgressTimeline>();

        // Assert        content.Should().NotBeNull();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest); content!.StudentId.Should().Be(_testStudentId);

        var content = await response.Content.ReadAsStringAsync();
    }

    content.Should().Contain("topN must be between 1 and 20");

}
[Fact]

public async Task GetProgressTimeline_WithValidDateRange_ReturnsOk()

    #endregion    {

// Arrange

#region Progress Timeline Tests        var startDate = DateTime.UtcNow.AddMonths(-3);

        var endDate = DateTime.UtcNow;

[Fact]

public async Task GetProgressTimeline_WithoutDateRange_ReturnsOk()        // Act

{
    var response = await _client.GetAsync(

    // Arrange            $"/api/v1/students/{_testStudentId}/analytics/progress-timeline?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    var client = _factory.CreateAuthenticatedClient(_studentToken);

    // Assert

    // Act        response.StatusCode.Should().Be(HttpStatusCode.OK);

    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/progress-timeline"); var content = await response.Content.ReadFromJsonAsync<ProgressTimeline>();

    content.Should().NotBeNull();

    // Assert    }

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var content = await response.Content.ReadFromJsonAsync<ProgressTimeline>(); [Fact]

    content.Should().NotBeNull();    public async Task GetProgressTimeline_WithInvalidDateRange_ReturnsBadRequest()

        content!.StudentId.Should().Be(_testStudentId);
{

}        // Arrange

var startDate = DateTime.UtcNow;

[Fact] var endDate = DateTime.UtcNow.AddMonths(-3);

public async Task GetProgressTimeline_WithValidDateRange_ReturnsOk()

{        // Act

    // Arrange        var response = await _client.GetAsync(

    var client = _factory.CreateAuthenticatedClient(_studentToken); $"/api/v1/students/{_testStudentId}/analytics/progress-timeline?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    var startDate = DateTime.UtcNow.AddMonths(-3);

    var endDate = DateTime.UtcNow;        // Assert

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    // Act        var content = await response.Content.ReadAsStringAsync();

    var response = await client.GetAsync(content.Should().Contain("startDate cannot be after endDate");

    $"/api/v1/students/{_testStudentId}/analytics/progress-timeline?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
}



// Assert    #endregion

response.StatusCode.Should().Be(HttpStatusCode.OK);

var content = await response.Content.ReadFromJsonAsync<ProgressTimeline>();    #region Peer Comparison Tests

        content.Should().NotBeNull();

    }    [Fact]

public async Task GetPeerComparison_WithoutFilters_ReturnsOk()

    [Fact]
{

    public async Task GetProgressTimeline_WithInvalidDateRange_ReturnsBadRequest()        // Act

{
    var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison");

    // Arrange

    var client = _factory.CreateAuthenticatedClient(_studentToken);        // Assert

    var startDate = DateTime.UtcNow; response.StatusCode.Should().Be(HttpStatusCode.OK);

    var endDate = DateTime.UtcNow.AddMonths(-3); var content = await response.Content.ReadFromJsonAsync<PeerComparison>();

    content.Should().NotBeNull();

    // Act        content!.StudentId.Should().Be(_testStudentId);

    var response = await client.GetAsync(    }

$"/api/v1/students/{_testStudentId}/analytics/progress-timeline?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

[Fact]

// Assert    public async Task GetPeerComparison_WithGradeLevel_ReturnsOk()

response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
{

    var content = await response.Content.ReadAsStringAsync();        // Act

    content.Should().Contain("startDate cannot be after endDate"); var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison?gradeLevel=9");

}

// Assert

#endregion        response.StatusCode.Should().Be(HttpStatusCode.OK);

var content = await response.Content.ReadFromJsonAsync<PeerComparison>();

#region Peer Comparison Tests        content.Should().NotBeNull();

content!.GradeLevel.Should().Be(GradeLevel.Grade9);

[Fact]    }

    public async Task GetPeerComparison_WithoutFilters_ReturnsOk()

{
    [Fact]

    // Arrange    public async Task GetPeerComparison_WithGradeLevelAndSubject_ReturnsOk()

    var client = _factory.CreateAuthenticatedClient(_studentToken);
    {

        // Act

        // Act        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison?gradeLevel=10&subject=0");

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison");

        // Assert

        // Assert        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.StatusCode.Should().Be(HttpStatusCode.OK); var content = await response.Content.ReadFromJsonAsync<PeerComparison>();

        var content = await response.Content.ReadFromJsonAsync<PeerComparison>(); content.Should().NotBeNull();

        content.Should().NotBeNull();
    }

    content!.StudentId.Should().Be(_testStudentId);

}    #endregion



    [Fact]    #region Content Type Tests

    public async Task GetPeerComparison_WithGradeLevel_ReturnsOk()

{
    [Fact]

    // Arrange    public async Task AllEndpoints_ReturnJsonContentType()

    var client = _factory.CreateAuthenticatedClient(_studentToken);
    {

        // Arrange

        // Act        var endpoints = new[]

        var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison?gradeLevel=9");
        {

            $"/api/v1/students/{_testStudentId}/analytics/performance-summary",

        // Assert            $"/api/v1/students/{_testStudentId}/analytics/subject-performance",

        response.StatusCode.Should().Be(HttpStatusCode.OK); $"/api/v1/students/{_testStudentId}/analytics/learning-objectives",

        var content = await response.Content.ReadFromJsonAsync<PeerComparison>(); $"/api/v1/students/{_testStudentId}/analytics/ability-estimates",

        content.Should().NotBeNull(); $"/api/v1/students/{_testStudentId}/analytics/improvement-areas",

        content!.GradeLevel.Should().Be(GradeLevel.Grade9); $"/api/v1/students/{_testStudentId}/analytics/progress-timeline",

    }
        $"/api/v1/students/{_testStudentId}/analytics/peer-comparison"

        }
    ;

    [Fact]

    public async Task GetPeerComparison_WithGradeLevelAndSubject_ReturnsOk()        foreach (var endpoint in endpoints)

    {
        {

            // Arrange            // Act

            var client = _factory.CreateAuthenticatedClient(_studentToken); var response = await _client.GetAsync(endpoint);



            // Act            // Assert

            var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/peer-comparison?gradeLevel=10&subject=0"); response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        }

        // Assert    }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<PeerComparison>();    #endregion

        content.Should().NotBeNull();

    }    #region Performance Tests



    #endregion    [Fact]

    public async Task GetPerformanceSummary_ResponseTime_ShouldBeLessThan500Ms()

    #region Content Type Tests    {

// Arrange

    [Fact] var stopwatch = System.Diagnostics.Stopwatch.StartNew();

public async Task AllEndpoints_ReturnJsonContentType()

{        // Act

    // Arrange        var response = await _client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

    var client = _factory.CreateAuthenticatedClient(_studentToken);

    var endpoints = new[]        // Assert

    {        stopwatch.Stop();

    $"/api/v1/students/{_testStudentId}/analytics/performance-summary",        response.StatusCode.Should().Be(HttpStatusCode.OK);

    $"/api/v1/students/{_testStudentId}/analytics/subject-performance",        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "API should respond quickly with stub data");

    $"/api/v1/students/{_testStudentId}/analytics/learning-objectives",    }

$"/api/v1/students/{_testStudentId}/analytics/ability-estimates",

            $"/api/v1/students/{_testStudentId}/analytics/improvement-areas",    #endregion

            $"/api/v1/students/{_testStudentId}/analytics/progress-timeline",}

            $"/api/v1/students/{_testStudentId}/analytics/peer-comparison"
        };

foreach (var endpoint in endpoints)
{
    // Act
    var response = await client.GetAsync(endpoint);

    // Assert
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
}
    }

    #endregion

    #region Performance Tests

    [Fact]
public async Task GetPerformanceSummary_ResponseTime_ShouldBeLessThan500Ms()
{
    // Arrange
    var client = _factory.CreateAuthenticatedClient(_studentToken);
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Act
    var response = await client.GetAsync($"/api/v1/students/{_testStudentId}/analytics/performance-summary");

    // Assert
    stopwatch.Stop();
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "API should respond quickly with stub data");
}

    #endregion
}
