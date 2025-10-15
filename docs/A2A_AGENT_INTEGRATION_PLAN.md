# A2A Multi-Agent Integration Plan - EduMind.AI

## Agent-to-Agent Protocol Architecture

## Executive Summary

This document outlines the implementation of an **Agent-to-Agent (A2A) protocol-based multi-agent system** for the EduMind.AI academic assessment platform. This is NOT a simple LLM integration - it's a sophisticated agent orchestration architecture where specialized subject agents communicate via structured tasks with a central orchestrator.

### Key Architecture Principles

1. **All agents inherit from A2ABaseAgent** - Standard base class with task processing
2. **Task-based communication** - Agents don't call each other directly, they send/receive AgentTask objects
3. **Agent discovery via AgentCard** - Each agent advertises capabilities, skills, and metadata
4. **Central orchestrator pattern** - StudentProgressOrchestrator coordinates all subject agents
5. **Real-time updates via SignalR** - Progress tracking and notifications
6. **LLMs are tools WITHIN agents** - Not the architecture itself

### System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          Web API Layer                           │
│  AssessmentController → StudentProgressOrchestrator             │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │ StudentProgressOrchestrator   │
        │   : A2ABaseAgent              │
        │                               │
        │ - ProcessTaskAsync()          │
        │ - DetermineNextSubject()      │
        │ - CalculatePriority()         │
        └───────────────┬───────────────┘
                        │
          ┌─────────────┴─────────────┐
          │      ITaskService         │
          │   (Message Routing)       │
          └─────────────┬─────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Mathematics  │ │   Physics    │ │   English    │
│ Agent        │ │   Agent      │ │   Agent      │
│ :A2ABase     │ │ :A2ABase     │ │ :A2ABase     │
│              │ │              │ │              │
│ Generate     │ │ Generate     │ │ Generate     │
│ Evaluate     │ │ Evaluate     │ │ Evaluate     │
│ AgentCard    │ │ AgentCard    │ │ AgentCard    │
└──────────────┘ └──────────────┘ └──────────────┘
        │               │               │
        └───────────────┴───────────────┘
                        │
                        ▼
            ┌──────────────────────┐
            │    SignalR Hub       │
            │  (Progress Updates)  │
            └──────────────────────┘
```

### Task Flow Example

```csharp
// 1. API receives assessment request
POST /api/assessments/student/123/start

// 2. Controller creates task for orchestrator
var task = new AgentTask
{
    Type = "assess_student",
    Data = new { StudentId = "123", Subject = "Auto" },
    Status = TaskStatus.Pending
};

// 3. Orchestrator determines which subject needs assessment
var orchestrator = new StudentProgressOrchestrator(...);
var result = await orchestrator.ProcessTaskAsync(task);

// 4. Orchestrator creates task for subject agent
var mathTask = new AgentTask
{
    Type = "generate_assessment",
    Data = new { StudentId = "123", Subject = "Mathematics", QuestionCount = 10 },
    Status = TaskStatus.Pending
};

// 5. TaskService routes to Mathematics agent
await _taskService.SendTaskAsync("MathematicsAgent", mathTask);

// 6. Math agent generates questions
var mathAgent = new MathematicsAssessmentAgent(...);
var assessment = await mathAgent.ProcessTaskAsync(mathTask);

// 7. Result returned to orchestrator
mathTask.Status = TaskStatus.Completed;
mathTask.Result = assessment;

// 8. Orchestrator broadcasts progress via SignalR
await _hubContext.Clients.Group($"student-123")
    .SendAsync("AssessmentReady", assessment);
```

## Current System State

### ✅ Completed Infrastructure (Task 3)

- Complete database schema with Entity Framework Core
- 24 demo students across 3 schools and 6 courses
- 171 questions covering 5 subjects (Math, Physics, Chemistry, Biology, English)
- 89 student assessments with 1,179 responses
- Realistic score distributions and analytics
- Full repository pattern with proper relationships

### 📦 Empty Agent Projects (Ready for Implementation)

- `src/AcademicAssessment.Agents/` - Structure exists, no code
  - Folders: Mathematics/, Physics/, Chemistry/, Biology/, English/, **Shared/**
  - Only references Core project, needs SignalR and other packages
- `src/AcademicAssessment.Orchestration/` - Structure exists, no code
  - Will contain StudentProgressOrchestrator

### 🛠️ Technology Stack

- .NET 8 with ASP.NET Core Web API
- Entity Framework Core with PostgreSQL
- SignalR for real-time agent communication
- Blazor Server for dashboards
- ML.NET for adaptive testing (IRT models)
- Azure OpenAI GPT-4o (content generation within agents)

## Phase 1: A2A Protocol Foundation (Week 1)

### 1.1 Add Required NuGet Packages

**Agents Project:**

```bash
cd /workspaces/edumind-ai

# Core A2A infrastructure
dotnet add src/AcademicAssessment.Agents package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet add src/AcademicAssessment.Agents package System.Text.Json --version 8.0.0
dotnet add src/AcademicAssessment.Agents package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add src/AcademicAssessment.Agents package Microsoft.Extensions.Logging --version 8.0.0

