# TODO-004: Implement Real-time Monitoring Dashboard

**Priority:** P1 - High  
**Area:** Infrastructure / Monitoring  
**Estimated Effort:** Large (6-8 hours)  
**Status:** In Progress (Day 5 of Week 1)

## Description

Build a SignalR-powered real-time monitoring dashboard for the StudentProgressOrchestrator that displays live metrics, agent utilization, circuit breaker status, queue depths, and alerting for degraded agents.

## Context

The StudentProgressOrchestrator coordinates multiple AI agents and needs visibility into:

- Live routing metrics (success rates, response times)
- Agent utilization and availability
- Circuit breaker states (open/closed/half-open)
- Task queue depths and processing rates
- Failed tasks and retry attempts
- System health indicators

This is **Day 5 of Week 1** in the roadmap and is currently in progress. The orchestrator logic is complete (99.2% tests passing), but monitoring visualization needs implementation.

Related files:

- Orchestrator: `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`
- Infrastructure: Circuit breaker, routing statistics repositories
- Dashboard: New components needed in `src/AcademicAssessment.Dashboard/`

## Technical Requirements

### SignalR Hub

Create a new SignalR hub for real-time updates:

```csharp
public class OrchestrationMonitoringHub : Hub
{
    public async Task SubscribeToMetrics(string filter = "all")
    {
        // Add connection to group for filtered metrics
        await Groups.AddToGroupAsync(Context.ConnectionId, filter);
    }
    
    public async Task UnsubscribeFromMetrics(string filter = "all")
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, filter);
    }
}
```

### Monitoring Service

Create background service to push metrics:

```csharp
public class OrchestrationMonitoringService : BackgroundService
{
    private readonly IHubContext<OrchestrationMonitoringHub> _hubContext;
    private readonly IRoutingStatisticsRepository _statsRepo;
    private readonly ICircuitBreakerStateRepository _circuitRepo;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var metrics = await GatherMetricsAsync();
            await _hubContext.Clients.All.SendAsync("MetricsUpdate", metrics);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### Dashboard UI Components

**1. Metrics Overview Card**

- Total tasks routed (today, this week)
- Overall success rate (percentage)
- Average response time
- Active agents count

**2. Agent Utilization Chart**

- Real-time bar chart or gauge
- One bar per agent showing current load
- Color coding: green (healthy), yellow (busy), red (degraded)
- Capacity percentage (0-100%)

**3. Circuit Breaker Status Panel**

- Table of all agents with circuit breaker state
- Status: Closed (healthy), Open (failed), Half-Open (testing)
- Failure count and last failure timestamp
- Next retry timestamp for open circuits

**4. Queue Depth Visualization**

- Line chart showing queue depth over time
- Separate lines for high/medium/low priority queues
- Alert threshold indicator

**5. Routing Statistics Table**

- Agent name, total routes, successes, failures
- Success rate percentage
- Average response time
- Last routed timestamp

**6. Failed Tasks List**

- Recent failed tasks with error messages
- Retry count and next retry time
- Option to manually retry or cancel

**7. System Alerts Panel**

- Red alerts for degraded agents
- Yellow warnings for high queue depths
- Blue info for circuit breaker state changes

## Acceptance Criteria

- [ ] SignalR hub created for orchestration monitoring
- [ ] Background service pushes metrics every 5 seconds
- [ ] Dashboard page created in Dashboard app
- [ ] Metrics overview card displays current stats
- [ ] Agent utilization chart updates in real-time
- [ ] Circuit breaker status panel shows all agents
- [ ] Queue depth visualization with historical data
- [ ] Routing statistics table is sortable
- [ ] Failed tasks list shows recent failures
- [ ] Alerts panel highlights degraded agents
- [ ] Color coding: green (healthy), yellow (busy), red (failed)
- [ ] Auto-refresh without page reload
- [ ] Responsive design for tablet/desktop
- [ ] Proper error handling for SignalR disconnects
- [ ] Reconnection logic when connection lost
- [ ] Unit tests for monitoring service (>80% coverage)
- [ ] Integration test for SignalR hub
- [ ] Manual testing of dashboard during load

## Dependencies

- **Completed:**
  - StudentProgressOrchestrator implementation (Days 1-4)
  - Circuit breaker pattern implementation
  - Routing statistics repositories
  - State persistence infrastructure

- **Required Libraries:**
  - SignalR (already in use for Student App)
  - Chart.js or similar for visualizations

## References

- **Files:**
  - `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`
  - `src/AcademicAssessment.Infrastructure/Repositories/RoutingStatisticsRepository.cs`
  - `src/AcademicAssessment.Infrastructure/Repositories/CircuitBreakerStateRepository.cs`
  
- **Documentation:**
  - `docs/planning/ROADMAP.md` (Week 1, Day 5)
  - `.github/adr/010-multi-agent-architecture.md`
  - `.github/adr/080-application-insights-for-observability.md`

- **Related TODOs:**
  - TODO-040: Implement Comprehensive Progress Analysis (orchestrator feature)
  - TODO-041: Implement Study Path Recommendation Engine
  - TODO-042: Implement Intelligent Scheduling System

## Implementation Notes

1. **Metrics Collection:** Query repositories every 5 seconds (configurable)
2. **Historical Data:** Store last hour of metrics in memory for charts
3. **Filtering:** Allow filtering by agent, time range, status
4. **Export:** Consider CSV export for metrics
5. **Threshold Alerts:** Configurable thresholds for alerts (e.g., >80% failure rate)
6. **Connection Management:** Handle SignalR connection drops gracefully
7. **Performance:** Use efficient queries (indexed fields, aggregations)
8. **Security:** Restrict dashboard access to admin/teacher roles

## Metrics to Display

### Real-time Metrics (5s refresh)

- Agent availability status
- Current queue depths
- Active task count per agent
- Circuit breaker states

### Historical Metrics (1 hour window)

- Success/failure rates over time
- Response time trends
- Queue depth trends
- Task throughput (tasks/minute)

### Aggregate Metrics (rolling windows)

- Today's stats (total, avg, peak)
- This week's stats
- Agent performance comparison

## UI/UX Considerations

- Use gauge charts for agent utilization (easy to scan)
- Red/yellow/green color coding (standard traffic light)
- Auto-scroll to alerts when they occur
- Minimize chart redraws (smooth updates)
- Show loading skeletons during initial load
- Provide pause/resume toggle for auto-refresh
- Allow zoom/pan on time-series charts

## Testing Strategy

**Unit Tests:**

- Test metrics gathering logic
- Test SignalR message formatting
- Mock repositories and verify queries

**Integration Tests:**

- Start background service and verify broadcasts
- Connect SignalR client and receive updates
- Simulate agent failures and verify alerts

**Load Tests:**

- 100+ concurrent dashboard connections
- Verify metrics accuracy under load
- Check for memory leaks in historical data

**Manual Tests:**

- Open dashboard and watch metrics update
- Trigger agent failure and verify circuit breaker UI
- Add tasks and watch queue depths change
- Test on multiple browsers
- Test reconnection after network interruption
