using System.Linq;
using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
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
    /// Determines which subject should be assessed next for a student using intelligent prioritization.
    /// Priority logic:
    /// 1. Never assessed subjects (highest priority)
    /// 2. Subjects with declining performance trends
    /// 3. Subjects not assessed recently
    /// 4. Subjects with lowest mastery scores
    /// </summary>
    private async Task<Subject> DetermineNextAssessmentSubjectAsync(Guid studentId, GradeLevel gradeLevel)
    {
        Logger.LogDebug("Determining next assessment subject for student {StudentId}", studentId);

        // Get all past assessments for this student
        var assessmentsResult = await _studentAssessmentRepository.GetByStudentIdAsync(studentId);
        
        if (assessmentsResult is AcademicAssessment.Core.Common.Result<IReadOnlyList<StudentAssessment>>.Failure failure)
        {
            Logger.LogWarning("Failed to load assessments for student {StudentId}: {Error}", 
                studentId, failure.Error.Message);
            // Default to Mathematics if we can't load history
            return Subject.Mathematics;
        }

        var assessments = ((AcademicAssessment.Core.Common.Result<IReadOnlyList<StudentAssessment>>.Success)assessmentsResult).Value;
        
        if (!assessments.Any())
        {
            // No assessment history - start with Mathematics as foundation subject
            Logger.LogInformation("Student {StudentId} has no assessment history, starting with Mathematics", studentId);
            return Subject.Mathematics;
        }

        // Calculate priority scores for each subject
        var subjectPriorities = new Dictionary<Subject, double>();
        var allSubjects = Enum.GetValues<Subject>();

        foreach (var subject in allSubjects)
        {
            var subjectAssessments = assessments
                .Where(a => GetAssessmentSubject(a.AssessmentId) == subject)
                .OrderByDescending(a => a.CompletedAt ?? a.StartedAt)
                .ToList();

            double priority = CalculateSubjectPriority(
                subject, 
                subjectAssessments, 
                DateTime.UtcNow);

            subjectPriorities[subject] = priority;

            Logger.LogDebug("Subject {Subject} priority: {Priority:F2}", subject, priority);
        }

        // Select subject with highest priority
        var selectedSubject = subjectPriorities.OrderByDescending(kvp => kvp.Value).First().Key;
        
        Logger.LogInformation(
            "Selected {Subject} for student {StudentId} (priority: {Priority:F2})", 
            selectedSubject, 
            studentId, 
            subjectPriorities[selectedSubject]);

        return selectedSubject;
    }

    /// <summary>
    /// Calculates priority score for a subject based on multiple factors.
    /// Higher score = higher priority for assessment.
    /// </summary>
    private double CalculateSubjectPriority(
        Subject subject,
        List<StudentAssessment> subjectAssessments,
        DateTime now)
    {
        // Base priority starts at 50
        double priority = 50.0;

        // Factor 1: Never assessed (highest priority boost)
        if (!subjectAssessments.Any())
        {
            Logger.LogDebug("{Subject}: Never assessed, adding +100 priority", subject);
            return priority + 100.0;
        }

        // Factor 2: Time since last assessment (recency)
        var lastAssessment = subjectAssessments.First();
        var daysSinceLastAssessment = (now - (lastAssessment.CompletedAt ?? lastAssessment.StartedAt!.Value).DateTime).TotalDays;
        
        // Add priority based on days elapsed (max +40 at 30+ days)
        var recencyBonus = Math.Min(daysSinceLastAssessment * 1.33, 40.0);
        priority += recencyBonus;
        Logger.LogDebug("{Subject}: {Days:F1} days since last assessment, adding +{Bonus:F1} priority", 
            subject, daysSinceLastAssessment, recencyBonus);

        // Factor 3: Performance trend (declining performance = higher priority)
        if (subjectAssessments.Count >= 3)
        {
            var recentThree = subjectAssessments.Take(3).ToList();
            var scores = recentThree
                .Where(a => a.PercentageScore.HasValue)
                .Select(a => a.PercentageScore!.Value)
                .ToList();

            if (scores.Count >= 2)
            {
                // Calculate trend (negative = declining)
                var trend = scores[0] - scores[^1]; // Most recent - oldest
                
                if (trend < 0) // Declining performance
                {
                    var trendPenalty = Math.Abs(trend) * 0.3; // Up to +30 for 100% decline
                    priority += trendPenalty;
                    Logger.LogDebug("{Subject}: Declining trend ({Trend:F1}%), adding +{Penalty:F1} priority",
                        subject, trend, trendPenalty);
                }
                else if (trend > 20) // Strong improvement
                {
                    priority -= 10.0; // Reduce priority for subjects doing well
                    Logger.LogDebug("{Subject}: Strong improvement (+{Trend:F1}%), reducing priority by 10",
                        subject, trend);
                }
            }
        }

        // Factor 4: Average mastery level (low mastery = higher priority)
        var completedAssessments = subjectAssessments
            .Where(a => a.Status == AssessmentStatus.Completed && a.PercentageScore.HasValue)
            .ToList();

        if (completedAssessments.Any())
        {
            var avgScore = completedAssessments.Average(a => a.PercentageScore!.Value);
            
            // Below 70% = need more practice
            if (avgScore < 70)
            {
                var masteryBonus = (70 - avgScore) * 0.4; // Up to +28 for 0% avg
                priority += masteryBonus;
                Logger.LogDebug("{Subject}: Low mastery ({Avg:F1}%), adding +{Bonus:F1} priority",
                    subject, avgScore, masteryBonus);
            }
            // Above 90% = doing well, reduce priority
            else if (avgScore > 90)
            {
                priority -= 15.0;
                Logger.LogDebug("{Subject}: High mastery ({Avg:F1}%), reducing priority by 15",
                    subject, avgScore);
            }
        }

        // Factor 5: Assessment frequency (avoid over-testing same subject)
        var assessmentsLastWeek = subjectAssessments.Count(a => 
            (now - (a.CompletedAt ?? a.StartedAt!.Value).DateTime).TotalDays <= 7);
        
        if (assessmentsLastWeek >= 3)
        {
            priority -= 25.0; // Reduce priority if assessed too frequently
            Logger.LogDebug("{Subject}: {Count} assessments in last week, reducing priority by 25",
                subject, assessmentsLastWeek);
        }

        return Math.Max(0, priority); // Never negative
    }

    /// <summary>
    /// Gets the subject for an assessment.
    /// TODO: In production, query the Assessment entity to get its subject.
    /// For now, we'll extract from assessment metadata or default to Mathematics.
    /// </summary>
    private Subject GetAssessmentSubject(Guid assessmentId)
    {
        // TODO: Query database to get assessment's subject
        // For now, return Mathematics as placeholder
        // This should be replaced with: await _assessmentRepository.GetSubjectAsync(assessmentId)
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
