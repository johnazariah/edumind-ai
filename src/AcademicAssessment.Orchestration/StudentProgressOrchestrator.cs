using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Orchestration;

/// <summary>
/// Central orchestrator that coordinates all subject agents and manages student assessment workflows.
/// Inherits from A2ABaseAgent to participate in the agent-to-agent protocol.
/// SignalR notifications are handled by A2ABaseAgent.BroadcastProgressAsync method.
/// </summary>
public class StudentProgressOrchestrator : A2ABaseAgent
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IStudentAssessmentRepository _studentAssessmentRepository;

    /// <summary>
    /// Initializes the orchestrator with required dependencies.
    /// </summary>
    public StudentProgressOrchestrator(
        ITaskService taskService,
        IStudentRepository studentRepository,
        IAssessmentRepository assessmentRepository,
        IStudentAssessmentRepository studentAssessmentRepository,
        ILogger<StudentProgressOrchestrator> logger,
        string? signalRHubUrl = "https://localhost:5001/hubs/agent-progress")
        : base(CreateAgentCard(), taskService, logger, signalRHubUrl)
    {
        _studentRepository = studentRepository;
        _assessmentRepository = assessmentRepository;
        _studentAssessmentRepository = studentAssessmentRepository;
    }

    /// <summary>
    /// Creates the agent card that describes this orchestrator's capabilities.
    /// </summary>
    private static AgentCard CreateAgentCard()
    {
        return new AgentCard
        {
            Name = "StudentProgressOrchestrator",
            Description = "Central coordinator that orchestrates student assessments across all subjects",
            Version = "1.0.0",
            Subject = null, // Orchestrator works across all subjects
            Skills = new List<string>
            {
                "assess_student",
                "analyze_progress",
                "recommend_study_path",
                "schedule_assessments",
                "coordinate_agents"
            },
            SupportedGradeLevels = Enum.GetValues<GradeLevel>().ToList(),
            Capabilities = new Dictionary<string, object>
            {
                { "max_concurrent_students", 100 },
                { "supported_subjects", new[] { "Mathematics", "Physics", "Chemistry", "Biology", "English" } },
                { "assessment_types", new[] { "adaptive", "standard", "diagnostic" } }
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// Routes to appropriate handler methods.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger.LogInformation("Processing task type: {TaskType} (TaskId: {TaskId})", task.Type, task.TaskId);

        return task.Type switch
        {
            "assess_student" => await AssessStudentAsync(task),
            "analyze_progress" => await AnalyzeProgressAsync(task),
            "recommend_study_path" => await RecommendStudyPathAsync(task),
            "schedule_assessments" => await ScheduleAssessmentsAsync(task),
            _ => throw new NotSupportedException($"Task type '{task.Type}' is not supported by StudentProgressOrchestrator")
        };
    }

    /// <summary>
    /// Assesses a student by determining which subject needs assessment and delegating to subject agent.
    /// </summary>
    private async Task<AgentTask> AssessStudentAsync(AgentTask task)
    {
        try
        {
            // Extract student ID from task data
            var taskData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                task.DataJson ?? "{}");

            if (taskData == null || !taskData.TryGetValue("studentId", out var studentIdObj))
            {
                throw new ArgumentException("Task data must contain 'studentId'");
            }

            var studentId = Guid.Parse(studentIdObj.ToString()!);

            Logger.LogInformation("Assessing student: {StudentId}", studentId);

            // Load student information
            var studentResult = await _studentRepository.GetByIdAsync(studentId);
            if (studentResult.IsFailure)
            {
                throw new InvalidOperationException($"Student {studentId} not found");
            }

            var student = ((AcademicAssessment.Core.Common.Result<AcademicAssessment.Core.Models.Student>.Success)studentResult).Value;

            // Determine which subject needs assessment
            var targetSubject = await DetermineNextAssessmentSubjectAsync(studentId, student.GradeLevel);

            Logger.LogInformation("Determined next assessment subject for student {StudentId}: {Subject}",
                studentId, targetSubject);

            // Broadcast progress to student
            await BroadcastProgressAsync($"Preparing {targetSubject} assessment for student {studentId}...");

            // Discover agents capable of assessing this subject
            var subjectAgents = await DiscoverAgentsAsync(subject: targetSubject.ToString());

            if (subjectAgents.Count() == 0)
            {
                throw new InvalidOperationException($"No agents available for subject: {targetSubject}");
            }

            // Select the first available agent (in production, add load balancing logic here)
            var selectedAgent = subjectAgents.First();

            Logger.LogInformation("Selected agent {AgentName} ({AgentId}) for {Subject} assessment",
                selectedAgent.Name, selectedAgent.AgentId, targetSubject);

            // Create task for subject agent
            var subjectTask = new AgentTask
            {
                Type = "generate_assessment",
                SourceAgentId = AgentCard.AgentId,
                TargetAgentId = selectedAgent.AgentId,
                Data = new
                {
                    studentId = studentId,
                    subject = targetSubject,
                    gradeLevel = student.GradeLevel,
                    questionCount = 10,
                    difficultyAdaptive = true
                }
            };

            // Send task to subject agent
            var result = await TaskService.SendTaskAsync(selectedAgent.AgentId, subjectTask);

            if (result.Status == AgentTaskStatus.Failed)
            {
                throw new InvalidOperationException($"Subject agent failed: {result.ErrorMessage}");
            }

            // Broadcast assessment ready notification
            await BroadcastProgressAsync($"Assessment ready for student {studentId}: {targetSubject} with 10 questions (generated by {selectedAgent.Name})");

            Logger.LogInformation("Assessment generated successfully for student {StudentId}, subject {Subject}",
                studentId, targetSubject);

            // Return orchestrator task result
            task.Result = new
            {
                success = true,
                studentId = studentId,
                subject = targetSubject.ToString(),
                assessmentResult = result.Result,
                agentUsed = selectedAgent.Name,
                timestamp = DateTime.UtcNow
            };

            return task;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assessing student in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Determines which subject should be assessed next for a student.
    /// Priority logic:
    /// 1. Never assessed subjects (highest priority)
    /// 2. Subjects with declining performance trends
    /// 3. Subjects not assessed recently
    /// 4. Subjects with lowest mastery scores
    /// </summary>
    private async Task<Subject> DetermineNextAssessmentSubjectAsync(Guid studentId, GradeLevel gradeLevel)
    {
        Logger.LogDebug("Determining next assessment subject for student {StudentId}", studentId);

        // TODO: Implement proper subject selection logic
        // For Phase 2, we'll use Mathematics as the default subject
        // In Phase 3, we'll enhance this with proper priority calculation based on:
        // - Time since last assessment per subject
        // - Average scores per subject  
        // - Performance trends
        // - Grade level curriculum requirements

        Logger.LogInformation("Selecting Mathematics as assessment subject for student {StudentId} (Phase 2 default)", studentId);
        return Subject.Mathematics;
    }



    /// <summary>
    /// Analyzes student progress across all subjects.
    /// TODO: Implement comprehensive progress analysis.
    /// </summary>
    private async Task<AgentTask> AnalyzeProgressAsync(AgentTask task)
    {
        Logger.LogInformation("Analyzing student progress (stub implementation)");

        // TODO: Implement progress analysis logic
        // - Load all student assessments
        // - Calculate subject mastery levels
        // - Identify strengths and weaknesses
        // - Detect learning patterns
        // - Generate progress report

        task.Result = new
        {
            implemented = false,
            message = "Progress analysis not yet implemented - Phase 2 stub"
        };

        return await Task.FromResult(task);
    }

    /// <summary>
    /// Recommends personalized study path for student.
    /// TODO: Implement study path recommendation engine.
    /// </summary>
    private async Task<AgentTask> RecommendStudyPathAsync(AgentTask task)
    {
        Logger.LogInformation("Recommending study path (stub implementation)");

        // TODO: Implement study path recommendation logic
        // - Analyze student's current knowledge state
        // - Identify knowledge gaps
        // - Sequence topics by prerequisite dependencies
        // - Consider student's learning velocity
        // - Generate personalized study plan

        task.Result = new
        {
            implemented = false,
            message = "Study path recommendation not yet implemented - Phase 2 stub"
        };

        return await Task.FromResult(task);
    }

    /// <summary>
    /// Schedules assessments for student across multiple subjects.
    /// TODO: Implement intelligent scheduling system.
    /// </summary>
    private async Task<AgentTask> ScheduleAssessmentsAsync(AgentTask task)
    {
        Logger.LogInformation("Scheduling assessments (stub implementation)");

        // TODO: Implement assessment scheduling logic
        // - Determine optimal assessment frequency per subject
        // - Avoid assessment fatigue (limit per day/week)
        // - Consider student's schedule and availability
        // - Balance across subjects
        // - Create calendar of upcoming assessments

        task.Result = new
        {
            implemented = false,
            message = "Assessment scheduling not yet implemented - Phase 2 stub"
        };

        return await Task.FromResult(task);
    }
}
