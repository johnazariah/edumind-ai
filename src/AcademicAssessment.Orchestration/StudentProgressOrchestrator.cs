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

            // Create task for subject agent (temporary task for routing)
            var taskForRouting = new AgentTask
            {
                Type = "generate_assessment",
                SourceAgentId = AgentCard.AgentId,
                Data = new
                {
                    studentId = studentId,
                    subject = targetSubject,
                    gradeLevel = student.GradeLevel,
                    questionCount = 10,
                    difficultyAdaptive = true
                }
            };

            // Use intelligent routing to select best agent
            var selectedAgent = await RouteTaskToAgent(
                requiredSkill: "generate_assessment",
                task: taskForRouting,
                subjectFilter: targetSubject,
                gradeLevelFilter: student.GradeLevel,
                priority: 5); // Normal priority

            if (selectedAgent == null)
            {
                throw new InvalidOperationException($"No agents available for subject: {targetSubject}");
            }

            Logger.LogInformation("Selected agent {AgentName} ({AgentId}) for {Subject} assessment via intelligent routing",
                selectedAgent.Name, selectedAgent.AgentId, targetSubject);

            // Create final task with selected agent
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

    /// <summary>
    /// Routes a task to the most suitable agent based on capabilities, availability, and current load.
    /// Implements intelligent agent selection with fallback logic and priority queuing.
    /// </summary>
    /// <param name="requiredSkill">The skill required to complete the task</param>
    /// <param name="task">The task to route</param>
    /// <param name="subjectFilter">Optional subject filter for subject-specific agents</param>
    /// <param name="gradeLevelFilter">Optional grade level filter</param>
    /// <param name="priority">Task priority (0=low, 5=medium, 10=high)</param>
    /// <returns>The selected agent's card, or null if no suitable agent found</returns>
    private async Task<AgentCard?> RouteTaskToAgent(
        string requiredSkill,
        AgentTask task,
        Subject? subjectFilter = null,
        GradeLevel? gradeLevelFilter = null,
        int priority = 5)
    {
        Logger.LogInformation(
            "Routing task {TaskId} (Type: {TaskType}, Skill: {Skill}, Priority: {Priority})",
            task.TaskId, task.Type, requiredSkill, priority);

        // Step 1: Discover agents with required capability
        var candidateAgents = await DiscoverAgentsByCapability(
            requiredSkill,
            subjectFilter,
            gradeLevelFilter);

        if (!candidateAgents.Any())
        {
            Logger.LogWarning(
                "No agents found for skill '{Skill}', subject '{Subject}', grade level '{GradeLevel}'",
                requiredSkill, subjectFilter, gradeLevelFilter);
            return null;
        }

        Logger.LogDebug("Found {Count} candidate agents for skill '{Skill}'",
            candidateAgents.Count, requiredSkill);

        // Step 2: Filter by availability and health
        var availableAgents = FilterAvailableAgents(candidateAgents);

        if (!availableAgents.Any())
        {
            Logger.LogWarning(
                "Found {CandidateCount} candidate agents, but none are currently available",
                candidateAgents.Count);
            
            // Fallback: use any candidate agent (best effort)
            Logger.LogInformation("Using fallback selection - picking first candidate agent");
            return candidateAgents.First();
        }

        Logger.LogDebug("{AvailableCount} of {CandidateCount} agents are available",
            availableAgents.Count, candidateAgents.Count);

        // Step 3: Score agents based on load balancing and capability match
        var scoredAgents = ScoreAgentsForTask(availableAgents, task, priority);

        // Step 4: Select best agent
        var selectedAgent = scoredAgents.OrderByDescending(s => s.Score).First();

        Logger.LogInformation(
            "Selected agent '{AgentName}' ({AgentId}) with score {Score:F2} for task {TaskId}",
            selectedAgent.Agent.Name,
            selectedAgent.Agent.AgentId,
            selectedAgent.Score,
            task.TaskId);

        // Step 5: Track agent workload (for load balancing)
        TrackAgentWorkload(selectedAgent.Agent.AgentId, task.TaskId);

        return selectedAgent.Agent;
    }

    /// <summary>
    /// Discovers agents by capability, with optional filters for subject and grade level.
    /// </summary>
    private async Task<List<AgentCard>> DiscoverAgentsByCapability(
        string requiredSkill,
        Subject? subjectFilter = null,
        GradeLevel? gradeLevelFilter = null)
    {
        // Discover all agents with the required skill
        var agentsWithSkill = await TaskService.DiscoverAgentsAsync(skill: requiredSkill);

        // Apply subject filter if specified
        if (subjectFilter.HasValue)
        {
            agentsWithSkill = agentsWithSkill
                .Where(a => a.Subject.HasValue && a.Subject.Value == subjectFilter.Value)
                .ToList();
        }

        // Apply grade level filter if specified
        if (gradeLevelFilter.HasValue)
        {
            agentsWithSkill = agentsWithSkill
                .Where(a => a.SupportedGradeLevels.Contains(gradeLevelFilter.Value))
                .ToList();
        }

        return agentsWithSkill;
    }

    /// <summary>
    /// Filters agents to only those currently available (health check passed).
    /// In production, this would check heartbeat, connection status, etc.
    /// </summary>
    private List<AgentCard> FilterAvailableAgents(List<AgentCard> agents)
    {
        // TODO: Implement actual health checking
        // For now, assume all agents are available
        // In production, check:
        // - Heartbeat timestamp (within last 30 seconds)
        // - Connection status
        // - Not in maintenance mode
        // - CPU/memory usage within limits

        return agents;
    }

    /// <summary>
    /// Scores agents for task assignment based on multiple factors.
    /// </summary>
    private List<ScoredAgent> ScoreAgentsForTask(
        List<AgentCard> agents,
        AgentTask task,
        int priority)
    {
        var scored = new List<ScoredAgent>();

        foreach (var agent in agents)
        {
            double score = 0.0;

            // Factor 1: Current workload (40 points max)
            // Agents with less work get higher scores
            int currentLoad = GetAgentWorkload(agent.AgentId);
            double loadScore = Math.Max(0, 40.0 - (currentLoad * 5.0)); // -5 points per task
            score += loadScore;

            // Factor 2: Capability match quality (30 points max)
            // Agents with more skills that match the task get higher scores
            double capabilityScore = CalculateCapabilityMatchScore(agent, task);
            score += capabilityScore;

            // Factor 3: Agent version/freshness (20 points max)
            // Newer agents (higher version) get preference for compatibility
            double versionScore = ParseVersionScore(agent.Version);
            score += versionScore;

            // Factor 4: Historical performance (10 points max)
            // Agents with better success rates get preference
            // TODO: Track historical success rate per agent
            double performanceScore = 10.0; // Default: assume good performance
            score += performanceScore;

            Logger.LogDebug(
                "Agent '{AgentName}' score: {TotalScore:F2} (load: {LoadScore:F2}, " +
                "capability: {CapScore:F2}, version: {VersionScore:F2}, perf: {PerfScore:F2})",
                agent.Name, score, loadScore, capabilityScore, versionScore, performanceScore);

            scored.Add(new ScoredAgent
            {
                Agent = agent,
                Score = score,
                LoadScore = loadScore,
                CapabilityScore = capabilityScore,
                VersionScore = versionScore,
                PerformanceScore = performanceScore
            });
        }

        return scored;
    }

    /// <summary>
    /// Calculates how well an agent's capabilities match the task requirements.
    /// </summary>
    private double CalculateCapabilityMatchScore(AgentCard agent, AgentTask task)
    {
        double score = 0.0;

        // Check if agent has the primary skill needed
        // This is already guaranteed by discovery, so give base points
        score += 15.0;

        // Bonus points for additional relevant capabilities
        if (agent.Capabilities != null)
        {
            // Check for specialized capabilities
            if (agent.Capabilities.ContainsKey("assessment_types"))
            {
                score += 5.0; // Agent supports multiple assessment types
            }

            if (agent.Capabilities.ContainsKey("adaptive_difficulty"))
            {
                score += 5.0; // Agent supports adaptive difficulty
            }

            if (agent.Capabilities.ContainsKey("max_concurrent_students"))
            {
                score += 5.0; // Agent can handle concurrent work
            }
        }

        return Math.Min(score, 30.0); // Cap at 30 points
    }

    /// <summary>
    /// Parses agent version string and returns a score (higher version = higher score).
    /// </summary>
    private double ParseVersionScore(string version)
    {
        try
        {
            // Parse semantic version (e.g., "1.2.3")
            var parts = version.Split('.');
            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor))
            {
                // Score: major * 10 + minor, capped at 20
                return Math.Min(major * 10.0 + minor, 20.0);
            }
        }
        catch
        {
            // Ignore parse errors
        }

        // Default score for unparseable versions
        return 10.0;
    }

    // Simple in-memory workload tracking
    // In production, this would use Redis or a distributed cache
    private readonly Dictionary<string, HashSet<string>> _agentWorkloads = new();
    private readonly object _workloadLock = new();

    /// <summary>
    /// Gets the current workload (number of active tasks) for an agent.
    /// </summary>
    private int GetAgentWorkload(string agentId)
    {
        lock (_workloadLock)
        {
            return _agentWorkloads.TryGetValue(agentId, out var tasks) ? tasks.Count : 0;
        }
    }

    /// <summary>
    /// Tracks that an agent is now working on a task.
    /// </summary>
    private void TrackAgentWorkload(string agentId, string taskId)
    {
        lock (_workloadLock)
        {
            if (!_agentWorkloads.ContainsKey(agentId))
            {
                _agentWorkloads[agentId] = new HashSet<string>();
            }
            _agentWorkloads[agentId].Add(taskId);

            Logger.LogDebug("Agent '{AgentId}' workload now: {Count} task(s)",
                agentId, _agentWorkloads[agentId].Count);
        }
    }

    /// <summary>
    /// Removes a task from an agent's workload tracking (when task completes).
    /// </summary>
    private void UntrackAgentWorkload(string agentId, string taskId)
    {
        lock (_workloadLock)
        {
            if (_agentWorkloads.TryGetValue(agentId, out var tasks))
            {
                tasks.Remove(taskId);
                Logger.LogDebug("Agent '{AgentId}' workload now: {Count} task(s)",
                    agentId, tasks.Count);
            }
        }
    }

    /// <summary>
    /// Adjusts difficulty for next assessment using Item Response Theory (IRT) principles.
    /// </summary>
    /// <param name="studentId">The student whose difficulty to adjust</param>
    /// <param name="subject">The subject being assessed</param>
    /// <param name="assessmentHistory">Recent assessment history for this subject</param>
    /// <returns>Adjusted difficulty level (IRT theta estimate) and recommended next difficulty</returns>
    private (double currentAbility, double recommendedDifficulty) AdjustDifficulty(
        Guid studentId,
        Subject subject,
        List<StudentAssessment> assessmentHistory)
    {
        Logger.LogDebug("Adjusting difficulty for Student {StudentId}, Subject: {Subject}",
            studentId, subject);

        // Default starting ability (IRT theta) for new students
        const double DefaultAbility = 0.0;
        const double DifficultyStep = 0.2;
        const double MinDifficulty = -3.0;
        const double MaxDifficulty = 3.0;

        // If no history, start at default
        if (!assessmentHistory.Any())
        {
            Logger.LogDebug("No assessment history - starting at default ability {DefaultAbility}",
                DefaultAbility);
            return (DefaultAbility, DefaultAbility);
        }

        // Get most recent completed assessments (up to last 5 for trend analysis)
        var recentAssessments = assessmentHistory
            .Where(a => a.Status == AssessmentStatus.Completed && a.PercentageScore.HasValue)
            .OrderByDescending(a => a.CompletedAt)
            .Take(5)
            .ToList();

        if (!recentAssessments.Any())
        {
            Logger.LogDebug("No completed assessments - starting at default ability");
            return (DefaultAbility, DefaultAbility);
        }

        // Calculate current ability estimate from most recent assessment
        var latestAssessment = recentAssessments.First();
        double currentAbility = latestAssessment.EstimatedAbility ?? DefaultAbility;

        // Calculate average accuracy across recent assessments
        double averageAccuracy = recentAssessments.Average(a => a.PercentageScore!.Value);

        // Calculate accuracy of most recent assessment
        double latestAccuracy = latestAssessment.PercentageScore!.Value;

        Logger.LogDebug(
            "Current ability: {CurrentAbility:F2}, Latest accuracy: {LatestAccuracy:F1}%, " +
            "Average accuracy (last {Count}): {AverageAccuracy:F1}%",
            currentAbility, latestAccuracy, recentAssessments.Count, averageAccuracy);

        // IRT-based difficulty adjustment
        double difficultyAdjustment = 0.0;

        // Rule 1: High performance (>80%) - increase difficulty
        if (latestAccuracy > 80.0)
        {
            difficultyAdjustment = DifficultyStep;
            Logger.LogDebug("High performance ({Accuracy:F1}%) - increasing difficulty by {Step}",
                latestAccuracy, DifficultyStep);
        }
        // Rule 2: Low performance (<50%) - decrease difficulty
        else if (latestAccuracy < 50.0)
        {
            difficultyAdjustment = -DifficultyStep;
            Logger.LogDebug("Low performance ({Accuracy:F1}%) - decreasing difficulty by {Step}",
                latestAccuracy, DifficultyStep);
        }
        // Rule 3: Medium performance (50-80%) - fine-tune based on velocity
        else
        {
            // Calculate velocity (change in performance over recent attempts)
            if (recentAssessments.Count >= 3)
            {
                var oldest = recentAssessments.Last().PercentageScore!.Value;
                var newest = recentAssessments.First().PercentageScore!.Value;
                double velocity = newest - oldest;

                // If improving rapidly, increase difficulty slightly
                if (velocity > 10.0)
                {
                    difficultyAdjustment = DifficultyStep * 0.5;
                    Logger.LogDebug(
                        "Improving trend (velocity: +{Velocity:F1}%) - small difficulty increase",
                        velocity);
                }
                // If declining, decrease difficulty slightly
                else if (velocity < -10.0)
                {
                    difficultyAdjustment = -DifficultyStep * 0.5;
                    Logger.LogDebug(
                        "Declining trend (velocity: {Velocity:F1}%) - small difficulty decrease",
                        velocity);
                }
                else
                {
                    Logger.LogDebug("Stable performance - maintaining current difficulty");
                }
            }
        }

        // Apply adjustment with bounds checking
        double recommendedDifficulty = Math.Clamp(
            currentAbility + difficultyAdjustment,
            MinDifficulty,
            MaxDifficulty);

        Logger.LogInformation(
            "Difficulty adjustment complete: Current={CurrentAbility:F2}, " +
            "Recommended={RecommendedDifficulty:F2}, Change={Change:+F2}",
            currentAbility, recommendedDifficulty, difficultyAdjustment);

        return (currentAbility, recommendedDifficulty);
    }

    /// <summary>
    /// Optimizes learning path by identifying knowledge gaps and sequencing topics.
    /// </summary>
    /// <param name="studentId">The student ID</param>
    /// <param name="assessmentHistory">Complete assessment history across all subjects</param>
    /// <returns>Optimized learning path with topic sequence and estimated time to mastery</returns>
    private async Task<LearningPath> OptimizeLearningPathAsync(
        Guid studentId,
        IReadOnlyList<StudentAssessment> assessmentHistory)
    {
        Logger.LogDebug("Optimizing learning path for Student {StudentId}", studentId);

        // Group assessments by subject to identify weak areas
        var subjectPerformance = assessmentHistory
            .Where(a => a.Status == AssessmentStatus.Completed && a.PercentageScore.HasValue)
            .GroupBy(a => GetAssessmentSubject(a.AssessmentId))
            .Select(g => new
            {
                Subject = g.Key,
                AverageScore = g.Average(a => a.PercentageScore!.Value),
                AssessmentCount = g.Count(),
                LastAssessment = g.OrderByDescending(a => a.CompletedAt).First(),
                Trend = CalculateTrend(g.OrderByDescending(a => a.CompletedAt).Take(3).ToList())
            })
            .OrderBy(s => s.AverageScore) // Weakest subjects first
            .ToList();

        Logger.LogDebug("Analyzed {Count} subjects for learning path optimization",
            subjectPerformance.Count);

        // Identify knowledge gaps (subjects with <70% average)
        var knowledgeGaps = subjectPerformance
            .Where(s => s.AverageScore < 70.0)
            .Select(s => new KnowledgeGap
            {
                Subject = s.Subject,
                CurrentMastery = s.AverageScore,
                TargetMastery = 80.0, // Target 80% mastery
                EstimatedHours = EstimateTimeToMastery(s.AverageScore, 80.0, s.Trend),
                Priority = CalculateGapPriority(s.AverageScore, s.Trend, (s.LastAssessment.CompletedAt ?? DateTimeOffset.UtcNow).DateTime)
            })
            .OrderByDescending(g => g.Priority)
            .ToList();

        // Identify subjects needing reinforcement (70-85% average)
        var reinforcementTopics = subjectPerformance
            .Where(s => s.AverageScore >= 70.0 && s.AverageScore < 85.0)
            .Select(s => new ReinforcementTopic
            {
                Subject = s.Subject,
                CurrentMastery = s.AverageScore,
                EstimatedHours = 2.0, // Quick reinforcement
                RecommendedFrequency = TimeSpan.FromDays(7) // Weekly review
            })
            .ToList();

        // Sequence topics by prerequisite dependencies (simplified for now)
        var sequencedTopics = SequenceTopicsByPrerequisites(knowledgeGaps, reinforcementTopics);

        // Calculate total estimated time
        double totalEstimatedHours = knowledgeGaps.Sum(g => g.EstimatedHours) +
                                      reinforcementTopics.Sum(r => r.EstimatedHours);

        Logger.LogInformation(
            "Learning path optimized: {GapCount} knowledge gaps, {ReinforcementCount} reinforcement topics, " +
            "estimated {TotalHours:F1} hours",
            knowledgeGaps.Count, reinforcementTopics.Count, totalEstimatedHours);

        return new LearningPath
        {
            StudentId = studentId,
            GeneratedAt = DateTime.UtcNow,
            KnowledgeGaps = knowledgeGaps,
            ReinforcementTopics = reinforcementTopics,
            SequencedTopics = sequencedTopics,
            TotalEstimatedHours = totalEstimatedHours,
            EstimatedCompletionDate = DateTime.UtcNow.AddHours(totalEstimatedHours * 2) // Assume 30 min/day study
        };
    }

    /// <summary>
    /// Calculates performance trend from recent assessments.
    /// </summary>
    /// <param name="recentAssessments">Recent assessments ordered by date (newest first)</param>
    /// <returns>Trend value: positive for improving, negative for declining</returns>
    private double CalculateTrend(List<StudentAssessment> recentAssessments)
    {
        if (recentAssessments.Count < 2)
        {
            return 0.0; // Not enough data for trend
        }

        // Calculate simple linear trend (newest - oldest)
        var newest = recentAssessments.First().PercentageScore ?? 0.0;
        var oldest = recentAssessments.Last().PercentageScore ?? 0.0;

        return newest - oldest;
    }

    /// <summary>
    /// Calculates priority for addressing a knowledge gap.
    /// </summary>
    private double CalculateGapPriority(double currentMastery, double trend, DateTime lastAssessment)
    {
        double priority = 0.0;

        // Factor 1: Severity of gap (0-50 points)
        priority += (70.0 - currentMastery) * 0.7; // Max 50 points for 0% mastery

        // Factor 2: Declining trend (0-30 points)
        if (trend < 0)
        {
            priority += Math.Abs(trend) * 0.3; // Up to 30 points for steep decline
        }

        // Factor 3: Recency (0-20 points)
        var daysSinceLastAssessment = (DateTime.UtcNow - lastAssessment).TotalDays;
        if (daysSinceLastAssessment > 30)
        {
            priority += 20.0; // High priority if not assessed recently
        }
        else if (daysSinceLastAssessment > 14)
        {
            priority += 10.0;
        }

        return priority;
    }

    /// <summary>
    /// Estimates hours needed to reach target mastery level.
    /// </summary>
    private double EstimateTimeToMastery(double currentMastery, double targetMastery, double trend)
    {
        double gap = targetMastery - currentMastery;

        // Base estimate: 1 hour per 5% improvement needed
        double baseHours = gap / 5.0;

        // Adjust based on trend
        if (trend > 0)
        {
            // Already improving - reduce estimate by 20%
            return baseHours * 0.8;
        }
        else if (trend < -5.0)
        {
            // Declining significantly - increase estimate by 50%
            return baseHours * 1.5;
        }

        return Math.Max(1.0, baseHours); // Minimum 1 hour
    }

    /// <summary>
    /// Sequences topics by prerequisite dependencies (simplified algorithm).
    /// </summary>
    private List<string> SequenceTopicsByPrerequisites(
        List<KnowledgeGap> gaps,
        List<ReinforcementTopic> reinforcements)
    {
        // Simplified sequencing: prioritize by priority score
        // In a full implementation, this would use a dependency graph
        var sequence = new List<string>();

        // Add critical gaps first
        sequence.AddRange(gaps
            .OrderByDescending(g => g.Priority)
            .Select(g => $"Master {g.Subject} (Current: {g.CurrentMastery:F0}%, Target: {g.TargetMastery:F0}%)"));

        // Add reinforcement topics
        sequence.AddRange(reinforcements
            .Select(r => $"Reinforce {r.Subject} (Current: {r.CurrentMastery:F0}%)"));

        return sequence;
    }
}

