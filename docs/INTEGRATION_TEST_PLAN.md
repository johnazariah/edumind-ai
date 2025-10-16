# Multi-Agent Integration Test Plan

**Date**: October 16, 2025  
**Sprint**: Week 1, Day 1  
**Component**: Orchestrator ↔ Agent Communication

## Overview

This document outlines integration testing strategies for multi-agent workflows orchestrated by `StudentProgressOrchestrator`. These tests validate agent-to-agent (A2A) communication patterns, task routing, and end-to-end orchestration scenarios.

## Current Test Coverage

### Unit Tests: ✅ 15/15 Passing

1. **Subject Selection Tests** (5 tests)
   - AssessStudent_WithNoHistory_ShouldStartWithMathematics
   - DetermineNextSubject_WithSingleSubject_ShouldUseThatSubject
   - DetermineNextSubject_WithDecliningPerformance_ShouldFocusOnWeakSubject
   - DetermineNextSubject_WithNeverAssessedSubject_ShouldPrioritizeIt ⭐ (newly fixed)
   - DetermineNextSubject_WithRecentAssessment_ShouldPenalizeThatSubject

2. **Task Routing Tests** (5 tests)
   - RouteTask_ForAssessStudent_ShouldCallDetermineNextSubject
   - RouteTask_ForGenerateAssessment_ShouldDiscoverAgents
   - RouteTask_ForAnalyzePerformance_ShouldRouteToAnalyticsAgent
   - ExecuteTask_WithInvalidTaskType_ShouldReturnFailure
   - ExecuteTask_WithValidTask_ShouldTransitionToCompleted

3. **State Transition Tests** (2 tests)
   - ExecuteTask_ShouldTransitionPendingToInProgress
   - ExecuteTask_WhenCompleted_ShouldSetFinalStatus

4. **Error Handling Tests** (3 tests)
   - ExecuteTask_WithRepositoryFailure_ShouldReturnFailure
   - ExecuteTask_WithAgentFailure_ShouldReturnFailure
   - ExecuteTask_WithInvalidData_ShouldReturnValidationFailure

## Integration Test Scenarios

### Scenario 1: End-to-End Student Assessment Workflow

**Objective**: Validate complete workflow from assessment request to agent execution

**Test Steps**:

1. Student requests new assessment
2. Orchestrator calls `DetermineNextAssessmentSubjectAsync()`
   - Queries IStudentAssessmentRepository for history
   - Calls `LoadAssessmentSubjectsAsync()` for batch subject loading
   - Applies priority scoring algorithm
3. Orchestrator discovers agents for selected subject
   - Calls ITaskService.DiscoverAgentsAsync()
4. Orchestrator routes task to assessment generation agent
   - Calls ITaskService.SendTaskAsync()
5. Agent generates assessment questions
6. Orchestrator returns completed task with generated assessment

**Expected Results**:

- Subject selection matches priority algorithm
- Correct agent discovered and called
- Task transitions: Pending → InProgress → Completed
- Assessment data returned in task result

**Test Implementation**:

```csharp
[Fact]
public async Task StudentAssessmentWorkflow_EndToEnd_ShouldCompleteSuccessfully()
{
    // Arrange
    var studentId = Guid.NewGuid();
    var student = CreateTestStudent(studentId, GradeLevel.Grade9);
    var assessmentHistory = CreateAssessmentHistory(studentId, 
        Subject.Mathematics, Subject.Physics);
    
    // Setup mocks for repositories
    _mockStudentRepository
        .Setup(r => r.GetByIdAsync(studentId, default))
        .ReturnsAsync(Result<Student>.Success(student));
    
    _mockStudentAssessmentRepository
        .Setup(r => r.GetByStudentIdAsync(studentId, default))
        .ReturnsAsync(Result<IReadOnlyList<StudentAssessment>>.Success(
            assessmentHistory));
    
    // Setup assessment repository with subject mappings
    foreach (var assessment in assessmentHistory)
    {
        _mockAssessmentRepository
            .Setup(r => r.GetByIdAsync(assessment.AssessmentId, default))
            .ReturnsAsync(Result<Assessment>.Success(
                CreateTestAssessment(assessment.AssessmentId, GetSubject(assessment))));
    }
    
    // Setup agents
    var biologySupportAgent = CreateTestAgentCard("BiologyAgent", Subject.Biology);
    _mockTaskService
        .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
        .ReturnsAsync(new[] { biologyAgent });
    
    // Setup agent response
    _mockTaskService
        .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
        .ReturnsAsync((string agentId, AgentTask task) =>
        {
            task.Status = AgentTaskStatus.Completed;
            task.Result = new { assessmentId = Guid.NewGuid() };
            return task;
        });
    
    var task = CreateTestTask("assess_student", new { studentId });
    
    // Act
    var result = await _orchestrator.ExecuteTaskAsync(task);
    
    // Assert
    result.Status.Should().Be(AgentTaskStatus.Completed);
    result.Result.Should().NotBeNull();
    
    // Verify subject selection (Biology never assessed, should be prioritized)
    _mockTaskService.Verify(
        s => s.DiscoverAgentsAsync(
            It.Is<string>(subject => subject.Contains("Biology")), 
            "generate_assessment"),
        Times.Once());
    
    // Verify agent was called
    _mockTaskService.Verify(
        s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()),
        Times.Once());
}
```

