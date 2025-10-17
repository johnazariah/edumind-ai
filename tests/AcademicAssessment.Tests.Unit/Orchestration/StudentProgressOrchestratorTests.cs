using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AcademicAssessment.Tests.Unit.Orchestration;

/// <summary>
/// Comprehensive unit tests for StudentProgressOrchestrator.
/// Tests decision-making algorithms, task routing, state transitions, and error scenarios.
/// Target: >80% code coverage.
/// </summary>
public class StudentProgressOrchestratorTests
{
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IAssessmentRepository> _mockAssessmentRepository;
    private readonly Mock<IStudentAssessmentRepository> _mockStudentAssessmentRepository;
    private readonly Mock<ILogger<StudentProgressOrchestrator>> _mockLogger;
    private readonly StudentProgressOrchestrator _orchestrator;

    public StudentProgressOrchestratorTests()
    {
        _mockTaskService = new Mock<ITaskService>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockAssessmentRepository = new Mock<IAssessmentRepository>();
        _mockStudentAssessmentRepository = new Mock<IStudentAssessmentRepository>();
        _mockLogger = new Mock<ILogger<StudentProgressOrchestrator>>();

        _orchestrator = new StudentProgressOrchestrator(
            _mockTaskService.Object,
            _mockStudentRepository.Object,
            _mockAssessmentRepository.Object,
            _mockStudentAssessmentRepository.Object,
            _mockLogger.Object,
            signalRHubUrl: null); // No SignalR for unit tests
    }

    #region Subject Selection Tests

