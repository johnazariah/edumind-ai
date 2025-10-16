using AcademicAssessment.Web.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AcademicAssessment.Web.Controllers;

/// <summary>
/// API endpoints for orchestration monitoring and metrics.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class OrchestrationController : ControllerBase
{
    private readonly IOrchestrationMetricsService _metricsService;
    private readonly ILogger<OrchestrationController> _logger;

    public OrchestrationController(
        IOrchestrationMetricsService metricsService,
        ILogger<OrchestrationController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger immediate broadcast of current orchestration metrics.
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("metrics/broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BroadcastMetrics()
    {
        try
        {
            await _metricsService.BroadcastCurrentMetricsAsync();
            _logger.LogInformation("Metrics broadcast triggered manually");
            return Ok(new { message = "Metrics broadcast successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting metrics");
            return StatusCode(500, new { error = "Failed to broadcast metrics" });
        }
    }

    /// <summary>
    /// Start metrics monitoring service.
    /// </summary>
    /// <param name="intervalSeconds">Broadcast interval in seconds (default: 5)</param>
    /// <returns>Success status</returns>
    [HttpPost("metrics/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult StartMonitoring([FromQuery] int intervalSeconds = 5)
    {
        _metricsService.StartMonitoring(intervalSeconds);
        _logger.LogInformation("Metrics monitoring started via API (interval: {Interval}s)", intervalSeconds);
        return Ok(new { message = $"Monitoring started with {intervalSeconds}s interval" });
    }

    /// <summary>
    /// Stop metrics monitoring service.
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("metrics/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult StopMonitoring()
    {
        _metricsService.StopMonitoring();
        _logger.LogInformation("Metrics monitoring stopped via API");
        return Ok(new { message = "Monitoring stopped" });
    }

    /// <summary>
    /// Record a circuit breaker state change.
    /// </summary>
    /// <param name="request">Circuit breaker state change request</param>
    /// <returns>Success status</returns>
    [HttpPost("circuit-breaker")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordCircuitBreakerState([FromBody] CircuitBreakerStateRequest request)
    {
        if (string.IsNullOrEmpty(request.AgentId))
        {
            return BadRequest(new { error = "AgentId is required" });
        }

        await _metricsService.RecordCircuitBreakerStateAsync(
            request.AgentId,
            request.IsOpen,
            request.OpenUntil);

        _logger.LogInformation("Circuit breaker state recorded for agent {AgentId}: {IsOpen}",
            request.AgentId, request.IsOpen);

        return Ok(new { message = "Circuit breaker state recorded" });
    }

    /// <summary>
    /// Update queue depth metric.
    /// </summary>
    /// <param name="queueDepth">Current queue depth</param>
    /// <returns>Success status</returns>
    [HttpPost("queue-depth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQueueDepth([FromQuery] int queueDepth)
    {
        await _metricsService.UpdateQueueDepthAsync(queueDepth);
        _logger.LogDebug("Queue depth updated: {Depth}", queueDepth);
        return Ok(new { message = "Queue depth updated", queueDepth });
    }
}

/// <summary>
/// Request model for circuit breaker state changes.
/// </summary>
public class CircuitBreakerStateRequest
{
    public string AgentId { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTime? OpenUntil { get; set; }
}
