using AcademicAssessment.Agents.Shared.Models;

namespace AcademicAssessment.Orchestration.Models;

/// <summary>
/// Defines a multi-step workflow that chains tasks across multiple agents.
/// Example: Generate Assessment -> Review Quality -> Auto-Grade Responses
/// </summary>
public class WorkflowDefinition
{
    /// <summary>
    /// Unique identifier for this workflow definition.
    /// </summary>
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name of the workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this workflow accomplishes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of steps to execute in sequence.
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// Maximum time allowed for entire workflow execution.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to continue workflow if a non-critical step fails.
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents a single step in a workflow.
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Unique identifier for this step within the workflow.
    /// </summary>
    public string StepId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name of this step.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task type to execute (e.g., "GENERATE_ASSESSMENT", "REVIEW_CONTENT").
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// Required agent capability for this step (e.g., "Mathematics", "content_review").
    /// </summary>
    public string? RequiredCapability { get; set; }

    /// <summary>
    /// Data to pass to the task. Can reference outputs from previous steps using ${stepId.field} syntax.
    /// </summary>
    public Dictionary<string, object> TaskData { get; set; } = new();

    /// <summary>
    /// IDs of steps that must complete before this step can start.
    /// Empty list means this step can run immediately or in parallel with others.
    /// </summary>
    public List<string> DependsOn { get; set; } = new();

    /// <summary>
    /// Whether this step can be skipped if its dependencies fail.
    /// </summary>
    public bool Optional { get; set; } = false;

    /// <summary>
    /// Maximum execution time for this step.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Number of retry attempts if step fails.
    /// </summary>
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Represents the runtime state of a workflow execution.
/// </summary>
public class WorkflowExecution
{
    /// <summary>
    /// Unique identifier for this workflow execution instance.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Reference to the workflow definition being executed.
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the workflow execution.
    /// </summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;

    /// <summary>
    /// When this execution started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this execution completed (or failed/cancelled).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// State of each step in the workflow.
    /// </summary>
    public Dictionary<string, StepExecution> StepExecutions { get; set; } = new();

    /// <summary>
    /// Outputs from completed steps. Key is stepId, value is the task result data.
    /// </summary>
    public Dictionary<string, object> StepOutputs { get; set; } = new();

    /// <summary>
    /// Error message if workflow failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Context data available to all steps.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Represents the execution state of a single workflow step.
/// </summary>
public class StepExecution
{
    /// <summary>
    /// ID of the step being executed.
    /// </summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of this step.
    /// </summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;

    /// <summary>
    /// Task that was submitted for this step.
    /// </summary>
    public AgentTask? Task { get; set; }

    /// <summary>
    /// Agent that was assigned to execute this step.
    /// </summary>
    public AgentCard? AssignedAgent { get; set; }

    /// <summary>
    /// When this step started executing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When this step completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of retry attempts so far.
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Error message if step failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of a workflow or workflow step.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow/step is waiting to start.
    /// </summary>
    Pending,

    /// <summary>
    /// Workflow/step is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Workflow/step completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow/step failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow/step was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Workflow/step is waiting for dependencies.
    /// </summary>
    Blocked
}
