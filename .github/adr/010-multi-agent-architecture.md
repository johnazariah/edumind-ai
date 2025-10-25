# ADR-010: Multi-Agent Architecture for Assessment Generation

**Status:** ✅ Accepted  
**Date:** October 2025  
**Context:** Phase 1-2 - AI Agent Design

## Context

Assessment generation required coordinating multiple concerns:

- Subject-specific knowledge (Math, Physics, Chemistry, Biology, English)
- Difficulty adaptation based on student ability
- Curriculum alignment (grade level, learning objectives)
- Question variety (multiple choice, short answer, essay)
- Real-time orchestration for 50+ concurrent assessments

Design options:

- Monolithic LLM service (single agent handles all subjects)
- Subject-specific agents with coordination layer
- Chain-of-thought prompting to single LLM
- Rule-based question generation (no LLM)

## Decision

Selected **Multi-Agent Architecture** with specialized subject agents coordinated by `StudentProgressOrchestrator`.

## Rationale

1. **Separation of Concerns**: Each agent focuses on domain expertise
2. **Scalability**: Agents can run in parallel, scale independently
3. **Maintainability**: Subject experts can update individual agents
4. **Testability**: Easy to unit test each agent in isolation
5. **Flexibility**: Can swap agent implementations (GPT-4, Ollama, custom)
6. **Agent-to-Agent Protocol**: Structured task-based communication
7. **Extensibility**: Easy to add new subjects (Geography, Art, etc.)

## Consequences

### Positive

- Clear ownership: Math experts own MathematicsAssessmentAgent
- Parallel execution: Multiple subjects evaluated simultaneously
- Better prompts: Subject-specific prompt engineering
- Easy to A/B test: Can compare agent versions
- Resilience: One agent failure doesn't break entire system

### Negative

- Coordination complexity: Orchestrator must route tasks correctly
- More code: 5 subject agents + base agent + orchestrator
- Overhead: Task serialization, message passing
- Debugging: Need distributed tracing to follow request flow
- Consistency: Must ensure agents follow common patterns

### Risks Mitigated

- `A2ABaseAgent` base class enforces common patterns
- `AgentCard` metadata for discovery and capabilities
- `AgentTask` standardized task format (type, data, status, result)
- `ITaskService` abstraction for message routing
- SignalR for real-time progress updates

## Architecture

**Agent Hierarchy**:

```
A2ABaseAgent (abstract base)
├── StudentProgressOrchestrator (meta-agent)
└── Subject Agents:
    ├── MathematicsAssessmentAgent
    ├── PhysicsAssessmentAgent
    ├── ChemistryAssessmentAgent
    ├── BiologyAssessmentAgent
    └── EnglishAssessmentAgent
```

**Task Flow**:

```
1. API Controller → StudentProgressOrchestrator
2. Orchestrator determines subject priority
3. Orchestrator creates AgentTask for subject agent
4. ITaskService routes task to correct agent
5. Agent generates questions using LLM
6. Agent returns AgentTask with result
7. Orchestrator broadcasts progress via SignalR
8. API returns assessment to student
```

**Agent Communication**:

```csharp
public class AgentTask
{
    public string TaskId { get; set; }
    public string Type { get; set; }  // "generate_assessment", "evaluate_response"
    public Dictionary<string, object> Data { get; set; }
    public TaskStatus Status { get; set; }  // Pending, InProgress, Completed, Failed
    public object? Result { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Implementation Details

**Base Agent** (src/AcademicAssessment.Agents/Shared/A2ABaseAgent.cs):

```csharp
public abstract class A2ABaseAgent
{
    public abstract AgentCard GetAgentCard();
    public abstract Task<AgentTask> ProcessTaskAsync(AgentTask task);
    
    protected async Task<string> CallLLMAsync(string prompt)
    {
        return await _llmService.GenerateAsync(prompt);
    }
}
```

**Mathematics Agent** (src/AcademicAssessment.Agents/Mathematics/MathematicsAssessmentAgent.cs):

```csharp
public class MathematicsAssessmentAgent : A2ABaseAgent
{
    public override AgentCard GetAgentCard() => new()
    {
        Name = "MathematicsAssessmentAgent",
        Subject = Subject.Mathematics,
        Skills = ["question_generation", "answer_evaluation", "difficulty_adjustment"],
        SupportedGradeLevels = [GradeLevel.Grade8, GradeLevel.Grade9, GradeLevel.Grade10]
    };
    
    public override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        if (task.Type == "generate_assessment")
        {
            var questions = await GenerateQuestionsAsync(task.Data);
            task.Result = questions;
            task.Status = TaskStatus.Completed;
        }
        return task;
    }
}
```

**Orchestrator** (src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs):

```csharp
public class StudentProgressOrchestrator : A2ABaseAgent
{
    private readonly Dictionary<Subject, A2ABaseAgent> _agents;
    
    public async Task<Assessment> GenerateAdaptiveAssessmentAsync(Guid studentId)
    {
        // 1. Analyze student progress
        var progress = await _analyticsService.GetStudentProgressAsync(studentId);
        
        // 2. Determine next subject using priority algorithm
        var subject = DetermineNextSubject(progress);
        
        // 3. Create task for subject agent
        var task = new AgentTask
        {
            Type = "generate_assessment",
            Data = new { StudentId = studentId, Subject = subject, QuestionCount = 10 }
        };
        
        // 4. Route to appropriate agent
        var agent = _agents[subject];
        var result = await agent.ProcessTaskAsync(task);
        
        // 5. Broadcast progress via SignalR
        await _hubContext.Clients.User(studentId.ToString())
            .SendAsync("AssessmentReady", result.Result);
        
        return (Assessment)result.Result;
    }
}
```

## Agent Scaling Strategy

**Current** (Single instance):

- All agents run in same process
- Simple in-memory task routing

**Future** (Distributed):

- Agents run as separate microservices
- Azure Service Bus for task routing
- Horizontal scaling per subject load
- Circuit breakers for agent failures

## Alternative Considered: Monolithic LLM Service

**Rejected because:**

- Single point of failure (one LLM issue breaks all subjects)
- Hard to specialize prompts (generic prompt for all subjects)
- Difficult to scale (can't scale Math independently of English)
- Poor separation of concerns (mixing business logic)
- Hard to test (mocking one agent affects others)

## Related Decisions

- ADR-003: Semantic Kernel for AI Integration
- ADR-011: Repository Pattern for Data Access
- ADR-013: API Versioning Strategy
- ADR-080: Application Insights Integration (agent tracing)

## References

- `src/AcademicAssessment.Agents/` - All agent implementations
- `src/AcademicAssessment.Orchestration/` - Orchestrator
- Commit: `a75945f` - "feat: Implement A2A Protocol Foundation"
- Commit: `f2c57ca` - "feat: Implement MathematicsAssessmentAgent"
- docs/architecture/A2A_AGENT_INTEGRATION_PLAN.md
