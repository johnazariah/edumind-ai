using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Agents.Shared.Models;

/// <summary>
/// Metadata describing an agent's capabilities and identity.
/// Used for agent discovery and routing in the A2A protocol.
/// </summary>
public class AgentCard
{
    /// <summary>
    /// Unique identifier for this agent instance.
    /// </summary>
    public string AgentId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name of the agent (e.g., "MathematicsAssessmentAgent").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of what this agent does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Version of the agent implementation (semantic versioning).
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Academic subject this agent specializes in (null for orchestrator).
    /// </summary>
    public Subject? Subject { get; set; }

    /// <summary>
    /// List of skills/capabilities this agent provides.
    /// Examples: "algebra", "geometry", "generate_assessment", "evaluate_response"
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Grade levels this agent can handle.
    /// </summary>
    public List<GradeLevel> SupportedGradeLevels { get; set; } = new();

    /// <summary>
    /// Additional capabilities as key-value pairs.
    /// Examples: {"max_questions": 30, "supports_adaptive": true}
    /// </summary>
    public Dictionary<string, object> Capabilities { get; set; } = new();

    /// <summary>
    /// When this agent was registered with the system.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current operational status of the agent.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Active;
}

/// <summary>
/// Operational status of an agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is ready to process tasks.
    /// </summary>
    Active,

    /// <summary>
    /// Agent is temporarily unavailable.
    /// </summary>
    Inactive,

    /// <summary>
    /// Agent is currently processing a task.
    /// </summary>
    Busy,

    /// <summary>
    /// Agent encountered an error and may need attention.
    /// </summary>
    Error
}
