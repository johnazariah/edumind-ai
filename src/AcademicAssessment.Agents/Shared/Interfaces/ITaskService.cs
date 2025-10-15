using AcademicAssessment.Agents.Shared.Models;

namespace AcademicAssessment.Agents.Shared.Interfaces;

/// <summary>
/// Service for routing tasks between agents in the A2A protocol.
/// In production, this would be implemented using message queues (RabbitMQ, Azure Service Bus, etc.).
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Send a task to a specific agent by ID.
    /// </summary>
    /// <param name="targetAgentId">ID of the agent to send the task to</param>
    /// <param name="task">The task to send</param>
    /// <returns>The task with updated status</returns>
    Task<AgentTask> SendTaskAsync(string targetAgentId, AgentTask task);

    /// <summary>
    /// Send a task to any agent capable of handling it (by skill/subject).
    /// Uses agent discovery to find suitable agent.
    /// </summary>
    /// <param name="skill">Required skill name</param>
    /// <param name="task">The task to send</param>
    /// <returns>The task routed to a capable agent</returns>
    Task<AgentTask> RouteByCapabilityAsync(string skill, AgentTask task);

    /// <summary>
    /// Get task status by ID.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <returns>Task if found, null otherwise</returns>
    Task<AgentTask?> GetTaskStatusAsync(string taskId);

    /// <summary>
    /// Register an agent with its AgentCard.
    /// Makes the agent discoverable by other agents.
    /// </summary>
    /// <param name="agentCard">Agent metadata</param>
    Task RegisterAgentAsync(AgentCard agentCard);

    /// <summary>
    /// Discover agents by subject or skill.
    /// </summary>
    /// <param name="subject">Filter by subject (optional)</param>
    /// <param name="skill">Filter by skill (optional)</param>
    /// <returns>List of matching agents</returns>
    Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null);

    /// <summary>
    /// Unregister an agent (e.g., on shutdown).
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    Task UnregisterAgentAsync(string agentId);
}
