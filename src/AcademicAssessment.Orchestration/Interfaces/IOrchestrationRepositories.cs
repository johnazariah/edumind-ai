using AcademicAssessment.Orchestration.Entities;
using AcademicAssessment.Orchestration.Models;

namespace AcademicAssessment.Orchestration.Interfaces;

/// <summary>
/// Repository for persisting and retrieving workflow execution state.
/// </summary>
public interface IWorkflowExecutionRepository
{
    /// <summary>
    /// Save a new workflow execution or update existing one.
    /// </summary>
    Task<WorkflowExecutionEntity> SaveAsync(WorkflowExecutionEntity execution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a workflow execution by ID.
    /// </summary>
    Task<WorkflowExecutionEntity?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all workflow executions for a tenant with optional filtering.
    /// </summary>
    Task<List<WorkflowExecutionEntity>> GetByTenantAsync(
        Guid tenantId,
        string? workflowName = null,
        string? status = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get incomplete workflows (for recovery after restart).
    /// </summary>
    Task<List<WorkflowExecutionEntity>> GetIncompleteAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old completed workflow executions (for cleanup).
    /// </summary>
    Task<int> DeleteOldExecutionsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for persisting and retrieving circuit breaker state.
/// </summary>
public interface ICircuitBreakerStateRepository
{
    /// <summary>
    /// Save or update circuit breaker state.
    /// </summary>
    Task<CircuitBreakerStateEntity> SaveAsync(CircuitBreakerStateEntity state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get circuit breaker state by agent key.
    /// </summary>
    Task<CircuitBreakerStateEntity?> GetByAgentKeyAsync(string agentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all circuit breaker states for a tenant.
    /// </summary>
    Task<List<CircuitBreakerStateEntity>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all open circuits (for monitoring).
    /// </summary>
    Task<List<CircuitBreakerStateEntity>> GetOpenCircuitsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset circuit breaker to closed state.
    /// </summary>
    Task ResetAsync(string agentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old circuit breaker states (for cleanup).
    /// </summary>
    Task<int> DeleteOldStatesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for audit trail of routing decisions.
/// </summary>
public interface IRoutingDecisionRepository
{
    /// <summary>
    /// Save a new routing decision.
    /// </summary>
    Task<RoutingDecisionEntity> SaveAsync(RoutingDecisionEntity decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get routing decisions for a specific task.
    /// </summary>
    Task<List<RoutingDecisionEntity>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get routing decisions with filtering.
    /// </summary>
    Task<List<RoutingDecisionEntity>> GetByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? taskType = null,
        bool? success = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get routing decisions for a specific workflow execution.
    /// </summary>
    Task<List<RoutingDecisionEntity>> GetByWorkflowExecutionAsync(
        string workflowExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get routing statistics grouped by agent.
    /// </summary>
    Task<Dictionary<string, (int total, int success, double avgDuration)>> GetStatisticsByAgentAsync(
        Guid tenantId,
        DateTime? startDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old routing decisions (for cleanup).
    /// </summary>
    Task<int> DeleteOldDecisionsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for routing statistics aggregation.
/// </summary>
public interface IRoutingStatisticsRepository
{
    /// <summary>
    /// Save or update routing statistics.
    /// </summary>
    Task<RoutingStatisticsEntity> SaveAsync(RoutingStatisticsEntity stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics for a specific agent.
    /// </summary>
    Task<RoutingStatisticsEntity?> GetByAgentKeyAsync(string agentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all statistics for a tenant.
    /// </summary>
    Task<List<RoutingStatisticsEntity>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top performing agents by success rate.
    /// </summary>
    Task<List<RoutingStatisticsEntity>> GetTopPerformingAsync(
        Guid tenantId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get underperforming agents (below threshold).
    /// </summary>
    Task<List<RoutingStatisticsEntity>> GetUnderperformingAsync(
        Guid tenantId,
        double successRateThreshold = 0.8,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculate statistics from routing decisions.
    /// </summary>
    Task RecalculateStatisticsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        CancellationToken cancellationToken = default);
}