### Scenario 2: Learning Path Optimization

**Objective**: Validate multi-subject learning path generation with agent coordination

**Test Steps**:

1. Orchestrator calls `OptimizeLearningPathAsync()`
2. Groups assessments by subject using `LoadAssessmentSubjectsAsync()`
3. Analyzes performance trends across subjects
4. Discovers agents for each subject area
5. Coordinates parallel agent tasks for resource generation
6. Aggregates agent responses into unified learning path

**Expected Results**:

- Correct subject grouping using batch-loaded assessment data
- Multiple agents discovered (one per subject)
- Parallel agent coordination
- Learning path includes resources from all agents

**Test Implementation**:

```csharp
[Fact]
public async Task OptimizeLearningPath_WithMultipleSubjects_ShouldCoordinateAgents()
{
    // Arrange
    var studentId = Guid.NewGuid();
    var assessments = CreateMultiSubjectAssessments(studentId, 
        new[] { Subject.Mathematics, Subject.Physics, Subject.Biology });
    
    // Setup assessment repository for all subjects
    foreach (var assessment in assessments)
    {
        _mockAssessmentRepository
            .Setup(r => r.GetByIdAsync(assessment.AssessmentId, default))
            .ReturnsAsync(Result<Assessment>.Success(
                CreateTestAssessment(assessment.AssessmentId, GetSubject(assessment))));
    }
    
    // Setup agents for all subjects
    var agents = new[]
    {
        CreateTestAgentCard("MathAgent", Subject.Mathematics),
        CreateTestAgentCard("PhysicsAgent", Subject.Physics),
        CreateTestAgentCard("BiologyAgent", Subject.Biology)
    };
    
    _mockTaskService
        .Setup(s => s.DiscoverAgentsAsync(null, "generate_resources"))
        .ReturnsAsync(agents);
    
    // Setup agent responses (parallel execution)
    var agentCallCount = 0;
    _mockTaskService
        .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
        .ReturnsAsync((string agentId, AgentTask task) =>
        {
            Interlocked.Increment(ref agentCallCount);
            task.Status = AgentTaskStatus.Completed;
            task.Result = new { resources = new[] { $"resource_from_{agentId}" } };
            return task;
        });
    
    var task = CreateTestTask("optimize_learning_path", new { studentId });
    
    // Act
    var result = await _orchestrator.ExecuteTaskAsync(task);
    
    // Assert
    result.Status.Should().Be(AgentTaskStatus.Completed);
    agentCallCount.Should().Be(3); // All 3 agents called
    
    // Verify batch loading was used (not N+1 queries)
    _mockAssessmentRepository.Verify(
        r => r.GetByIdAsync(It.IsAny<Guid>(), default),
        Times.Exactly(assessments.Count())); // One query per unique assessment
}
```

### Scenario 3: Agent Failure Handling

**Objective**: Validate graceful degradation when agents fail

**Test Steps**:

1. Orchestrator discovers multiple agents
2. One agent returns failure status
3. Orchestrator handles failure gracefully
4. Other agents continue execution
5. Partial results returned with error details

**Expected Results**:

- Task status reflects partial failure
- Error message includes failing agent details
- Successful agent results preserved
- System remains stable

### Scenario 4: Performance Under Load

**Objective**: Validate batch loading performance with many assessments

**Test Steps**:

1. Create student with 100+ historical assessments
2. Call `OptimizeLearningPathAsync()`
3. Measure database query count
4. Measure execution time

**Expected Results**:

- Query count = number of unique assessment IDs (not N×subjects)
- Execution time < 500ms for 100 assessments
- Memory usage remains constant