// Supporting classes for learning path optimization

public record LearningPath
{
    public required Guid StudentId { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required List<KnowledgeGap> KnowledgeGaps { get; init; }
    public required List<ReinforcementTopic> ReinforcementTopics { get; init; }
    public required List<string> SequencedTopics { get; init; }
    public required double TotalEstimatedHours { get; init; }
    public required DateTime EstimatedCompletionDate { get; init; }
}

public record KnowledgeGap
{
    public required Subject Subject { get; init; }
    public required double CurrentMastery { get; init; }
    public required double TargetMastery { get; init; }
    public required double EstimatedHours { get; init; }
    public required double Priority { get; init; }
}

public record ReinforcementTopic
{
    public required Subject Subject { get; init; }
    public required double CurrentMastery { get; init; }
    public required double EstimatedHours { get; init; }
    public required TimeSpan RecommendedFrequency { get; init; }
}

/// <summary>
/// Represents an agent with its calculated routing score.
/// Used for intelligent task routing and load balancing.
/// </summary>
internal class ScoredAgent
{
    public required AgentCard Agent { get; init; }
    public required double Score { get; init; }
    public required double LoadScore { get; init; }
    public required double CapabilityScore { get; init; }
    public required double VersionScore { get; init; }
    public required double PerformanceScore { get; init; }
}
