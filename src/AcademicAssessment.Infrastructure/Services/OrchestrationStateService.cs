using System.Text.Json;
using AcademicAssessment.Orchestration.Entities;
using AcademicAssessment.Orchestration.Interfaces;
using AcademicAssessment.Orchestration.Models;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Infrastructure.Services;

/// <summary>
/// Implementation of orchestration state service for persistence and recovery.
/// </summary>
public class OrchestrationStateService : IOrchestrationStateService
{
    private readonly IWorkflowExecutionRepository _workflowRepo;
    private readonly ICircuitBreakerStateRepository _circuitBreakerRepo;
    private readonly IRoutingDecisionRepository _routingDecisionRepo;
    private readonly IRoutingStatisticsRepository _routingStatsRepo;
    private readonly ILogger<OrchestrationStateService> _logger;

    public OrchestrationStateService(
        IWorkflowExecutionRepository workflowRepo,
        ICircuitBreakerStateRepository circuitBreakerRepo,
        IRoutingDecisionRepository routingDecisionRepo,
        IRoutingStatisticsRepository routingStatsRepo,
        ILogger<OrchestrationStateService> logger)
    {
        _workflowRepo = workflowRepo;
        _circuitBreakerRepo = circuitBreakerRepo;
        _routingDecisionRepo = routingDecisionRepo;
        _routingStatsRepo = routingStatsRepo;
        _logger = logger;
    }

    #region Workflow Execution State

    public async Task SaveWorkflowExecutionAsync(
        WorkflowExecution execution,
        WorkflowDefinition definition,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new WorkflowExecutionEntity
            {
                ExecutionId = execution.ExecutionId,
                WorkflowName = definition.Name,
                Status = execution.Status.ToString(),
                WorkflowDefinitionJson = JsonSerializer.Serialize(definition),
                ContextJson = JsonSerializer.Serialize(execution.Context),
                StepExecutionsJson = JsonSerializer.Serialize(execution.StepExecutions),
                StepOutputsJson = JsonSerializer.Serialize(execution.StepOutputs),
                ErrorMessage = execution.ErrorMessage,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                TenantId = tenantId
            };

            await _workflowRepo.SaveAsync(entity, cancellationToken);
            _logger.LogDebug("Saved workflow execution: {ExecutionId}", execution.ExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow execution: {ExecutionId}", execution.ExecutionId);
            throw;
        }
    }

