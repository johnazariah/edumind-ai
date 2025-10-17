using AcademicAssessment.Infrastructure.Data;
using AcademicAssessment.Orchestration.Entities;
using AcademicAssessment.Orchestration.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for workflow execution persistence.
/// </summary>
public class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly AcademicContext _context;

    public WorkflowExecutionRepository(AcademicContext context)
    {
        _context = context;
    }

    public async Task<WorkflowExecutionEntity> SaveAsync(
        WorkflowExecutionEntity execution,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.WorkflowExecutions
            .FirstOrDefaultAsync(w => w.ExecutionId == execution.ExecutionId, cancellationToken);

        if (existing == null)
        {
            _context.WorkflowExecutions.Add(execution);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(execution);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return execution;
    }

    public async Task<WorkflowExecutionEntity?> GetByIdAsync(
        string executionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .FirstOrDefaultAsync(w => w.ExecutionId == executionId, cancellationToken);
    }

    public async Task<List<WorkflowExecutionEntity>> GetByTenantAsync(
        Guid tenantId,
        string? workflowName = null,
        string? status = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowExecutions
            .Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrEmpty(workflowName))
        {
            query = query.Where(w => w.WorkflowName == workflowName);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(w => w.Status == status);
        }

        return await query
            .OrderByDescending(w => w.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowExecutionEntity>> GetIncompleteAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Where(w => w.TenantId == tenantId &&
                       (w.Status == "Pending" || w.Status == "Running" || w.Status == "Blocked"))
            .OrderBy(w => w.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOldExecutionsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldExecutions = await _context.WorkflowExecutions
            .Where(w => w.CompletedAt != null && w.CompletedAt < olderThan)
            .ToListAsync(cancellationToken);

        _context.WorkflowExecutions.RemoveRange(oldExecutions);
        await _context.SaveChangesAsync(cancellationToken);

        return oldExecutions.Count;
    }
}

/// <summary>
/// Repository implementation for circuit breaker state persistence.
/// </summary>
public class CircuitBreakerStateRepository : ICircuitBreakerStateRepository
{
    private readonly AcademicContext _context;

    public CircuitBreakerStateRepository(AcademicContext context)
    {
        _context = context;
    }

    public async Task<CircuitBreakerStateEntity> SaveAsync(
        CircuitBreakerStateEntity state,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.CircuitBreakerStates
            .FirstOrDefaultAsync(c => c.AgentKey == state.AgentKey, cancellationToken);

        if (existing == null)
        {
            _context.CircuitBreakerStates.Add(state);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(state);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return state;
    }

    public async Task<CircuitBreakerStateEntity?> GetByAgentKeyAsync(
        string agentKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.CircuitBreakerStates
            .FirstOrDefaultAsync(c => c.AgentKey == agentKey, cancellationToken);
    }

    public async Task<List<CircuitBreakerStateEntity>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CircuitBreakerStates
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.AgentKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CircuitBreakerStateEntity>> GetOpenCircuitsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CircuitBreakerStates
            .Where(c => c.TenantId == tenantId && c.State == "Open")
            .OrderByDescending(c => c.OpenedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task ResetAsync(
        string agentKey,
        CancellationToken cancellationToken = default)
    {
        var state = await GetByAgentKeyAsync(agentKey, cancellationToken);
        if (state != null)
        {
            state.State = "Closed";
            state.FailureCount = 0;
            state.SuccessCount = 0;
            state.OpenedAt = null;
            state.ResetAt = null;
            state.LastUpdated = DateTime.UtcNow;

            await SaveAsync(state, cancellationToken);
        }
    }

    public async Task<int> DeleteOldStatesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldStates = await _context.CircuitBreakerStates
            .Where(c => c.LastUpdated < olderThan && c.State == "Closed")
            .ToListAsync(cancellationToken);

        _context.CircuitBreakerStates.RemoveRange(oldStates);
        await _context.SaveChangesAsync(cancellationToken);

        return oldStates.Count;
    }
}

/// <summary>
/// Repository implementation for routing decision audit trail.
/// </summary>
public class RoutingDecisionRepository : IRoutingDecisionRepository
{
    private readonly AcademicContext _context;

    public RoutingDecisionRepository(AcademicContext context)
    {
        _context = context;
    }

    public async Task<RoutingDecisionEntity> SaveAsync(
        RoutingDecisionEntity decision,
        CancellationToken cancellationToken = default)
    {
        _context.RoutingDecisions.Add(decision);
        await _context.SaveChangesAsync(cancellationToken);
        return decision;
    }

    public async Task<List<RoutingDecisionEntity>> GetByTaskIdAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingDecisions
            .Where(r => r.TaskId == taskId)
            .OrderBy(r => r.RoutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoutingDecisionEntity>> GetByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? taskType = null,
        bool? success = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RoutingDecisions
            .Where(r => r.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RoutedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.RoutedAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.Where(r => r.TaskType == taskType);
        }

        if (success.HasValue)
        {
            query = query.Where(r => r.Success == success.Value);
        }

        return await query
            .OrderByDescending(r => r.RoutedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoutingDecisionEntity>> GetByWorkflowExecutionAsync(
        string workflowExecutionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingDecisions
            .Where(r => r.WorkflowExecutionId == workflowExecutionId)
            .OrderBy(r => r.RoutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, (int total, int success, double avgDuration)>> GetStatisticsByAgentAsync(
        Guid tenantId,
        DateTime? startDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RoutingDecisions
            .Where(r => r.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RoutedAt >= startDate.Value);
        }

        var stats = await query
            .GroupBy(r => r.SelectedAgent)
            .Select(g => new
            {
                Agent = g.Key,
                Total = g.Count(),
                Success = g.Count(r => r.Success),
                AvgDuration = g.Average(r => r.RoutingDurationMs)
            })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(
            s => s.Agent,
            s => (s.Total, s.Success, s.AvgDuration));
    }

    public async Task<int> DeleteOldDecisionsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldDecisions = await _context.RoutingDecisions
            .Where(r => r.RoutedAt < olderThan)
            .ToListAsync(cancellationToken);

        _context.RoutingDecisions.RemoveRange(oldDecisions);
        await _context.SaveChangesAsync(cancellationToken);

        return oldDecisions.Count;
    }
}

/// <summary>
/// Repository implementation for routing statistics aggregation.
/// </summary>
public class RoutingStatisticsRepository : IRoutingStatisticsRepository
{
    private readonly AcademicContext _context;

    public RoutingStatisticsRepository(AcademicContext context)
    {
        _context = context;
    }

    public async Task<RoutingStatisticsEntity> SaveAsync(
        RoutingStatisticsEntity stats,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.RoutingStatistics
            .FirstOrDefaultAsync(s => s.AgentKey == stats.AgentKey, cancellationToken);

        if (existing == null)
        {
            _context.RoutingStatistics.Add(stats);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(stats);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return stats;
    }

    public async Task<RoutingStatisticsEntity?> GetByAgentKeyAsync(
        string agentKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingStatistics
            .FirstOrDefaultAsync(s => s.AgentKey == agentKey, cancellationToken);
    }

    public async Task<List<RoutingStatisticsEntity>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingStatistics
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.SuccessRate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoutingStatisticsEntity>> GetTopPerformingAsync(
        Guid tenantId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingStatistics
            .Where(s => s.TenantId == tenantId && s.TotalTasks > 0)
            .OrderByDescending(s => s.SuccessRate)
            .ThenByDescending(s => s.TotalTasks)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoutingStatisticsEntity>> GetUnderperformingAsync(
        Guid tenantId,
        double successRateThreshold = 0.8,
        CancellationToken cancellationToken = default)
    {
        return await _context.RoutingStatistics
            .Where(s => s.TenantId == tenantId &&
                       s.TotalTasks >= 5 &&
                       s.SuccessRate < successRateThreshold)
            .OrderBy(s => s.SuccessRate)
            .ToListAsync(cancellationToken);
    }

    public async Task RecalculateStatisticsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RoutingDecisions
            .Where(r => r.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RoutedAt >= startDate.Value);
        }

        var agentStats = await query
            .GroupBy(r => r.SelectedAgent)
            .Select(g => new
            {
                AgentKey = g.Key,
                TotalTasks = g.Count(),
                SuccessfulTasks = g.Count(r => r.Success),
                FailedTasks = g.Count(r => !r.Success),
                TotalDurationMs = g.Sum(r => r.RoutingDurationMs)
            })
            .ToListAsync(cancellationToken);

        foreach (var stat in agentStats)
        {
            var entity = new RoutingStatisticsEntity
            {
                AgentKey = stat.AgentKey,
                TotalTasks = stat.TotalTasks,
                SuccessfulTasks = stat.SuccessfulTasks,
                FailedTasks = stat.FailedTasks,
                TotalDurationMs = stat.TotalDurationMs,
                SuccessRate = stat.TotalTasks > 0 ? (double)stat.SuccessfulTasks / stat.TotalTasks : 0.0,
                AverageDurationMs = stat.TotalTasks > 0 ? (double)stat.TotalDurationMs / stat.TotalTasks : 0.0,
                LastUpdated = DateTime.UtcNow,
                TenantId = tenantId
            };

            await SaveAsync(entity, cancellationToken);
        }
    }
}