# LLM packages (for Phase 4)
# dotnet add src/AcademicAssessment.Agents package Azure.AI.OpenAI --version 1.0.0-beta.12
# dotnet add src/AcademicAssessment.Agents package Azure.Identity --version 1.10.0
```

**Orchestration Project:**

```bash
dotnet add src/AcademicAssessment.Orchestration package Microsoft.AspNetCore.SignalR --version 8.0.0
```

### 1.2 Create A2A Base Infrastructure

**File: `src/AcademicAssessment.Agents/Shared/Models/AgentCard.cs`**

```csharp
using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Agents.Shared.Models;

/// <summary>
/// Metadata describing an agent's capabilities and identity.
/// Used for agent discovery and routing.
/// </summary>
public class AgentCard
{
    public string AgentId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public Subject? Subject { get; set; } // Null for orchestrator
    public List<string> Skills { get; set; } = new();
    public List<GradeLevel> SupportedGradeLevels { get; set; } = new();
    public Dictionary<string, object> Capabilities { get; set; } = new();
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public AgentStatus Status { get; set; } = AgentStatus.Active;
}

public enum AgentStatus
{
    Active,
    Inactive,
    Busy,
    Error
}
```

**File: `src/AcademicAssessment.Agents/Shared/Models/AgentTask.cs`**

```csharp
using System.Text.Json;

namespace AcademicAssessment.Agents.Shared.Models;

/// <summary>
/// Represents a task sent between agents in the A2A protocol.
/// </summary>
public class AgentTask
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // "assess_student", "generate_assessment", etc.
    public string SourceAgentId { get; set; } = string.Empty;
    public string TargetAgentId { get; set; } = string.Empty;
    public object? Data { get; set; } // Task-specific payload
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public object? Result { get; set; } // Task result after completion
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public string DataJson
    {
        get => Data != null ? JsonSerializer.Serialize(Data) : "{}";
        set => Data = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<object>(value);
    }

    public string ResultJson
    {
        get => Result != null ? JsonSerializer.Serialize(Result) : "{}";
        set => Result = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<object>(value);
    }
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
```

**File: `src/AcademicAssessment.Agents/Shared/Interfaces/ITaskService.cs`**

```csharp
using AcademicAssessment.Agents.Shared.Models;

namespace AcademicAssessment.Agents.Shared.Interfaces;

/// <summary>
/// Service for routing tasks between agents in the A2A protocol.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Send a task to a specific agent by ID.
    /// </summary>
    Task<AgentTask> SendTaskAsync(string targetAgentId, AgentTask task);

    /// <summary>
    /// Send a task to any agent capable of handling it (by skill/subject).
    /// </summary>
    Task<AgentTask> RouteByCabilityAsync(string skill, AgentTask task);

    /// <summary>
    /// Get task status by ID.
    /// </summary>
    Task<AgentTask?> GetTaskStatusAsync(string taskId);

    /// <summary>
    /// Register an agent with its AgentCard.
    /// </summary>
    Task RegisterAgentAsync(AgentCard agentCard);

    /// <summary>
    /// Discover agents by subject or skill.
    /// </summary>
    Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null);
}
```

**File: `src/AcademicAssessment.Agents/Shared/Services/TaskService.cs`**

```csharp
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AcademicAssessment.Agents.Shared.Services;

/// <summary>
/// In-memory implementation of ITaskService.
/// In production, this would use message queues (RabbitMQ, Azure Service Bus, etc.).
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

    public async Task RegisterAgentAsync(AgentCard agentCard)
    {
        _registeredAgents[agentCard.AgentId] = agentCard;
        _logger.LogInformation("Registered agent: {AgentName} ({AgentId})", 
            agentCard.Name, agentCard.AgentId);
        await Task.CompletedTask;
    }

    public async Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null)
    {
        var agents = _registeredAgents.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(subject))
        {
            agents = agents.Where(a => a.Subject?.ToString() == subject);
        }

        if (!string.IsNullOrEmpty(skill))
        {
            agents = agents.Where(a => a.Skills.Contains(skill));
        }

        return await Task.FromResult(agents.ToList());
    }

    public async Task<AgentTask> SendTaskAsync(string targetAgentId, AgentTask task)
    {
        task.TargetAgentId = targetAgentId;
        task.Status = TaskStatus.Pending;
        _tasks[task.TaskId] = task;

        _logger.LogInformation("Sending task {TaskId} to agent {AgentId}", 
            task.TaskId, targetAgentId);

        // In production, this would publish to a message queue
        // For now, invoke handler directly if registered
        if (_agentHandlers.TryGetValue(targetAgentId, out var handler))
        {
            task.StartedAt = DateTime.UtcNow;
            task.Status = TaskStatus.InProgress;
            
            try
            {
                var result = await handler(task);
                result.CompletedAt = DateTime.UtcNow;
                _tasks[task.TaskId] = result;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {TaskId}", task.TaskId);
                task.Status = TaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                return task;
            }
        }

        return task;
    }

    public async Task<AgentTask> RouteByCabilityAsync(string skill, AgentTask task)
    {
        var agents = await DiscoverAgentsAsync(skill: skill);
        if (!agents.Any())
        {
            throw new InvalidOperationException($"No agent found with skill: {skill}");
        }

        // Simple routing: pick first available agent
        // In production, implement load balancing, health checks, etc.
        var selectedAgent = agents.First(a => a.Status == AgentStatus.Active);
        return await SendTaskAsync(selectedAgent.AgentId, task);
    }

    public async Task<AgentTask?> GetTaskStatusAsync(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return await Task.FromResult(task);
    }

    /// <summary>
    /// Internal method for agents to register their task handlers.
    /// This is a simplification - in production, use message queues.
    /// </summary>
    public void RegisterHandler(string agentId, Func<AgentTask, Task<AgentTask>> handler)
    {
        _agentHandlers[agentId] = handler;
        _logger.LogInformation("Registered handler for agent {AgentId}", agentId);
    }
}
```

**File: `src/AcademicAssessment.Agents/Shared/A2ABaseAgent.cs`**

```csharp
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Shared;

