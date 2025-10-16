namespace AcademicAssessment.Web.Services;

/// <summary>
/// Service for collecting and broadcasting real-time orchestration metrics.
/// </summary>
public interface IOrchestrationMetricsService
{
    /// <summary>
    /// Start collecting and broadcasting metrics at the specified interval.
    /// </summary>
    /// <param name="intervalSeconds">Broadcast interval in seconds</param>
    void StartMonitoring(int intervalSeconds = 5);

    /// <summary>
    /// Stop collecting and broadcasting metrics.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Broadcast current metrics immediately.
    /// </summary>
    Task BroadcastCurrentMetricsAsync();

    /// <summary>
    /// Record a routing event for metrics tracking.
    /// </summary>
    /// <param name="agentId">Agent that handled the routing</param>
    /// <param name="success">Whether routing was successful</param>
    /// <param name="usedFallback">Whether fallback was used</param>
    Task RecordRoutingEventAsync(string agentId, bool success, bool usedFallback);

    /// <summary>
    /// Record a circuit breaker state change.
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="isOpen">Whether circuit breaker is open</param>
    /// <param name="openUntil">Time when circuit breaker will close (if open)</param>
    Task RecordCircuitBreakerStateAsync(string agentId, bool isOpen, DateTime? openUntil);

    /// <summary>
    /// Update queue depth metric.
    /// </summary>
    /// <param name="queueDepth">Current depth of the routing queue</param>
    Task UpdateQueueDepthAsync(int queueDepth);
}