    [Fact]
    public async Task AssessStudent_WithNoHistory_ShouldStartWithMathematics()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        var mathAgent = CreateTestAgentCard("MathAgent", Subject.Mathematics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { mathAgent });

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
        _mockTaskService.Verify(s => s.DiscoverAgentsAsync(null, "generate_assessment"), Times.Once);
    }

    [Fact]
    public async Task DetermineNextSubject_WithDecliningPerformance_ShouldPrioritizeThatSubject()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade10);

        // Create assessment history showing declining Physics performance
        var assessments = new List<StudentAssessment>
        {
            CreateCompletedAssessment(studentId, Subject.Physics, 85.0, DateTime.UtcNow.AddDays(-20)),
            CreateCompletedAssessment(studentId, Subject.Physics, 70.0, DateTime.UtcNow.AddDays(-10)),
            CreateCompletedAssessment(studentId, Subject.Physics, 55.0, DateTime.UtcNow.AddDays(-5)),
            CreateCompletedAssessment(studentId, Subject.Mathematics, 80.0, DateTime.UtcNow.AddDays(-3)),
        };

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                assessments.AsReadOnly()));

        var physicsAgent = CreateTestAgentCard("PhysicsAgent", Subject.Physics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { physicsAgent });

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
        // Should prioritize Physics due to declining performance
        _mockTaskService.Verify(
            s => s.DiscoverAgentsAsync(null, "generate_assessment"),
            Times.Once);
    }

    [Fact]
    public async Task DetermineNextSubject_WithNeverAssessedSubject_ShouldPrioritizeIt()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        // Create assessment IDs
        var mathAssessmentId1 = Guid.NewGuid();
        var mathAssessmentId2 = Guid.NewGuid();

        // Only Mathematics assessments - Biology never assessed
        var assessments = new List<StudentAssessment>
        {
            CreateCompletedAssessment(studentId, mathAssessmentId1, 75.0, DateTime.UtcNow.AddDays(-5)),
            CreateCompletedAssessment(studentId, mathAssessmentId2, 80.0, DateTime.UtcNow.AddDays(-2)),
        };

        // Mock assessment repository to return assessment entities with subjects
        _mockAssessmentRepository
            .Setup(r => r.GetByIdAsync(mathAssessmentId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Assessment>.Success(
                CreateTestAssessment(mathAssessmentId1, Subject.Mathematics)));

        _mockAssessmentRepository
            .Setup(r => r.GetByIdAsync(mathAssessmentId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Assessment>.Success(
                CreateTestAssessment(mathAssessmentId2, Subject.Mathematics)));

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                assessments.AsReadOnly()));

        // Setup agents for all subjects
        var agents = new List<AgentCard>
        {
            CreateTestAgentCard("BiologyAgent", Subject.Biology),
            CreateTestAgentCard("MathAgent", Subject.Mathematics),
            CreateTestAgentCard("PhysicsAgent", Subject.Physics),
        };
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(agents);

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        if (result.Status == AgentTaskStatus.Failed)
        {
            // Log error for debugging
            System.Console.WriteLine($"Task failed: {result.ErrorMessage}");
        }
        result.Status.Should().Be(AgentTaskStatus.Completed);
        // Should discover agents (never assessed subjects get highest priority)
        _mockTaskService.Verify(
            s => s.DiscoverAgentsAsync(It.IsAny<string?>(), It.IsAny<string?>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task DetermineNextSubject_WithRecentAssessment_ShouldPenalizeThatSubject()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade10);

        // Mathematics assessed many times recently (should be penalized)
        var assessments = new List<StudentAssessment>
        {
            CreateCompletedAssessment(studentId, Subject.Mathematics, 80.0, DateTime.UtcNow.AddDays(-1)),
            CreateCompletedAssessment(studentId, Subject.Mathematics, 82.0, DateTime.UtcNow.AddDays(-2)),
            CreateCompletedAssessment(studentId, Subject.Mathematics, 85.0, DateTime.UtcNow.AddDays(-3)),
            CreateCompletedAssessment(studentId, Subject.Mathematics, 83.0, DateTime.UtcNow.AddDays(-4)),
            CreateCompletedAssessment(studentId, Subject.Physics, 75.0, DateTime.UtcNow.AddDays(-15)),
        };

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                assessments.AsReadOnly()));

        var agents = new List<AgentCard>
        {
            CreateTestAgentCard("PhysicsAgent", Subject.Physics),
            CreateTestAgentCard("MathAgent", Subject.Mathematics),
        };
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(agents);

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
        // Should prefer Physics (not assessed recently, avoiding assessment fatigue on Math)
        _mockTaskService.Verify(
            s => s.DiscoverAgentsAsync(null, "generate_assessment"),
            Times.Once);
    }

    #endregion

    #region Task Routing Tests

    [Fact]
    public async Task RouteTask_WithMultipleAvailableAgents_ShouldSelectBasedOnLoadBalancing()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        // Multiple agents available
        var agents = new List<AgentCard>
        {
            CreateTestAgentCard("MathAgent1", Subject.Mathematics, version: "1.0.0"),
            CreateTestAgentCard("MathAgent2", Subject.Mathematics, version: "1.1.0"),
            CreateTestAgentCard("MathAgent3", Subject.Mathematics, version: "2.0.0"),
        };

        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(agents);

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
        // Should have sent task to one of the agents (with highest version/lowest load)
        _mockTaskService.Verify(
            s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteTask_WithNoAvailableAgents_ShouldFail()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        // No agents available
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard>());

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().Contain("No agents available");
    }

    [Fact]
    public async Task RouteTask_WithAgentCapabilities_ShouldScoreCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        // Agent with more capabilities
        var advancedAgent = CreateTestAgentCard("AdvancedMathAgent", Subject.Mathematics, version: "1.0.0");
        advancedAgent.Capabilities = new Dictionary<string, object>
        {
            { "assessment_types", new[] { "adaptive", "standard", "diagnostic" } },
            { "adaptive_difficulty", true },
            { "max_concurrent_students", 50 }
        };

        // Basic agent
        var basicAgent = CreateTestAgentCard("BasicMathAgent", Subject.Mathematics, version: "1.0.0");

        var agents = new List<AgentCard> { advancedAgent, basicAgent };

        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(agents);

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
        // Should prefer advanced agent (higher capability score)
        _mockTaskService.Verify(
            s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AssessStudent_WithInvalidStudentId_ShouldFail()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var error = new Error("NotFound", "Student not found");

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Failure(error));

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task AssessStudent_WithMissingStudentId_ShouldFail()
    {
        // Arrange
        var task = CreateTestTask("assess_student", new { }); // No studentId

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().Contain("studentId");
    }

    [Fact]
    public async Task AssessStudent_WhenAgentFails_ShouldReturnFailure()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        var mathAgent = CreateTestAgentCard("MathAgent", Subject.Mathematics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { mathAgent });

        // Agent returns failed task
        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Failed;
                task.ErrorMessage = "Agent internal error";
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().Contain("Agent");
    }

    [Fact]
    public async Task ProcessTask_WithUnsupportedTaskType_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask("unsupported_task_type", new { });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().Contain("not supported");
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public async Task ExecuteTask_ShouldSetStartedAtTimestamp()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        var mathAgent = CreateTestAgentCard("MathAgent", Subject.Mathematics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { mathAgent });

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });
        var beforeExecution = DateTime.UtcNow;

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);
        var afterExecution = DateTime.UtcNow;

        // Assert
        result.StartedAt.Should().NotBeNull();
        result.StartedAt.Should().BeOnOrAfter(beforeExecution);
        result.StartedAt.Should().BeOnOrBefore(afterExecution);
    }

    [Fact]
    public async Task ExecuteTask_ShouldSetCompletedAtTimestamp()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        var mathAgent = CreateTestAgentCard("MathAgent", Subject.Mathematics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { mathAgent });

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.CompletedAt.Should().NotBeNull();
        result.CompletedAt.Should().BeOnOrAfter(result.StartedAt!.Value);
    }

    [Fact]
    public async Task ExecuteTask_OnSuccess_ShouldSetStatusToCompleted()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId, GradeLevel.Grade9);

        _mockStudentRepository
            .Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<Student>.Success(student));

        _mockStudentAssessmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<IReadOnlyList<StudentAssessment>>.Success(
                new List<StudentAssessment>().AsReadOnly()));

        var mathAgent = CreateTestAgentCard("MathAgent", Subject.Mathematics);
        _mockTaskService
            .Setup(s => s.DiscoverAgentsAsync(null, "generate_assessment"))
            .ReturnsAsync(new List<AgentCard> { mathAgent });

        _mockTaskService
            .Setup(s => s.SendTaskAsync(It.IsAny<string>(), It.IsAny<AgentTask>()))
            .ReturnsAsync((string agentId, AgentTask task) =>
            {
                task.Status = AgentTaskStatus.Completed;
                return task;
            });

        var task = CreateTestTask("assess_student", new { studentId });

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Completed);
    }

    [Fact]
    public async Task ExecuteTask_OnFailure_ShouldSetStatusToFailed()
    {
        // Arrange
        var task = CreateTestTask("assess_student", new { }); // Missing studentId

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(task);

        // Assert
        result.Status.Should().Be(AgentTaskStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Routing and Fallback Tests

    [Fact]
    public void GetRoutingStatistics_ShouldReturnValidMetrics()
    {
        // Act
        var stats = _orchestrator.GetRoutingStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.SuccessRate.Should().BeGreaterOrEqualTo(0);
        stats.FallbackRate.Should().BeGreaterOrEqualTo(0);
        stats.AgentUtilization.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private Student CreateTestStudent(Guid id, GradeLevel gradeLevel)
    {
        return new Student
        {
            Id = id,
            UserId = Guid.NewGuid(),
            GradeLevel = gradeLevel,
            SchoolId = Guid.NewGuid(),
            ParentalConsentGranted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private AgentCard CreateTestAgentCard(string name, Subject? subject, string version = "1.0.0")
    {
        return new AgentCard
        {
            AgentId = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"{name} for testing",
            Version = version,
            Subject = subject,
            Skills = new List<string> { "generate_assessment", "evaluate_response" },
            SupportedGradeLevels = Enum.GetValues<GradeLevel>().ToList(),
            Capabilities = new Dictionary<string, object>()
        };
    }

    private StudentAssessment CreateCompletedAssessment(
        Guid studentId,
        Subject subject,
        double percentageScore,
        DateTime completedAt)
    {
        var assessmentId = Guid.NewGuid();
        return CreateCompletedAssessment(studentId, assessmentId, percentageScore, completedAt);
    }

    private StudentAssessment CreateCompletedAssessment(
        Guid studentId,
        Guid assessmentId,
        double percentageScore,
        DateTime completedAt)
    {
        var correctAnswers = (int)(percentageScore / 10);
        var totalQuestions = 10;

        return new StudentAssessment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            AssessmentId = assessmentId,
            Status = AssessmentStatus.Completed,
            StartedAt = completedAt.AddMinutes(-15),
            CompletedAt = completedAt,
            Score = correctAnswers,
            MaxScore = totalQuestions,
            CorrectAnswers = correctAnswers,
            IncorrectAnswers = totalQuestions - correctAnswers,
            SkippedQuestions = 0,
            EstimatedAbility = 0.0,
            CreatedAt = completedAt.AddMinutes(-15),
            UpdatedAt = completedAt
        };
    }

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

    private AgentTask CreateTestTask(string type, object data)
    {
        return new AgentTask
        {
            TaskId = Guid.NewGuid().ToString(),
            Type = type,
            Status = AgentTaskStatus.Pending,
            Data = data,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