    public async Task<WorkflowExecution?> LoadWorkflowExecutionAsync(
        string executionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _workflowRepo.GetByIdAsync(executionId, cancellationToken);
            if (entity == null)
            {
                return null;
            }

            var execution = new WorkflowExecution
            {
                ExecutionId = entity.ExecutionId,
                WorkflowId = entity.WorkflowName, // Storing workflow name in WorkflowId for now
                Status = Enum.Parse<WorkflowStatus>(entity.Status),
                Context = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ContextJson ?? "{}") ?? new(),
                StepExecutions = JsonSerializer.Deserialize<Dictionary<string, StepExecution>>(entity.StepExecutionsJson ?? "{}") ?? new(),
                StepOutputs = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.StepOutputsJson ?? "{}") ?? new(),
                ErrorMessage = entity.ErrorMessage,
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt
            };

            _logger.LogDebug("Loaded workflow execution: {ExecutionId}", executionId);
            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow execution: {ExecutionId}", executionId);
            return null;
        }
    }

    public async Task<List<WorkflowExecution>> GetIncompleteWorkflowsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _workflowRepo.GetIncompleteAsync(tenantId, cancellationToken);
            var executions = new List<WorkflowExecution>();

            foreach (var entity in entities)
            {
                var execution = new WorkflowExecution
                {
                    ExecutionId = entity.ExecutionId,
                    WorkflowId = entity.WorkflowName, // Storing workflow name in WorkflowId for now
                    Status = Enum.Parse<WorkflowStatus>(entity.Status),
                    Context = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ContextJson ?? "{}") ?? new(),
                    StepExecutions = JsonSerializer.Deserialize<Dictionary<string, StepExecution>>(entity.StepExecutionsJson ?? "{}") ?? new(),
                    StepOutputs = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.StepOutputsJson ?? "{}") ?? new(),
                    ErrorMessage = entity.ErrorMessage,
                    StartedAt = entity.StartedAt,
                    CompletedAt = entity.CompletedAt
                };
                executions.Add(execution);
            }

            _logger.LogInformation("Loaded {Count} incomplete workflows for tenant {TenantId}", executions.Count, tenantId);
            return executions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load incomplete workflows for tenant {TenantId}", tenantId);
            return new List<WorkflowExecution>();
        }
    }

    #endregion

    #region Circuit Breaker State

    public async Task SaveCircuitBreakerStateAsync(
        string agentKey,
        string state,
        int failureCount,
        int successCount,
        DateTime? openedAt,
        DateTime? resetAt,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new CircuitBreakerStateEntity
            {
                AgentKey = agentKey,
                State = state,
                FailureCount = failureCount,
                SuccessCount = successCount,
                OpenedAt = openedAt,
                ResetAt = resetAt,
                LastUpdated = DateTime.UtcNow,
                TenantId = tenantId
            };

            await _circuitBreakerRepo.SaveAsync(entity, cancellationToken);
            _logger.LogDebug("Saved circuit breaker state for {AgentKey}: {State}", agentKey, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save circuit breaker state for {AgentKey}", agentKey);
            throw;
        }
    }

    public async Task<(string state, int failureCount, int successCount, DateTime? openedAt, DateTime? resetAt)?> LoadCircuitBreakerStateAsync(
        string agentKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _circuitBreakerRepo.GetByAgentKeyAsync(agentKey, cancellationToken);
            if (entity == null)
            {
                return null;
            }

            _logger.LogDebug("Loaded circuit breaker state for {AgentKey}: {State}", agentKey, entity.State);
            return (entity.State, entity.FailureCount, entity.SuccessCount, entity.OpenedAt, entity.ResetAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load circuit breaker state for {AgentKey}", agentKey);
            return null;
        }
    }

    public async Task<Dictionary<string, (string state, int failureCount, DateTime? openedAt)>> GetAllCircuitBreakerStatesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _circuitBreakerRepo.GetByTenantAsync(tenantId, cancellationToken);
            var states = entities.ToDictionary(
                e => e.AgentKey,
                e => (e.State, e.FailureCount, e.OpenedAt));

            _logger.LogInformation("Loaded {Count} circuit breaker states for tenant {TenantId}", states.Count, tenantId);
            return states;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load circuit breaker states for tenant {TenantId}", tenantId);
            return new Dictionary<string, (string state, int failureCount, DateTime? openedAt)>();
        }
    }

    #endregion

    #region Routing Decisions

    public async Task SaveRoutingDecisionAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new RoutingDecisionEntity
            {
                TaskId = taskId,
                TaskType = taskType,
                RequiredCapability = requiredCapability,
                SelectedAgent = selectedAgent,
                Priority = priority,
                AgentScore = agentScore,
                Success = success,
                ErrorMessage = errorMessage,
                RetryAttempts = retryAttempts,
                RoutingDurationMs = routingDurationMs,
                RoutedAt = DateTime.UtcNow,
                TenantId = tenantId,
                WorkflowExecutionId = workflowExecutionId,
                WorkflowStepId = workflowStepId
            };

            await _routingDecisionRepo.SaveAsync(entity, cancellationToken);
            _logger.LogDebug("Saved routing decision: Task {TaskId} → Agent {Agent}", taskId, selectedAgent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save routing decision for task {TaskId}", taskId);
            // Don't throw - routing decisions are for audit trail, shouldn't break workflow
        }
    }

    public async Task<List<RoutingDecisionEntity>> GetRoutingDecisionsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        string? taskType = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _routingDecisionRepo.GetByTenantAsync(
                tenantId,
                startDate,
                null,
                taskType,
                null,
                skip,
                take,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get routing decisions for tenant {TenantId}", tenantId);
            return new List<RoutingDecisionEntity>();
        }
    }

    #endregion

    #region Routing Statistics

    public async Task UpdateRoutingStatisticsAsync(
        string agentKey,
        bool success,
        long durationMs,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _routingStatsRepo.GetByAgentKeyAsync(agentKey, cancellationToken);

            var stats = existing ?? new RoutingStatisticsEntity
            {
                AgentKey = agentKey,
                TenantId = tenantId
            };

            stats.TotalTasks++;
            if (success)
            {
                stats.SuccessfulTasks++;
            }
            else
            {
                stats.FailedTasks++;
            }

            stats.TotalDurationMs += durationMs;
            stats.SuccessRate = (double)stats.SuccessfulTasks / stats.TotalTasks;
            stats.AverageDurationMs = (double)stats.TotalDurationMs / stats.TotalTasks;
            stats.LastUpdated = DateTime.UtcNow;

            await _routingStatsRepo.SaveAsync(stats, cancellationToken);
            _logger.LogDebug("Updated routing statistics for {AgentKey}", agentKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update routing statistics for {AgentKey}", agentKey);
            // Don't throw - statistics updates shouldn't break workflow
        }
    }

    public async Task<Dictionary<string, (int total, int success, double avgDurationMs, double successRate)>> GetRoutingStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _routingStatsRepo.GetByTenantAsync(tenantId, cancellationToken);
            return entities.ToDictionary(
                e => e.AgentKey,
                e => (e.TotalTasks, e.SuccessfulTasks, e.AverageDurationMs, e.SuccessRate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get routing statistics for tenant {TenantId}", tenantId);
            return new Dictionary<string, (int total, int success, double avgDurationMs, double successRate)>();
        }
    }

    public async Task RecalculateStatisticsAsync(
        Guid tenantId,
        DateTime? startDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _routingStatsRepo.RecalculateStatisticsAsync(tenantId, startDate, cancellationToken);
            _logger.LogInformation("Recalculated routing statistics for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    #endregion

    #region Recovery Operations

    public async Task RecoverCircuitBreakersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var states = await _circuitBreakerRepo.GetByTenantAsync(tenantId, cancellationToken);
            var now = DateTime.UtcNow;
            var recoveredCount = 0;

            foreach (var state in states.Where(s => s.State == "Open" && s.ResetAt.HasValue && s.ResetAt.Value <= now))
            {
                state.State = "HalfOpen";
                state.SuccessCount = 0;
                state.LastUpdated = now;
                await _circuitBreakerRepo.SaveAsync(state, cancellationToken);
                recoveredCount++;

                _logger.LogInformation("Recovered circuit breaker for {AgentKey} from Open to HalfOpen", state.AgentKey);
            }

            if (recoveredCount > 0)
            {
                _logger.LogInformation("Recovered {Count} circuit breakers for tenant {TenantId}", recoveredCount, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover circuit breakers for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<int> CleanupOldDataAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflowsDeleted = await _workflowRepo.DeleteOldExecutionsAsync(olderThan, cancellationToken);
            var decisionsDeleted = await _routingDecisionRepo.DeleteOldDecisionsAsync(olderThan, cancellationToken);
            var circuitsDeleted = await _circuitBreakerRepo.DeleteOldStatesAsync(olderThan, cancellationToken);

            var totalDeleted = workflowsDeleted + decisionsDeleted + circuitsDeleted;
            _logger.LogInformation("Cleaned up {Total} old records: {Workflows} workflows, {Decisions} decisions, {Circuits} circuits",
                totalDeleted, workflowsDeleted, decisionsDeleted, circuitsDeleted);

            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
            throw;
        }
    }

    #endregion
}
