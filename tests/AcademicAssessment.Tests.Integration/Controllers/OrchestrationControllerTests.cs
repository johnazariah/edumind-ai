using System.Net;
using System.Net.Http.Json;
using AcademicAssessment.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace AcademicAssessment.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for OrchestrationController endpoints.
/// Tests orchestration monitoring and metrics management.
/// </summary>
public class OrchestrationControllerTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>
{
    private readonly AuthenticatedWebApplicationFactory<Program> _factory;
    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private readonly string _teacherToken;

    public OrchestrationControllerTests(AuthenticatedWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _teacherToken = JwtTokenGenerator.GenerateTeacherToken(
            Guid.NewGuid(), _testSchoolId, "teacher@test.com", "Test Teacher", new[] { Guid.NewGuid() });
    }

    #region Metrics Broadcast Tests

    [Fact]
    public async Task BroadcastMetrics_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/broadcast", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BroadcastMetrics_ReturnsSuccessMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/broadcast", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("broadcast");
        content.Should().Contain("successful");
    }

    [Fact]
    public async Task BroadcastMetrics_ReturnsJsonContentType()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/broadcast", null);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Monitoring Start Tests

    [Fact]
    public async Task StartMonitoring_DefaultInterval_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartMonitoring_DefaultInterval_ReturnsSuccessMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/start", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Monitoring started");
        content.Should().Contain("5s interval"); // Default interval
    }

    [Fact]
    public async Task StartMonitoring_CustomInterval_ReturnsSuccessWithCorrectInterval()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/start?intervalSeconds=10", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("10s interval");
    }

    [Fact]
    public async Task StartMonitoring_SmallInterval_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/start?intervalSeconds=1", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartMonitoring_LargeInterval_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/start?intervalSeconds=3600", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Monitoring Stop Tests

    [Fact]
    public async Task StopMonitoring_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StopMonitoring_ReturnsSuccessMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Monitoring stopped");
    }

    [Fact]
    public async Task StopMonitoring_WhenNotStarted_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act - Stop without starting
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);

        // Assert - Should not error
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Circuit Breaker State Tests

    [Fact]
    public async Task RecordCircuitBreakerState_ValidRequest_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "test-agent-001",
            IsOpen = true,
            OpenUntil = (DateTimeOffset?)DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordCircuitBreakerState_ValidRequest_ReturnsSuccessMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "test-agent-002",
            IsOpen = false,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Circuit breaker state recorded");
    }

    [Fact]
    public async Task RecordCircuitBreakerState_OpenState_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "math-agent",
            IsOpen = true,
            OpenUntil = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordCircuitBreakerState_ClosedState_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "science-agent",
            IsOpen = false,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordCircuitBreakerState_NullAgentId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = (string?)null,
            IsOpen = true,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordCircuitBreakerState_EmptyAgentId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "",
            IsOpen = true,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordCircuitBreakerState_InvalidAgentId_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "",
            IsOpen = false,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("AgentId");
        content.Should().Contain("required");
    }

    #endregion

    #region Monitoring Lifecycle Tests

    [Fact]
    public async Task MonitoringLifecycle_StartAndStop_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act - Start monitoring
        var startResponse = await client.PostAsync("/api/v1.0/Orchestration/metrics/start?intervalSeconds=10", null);

        // Act - Stop monitoring
        var stopResponse = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);

        // Assert
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MonitoringLifecycle_MultipleStarts_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act - Start monitoring twice
        var response1 = await client.PostAsync("/api/v1.0/Orchestration/metrics/start", null);
        var response2 = await client.PostAsync("/api/v1.0/Orchestration/metrics/start", null);

        // Assert - Should not error
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MonitoringLifecycle_MultipleStops_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act - Stop monitoring twice
        var response1 = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);
        var response2 = await client.PostAsync("/api/v1.0/Orchestration/metrics/stop", null);

        // Assert - Should not error
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region API Versioning Tests

    [Fact]
    public async Task BroadcastMetrics_WithoutVersion_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/Orchestration/metrics/broadcast", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartMonitoring_InvalidVersion_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v99.0/Orchestration/metrics/start", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task BroadcastMetrics_ReturnsJson()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/metrics/broadcast", null);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task RecordCircuitBreaker_AcceptsJson()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var request = new
        {
            AgentId = "test-agent",
            IsOpen = true,
            OpenUntil = (DateTimeOffset?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Orchestration/circuit-breaker", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RecordCircuitBreakerState_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_teacherToken);
        var content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/v1.0/Orchestration/circuit-breaker", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
