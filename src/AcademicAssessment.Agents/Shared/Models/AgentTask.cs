using System.Text.Json;

namespace AcademicAssessment.Agents.Shared.Models;

/// <summary>
/// Represents a task sent between agents in the A2A protocol.
/// Tasks are the primary communication mechanism between agents.
/// </summary>
public class AgentTask
{
    /// <summary>
    /// Unique identifier for this task.
    /// </summary>
    public string TaskId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of task to perform.
    /// Examples: "assess_student", "generate_assessment", "evaluate_response", "analyze_progress"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// ID of the agent that created this task.
    /// </summary>
    public string SourceAgentId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the agent this task is assigned to.
    /// </summary>
    public string TargetAgentId { get; set; } = string.Empty;

    /// <summary>
    /// Task-specific payload data.
    /// Structure depends on task type.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;

    /// <summary>
    /// Result data after task completion.
    /// Structure depends on task type.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error message if task failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the task started processing (null if not started).
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the task completed (null if not completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Additional metadata for the task.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Serialized JSON representation of Data property.
    /// </summary>
    public string DataJson
    {
        get => Data != null ? JsonSerializer.Serialize(Data) : "{}";
        set => Data = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<object>(value);
    }

    /// <summary>
    /// Serialized JSON representation of Result property.
    /// </summary>
    public string ResultJson
    {
        get => Result != null ? JsonSerializer.Serialize(Result) : "{}";
        set => Result = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<object>(value);
    }

    /// <summary>
    /// Calculate task duration if completed.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
}

/// <summary>
/// Status of an agent task.
/// </summary>
public enum AgentTaskStatus
{
    /// <summary>
    /// Task has been created but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// Task is currently being processed.
    /// </summary>
    InProgress,

    /// <summary>
    /// Task completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Task failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Task was cancelled before completion.
    /// </summary>
    Cancelled
}
