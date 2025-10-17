using AcademicAssessment.Orchestration.Entities;
using AcademicAssessment.Orchestration.Models;

namespace AcademicAssessment.Orchestration.Interfaces;

/// <summary>
/// Service for persisting and recovering orchestrator state.
/// Handles workflow executions, circuit breaker states, and routing decisions.
/// </summary>
public interface IOrchestrationStateService
{
    // Workflow Execution State
    Task SaveWorkflowExecutionAsync(WorkflowExecution execution, WorkflowDefinition definition, Guid tenantId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> LoadWorkflowExecutionAsync(string executionId, CancellationToken cancellationToken = default);
    Task<List<WorkflowExecution>> GetIncompleteWorkflowsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // Circuit Breaker State
    Task SaveCircuitBreakerStateAsync(string agentKey, string state, int failureCount, int successCount, DateTime? openedAt, DateTime? resetAt, Guid tenantId, CancellationToken cancellationToken = default);
    Task<(string state, int failureCount, int successCount, DateTime? openedAt, DateTime? resetAt)?> LoadCircuitBreakerStateAsync(string agentKey, CancellationToken cancellationToken = default);
    Task<Dictionary<string, (string state, int failureCount, DateTime? openedAt)>> GetAllCircuitBreakerStatesAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // Routing Decisions (Audit Trail)
    Task SaveRoutingDecisionAsync(
        string taskId,
        string taskType,
        string? requiredCapability,
        string selectedAgent,
        int priority,
        double agentScore,
        bool success,
        string? errorMessage,
        int retryAttempts,
        long routingDurationMs,
        Guid tenantId,
        string? workflowExecutionId = null,
        string? workflowStepId = null,
        CancellationToken cancellationToken = default);

    Task<List<RoutingDecisionEntity>> GetRoutingDecisionsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        string? taskType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    // Routing Statistics
    Task UpdateRoutingStatisticsAsync(
        string agentKey,
        bool success,
        long durationMs,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, (int total, int success, double avgDurationMs, double successRate)>> GetRoutingStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task RecalculateStatisticsAsync(Guid tenantId, DateTime? startDate = null, CancellationToken cancellationToken = default);

    // Recovery Operations
    Task RecoverCircuitBreakersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> CleanupOldDataAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
