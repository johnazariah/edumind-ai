using System.Collections.Concurrent;
using AcademicAssessment.Orchestration;
using AcademicAssessment.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Service for collecting and broadcasting real-time orchestration metrics via SignalR.
/// Monitors routing statistics, agent utilization, circuit breaker status, and queue depth.
/// </summary>
public class OrchestrationMetricsService : IOrchestrationMetricsService, IDisposable
{
    private readonly IHubContext<OrchestrationHub> _hubContext;
    private readonly ILogger<OrchestrationMetricsService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private Timer? _broadcastTimer;
    private bool _isMonitoring;

    // Real-time metrics tracking
    private readonly ConcurrentDictionary<string, CircuitBreakerStatus> _circuitBreakerStates = new();
    private int _currentQueueDepth;

    public OrchestrationMetricsService(
        IHubContext<OrchestrationHub> hubContext,
        ILogger<OrchestrationMetricsService> logger,
        IServiceProvider serviceProvider)
    {
        _hubContext = hubContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void StartMonitoring(int intervalSeconds = 5)
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("Monitoring already started");
            return;
        }

        _isMonitoring = true;
        _broadcastTimer = new Timer(
            _ => BroadcastCurrentMetricsAsync().GetAwaiter().GetResult(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(intervalSeconds));

        _logger.LogInformation("Orchestration metrics monitoring started with {Interval}s interval",
            intervalSeconds);
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _broadcastTimer?.Dispose();
        _broadcastTimer = null;

        _logger.LogInformation("Orchestration metrics monitoring stopped");
    }

    /// <inheritdoc/>
    public async Task BroadcastCurrentMetricsAsync()
    {
        try
        {
            // Create a scope to get the scoped orchestrator
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetService<StudentProgressOrchestrator>();

            if (orchestrator == null)
            {
                _logger.LogWarning("StudentProgressOrchestrator not available");
                return;
            }

            var stats = orchestrator.GetRoutingStatistics();

            var metrics = new OrchestrationMetrics
            {
                Timestamp = DateTime.UtcNow,

                // Routing statistics
                TotalRoutingAttempts = stats.TotalRoutingAttempts,
                SuccessfulRoutings = stats.SuccessfulRoutings,
                FallbackSelections = stats.FallbackSelections,
                FailedRoutings = stats.FailedRoutings,
                SuccessRate = stats.SuccessRate,
                FallbackRate = stats.FallbackRate,

                // Agent utilization
                AgentUtilization = stats.AgentUtilization.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                ),

                // Failed agents
                FailedAgents = stats.FailedAgents.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                ),

                // Circuit breaker status
                CircuitBreakerStates = _circuitBreakerStates.Values.ToList(),

                // Queue depth
                QueueDepth = _currentQueueDepth,

                // Health status
                OverallHealth = CalculateOverallHealth(stats)
            };

            await _hubContext.Clients.Group("monitoring")
                .SendAsync("MetricsUpdate", metrics);

            _logger.LogDebug("Broadcasted metrics: {SuccessRate}% success rate, {Agents} agents, {Queue} queued",
                metrics.SuccessRate.ToString("F1"),
                metrics.AgentUtilization.Count,
                metrics.QueueDepth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting orchestration metrics");
        }
    }

    /// <inheritdoc/>
    public Task RecordRoutingEventAsync(string agentId, bool success, bool usedFallback)
    {
        // Routing events are tracked in StudentProgressOrchestrator's RoutingStatistics
        // This method is here for extensibility if we want additional tracking
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RecordCircuitBreakerStateAsync(string agentId, bool isOpen, DateTime? openUntil)
    {
        var status = new CircuitBreakerStatus
        {
            AgentId = agentId,
            IsOpen = isOpen,
            OpenUntil = openUntil,
            LastUpdated = DateTime.UtcNow
        };

        _circuitBreakerStates.AddOrUpdate(agentId, status, (_, _) => status);

        // Broadcast alert if circuit breaker just opened
        if (isOpen)
        {
            await BroadcastAlertAsync(new OrchestrationAlert
            {
                Severity = AlertSeverity.Warning,
                Message = $"Circuit breaker opened for agent '{agentId}' until {openUntil:yyyy-MM-dd HH:mm:ss} UTC",
                AgentId = agentId,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogWarning("Circuit breaker opened for agent {AgentId} until {OpenUntil}",
                agentId, openUntil);
        }
    }

    /// <inheritdoc/>
    public Task UpdateQueueDepthAsync(int queueDepth)
    {
        _currentQueueDepth = queueDepth;

        // Alert if queue is getting too deep
        if (queueDepth > 50)
        {
            return BroadcastAlertAsync(new OrchestrationAlert
            {
                Severity = AlertSeverity.Warning,
                Message = $"High queue depth: {queueDepth} tasks pending",
                Timestamp = DateTime.UtcNow
            });
        }

        return Task.CompletedTask;
    }

    private async Task BroadcastAlertAsync(OrchestrationAlert alert)
    {
        try
        {
            await _hubContext.Clients.Group("monitoring")
                .SendAsync("Alert", alert);

            _logger.LogInformation("Broadcasted {Severity} alert: {Message}",
                alert.Severity, alert.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting alert");
        }
    }

    private string CalculateOverallHealth(Orchestration.RoutingStatistics stats)
    {
        var openCircuitBreakers = _circuitBreakerStates.Values.Count(cb => cb.IsOpen);

        if (openCircuitBreakers > 0)
        {
            return "Degraded";
        }

        if (stats.SuccessRate < 80.0 && stats.TotalRoutingAttempts > 10)
        {
            return "Degraded";
        }

        if (stats.SuccessRate > 95.0 || stats.TotalRoutingAttempts < 10)
        {
            return "Healthy";
        }

        return "Warning";
    }

    public void Dispose()
    {
        StopMonitoring();
        _broadcastTimer?.Dispose();
    }
}

/// <summary>
/// Orchestration metrics snapshot for dashboard display.
/// </summary>
public class OrchestrationMetrics
{
    public DateTime Timestamp { get; set; }

    // Routing statistics
    public int TotalRoutingAttempts { get; set; }
    public int SuccessfulRoutings { get; set; }
    public int FallbackSelections { get; set; }
    public int FailedRoutings { get; set; }
    public double SuccessRate { get; set; }
    public double FallbackRate { get; set; }

    // Agent utilization (agentId -> task count)
    public Dictionary<string, int> AgentUtilization { get; set; } = new();

    // Failed agents (agentId -> failure count)
    public Dictionary<string, int> FailedAgents { get; set; } = new();

    // Circuit breaker status
    public List<CircuitBreakerStatus> CircuitBreakerStates { get; set; } = new();

    // Queue metrics
    public int QueueDepth { get; set; }

    // Overall health
    public string OverallHealth { get; set; } = "Unknown";
}

/// <summary>
/// Circuit breaker status for an agent.
/// </summary>
public class CircuitBreakerStatus
{
    public string AgentId { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTime? OpenUntil { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Alert notification for monitoring dashboard.
/// </summary>
public class OrchestrationAlert
{
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
