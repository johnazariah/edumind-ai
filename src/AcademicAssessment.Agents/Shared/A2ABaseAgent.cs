using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Shared;

/// <summary>
/// Abstract base class for all agents in the A2A protocol.
/// Provides common functionality for task processing, registration, and communication.
/// All subject agents and the orchestrator inherit from this class.
/// </summary>
public abstract class A2ABaseAgent
{
    protected readonly ITaskService TaskService;
    protected readonly ILogger Logger;
    protected readonly HubConnection? HubConnection;

    /// <summary>
    /// Metadata describing this agent's capabilities.
    /// </summary>
    public AgentCard AgentCard { get; protected set; }

    protected A2ABaseAgent(
        AgentCard agentCard,
        ITaskService taskService,
        ILogger logger,
        string? hubUrl = null)
    {
        AgentCard = agentCard ?? throw new ArgumentNullException(nameof(agentCard));
        TaskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Optional SignalR connection for real-time updates
        if (!string.IsNullOrEmpty(hubUrl))
        {
            HubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();
        }
    }

    /// <summary>
    /// Initialize and register the agent with the task service.
    /// Must be called before the agent can process tasks.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await TaskService.RegisterAgentAsync(AgentCard);

        if (HubConnection != null)
        {
            try
            {
                await HubConnection.StartAsync();
                Logger.LogInformation("Agent {AgentName} connected to SignalR hub", AgentCard.Name);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to connect to SignalR hub for agent {AgentName}. Continuing without real-time updates.", AgentCard.Name);
            }
        }

        Logger.LogInformation("Agent {AgentName} ({AgentId}) initialized successfully with {SkillCount} skills",
            AgentCard.Name, AgentCard.AgentId, AgentCard.Skills.Count);
    }

    /// <summary>
    /// Shutdown the agent and clean up resources.
    /// </summary>
    public virtual async Task ShutdownAsync()
    {
        await TaskService.UnregisterAgentAsync(AgentCard.AgentId);

        if (HubConnection != null)
        {
            try
            {
                await HubConnection.StopAsync();
                await HubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error while disconnecting from SignalR hub");
            }
        }

        Logger.LogInformation("Agent {AgentName} shutdown complete", AgentCard.Name);
    }

    /// <summary>
    /// Main task processing method - must be implemented by derived agents.
    /// This is where agent-specific logic goes.
    /// </summary>
    /// <param name="task">The task to process</param>
    /// <returns>The completed task with results</returns>
    protected abstract Task<AgentTask> ProcessTaskAsync(AgentTask task);

    /// <summary>
    /// Public entry point for task execution.
    /// Wraps ProcessTaskAsync with common logic (logging, error handling, progress updates).
    /// </summary>
    public async Task<AgentTask> ExecuteTaskAsync(AgentTask task)
    {
        task.StartedAt = DateTime.UtcNow;
        task.Status = AgentTaskStatus.InProgress;

        try
        {
            Logger.LogInformation("Agent {AgentName} processing task {TaskId} of type '{TaskType}'",
                AgentCard.Name, task.TaskId, task.Type);

            await BroadcastProgressAsync($"Agent {AgentCard.Name} started task {task.Type}", task.TaskId);

            var result = await ProcessTaskAsync(task);

            result.Status = AgentTaskStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;

            Logger.LogInformation("Agent {AgentName} completed task {TaskId} in {Duration}ms",
                AgentCard.Name, task.TaskId, result.Duration?.TotalMilliseconds ?? 0);

            await BroadcastProgressAsync($"Agent {AgentCard.Name} completed task {task.Type}", task.TaskId, result);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing task {TaskId} in agent {AgentName}",
                task.TaskId, AgentCard.Name);

            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.UtcNow;

            await BroadcastProgressAsync($"Agent {AgentCard.Name} failed task {task.Type}: {ex.Message}", task.TaskId);

            return task;
        }
    }

    /// <summary>
    /// Broadcast progress update via SignalR.
    /// Sends updates to all connected clients (dashboards, student UIs, etc.).
    /// </summary>
    /// <param name="message">Progress message</param>
    /// <param name="taskId">Related task ID (optional)</param>
    /// <param name="result">Task result (optional)</param>
    protected virtual async Task BroadcastProgressAsync(string message, string? taskId = null, AgentTask? result = null)
    {
        if (HubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await HubConnection.SendAsync("AgentProgress", new
                {
                    AgentId = AgentCard.AgentId,
                    AgentName = AgentCard.Name,
                    Message = message,
                    TaskId = taskId,
                    Timestamp = DateTime.UtcNow,
                    Result = result?.Result
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to broadcast progress update");
            }
        }
    }

    /// <summary>
    /// Send a task to another agent via the task service.
    /// Enables agent-to-agent communication.
    /// </summary>
    /// <param name="targetAgentId">ID of the agent to send to</param>
    /// <param name="task">The task to send</param>
    /// <returns>The task response</returns>
    protected async Task<AgentTask> SendTaskToAgentAsync(string targetAgentId, AgentTask task)
    {
        task.SourceAgentId = AgentCard.AgentId;
        Logger.LogDebug("Agent {AgentName} sending task {TaskId} to agent {TargetId}",
            AgentCard.Name, task.TaskId, targetAgentId);
        return await TaskService.SendTaskAsync(targetAgentId, task);
    }

    /// <summary>
    /// Send a task to any agent with a specific skill.
    /// Uses agent discovery to find capable agents.
    /// </summary>
    /// <param name="skill">Required skill</param>
    /// <param name="task">The task to send</param>
    /// <returns>The task response</returns>
    protected async Task<AgentTask> SendTaskBySkillAsync(string skill, AgentTask task)
    {
        task.SourceAgentId = AgentCard.AgentId;
        Logger.LogDebug("Agent {AgentName} routing task {TaskId} by skill '{Skill}'",
            AgentCard.Name, task.TaskId, skill);
        return await TaskService.RouteByCapabilityAsync(skill, task);
    }

    /// <summary>
    /// Discover agents with specific capabilities.
    /// Useful for finding subject agents or other specialized agents.
    /// </summary>
    /// <param name="subject">Filter by subject (optional)</param>
    /// <param name="skill">Filter by skill (optional)</param>
    /// <returns>List of matching agent cards</returns>
    protected async Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null)
    {
        var agents = await TaskService.DiscoverAgentsAsync(subject, skill);
        Logger.LogDebug("Agent {AgentName} discovered {Count} agents (subject: {Subject}, skill: {Skill})",
            AgentCard.Name, agents.Count, subject ?? "any", skill ?? "any");
        return agents;
    }

    /// <summary>
    /// Update agent status (e.g., mark as busy during long-running operations).
    /// </summary>
    protected void UpdateStatus(AgentStatus status)
    {
        AgentCard.Status = status;
        Logger.LogDebug("Agent {AgentName} status changed to {Status}", AgentCard.Name, status);
    }
}