/// <summary>
/// Abstract base class for all agents in the A2A protocol.
/// Provides common functionality for task processing, registration, and communication.
/// </summary>
public abstract class A2ABaseAgent
{
    protected readonly ITaskService TaskService;
    protected readonly ILogger Logger;
    protected readonly HubConnection? HubConnection;
    public AgentCard AgentCard { get; protected set; }

    protected A2ABaseAgent(
        AgentCard agentCard,
        ITaskService taskService,
        ILogger logger,
        string? hubUrl = null)
    {
        AgentCard = agentCard;
        TaskService = taskService;
        Logger = logger;

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
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await TaskService.RegisterAgentAsync(AgentCard);
        
        if (HubConnection != null)
        {
            await HubConnection.StartAsync();
            Logger.LogInformation("Agent {AgentName} connected to SignalR hub", AgentCard.Name);
        }

        Logger.LogInformation("Agent {AgentName} initialized successfully", AgentCard.Name);
    }

    /// <summary>
    /// Main task processing method - must be implemented by derived agents.
    /// </summary>
    /// <param name="task">The task to process</param>
    /// <returns>The completed task with results</returns>
    protected abstract Task<AgentTask> ProcessTaskAsync(AgentTask task);

    /// <summary>
    /// Public entry point for task execution.
    /// </summary>
    public async Task<AgentTask> ExecuteTaskAsync(AgentTask task)
    {
        task.StartedAt = DateTime.UtcNow;
        task.Status = TaskStatus.InProgress;

        try
        {
            Logger.LogInformation("Agent {AgentName} processing task {TaskId} of type {TaskType}",
                AgentCard.Name, task.TaskId, task.Type);

            await BroadcastProgressAsync($"Agent {AgentCard.Name} started task {task.Type}");

            var result = await ProcessTaskAsync(task);
            
            result.Status = TaskStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;

            await BroadcastProgressAsync($"Agent {AgentCard.Name} completed task {task.Type}");

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing task {TaskId} in agent {AgentName}", 
                task.TaskId, AgentCard.Name);

            task.Status = TaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.UtcNow;

            await BroadcastProgressAsync($"Agent {AgentCard.Name} failed task {task.Type}: {ex.Message}");

            return task;
        }
    }

    /// <summary>
    /// Broadcast progress update via SignalR.
    /// </summary>
    protected virtual async Task BroadcastProgressAsync(string message)
    {
        if (HubConnection?.State == HubConnectionState.Connected)
        {
            await HubConnection.SendAsync("AgentProgress", new
            {
                AgentId = AgentCard.AgentId,
                AgentName = AgentCard.Name,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Send a task to another agent.
    /// </summary>
    protected async Task<AgentTask> SendTaskToAgentAsync(string targetAgentId, AgentTask task)
    {
        task.SourceAgentId = AgentCard.AgentId;
        return await TaskService.SendTaskAsync(targetAgentId, task);
    }

    /// <summary>
    /// Discover agents with specific capabilities.
    /// </summary>
    protected async Task<List<AgentCard>> DiscoverAgentsAsync(string? subject = null, string? skill = null)
    {
        return await TaskService.DiscoverAgentsAsync(subject, skill);
    }
}
```

### 1.3 Create SignalR Hub for Agent Communication

**File: `src/AcademicAssessment.Web/Hubs/AgentProgressHub.cs`**

```csharp
using Microsoft.AspNetCore.SignalR;

namespace AcademicAssessment.Web.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time agent progress updates.
/// </summary>
public class AgentProgressHub : Hub
{
    private readonly ILogger<AgentProgressHub> _logger;

    public AgentProgressHub(ILogger<AgentProgressHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a student-specific group to receive updates for that student.
    /// </summary>
    public async Task JoinStudentGroup(string studentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"student-{studentId}");
        _logger.LogInformation("Client {ConnectionId} joined student group {StudentId}", 
            Context.ConnectionId, studentId);
    }

    /// <summary>
    /// Leave a student group.
    /// </summary>
    public async Task LeaveStudentGroup(string studentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"student-{studentId}");
    }

    /// <summary>
    /// Broadcast progress update to all connected clients.
    /// Called by agents via hub connection.
    /// </summary>
    public async Task AgentProgress(object progressData)
    {
        await Clients.All.SendAsync("AgentProgress", progressData);
    }

    /// <summary>
    /// Broadcast progress to a specific student's group.
    /// </summary>
    public async Task StudentProgress(string studentId, object progressData)
    {
        await Clients.Group($"student-{studentId}").SendAsync("StudentProgress", progressData);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

### 1.4 Register Services in Program.cs

**Update: `src/AcademicAssessment.Web/Program.cs`**

Add after existing service registrations:

```csharp
// A2A Agent Infrastructure
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSignalR();

// Register SignalR endpoint
app.MapHub<AgentProgressHub>("/hubs/agent-progress");
```

### 1.5 Unit Tests for A2A Infrastructure

**File: `tests/AcademicAssessment.Tests.Unit/Agents/A2ABaseInfrastructureTests.cs`**

```csharp
using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Agents.Shared.Services;
using AcademicAssessment.Core.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AcademicAssessment.Tests.Unit.Agents;

public class A2ABaseInfrastructureTests
{
    [Fact]
    public void AgentCard_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var card = new AgentCard
        {
            Name = "Test Agent",
            Description = "Test Description",
            Subject = Subject.Mathematics,
            Skills = new List<string> { "algebra", "geometry" }
        };

        // Assert
        Assert.NotEmpty(card.AgentId);
        Assert.Equal("1.0.0", card.Version);
        Assert.Equal(AgentStatus.Active, card.Status);
        Assert.True(card.RegisteredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void AgentTask_ShouldInitializeWithPendingStatus()
    {
        // Arrange & Act
        var task = new AgentTask
        {
            Type = "test_task",
            Data = new { TestProperty = "value" }
        };

        // Assert
        Assert.NotEmpty(task.TaskId);
        Assert.Equal(TaskStatus.Pending, task.Status);
        Assert.Null(task.Result);
        Assert.True(task.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task TaskService_ShouldRegisterAgent()
    {
        // Arrange
        var logger = new Mock<ILogger<TaskService>>().Object;
        var taskService = new TaskService(logger);
        var agentCard = new AgentCard
        {
            Name = "Math Agent",
            Subject = Subject.Mathematics
        };

        // Act
        await taskService.RegisterAgentAsync(agentCard);
        var discovered = await taskService.DiscoverAgentsAsync(subject: "Mathematics");

        // Assert
        Assert.Single(discovered);
        Assert.Equal("Math Agent", discovered[0].Name);
    }

    [Fact]
    public async Task TaskService_ShouldDiscoverAgentsBySkill()
    {
        // Arrange
        var logger = new Mock<ILogger<TaskService>>().Object;
        var taskService = new TaskService(logger);
        
        var mathAgent = new AgentCard
        {
            Name = "Math Agent",
            Skills = new List<string> { "algebra", "geometry" }
        };
        
        var physicsAgent = new AgentCard
        {
            Name = "Physics Agent",
            Skills = new List<string> { "mechanics", "thermodynamics" }
        };

        // Act
        await taskService.RegisterAgentAsync(mathAgent);
        await taskService.RegisterAgentAsync(physicsAgent);
        
        var algebraAgents = await taskService.DiscoverAgentsAsync(skill: "algebra");
        var mechanicsAgents = await taskService.DiscoverAgentsAsync(skill: "mechanics");

        // Assert
        Assert.Single(algebraAgents);
        Assert.Equal("Math Agent", algebraAgents[0].Name);
        Assert.Single(mechanicsAgents);
        Assert.Equal("Physics Agent", mechanicsAgents[0].Name);
    }

    // Mock agent for testing
    private class TestAgent : A2ABaseAgent
    {
        public TestAgent(AgentCard card, ITaskService taskService, ILogger logger)
            : base(card, taskService, logger)
        {
        }

        protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
        {
            task.Result = new { Processed = true };
            return await Task.FromResult(task);
        }
    }

    [Fact]
    public async Task A2ABaseAgent_ShouldProcessTaskSuccessfully()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TestAgent>>();
        var taskServiceMock = new Mock<ITaskService>();
        
        var card = new AgentCard { Name = "Test Agent" };
        var agent = new TestAgent(card, taskServiceMock.Object, loggerMock.Object);
        
        var task = new AgentTask
        {
            Type = "test",
            Data = new { Value = 42 }
        };

        // Act
        var result = await agent.ExecuteTaskAsync(task);

        // Assert
        Assert.Equal(TaskStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.CompletedAt);
    }
}
```

## Phase 2: Central Orchestrator (Week 2)

### 2.1 Student Progress Orchestrator

**File: `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`**

```csharp
using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Orchestration;

/// <summary>
/// Central orchestrator responsible for coordinating student assessments
/// across multiple subject agents. Implements A2A protocol.
/// </summary>
public class StudentProgressOrchestrator : A2ABaseAgent
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IHubContext<Web.Hubs.AgentProgressHub> _hubContext;

    public StudentProgressOrchestrator(
        ITaskService taskService,
        IStudentRepository studentRepository,
        IAssessmentRepository assessmentRepository,
        IHubContext<Web.Hubs.AgentProgressHub> hubContext,
        ILogger<StudentProgressOrchestrator> logger)
        : base(CreateAgentCard(), taskService, logger)
    {
        _studentRepository = studentRepository;
        _assessmentRepository = assessmentRepository;
        _hubContext = hubContext;
    }

    private static AgentCard CreateAgentCard()
    {
        return new AgentCard
        {
            Name = "StudentProgressOrchestrator",
            Description = "Central coordinator for student assessment and progress tracking",
            Skills = new List<string>
            {
                "assess_student",
                "analyze_progress",
                "recommend_study_path",
                "schedule_assessments",
                "coordinate_agents"
            },
            Capabilities = new Dictionary<string, object>
            {
                ["max_concurrent_students"] = 100,
                ["supported_subjects"] = new[] { "Mathematics", "Physics", "Chemistry", "Biology", "English" }
            }
        };
    }

    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        return task.Type switch
        {
            "assess_student" => await AssessStudentAsync(task),
            "analyze_progress" => await AnalyzeProgressAsync(task),
            "recommend_study_path" => await RecommendStudyPathAsync(task),
            "schedule_assessments" => await ScheduleAssessmentsAsync(task),
            _ => throw new NotSupportedException($"Task type '{task.Type}' not supported by orchestrator")
        };
    }

    private async Task<AgentTask> AssessStudentAsync(AgentTask task)
    {
        // Extract student ID from task data
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataJson);
        var studentId = Guid.Parse(data["studentId"].ToString());

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            task.ErrorMessage = $"Student {studentId} not found";
            task.Status = TaskStatus.Failed;
            return task;
        }

        await _hubContext.Clients.Group($"student-{studentId}")
            .SendAsync("AssessmentStarting", new { StudentId = studentId, Timestamp = DateTime.UtcNow });

        // Determine which subject needs assessment
        var targetSubject = await DetermineNextAssessmentSubject(student);

        Logger.LogInformation("Orchestrator determined student {StudentId} needs {Subject} assessment", 
            student.Id, targetSubject);

        // Find subject agent
        var agents = await DiscoverAgentsAsync(subject: targetSubject.ToString());
        if (!agents.Any())
        {
            task.ErrorMessage = $"No agent found for subject {targetSubject}";
            task.Status = TaskStatus.Failed;
            return task;
        }

        // Create task for subject agent
        var subjectTask = new AgentTask
        {
            Type = "generate_assessment",
            SourceAgentId = AgentCard.AgentId,
            Data = new
            {
                StudentId = student.Id,
                Subject = targetSubject,
                GradeLevel = student.CurrentGrade,
                QuestionCount = 10,
                AdaptiveDifficulty = true
            }
        };

        // Send to subject agent and await result
        var subjectAgent = agents.First();
        var assessmentResult = await TaskService.SendTaskAsync(subjectAgent.AgentId, subjectTask);

        if (assessmentResult.Status == TaskStatus.Completed)
        {
            await _hubContext.Clients.Group($"student-{studentId}")
                .SendAsync("AssessmentGenerated", assessmentResult.Result);

            task.Result = new
            {
                StudentId = studentId,
                Subject = targetSubject,
                Assessment = assessmentResult.Result,
                GeneratedBy = subjectAgent.Name
            };
        }
        else
        {
            task.ErrorMessage = assessmentResult.ErrorMessage;
            task.Status = TaskStatus.Failed;
        }

        return task;
    }

    private async Task<AgentTask> AnalyzeProgressAsync(AgentTask task)
    {
        // TODO: Implement comprehensive progress analysis
        await Task.CompletedTask;
        task.Result = new { Analysis = "Not yet implemented" };
        return task;
    }

    private async Task<AgentTask> RecommendStudyPathAsync(AgentTask task)
    {
        // TODO: Implement personalized study path recommendations
        await Task.CompletedTask;
        task.Result = new { Recommendations = "Not yet implemented" };
        return task;
    }

    private async Task<AgentTask> ScheduleAssessmentsAsync(AgentTask task)
    {
        // TODO: Implement intelligent scheduling
        await Task.CompletedTask;
        task.Result = new { Schedule = "Not yet implemented" };
        return task;
    }

    /// <summary>
    /// Determine which subject the student should be assessed in next.
    /// Based on: time since last assessment, mastery level, learning objectives.
    /// </summary>
    private async Task<Subject> DetermineNextAssessmentSubject(Student student)
    {
        var assessments = await _assessmentRepository.GetByStudentIdAsync(student.Id);
        
        // Calculate priority for each subject
        var subjectPriorities = new Dictionary<Subject, double>();

        foreach (Subject subject in Enum.GetValues(typeof(Subject)))
        {
            var subjectAssessments = assessments.Where(a => a.Subject == subject).OrderByDescending(a => a.StartTime).ToList();
            
            if (!subjectAssessments.Any())
            {
                // Never assessed - highest priority
                subjectPriorities[subject] = 100.0;
            }
            else
            {
                var lastAssessment = subjectAssessments.First();
                var daysSinceLastAssessment = (DateTime.UtcNow - lastAssessment.StartTime).TotalDays;
                var averageScore = subjectAssessments.Average(a => a.Score);

                // Priority factors:
                // 1. Time since last assessment (more time = higher priority)
                // 2. Lower mastery = higher priority
                // 3. Declining trend = higher priority
                var priority = (daysSinceLastAssessment * 2.0) + ((100 - averageScore) * 0.5);
                
                subjectPriorities[subject] = priority;
            }
        }

        // Return subject with highest priority
        return subjectPriorities.OrderByDescending(kvp => kvp.Value).First().Key;
    }
}
```

### 2.2 Register Orchestrator in Program.cs

**Update: `src/AcademicAssessment.Web/Program.cs`**

```csharp
// Orchestrator
builder.Services.AddSingleton<StudentProgressOrchestrator>();
```

### 2.3 Update Assessment Controller

**Update: `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`**

Add endpoint to trigger orchestrated assessment:

```csharp
[HttpPost("student/{studentId}/orchestrated")]
public async Task<ActionResult<AssessmentResponse>> StartOrchestratedAssessment(Guid studentId)
{
    var orchestrator = HttpContext.RequestServices.GetRequiredService<StudentProgressOrchestrator>();
    
    var task = new AgentTask
    {
        Type = "assess_student",
        Data = new { studentId }
    };

    var result = await orchestrator.ExecuteTaskAsync(task);

    if (result.Status == TaskStatus.Completed)
    {
        return Ok(result.Result);
    }

    return StatusCode(500, new { Error = result.ErrorMessage });
}
```

## Phase 3: First Subject Agent - Mathematics (Week 3)

### 3.1 Mathematics Assessment Agent

**File: `src/AcademicAssessment.Agents/Mathematics/MathematicsAssessmentAgent.cs`**

```csharp
using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Mathematics;

/// <summary>
/// Subject agent specialized in mathematics assessment.
/// Generates and evaluates math questions across algebra, geometry, calculus, etc.
/// </summary>
public class MathematicsAssessmentAgent : A2ABaseAgent
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IStudentResponseRepository _responseRepository;

    public MathematicsAssessmentAgent(
        ITaskService taskService,
        IQuestionRepository questionRepository,
        IStudentResponseRepository responseRepository,
        ILogger<MathematicsAssessmentAgent> logger)
        : base(CreateAgentCard(), taskService, logger)
    {
        _questionRepository = questionRepository;
        _responseRepository = responseRepository;
    }

    private static AgentCard CreateAgentCard()
    {
        return new AgentCard
        {
            Name = "MathematicsAssessmentAgent",
            Description = "Specialized agent for mathematics assessment and evaluation",
            Subject = Subject.Mathematics,
            Skills = new List<string>
            {
                "algebra",
                "geometry",
                "calculus",
                "statistics",
                "trigonometry",
                "pre_calculus",
                "generate_assessment",
                "evaluate_response"
            },
            SupportedGradeLevels = new List<GradeLevel>
            {
                GradeLevel.Grade8,
                GradeLevel.Grade9,
                GradeLevel.Grade10,
                GradeLevel.Grade11,
                GradeLevel.Grade12
            },
            Capabilities = new Dictionary<string, object>
            {
                ["max_questions_per_assessment"] = 30,
                ["supports_adaptive_difficulty"] = true,
                ["supports_step_by_step_solutions"] = true
            }
        };
    }

    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            "generate_question" => await GenerateQuestionAsync(task),
            _ => throw new NotSupportedException($"Task type '{task.Type}' not supported by MathematicsAgent")
        };
    }

    private async Task<AgentTask> GenerateAssessmentAsync(AgentTask task)
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataJson);
        var studentId = Guid.Parse(data["studentId"].ToString());
        var gradeLevel = Enum.Parse<GradeLevel>(data["gradeLevel"].ToString());
        var questionCount = int.Parse(data["questionCount"].ToString());

        Logger.LogInformation("Generating mathematics assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
            studentId, gradeLevel, questionCount);

        // For now, use existing questions from database
        // In Phase 4, we'll use LLMs to generate new questions
        var availableQuestions = await _questionRepository.GetBySubjectAsync(Subject.Mathematics);
        var selectedQuestions = availableQuestions
            .OrderBy(x => Guid.NewGuid()) // Random selection
            .Take(questionCount)
            .ToList();

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Subject = Subject.Mathematics,
            GradeLevel = gradeLevel,
            Type = AssessmentType.Adaptive,
            Questions = selectedQuestions,
            StartTime = DateTime.UtcNow,
            Status = AssessmentStatus.InProgress
        };

        task.Result = new
        {
            AssessmentId = assessment.Id,
            Subject = assessment.Subject,
            QuestionCount = selectedQuestions.Count,
            Questions = selectedQuestions.Select(q => new
            {
                q.Id,
                q.QuestionText,
                q.Type,
                q.Options,
                q.Difficulty,
                q.LearningObjectives
            }).ToList(),
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = AgentCard.Name
        };

        return task;
    }

    private async Task<AgentTask> EvaluateResponseAsync(AgentTask task)
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataJson);
        var responseId = Guid.Parse(data["responseId"].ToString());

        var response = await _responseRepository.GetByIdAsync(responseId);
        if (response == null)
        {
            task.ErrorMessage = $"Response {responseId} not found";
            task.Status = TaskStatus.Failed;
            return task;
        }

        var question = await _questionRepository.GetByIdAsync(response.QuestionId);
        if (question == null)
        {
            task.ErrorMessage = $"Question {response.QuestionId} not found";
            task.Status = TaskStatus.Failed;
            return task;
        }

        // Simple evaluation for now - exact match
        // In Phase 4, use LLMs for semantic evaluation
        var isCorrect = string.Equals(response.Answer?.Trim(), question.CorrectAnswer?.Trim(), 
            StringComparison.OrdinalIgnoreCase);

        response.IsCorrect = isCorrect;
        response.Score = isCorrect ? 1.0 : 0.0;

        await _responseRepository.UpdateAsync(response);

        task.Result = new
        {
            ResponseId = response.Id,
            IsCorrect = isCorrect,
            Score = response.Score,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            EvaluatedBy = AgentCard.Name,
            EvaluatedAt = DateTime.UtcNow
        };

        return task;
    }

    private async Task<AgentTask> GenerateQuestionAsync(AgentTask task)
    {
        // TODO: In Phase 4, use LLMs to generate new questions
        await Task.CompletedTask;
        task.Result = new { Message = "Question generation with LLMs not yet implemented" };
        return task;
    }
}
```

### 3.2 Register Mathematics Agent

**Update: `src/AcademicAssessment.Web/Program.cs`**

```csharp
// Subject Agents
builder.Services.AddSingleton<MathematicsAssessmentAgent>();

// Initialize agents on startup
var app = builder.Build();

// Register agents with task service
var taskService = app.Services.GetRequiredService<ITaskService>();
var mathAgent = app.Services.GetRequiredService<MathematicsAssessmentAgent>();
await mathAgent.InitializeAsync();

// Register math agent handler
if (taskService is TaskService ts)
{
    ts.RegisterHandler(mathAgent.AgentCard.AgentId, mathAgent.ExecuteTaskAsync);
}
```

### 3.3 Integration Test

**File: `tests/AcademicAssessment.Tests.Integration/OrchestratedAssessmentTests.cs`**

```csharp
using AcademicAssessment.Agents.Mathematics;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Agents.Shared.Services;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AcademicAssessment.Tests.Integration;

public class OrchestratedAssessmentTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public OrchestratedAssessmentTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Orchestrator_ShouldCoordinateAssessmentGeneration()
    {
        // Arrange
        var taskService = new TaskService(new Mock<ILogger<TaskService>>().Object);
        
        var mathAgent = new MathematicsAssessmentAgent(
            taskService,
            _fixture.QuestionRepository,
            _fixture.ResponseRepository,
            new Mock<ILogger<MathematicsAssessmentAgent>>().Object
        );

        await mathAgent.InitializeAsync();
        ((TaskService)taskService).RegisterHandler(mathAgent.AgentCard.AgentId, mathAgent.ExecuteTaskAsync);

        var orchestrator = new StudentProgressOrchestrator(
            taskService,
            _fixture.StudentRepository,
            _fixture.AssessmentRepository,
            Mock.Of<IHubContext<AgentProgressHub>>(),
            new Mock<ILogger<StudentProgressOrchestrator>>().Object
        );

        await orchestrator.InitializeAsync();

        // Get a test student
        var students = await _fixture.StudentRepository.GetAllAsync();
        var testStudent = students.First();

        // Act
        var task = new AgentTask
        {
            Type = "assess_student",
            Data = new { studentId = testStudent.Id }
        };

        var result = await orchestrator.ExecuteTaskAsync(task);

        // Assert
        Assert.Equal(TaskStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        
        // Verify assessment was generated
        var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(result.Result));
        Assert.True(resultDict.ContainsKey("Assessment"));
    }
}
```

## Phase 4: LLM Integration (Week 4)

### 4.1 Add Azure OpenAI Packages

```bash
dotnet add src/AcademicAssessment.Agents package Azure.AI.OpenAI --version 1.0.0-beta.12
dotnet add src/AcademicAssessment.Agents package Azure.Identity --version 1.10.0
```

### 4.2 LLM Service

**File: `src/AcademicAssessment.Agents/Shared/Services/LLMService.cs`**

```csharp
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AcademicAssessment.Agents.Shared.Services;

public class LLMService
{
    private readonly OpenAIClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LLMService> _logger;

    public LLMService(IConfiguration configuration, ILogger<LLMService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var endpoint = new Uri(_configuration["AzureOpenAI:Endpoint"]!);
        var credential = new DefaultAzureCredential();
        _client = new OpenAIClient(endpoint, credential);
    }

    public async Task<string> GenerateQuestionAsync(string subject, string gradeLevel, string difficulty, string topic)
    {
        var systemPrompt = $@"You are an expert {subject} educator creating assessment questions for {gradeLevel} students.
Generate a {difficulty} level question about {topic} that:
1. Aligns with grade-appropriate learning objectives
2. Uses clear, accessible language
3. Includes 4 plausible multiple choice options
4. Provides detailed explanation of the correct answer
5. Identifies specific learning objectives

Respond in JSON format with fields: questionText, options (array of 4), correctAnswer, explanation, learningObjectives (array), difficulty, bloomsTaxonomy (1-6).";

        var response = await _client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4o",
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage($"Generate a {difficulty} {subject} question for {gradeLevel} about {topic}")
            },
            Temperature = 0.7f,
            MaxTokens = 1000,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        });

        return response.Value.Choices[0].Message.Content;
    }

    public async Task<string> EvaluateAnswerAsync(string questionText, string studentAnswer, string correctAnswer, string subject)
    {
        var systemPrompt = $@"You are an expert {subject} educator evaluating student responses.
Determine if the student's answer is correct, partially correct, or incorrect.
Provide constructive feedback and explanation.

Respond in JSON format with fields: isCorrect (boolean), score (0.0-1.0), feedback (string), suggestions (array of strings).";

        var response = await _client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4o",
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage($@"Question: {questionText}
Student Answer: {studentAnswer}
Correct Answer: {correctAnswer}

Evaluate the student's response.")
            },
            Temperature = 0.3f, // Lower temperature for evaluation
            MaxTokens = 500,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        });

        return response.Value.Choices[0].Message.Content;
    }
}
```

### 4.3 Update Mathematics Agent with LLM

Update the `GenerateQuestionAsync` method in `MathematicsAssessmentAgent.cs`:

```csharp
private async Task<AgentTask> GenerateQuestionAsync(AgentTask task)
{
    var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataJson);
    var topic = data["topic"].ToString();
    var difficulty = data["difficulty"].ToString();
    var gradeLevel = data["gradeLevel"].ToString();

    var llmService = new LLMService(_configuration, _loggerFactory.CreateLogger<LLMService>());
    
    var questionJson = await llmService.GenerateQuestionAsync(
        "Mathematics", 
        gradeLevel, 
        difficulty, 
        topic
    );

    var question = JsonSerializer.Deserialize<Question>(questionJson);

    task.Result = new
    {
        Question = question,
        GeneratedBy = AgentCard.Name,
        GeneratedAt = DateTime.UtcNow,
        UsedLLM = true
    };

    return task;
}
```

## Phase 5: Additional Subject Agents (Weeks 5-6)

Repeat Phase 3 pattern for remaining subjects:

### 5.1 Physics Agent

- Mechanics, thermodynamics, electromagnetism
- Similar structure to Mathematics agent
- Subject-specific question generation prompts

### 5.2 Chemistry Agent

- Stoichiometry, reactions, molecular structure
- Similar structure to Mathematics agent
- Subject-specific question generation prompts

### 5.3 Biology Agent

- Cells, genetics, ecology
- Similar structure to Mathematics agent
- Subject-specific question generation prompts

### 5.4 English Agent

- Reading comprehension, writing, grammar
- More complex evaluation (use LLMs for essay scoring)
- Subject-specific question generation prompts

## Success Criteria

### Phase 1 (A2A Foundation) - ✅ Complete When

- [ ] A2ABaseAgent compiles and can be inherited
- [ ] AgentCard and AgentTask models serialize/deserialize correctly
- [ ] TaskService can register agents and route messages
- [ ] SignalR hub broadcasts progress updates
- [ ] Unit tests pass for all base infrastructure

### Phase 2 (Orchestrator) - ✅ Complete When

- [ ] StudentProgressOrchestrator receives "assess_student" task
- [ ] Determines appropriate subject for assessment
- [ ] Creates task for subject agent
- [ ] Receives response from subject agent
- [ ] Broadcasts progress via SignalR
- [ ] Integration test passes end-to-end

### Phase 3 (Mathematics Agent) - ✅ Complete When

- [ ] MathematicsAssessmentAgent registers with orchestrator
- [ ] Responds to "generate_assessment" task
- [ ] Generates 10 math questions from database
- [ ] Evaluates student responses correctly
- [ ] Returns results via A2A protocol
- [ ] End-to-end test: API → Orchestrator → Math Agent → Response

### Phase 4 (LLM Integration) - ✅ Complete When

- [ ] Azure OpenAI client configured
- [ ] LLMService generates mathematics questions
- [ ] Questions match quality of demo data
- [ ] LLMService evaluates open-ended responses
- [ ] Cost per assessment < $0.05
- [ ] Response time < 2 seconds

### Phase 5 (All Subjects) - ✅ Complete When

- [ ] All 5 subject agents implemented
- [ ] Each agent registered and discoverable
- [ ] Orchestrator routes to all agents correctly
- [ ] Full integration tests pass
- [ ] Performance benchmarks met (1000+ concurrent students)

## Timeline Summary

| Phase | Duration | Key Deliverable |
|-------|----------|----------------|
| Phase 1 | Week 1 | A2A base infrastructure |
| Phase 2 | Week 2 | Student Progress Orchestrator |
| Phase 3 | Week 3 | Mathematics Agent (database questions) |
| Phase 4 | Week 4 | LLM integration (Azure OpenAI) |
| Phase 5 | Weeks 5-6 | Physics, Chemistry, Biology, English agents |

**Total**: 6 weeks to full multi-agent A2A system

## Next Immediate Steps

1. ✅ **Read and understand A2A architecture** (DONE - reading copilot-instructions.md)
2. 📝 **Create this plan document** (IN PROGRESS)
3. 🔨 **Implement Phase 1: A2A base infrastructure**
   - Create AgentCard, AgentTask models
   - Implement TaskService
   - Create A2ABaseAgent abstract class
   - Set up SignalR hub
   - Write unit tests
4. 🔨 **Implement Phase 2: StudentProgressOrchestrator**
5. 🔨 **Implement Phase 3: MathematicsAssessmentAgent**

## References

- `docs/CONTEXT.md` - System overview and A2A protocol description
- `docs/copilot-instructions.md` - Detailed A2A implementation examples
- `docs/TASK_JOURNAL.md` - Development progress tracking
- `docs/SOLUTION_STRUCTURE.md` - Codebase organization

---

**Status**: Plan created - ready to start Phase 1 implementation
**Last Updated**: 2025-06-XX
**Document Owner**: AI Agent Development Team