### Scenario 5: Concurrent Orchestration

**Objective**: Validate thread-safety with concurrent students

**Test Steps**:

1. Create 10 concurrent student assessment requests
2. Execute all tasks simultaneously
3. Verify no race conditions
4. Verify correct agent routing per student

**Expected Results**:

- All tasks complete successfully
- No cross-student contamination
- Agent calls isolated per student
- Result data integrity maintained

## Test Data Requirements

### Mock Repositories

1. **IStudentRepository**
   - GetByIdAsync() returns test students
   - Various grade levels
   - Different learning profiles

2. **IStudentAssessmentRepository**
   - GetByStudentIdAsync() returns assessment history
   - Multiple subjects
   - Various completion dates
   - Mixed performance scores

3. **IAssessmentRepository** ⭐ (newly implemented)
   - GetByIdAsync() returns assessment entities with subjects
   - Batch loading support
   - Subject variety: Mathematics, Physics, Biology, Chemistry, English

4. **ITaskService**
   - DiscoverAgentsAsync() returns agent cards
   - SendTaskAsync() simulates agent responses
   - Configurable success/failure scenarios

## Test Helpers

### Newly Added in Task 1 ✅

```csharp
// Create assessment with specific ID and subject
private Assessment CreateTestAssessment(Guid id, Subject subject)
{
    return new Assessment
    {
        Id = id,
        CourseId = Guid.NewGuid(),
        Subject = subject,
        Title = $"{subject} Assessment",
        Description = $"Test assessment for {subject}",
        AssessmentType = AssessmentType.Practice,
        GradeLevel = GradeLevel.Grade9,
        TotalPoints = 100,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}

// Create completed assessment with specific assessment ID
private StudentAssessment CreateCompletedAssessment(
    Guid studentId,
    Guid assessmentId,
    double percentageScore,
    DateTime completedAt)
{
    // Implementation provided in Task 1
}
```

### Recommended New Helpers

```csharp
// Create multi-subject assessment history
private IReadOnlyList<StudentAssessment> CreateMultiSubjectAssessments(
    Guid studentId,
    Subject[] subjects)
{
    var assessments = new List<StudentAssessment>();
    foreach (var subject in subjects)
    {
        var assessmentId = Guid.NewGuid();
        assessments.Add(CreateCompletedAssessment(
            studentId, assessmentId, 75.0, DateTime.UtcNow.AddDays(-Random.Next(1, 30))));
    }
    return assessments.AsReadOnly();
}

// Create agent card for subject
private AgentCard CreateTestAgentCard(string agentId, Subject subject)
{
    return new AgentCard
    {
        AgentId = agentId,
        AgentType = $"{subject}AssessmentAgent",
        Capabilities = new[] { "generate_assessment", "analyze_performance" },
        Subject = subject,
        IsAvailable = true
    };
}
```

## Implementation Priority

### Phase 1: Core Workflows ⭐ CURRENT
- ✅ Unit tests for orchestrator logic
- ✅ GetAssessmentSubject() implementation with batch loading
- ⏳ **Scenario 1**: End-to-end student assessment workflow

### Phase 2: Advanced Orchestration
- **Scenario 2**: Learning path optimization with multiple agents
- **Scenario 3**: Agent failure handling

### Phase 3: Performance & Scale
- **Scenario 4**: Performance under load testing
- **Scenario 5**: Concurrent orchestration testing

## Success Criteria

1. **Coverage**: All integration scenarios pass
2. **Performance**: <500ms response time for typical workflows
3. **Reliability**: 99.9% success rate in production
4. **Scalability**: Handle 100+ concurrent students
5. **Observability**: Complete logging and tracing

## Tools & Frameworks

- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **Coverlet**: Code coverage
- **BenchmarkDotNet**: Performance testing (future)

## Next Steps

1. ✅ **Task 1 COMPLETE**: Fix GetAssessmentSubject() stub
2. ✅ **Task 2 COMPLETE**: Measure code coverage
3. ⏳ **Task 3 IN PROGRESS**: Document integration tests (this document)
4. 🔜 **Next Sprint**: Implement Scenario 1 integration test
5. 🔜 **Week 1, Day 2**: Add remaining integration test scenarios

## Conclusion

This integration test plan provides a roadmap for validating multi-agent workflows. The focus is on realistic scenarios that mirror production usage patterns. With Task 1 complete (GetAssessmentSubject implementation), we now have the foundation for comprehensive integration testing.

**Current Status**: Foundation complete, ready for full integration test implementation in next sprint.
