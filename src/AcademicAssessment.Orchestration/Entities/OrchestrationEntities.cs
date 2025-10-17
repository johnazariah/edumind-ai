using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcademicAssessment.Orchestration.Entities;

/// <summary>
/// Database entity for persisting workflow execution state.
/// Enables recovery and monitoring of long-running workflows.
/// </summary>
[Table("workflow_executions")]
public class WorkflowExecutionEntity
{
    /// <summary>
    /// Unique identifier for this workflow execution instance.
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the workflow being executed.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the workflow execution.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized workflow definition.
    /// </summary>
    [Required]
    public string WorkflowDefinitionJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized execution context (input parameters).
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// JSON-serialized step executions dictionary.
    /// Key: StepId, Value: StepExecution state
    /// </summary>
    public string? StepExecutionsJson { get; set; }

    /// <summary>
    /// JSON-serialized step outputs dictionary.
    /// Key: StepId, Value: Output data
    /// </summary>
    public string? StepOutputsJson { get; set; }

    /// <summary>
    /// Error message if workflow failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the workflow execution started.
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the workflow execution completed (null if still running).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Tenant/School ID for multi-tenancy support.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// User who initiated the workflow.
    /// </summary>
    [MaxLength(100)]
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Tags for filtering and categorization.
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Version tracking for optimistic concurrency.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Database entity for persisting circuit breaker state.
/// Enables recovery of circuit breaker status after restarts.
/// </summary>
[Table("circuit_breaker_states")]
public class CircuitBreakerStateEntity
{
    /// <summary>
    /// Agent ID or capability identifier.
    /// </summary>
    [Key]
    [MaxLength(200)]
    public string AgentKey { get; set; } = string.Empty;

    /// <summary>
    /// Current state: Closed, Open, HalfOpen
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string State { get; set; } = "Closed";

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Number of consecutive successes (for HalfOpen state).
    /// </summary>
    public int SuccessCount { get; set; } = 0;

    /// <summary>
    /// When the circuit was opened (null if not open).
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// When the circuit should transition to HalfOpen.
    /// </summary>
    public DateTime? ResetAt { get; set; }

    /// <summary>
    /// Last time this circuit breaker was updated.
    /// </summary>
    [Required]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Tenant/School ID for multi-tenancy support.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Version tracking for optimistic concurrency.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Database entity for audit trail of routing decisions.
/// Enables analysis of agent selection and performance debugging.
/// </summary>
[Table("routing_decisions")]
public class RoutingDecisionEntity
{
    /// <summary>
    /// Unique identifier for this routing decision.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Task identifier that was routed.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Type of task being routed.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// Required capability for the task.
    /// </summary>
    [MaxLength(100)]
    public string? RequiredCapability { get; set; }

    /// <summary>
    /// Agent that was selected for this task.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string SelectedAgent { get; set; } = string.Empty;

    /// <summary>
    /// Priority level assigned to this task (0-10).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Score calculated for the selected agent.
    /// </summary>
    public double AgentScore { get; set; }

    /// <summary>
    /// JSON-serialized list of all candidate agents considered.
    /// </summary>
    public string? CandidateAgentsJson { get; set; }

    /// <summary>
    /// Whether routing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if routing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts needed.
    /// </summary>
    public int RetryAttempts { get; set; } = 0;

    /// <summary>
    /// Time taken to route the task (milliseconds).
    /// </summary>
    public long RoutingDurationMs { get; set; }

    /// <summary>
    /// When this routing decision was made.
    /// </summary>
    [Required]
    public DateTime RoutedAt { get; set; }

    /// <summary>
    /// Tenant/School ID for multi-tenancy support.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Workflow execution ID if this task is part of a workflow.
    /// </summary>
    [MaxLength(100)]
    public string? WorkflowExecutionId { get; set; }

    /// <summary>
    /// Workflow step ID if this task is part of a workflow.
    /// </summary>
    [MaxLength(100)]
    public string? WorkflowStepId { get; set; }
}

/// <summary>
/// Database entity for persisting routing statistics.
/// Enables dashboard metrics and performance monitoring.
/// </summary>
[Table("routing_statistics")]
public class RoutingStatisticsEntity
{
    /// <summary>
    /// Unique identifier (Agent ID or capability).
    /// </summary>
    [Key]
    [MaxLength(200)]
    public string AgentKey { get; set; } = string.Empty;

    /// <summary>
    /// Total number of tasks routed to this agent.
    /// </summary>
    public int TotalTasks { get; set; } = 0;

    /// <summary>
    /// Number of successful task completions.
    /// </summary>
    public int SuccessfulTasks { get; set; } = 0;

    /// <summary>
    /// Number of failed tasks.
    /// </summary>
    public int FailedTasks { get; set; } = 0;

    /// <summary>
    /// Total duration of all tasks (milliseconds).
    /// </summary>
    public long TotalDurationMs { get; set; } = 0;

    /// <summary>
    /// Success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;

    /// <summary>
    /// Average task duration (milliseconds).
    /// </summary>
    public double AverageDurationMs { get; set; } = 0.0;

    /// <summary>
    /// Last time these statistics were updated.
    /// </summary>
    [Required]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Tenant/School ID for multi-tenancy support.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Version tracking for optimistic concurrency.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
