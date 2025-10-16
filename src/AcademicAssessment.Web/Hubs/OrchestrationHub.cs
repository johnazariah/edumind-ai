using Microsoft.AspNetCore.SignalR;

namespace AcademicAssessment.Web.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time orchestration metrics and monitoring data.
/// Provides live updates for routing statistics, agent utilization, circuit breaker status,
/// and queue depth to monitoring dashboards.
/// </summary>
public class OrchestrationHub : Hub
{
    private readonly ILogger<OrchestrationHub> _logger;

    public OrchestrationHub(ILogger<OrchestrationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join the monitoring group to receive orchestration metrics.
    /// </summary>
    public async Task JoinMonitoringGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "monitoring");
        _logger.LogInformation("Client {ConnectionId} joined monitoring group", Context.ConnectionId);
    }

    /// <summary>
    /// Leave the monitoring group.
    /// </summary>
    public async Task LeaveMonitoringGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "monitoring");
        _logger.LogInformation("Client {ConnectionId} left monitoring group", Context.ConnectionId);
    }

    /// <summary>
    /// Request current orchestration metrics on demand.
    /// </summary>
    public async Task RequestCurrentMetrics()
    {
        _logger.LogDebug("Client {ConnectionId} requested current metrics", Context.ConnectionId);
        // Metrics service will respond via BroadcastMetrics
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Monitoring client connected: {ConnectionId} from {UserAgent}",
            Context.ConnectionId,
            Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString() ?? "Unknown");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Monitoring client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Monitoring client disconnected: {ConnectionId}",
                Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
