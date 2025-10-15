using System.Collections.Concurrent;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Shared.Services;

/// <summary>
/// In-memory implementation of ITaskService.
/// In production, this would use message queues (RabbitMQ, Azure Service Bus, etc.).
/// This implementation is suitable for MVP and single-instance deployments.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ILogger<TaskService> _logger;
    private readonly ConcurrentDictionary<string, AgentCard> _registeredAgents = new();
    private readonly ConcurrentDictionary<string, AgentTask> _tasks = new();
    private readonly ConcurrentDictionary<string, Func<AgentTask, Task<AgentTask>>> _agentHandlers = new();

    public TaskService(ILogger<TaskService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RegisterAgentAsync(AgentCard agentCard)
    {
        _registeredAgents[agentCard.AgentId] = agentCard;
        _logger.LogInformation("Registered agent: {AgentName} ({AgentId}) with {SkillCount} skills",
            agentCard.Name, agentCard.AgentId, agentCard.Skills.Count);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UnregisterAgentAsync(string agentId)
    {
        if (_registeredAgents.TryRemove(agentId, out var agent))
        {
            agent.Status = AgentStatus.Inactive;
            _agentHandlers.TryRemove(agentId, out _);
            _logger.LogInformation("Unregistered agent: {AgentName} ({AgentId})",
                agent.Name, agentId);
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null)
    {
        var agents = _registeredAgents.Values.Where(a => a.Status == AgentStatus.Active).AsEnumerable();

        if (!string.IsNullOrEmpty(subject))
        {
            agents = agents.Where(a => a.Subject?.ToString() == subject);
        }

        if (!string.IsNullOrEmpty(skill))
        {
            agents = agents.Where(a => a.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase));
        }

        var result = agents.ToList();
        _logger.LogDebug("Discovered {Count} agents (subject: {Subject}, skill: {Skill})",
            result.Count, subject ?? "any", skill ?? "any");

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<AgentTask> SendTaskAsync(string targetAgentId, AgentTask task)
    {
        task.TargetAgentId = targetAgentId;
        task.Status = AgentTaskStatus.Pending;
        _tasks[task.TaskId] = task;

        _logger.LogInformation("Sending task {TaskId} (type: {TaskType}) to agent {AgentId}",
            task.TaskId, task.Type, targetAgentId);

        // In production, this would publish to a message queue
        // For now, invoke handler directly if registered
        if (_agentHandlers.TryGetValue(targetAgentId, out var handler))
        {
            task.StartedAt = DateTime.UtcNow;
            task.Status = AgentTaskStatus.InProgress;

            try
            {
                var result = await handler(task);
                result.CompletedAt = DateTime.UtcNow;
                _tasks[task.TaskId] = result;

                _logger.LogInformation("Task {TaskId} completed successfully in {Duration}ms",
                    result.TaskId, result.Duration?.TotalMilliseconds ?? 0);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {TaskId}", task.TaskId);
                task.Status = AgentTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.CompletedAt = DateTime.UtcNow;
                _tasks[task.TaskId] = task;
                return task;
            }
        }
        else
        {
            _logger.LogWarning("No handler registered for agent {AgentId}", targetAgentId);
            task.ErrorMessage = $"No handler registered for agent {targetAgentId}";
            task.Status = AgentTaskStatus.Failed;
        }

        return task;
    }    /// <inheritdoc />
    public async Task<AgentTask> RouteByCapabilityAsync(string skill, AgentTask task)
    {
        var agents = await DiscoverAgentsAsync(skill: skill);

        if (!agents.Any())
        {
            var errorMsg = $"No agent found with skill: {skill}";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // Simple routing: pick first available agent
        // In production, implement load balancing, health checks, etc.
        var selectedAgent = agents.First(a => a.Status == AgentStatus.Active);

        _logger.LogInformation("Routing task {TaskId} to agent {AgentName} with skill {Skill}",
            task.TaskId, selectedAgent.Name, skill);

        return await SendTaskAsync(selectedAgent.AgentId, task);
    }

    /// <inheritdoc />
    public async Task<AgentTask?> GetTaskStatusAsync(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return await Task.FromResult(task);
    }

    /// <summary>
    /// Internal method for agents to register their task handlers.
    /// This is a simplification for the MVP - in production, use message queues.
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="handler">Function that processes tasks</param>
    public void RegisterHandler(string agentId, Func<AgentTask, Task<AgentTask>> handler)
    {
        _agentHandlers[agentId] = handler;
        _logger.LogInformation("Registered task handler for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Get statistics about the task service (for monitoring/debugging).
    /// </summary>
    public TaskServiceStats GetStats()
    {
        var completedTasks = _tasks.Values.Count(t => t.Status == AgentTaskStatus.Completed);
        var failedTasks = _tasks.Values.Count(t => t.Status == AgentTaskStatus.Failed);
        var pendingTasks = _tasks.Values.Count(t => t.Status == AgentTaskStatus.Pending);
        var inProgressTasks = _tasks.Values.Count(t => t.Status == AgentTaskStatus.InProgress);

        return new TaskServiceStats
        {
            RegisteredAgents = _registeredAgents.Count,
            ActiveAgents = _registeredAgents.Values.Count(a => a.Status == AgentStatus.Active),
            TotalTasks = _tasks.Count,
            CompletedTasks = completedTasks,
            FailedTasks = failedTasks,
            PendingTasks = pendingTasks,
            InProgressTasks = inProgressTasks
        };
    }
}

/// <summary>
/// Statistics about the task service.
/// </summary>
public class TaskServiceStats
{
    public int RegisteredAgents { get; set; }
    public int ActiveAgents { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
}
