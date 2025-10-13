using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Analytics.Services;

/// <summary>
/// Student analytics service implementation
/// Provides performance metrics, progress tracking, and peer comparisons
/// </summary>
public sealed class StudentAnalyticsService : IStudentAnalyticsService
{
    private readonly IStudentAssessmentRepository _studentAssessmentRepository;
    private readonly IStudentResponseRepository _studentResponseRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<StudentAnalyticsService> _logger;

    public StudentAnalyticsService(
        IStudentAssessmentRepository studentAssessmentRepository,
        IStudentResponseRepository studentResponseRepository,
        IQuestionRepository questionRepository,
        IAssessmentRepository assessmentRepository,
        ILogger<StudentAnalyticsService> logger)
    {
        _studentAssessmentRepository = studentAssessmentRepository ?? throw new ArgumentNullException(nameof(studentAssessmentRepository));
        _studentResponseRepository = studentResponseRepository ?? throw new ArgumentNullException(nameof(studentResponseRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _assessmentRepository = assessmentRepository ?? throw new ArgumentNullException(nameof(assessmentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<StudentPerformanceSummary>> GetStudentPerformanceSummaryAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting performance summary for student {StudentId}", studentId);

        // Get all completed assessments for the student
        var completedResult = await _studentAssessmentRepository.GetCompletedByStudentAsync(studentId, cancellationToken);
        if (completedResult is not Result<IReadOnlyList<StudentAssessment>>.Success completedSuccess)
        {
            // Propagate the failure
            return completedResult is Result<IReadOnlyList<StudentAssessment>>.Failure failure
                ? Result.Failure<StudentPerformanceSummary>(failure.Error)
                : Result.Failure<StudentPerformanceSummary>(new Error("Unexpected", "Unexpected result state"));
        }

        var completedAssessments = completedSuccess.Value;

        // If no assessments, return zero-state summary
        if (completedAssessments.Count == 0)
        {
            return Result.Success(new StudentPerformanceSummary
            {
                StudentId = studentId,
                TotalAssessmentsTaken = 0,
                AverageScore = 0.0,
                OverallMastery = 0.0,
                SubjectScores = new Dictionary<Subject, double>(),
                TotalTimeSpent = TimeSpan.Zero,
                LastAssessmentDate = DateTimeOffset.MinValue,
                CurrentStreak = 0
            });
        }

        // Get all assessments to join with subject information
        var assessmentIds = completedAssessments.Select(sa => sa.AssessmentId).Distinct().ToList();
        var assessmentTasks = assessmentIds.Select(id => _assessmentRepository.GetByIdAsync(id, cancellationToken));
        var assessmentResults = await Task.WhenAll(assessmentTasks);

        // Build assessment lookup dictionary
        var assessmentLookup = new Dictionary<Guid, Assessment>();
        foreach (var result in assessmentResults)
        {
            if (result is Result<Assessment>.Success success)
            {
                assessmentLookup[success.Value.Id] = success.Value;
            }
        }

        // Calculate metrics
        var scoresWithAssessments = completedAssessments
            .Where(sa => sa.PercentageScore.HasValue && assessmentLookup.ContainsKey(sa.AssessmentId))
            .ToList();

        var totalAssessmentsTaken = completedAssessments.Count;
        var averageScore = scoresWithAssessments.Any()
            ? scoresWithAssessments.Average(sa => sa.PercentageScore!.Value)
            : 0.0;

        // Calculate subject scores
        var subjectScores = scoresWithAssessments
            .GroupBy(sa => assessmentLookup[sa.AssessmentId].Subject)
            .ToDictionary(
                g => g.Key,
                g => g.Average(sa => sa.PercentageScore!.Value)
            );

        // Calculate overall mastery (average of subject scores, or overall average if no subjects)
        var overallMastery = subjectScores.Any()
            ? subjectScores.Values.Average() / 100.0  // Convert to 0-1 scale
            : averageScore / 100.0;

        // Calculate total time spent
        var totalTimeSpent = TimeSpan.FromSeconds(
            completedAssessments.Sum(sa => sa.TimeSpentSeconds ?? 0)
        );

        // Get last assessment date
        var lastAssessmentDate = completedAssessments
            .Where(sa => sa.CompletedAt.HasValue)
            .Max(sa => sa.CompletedAt) ?? DateTimeOffset.MinValue;

        // Calculate current streak (consecutive days with assessments)
        var currentStreak = CalculateCurrentStreak(completedAssessments);

        var summary = new StudentPerformanceSummary
        {
            StudentId = studentId,
            TotalAssessmentsTaken = totalAssessmentsTaken,
            AverageScore = averageScore,
            OverallMastery = overallMastery,
            SubjectScores = subjectScores,
            TotalTimeSpent = totalTimeSpent,
            LastAssessmentDate = lastAssessmentDate,
            CurrentStreak = currentStreak
        };

        return Result.Success(summary);
    }

    private int CalculateCurrentStreak(IReadOnlyList<StudentAssessment> completedAssessments)
    {
        var datesWithAssessments = completedAssessments
            .Where(sa => sa.CompletedAt.HasValue)
            .Select(sa => sa.CompletedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (datesWithAssessments.Count == 0)
            return 0;

        var today = DateTimeOffset.UtcNow.Date;
        var streak = 0;
        var checkDate = today;

        foreach (var assessmentDate in datesWithAssessments)
        {
            if (assessmentDate == checkDate || assessmentDate == checkDate.AddDays(-1))
            {
                streak++;
                checkDate = assessmentDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    public async Task<Result<SubjectPerformance>> GetSubjectPerformanceAsync(
        Guid studentId,
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting subject performance for student {StudentId} in {Subject}", studentId, subject);

        // Get all completed assessments for the student
        var completedResult = await _studentAssessmentRepository.GetCompletedByStudentAsync(studentId, cancellationToken);
        if (completedResult is not Result<IReadOnlyList<StudentAssessment>>.Success completedSuccess)
        {
            return completedResult is Result<IReadOnlyList<StudentAssessment>>.Failure failure
                ? Result.Failure<SubjectPerformance>(failure.Error)
                : Result.Failure<SubjectPerformance>(new Error("Unexpected", "Unexpected result state"));
        }

        var completedAssessments = completedSuccess.Value;

        // Get all assessments to filter by subject
        var assessmentIds = completedAssessments.Select(sa => sa.AssessmentId).Distinct().ToList();
        var assessmentTasks = assessmentIds.Select(id => _assessmentRepository.GetByIdAsync(id, cancellationToken));
        var assessmentResults = await Task.WhenAll(assessmentTasks);

        // Build lookup and filter by subject
        var subjectAssessmentIds = new HashSet<Guid>();
        foreach (var result in assessmentResults)
        {
            if (result is Result<Assessment>.Success success && success.Value.Subject == subject)
            {
                subjectAssessmentIds.Add(success.Value.Id);
            }
        }

        // Filter completed assessments by subject
        var subjectAssessments = completedAssessments
            .Where(sa => subjectAssessmentIds.Contains(sa.AssessmentId))
            .ToList();

        // Zero-state handling
        if (subjectAssessments.Count == 0)
        {
            return Result.Success(new SubjectPerformance
            {
                Subject = subject,
                AssessmentsTaken = 0,
                AverageScore = 0.0,
                MasteryLevel = 0.0,
                AbilityEstimate = 0.0,
                QuestionsAnswered = 0,
                QuestionsCorrect = 0,
                AccuracyRate = 0.0,
                AverageTimePerQuestion = TimeSpan.Zero,
                StrongTopics = Array.Empty<string>(),
                WeakTopics = Array.Empty<string>()
            });
        }

        // Calculate assessment-level metrics
        var assessmentsWithScores = subjectAssessments.Where(sa => sa.PercentageScore.HasValue).ToList();
        var assessmentsTaken = subjectAssessments.Count;
        var averageScore = assessmentsWithScores.Any()
            ? assessmentsWithScores.Average(sa => sa.PercentageScore!.Value)
            : 0.0;
        var masteryLevel = averageScore / 100.0; // Convert to 0-1 scale

        // Get student responses for detailed question-level analysis
        var responsesResult = await _studentResponseRepository.GetByStudentIdAsync(studentId, cancellationToken);
        if (responsesResult is not Result<IReadOnlyList<StudentResponse>>.Success responsesSuccess)
        {
            // If we can't get responses, return what we have so far
            return Result.Success(new SubjectPerformance
            {
                Subject = subject,
                AssessmentsTaken = assessmentsTaken,
                AverageScore = averageScore,
                MasteryLevel = masteryLevel,
                AbilityEstimate = (averageScore - 50) / 50 * 3, // Simple IRT scale
                QuestionsAnswered = 0,
                QuestionsCorrect = 0,
                AccuracyRate = 0.0,
                AverageTimePerQuestion = TimeSpan.Zero,
                StrongTopics = Array.Empty<string>(),
                WeakTopics = Array.Empty<string>()
            });
        }

        var allResponses = responsesSuccess.Value;

        // Get all unique question IDs from responses
        var questionIds = allResponses.Select(r => r.QuestionId).Distinct().ToList();
        var questionTasks = questionIds.Select(id => _questionRepository.GetByIdAsync(id, cancellationToken));
        var questionResults = await Task.WhenAll(questionTasks);

        // Build question lookup (only for the target subject)
        var questionLookup = new Dictionary<Guid, Question>();
        foreach (var result in questionResults)
        {
            if (result is Result<Question>.Success success && success.Value.Subject == subject)
            {
                questionLookup[success.Value.Id] = success.Value;
            }
        }

        // Filter responses to only those for questions in this subject
        var subjectResponses = allResponses
            .Where(r => questionLookup.ContainsKey(r.QuestionId))
            .ToList();

        // Calculate question-level metrics
        var questionsAnswered = subjectResponses.Count;
        var questionsCorrect = subjectResponses.Count(r => r.IsCorrect);
        var accuracyRate = questionsAnswered > 0 ? (double)questionsCorrect / questionsAnswered : 0.0;

        var responsesWithTime = subjectResponses.Where(r => r.TimeSpentSeconds > 0).ToList();
        var averageTimePerQuestion = responsesWithTime.Any()
            ? TimeSpan.FromSeconds(responsesWithTime.Average(r => r.TimeSpentSeconds))
            : TimeSpan.Zero;

        // Analyze topics
        var topicPerformance = new Dictionary<string, (int correct, int total)>();
        foreach (var response in subjectResponses)
        {
            if (questionLookup.TryGetValue(response.QuestionId, out var question))
            {
                foreach (var topic in question.Topics)
                {
                    if (!topicPerformance.ContainsKey(topic))
                    {
                        topicPerformance[topic] = (0, 0);
                    }

                    var (correct, total) = topicPerformance[topic];
                    topicPerformance[topic] = (
                        correct + (response.IsCorrect ? 1 : 0),
                        total + 1
                    );
                }
            }
        }

        // Identify strong topics (>80% accuracy) and weak topics (<60% accuracy)
        var strongTopics = topicPerformance
            .Where(kvp => kvp.Value.total >= 3 && (double)kvp.Value.correct / kvp.Value.total > 0.80)
            .OrderByDescending(kvp => (double)kvp.Value.correct / kvp.Value.total)
            .Select(kvp => kvp.Key)
            .ToList();

        var weakTopics = topicPerformance
            .Where(kvp => kvp.Value.total >= 3 && (double)kvp.Value.correct / kvp.Value.total < 0.60)
            .OrderBy(kvp => (double)kvp.Value.correct / kvp.Value.total)
            .Select(kvp => kvp.Key)
            .ToList();

        // Calculate ability estimate (simplified IRT)
        var abilityEstimate = (averageScore - 50) / 50 * 3; // Maps 0-100% to -3 to +3

        return Result.Success(new SubjectPerformance
        {
            Subject = subject,
            AssessmentsTaken = assessmentsTaken,
            AverageScore = averageScore,
            MasteryLevel = masteryLevel,
            AbilityEstimate = abilityEstimate,
            QuestionsAnswered = questionsAnswered,
            QuestionsCorrect = questionsCorrect,
            AccuracyRate = accuracyRate,
            AverageTimePerQuestion = averageTimePerQuestion,
            StrongTopics = strongTopics,
            WeakTopics = weakTopics
        });
    }

    public async Task<Result<IReadOnlyList<LearningObjectiveMastery>>> GetLearningObjectiveMasteryAsync(
        Guid studentId,
        Subject? subject = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting learning objective mastery for student {StudentId} in subject {Subject}",
            studentId, subject?.ToString() ?? "All");

        // Get student responses
        var responsesResult = await _studentResponseRepository.GetByStudentIdAsync(studentId, cancellationToken);
        if (responsesResult is not Result<IReadOnlyList<StudentResponse>>.Success responsesSuccess)
        {
            return responsesResult is Result<IReadOnlyList<StudentResponse>>.Failure failure
                ? Result.Failure<IReadOnlyList<LearningObjectiveMastery>>(failure.Error)
                : Result.Failure<IReadOnlyList<LearningObjectiveMastery>>(new Error("Unexpected", "Unexpected result state"));
        }

        var responses = responsesSuccess.Value;
        if (responses.Count == 0)
        {
            return Result.Success<IReadOnlyList<LearningObjectiveMastery>>(Array.Empty<LearningObjectiveMastery>());
        }

        // Get questions for all responses
        var questionIds = responses.Select(r => r.QuestionId).Distinct().ToList();
        var questionTasks = questionIds.Select(id => _questionRepository.GetByIdAsync(id, cancellationToken));
        var questionResults = await Task.WhenAll(questionTasks);

        // Build question lookup
        var questionLookup = new Dictionary<Guid, Question>();
        foreach (var result in questionResults)
        {
            if (result is Result<Question>.Success success)
            {
                questionLookup[success.Value.Id] = success.Value;
            }
        }

        // Group responses by learning objective
        var objectiveData = new Dictionary<(Subject Subject, string Objective), (int correct, int total, DateTimeOffset lastSeen)>();

        foreach (var response in responses)
        {
            if (!questionLookup.TryGetValue(response.QuestionId, out var question))
            {
                continue;
            }

            // Skip if subject filter is applied and doesn't match
            if (subject.HasValue && question.Subject != subject.Value)
            {
                continue;
            }

            foreach (var objective in question.LearningObjectives)
            {
                var key = (question.Subject, objective);

                if (!objectiveData.ContainsKey(key))
                {
                    objectiveData[key] = (0, 0, response.CreatedAt);
                }

                var current = objectiveData[key];
                objectiveData[key] = (
                    current.correct + (response.IsCorrect ? 1 : 0),
                    current.total + 1,
                    response.CreatedAt > current.lastSeen ? response.CreatedAt : current.lastSeen
                );
            }
        }

        // Convert to LearningObjectiveMastery records
        var masteryList = new List<LearningObjectiveMastery>();
        foreach (var ((subj, objective), (correct, total, lastSeen)) in objectiveData)
        {
            double masteryLevel = total > 0 ? (double)correct / total : 0.0;
            var status = DetermineMasteryStatus(masteryLevel);

            masteryList.Add(new LearningObjectiveMastery
            {
                LearningObjective = objective,
                Subject = subj,
                MasteryLevel = masteryLevel,
                TimesAssessed = total,
                TimesCorrect = correct,
                LastAssessedAt = lastSeen,
                Status = status
            });
        }

        // Sort by subject then by mastery level ascending (weakest first)
        var sortedList = masteryList
            .OrderBy(m => m.Subject)
            .ThenBy(m => m.MasteryLevel)
            .ToList();

        return Result.Success<IReadOnlyList<LearningObjectiveMastery>>(sortedList);
    }

    private static MasteryStatus DetermineMasteryStatus(double masteryLevel)
    {
        return masteryLevel switch
        {
            0.0 => MasteryStatus.NotStarted,
            < 0.25 => MasteryStatus.Beginning,
            < 0.50 => MasteryStatus.Developing,
            < 0.75 => MasteryStatus.Proficient,
            < 0.90 => MasteryStatus.Advanced,
            _ => MasteryStatus.Mastered
        };
    }

    public async Task<Result<IReadOnlyDictionary<Subject, double>>> GetAbilityEstimatesAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting ability estimates for student {StudentId}", studentId);

        // Get all completed assessments for the student
        var completedResult = await _studentAssessmentRepository.GetCompletedByStudentAsync(studentId, cancellationToken);
        if (completedResult is not Result<IReadOnlyList<StudentAssessment>>.Success completedSuccess)
        {
            return completedResult is Result<IReadOnlyList<StudentAssessment>>.Failure failure
                ? Result.Failure<IReadOnlyDictionary<Subject, double>>(failure.Error)
                : Result.Failure<IReadOnlyDictionary<Subject, double>>(new Error("Unexpected", "Unexpected result state"));
        }

        var completedAssessments = completedSuccess.Value
            .Where(a => a.Score.HasValue)  // Ensure assessment has a score
            .ToList();

        // If no completed assessments with scores, return empty dictionary
        if (completedAssessments.Count == 0)
        {
            return Result.Success<IReadOnlyDictionary<Subject, double>>(new Dictionary<Subject, double>());
        }

        // Get all unique assessment IDs
        var assessmentIds = completedAssessments.Select(a => a.AssessmentId).Distinct().ToList();

        // Get assessments in parallel to determine subjects
        var assessmentTasks = assessmentIds.Select(id => _assessmentRepository.GetByIdAsync(id, cancellationToken));
        var assessmentResults = await Task.WhenAll(assessmentTasks);

        // Build assessment ID to subject lookup
        var assessmentSubjects = new Dictionary<Guid, Subject>();
        foreach (var result in assessmentResults)
        {
            if (result is Result<Assessment>.Success success)
            {
                assessmentSubjects[success.Value.Id] = success.Value.Subject;
            }
        }

        // Group assessments by subject and calculate ability estimates
        var abilityEstimates = new Dictionary<Subject, double>();

        var assessmentsBySubject = completedAssessments
            .Where(a => assessmentSubjects.ContainsKey(a.AssessmentId))
            .GroupBy(a => assessmentSubjects[a.AssessmentId]);

        foreach (var subjectGroup in assessmentsBySubject)
        {
            var subject = subjectGroup.Key;
            var scores = subjectGroup.Select(a => a.Score!.Value).ToList();

            // Calculate average score (0-100)
            var averageScore = scores.Average();

            // Scale to IRT range: -3 to +3
            // Formula: (avgScore - 50) / 50 * 3
            // This maps:
            //   0% -> -3.0 (very low ability)
            //  50% -> 0.0 (average ability)
            // 100% -> +3.0 (very high ability)
            var abilityEstimate = (averageScore - 50) / 50 * 3;

            abilityEstimates[subject] = abilityEstimate;
        }

        _logger.LogInformation(
            "Calculated ability estimates for student {StudentId} across {SubjectCount} subjects",
            studentId,
            abilityEstimates.Count);

        return Result.Success<IReadOnlyDictionary<Subject, double>>(abilityEstimates);
    }

    public async Task<Result<IReadOnlyList<ImprovementArea>>> GetImprovementAreasAsync(
        Guid studentId,
        int topN = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Identifying improvement areas for student {StudentId}", studentId);

        const double proficientThreshold = 0.75;

        // Get all student responses
        var responsesResult = await _studentResponseRepository.GetByStudentIdAsync(studentId, cancellationToken);
        if (responsesResult is not Result<IReadOnlyList<StudentResponse>>.Success responsesSuccess)
        {
            return responsesResult is Result<IReadOnlyList<StudentResponse>>.Failure failure
                ? Result.Failure<IReadOnlyList<ImprovementArea>>(failure.Error)
                : Result.Failure<IReadOnlyList<ImprovementArea>>(new Error("Unexpected", "Unexpected result state"));
        }

        var responses = responsesSuccess.Value;
        if (responses.Count == 0)
        {
            return Result.Success<IReadOnlyList<ImprovementArea>>(Array.Empty<ImprovementArea>());
        }

        // Get questions for all responses
        var questionIds = responses.Select(r => r.QuestionId).Distinct().ToList();
        var questionTasks = questionIds.Select(id => _questionRepository.GetByIdAsync(id, cancellationToken));
        var questionResults = await Task.WhenAll(questionTasks);

        // Build question lookup
        var questionLookup = new Dictionary<Guid, Question>();
        foreach (var result in questionResults)
        {
            if (result is Result<Question>.Success success)
            {
                questionLookup[success.Value.Id] = success.Value;
            }
        }

        // Group responses by (Subject, Topic, LearningObjective)
        var areaData = new Dictionary<(Subject Subject, string Topic, string Objective), (int correct, int total)>();

        foreach (var response in responses)
        {
            if (!questionLookup.TryGetValue(response.QuestionId, out var question))
            {
                continue;
            }

            // Create combinations of topics and learning objectives
            var topics = question.Topics.Count > 0 ? question.Topics : new[] { "General" };
            var objectives = question.LearningObjectives.Count > 0 ? question.LearningObjectives : new[] { "General" };

            foreach (var topic in topics)
            {
                foreach (var objective in objectives)
                {
                    var key = (question.Subject, topic, objective);

                    if (!areaData.ContainsKey(key))
                    {
                        areaData[key] = (0, 0);
                    }

                    var (correct, total) = areaData[key];
                    areaData[key] = (correct + (response.IsCorrect ? 1 : 0), total + 1);
                }
            }
        }

        // Create improvement areas
        var improvementAreas = new List<ImprovementArea>();

        foreach (var kvp in areaData)
        {
            var (subject, topic, objective) = kvp.Key;
            var (correct, total) = kvp.Value;

            var currentMastery = (double)correct / total;
            var accuracyRate = currentMastery;
            var masteryGap = proficientThreshold - currentMastery;

            // Determine priority level
            var priority = currentMastery switch
            {
                < 0.25 => PriorityLevel.Critical,
                < 0.50 => PriorityLevel.High,
                < 0.75 => PriorityLevel.Medium,
                _ => PriorityLevel.Low
            };

            // Generate recommended action
            var recommendedAction = priority switch
            {
                PriorityLevel.Critical => $"Immediate intervention needed. Review foundational concepts in {topic}. Consider one-on-one tutoring.",
                PriorityLevel.High => $"Focus practice on {topic}. Review incorrect responses and work through similar problems.",
                PriorityLevel.Medium => $"Continue practicing {topic}. Review key concepts to reach proficiency.",
                PriorityLevel.Low => $"Maintain proficiency in {topic} through regular practice.",
                _ => "Continue practicing."
            };

            improvementAreas.Add(new ImprovementArea
            {
                Subject = subject,
                Topic = topic,
                LearningObjective = objective,
                CurrentMastery = currentMastery,
                TargetMastery = proficientThreshold,
                QuestionsAttempted = total,
                AccuracyRate = accuracyRate,
                RecommendedAction = recommendedAction,
                Priority = priority
            });
        }

        // Sort by priority (Critical > High > Medium > Low) then by mastery ascending (worst first)
        // Priority enum: Low=0, Medium=1, High=2, Critical=3, so we need descending order
        var sortedAreas = improvementAreas
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.CurrentMastery)
            .Take(topN)
            .ToList();

        return Result.Success<IReadOnlyList<ImprovementArea>>(sortedAreas);
    }
    public async Task<Result<ProgressTimeline>> GetProgressTimelineAsync(
        Guid studentId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting progress timeline for student {StudentId}", studentId);

        var effectiveStartDate = startDate ?? DateTimeOffset.MinValue;
        var effectiveEndDate = endDate ?? DateTimeOffset.UtcNow;

        // Get all completed student assessments
        var assessmentsResult = await _studentAssessmentRepository.GetCompletedByStudentAsync(studentId, cancellationToken);
        if (assessmentsResult is not Result<IReadOnlyList<StudentAssessment>>.Success assessmentsSuccess)
        {
            return assessmentsResult is Result<IReadOnlyList<StudentAssessment>>.Failure failure
                ? Result.Failure<ProgressTimeline>(failure.Error)
                : Result.Failure<ProgressTimeline>(new Error("Unexpected", "Unexpected result state"));
        }

        var assessments = assessmentsSuccess.Value;

        // Filter by date range (already filtered for completed status)
        var completedAssessments = assessments
            .Where(sa => sa.CompletedAt.HasValue &&
                        sa.Score.HasValue &&
                        sa.CompletedAt.Value >= effectiveStartDate &&
                        sa.CompletedAt.Value <= effectiveEndDate)
            .OrderBy(sa => sa.CompletedAt!.Value)
            .ToList(); if (completedAssessments.Count == 0)
        {
            return Result.Success(new ProgressTimeline
            {
                StudentId = studentId,
                StartDate = effectiveStartDate,
                EndDate = effectiveEndDate,
                DataPoints = Array.Empty<ProgressDataPoint>(),
                AverageGrowthRate = 0.0,
                SubjectGrowthRates = new Dictionary<Subject, double>()
            });
        }

        // Get assessment details for all completed assessments
        var assessmentIds = completedAssessments.Select(sa => sa.AssessmentId).Distinct().ToList();
        var assessmentTasks = assessmentIds.Select(id => _assessmentRepository.GetByIdAsync(id, cancellationToken));
        var assessmentResults = await Task.WhenAll(assessmentTasks);

        // Build assessment lookup
        var assessmentLookup = new Dictionary<Guid, Assessment>();
        foreach (var result in assessmentResults)
        {
            if (result is Result<Assessment>.Success success)
            {
                assessmentLookup[success.Value.Id] = success.Value;
            }
        }

        // Create data points
        var dataPoints = new List<ProgressDataPoint>();
        foreach (var sa in completedAssessments)
        {
            if (!assessmentLookup.TryGetValue(sa.AssessmentId, out var assessment))
            {
                continue;
            }

            var percentageScore = sa.PercentageScore ?? 0.0;
            var masteryLevel = percentageScore / 100.0;

            dataPoints.Add(new ProgressDataPoint
            {
                Date = sa.CompletedAt!.Value,
                Subject = assessment.Subject,
                Score = percentageScore,
                MasteryLevel = masteryLevel,
                AssessmentType = assessment.AssessmentType
            });
        }

        // Calculate overall growth rate
        double averageGrowthRate = 0.0;
        if (dataPoints.Count >= 2)
        {
            var firstScore = dataPoints.First().Score;
            var lastScore = dataPoints.Last().Score;
            var daysDiff = (dataPoints.Last().Date - dataPoints.First().Date).TotalDays;

            if (daysDiff > 0)
            {
                averageGrowthRate = (lastScore - firstScore) / daysDiff;
            }
        }

        // Calculate per-subject growth rates
        var subjectGrowthRates = new Dictionary<Subject, double>();
        var subjectGroups = dataPoints.GroupBy(dp => dp.Subject);

        foreach (var group in subjectGroups)
        {
            var subjectPoints = group.OrderBy(dp => dp.Date).ToList();
            if (subjectPoints.Count >= 2)
            {
                var firstScore = subjectPoints.First().Score;
                var lastScore = subjectPoints.Last().Score;
                var daysDiff = (subjectPoints.Last().Date - subjectPoints.First().Date).TotalDays;

                if (daysDiff > 0)
                {
                    subjectGrowthRates[group.Key] = (lastScore - firstScore) / daysDiff;
                }
                else
                {
                    subjectGrowthRates[group.Key] = 0.0;
                }
            }
            else
            {
                subjectGrowthRates[group.Key] = 0.0;
            }
        }

        return Result.Success(new ProgressTimeline
        {
            StudentId = studentId,
            StartDate = effectiveStartDate,
            EndDate = effectiveEndDate,
            DataPoints = dataPoints,
            AverageGrowthRate = averageGrowthRate,
            SubjectGrowthRates = subjectGrowthRates
        });
    }

    public Task<Result<PeerComparison>> GetPeerComparisonAsync(
        Guid studentId,
        GradeLevel? gradeLevel = null,
        Subject? subject = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting peer comparison for student {StudentId}", studentId);

        // Stub implementation - returns placeholder data
        Result<PeerComparison> result = new PeerComparison
        {
            StudentId = studentId,
            StudentScore = 0.0,
            PeerAverageScore = 0.0,
            PeerMedianScore = 0.0,
            Percentile = 0,
            PeerGroupSize = 0,
            GradeLevel = gradeLevel ?? GradeLevel.Grade9,
            Subject = subject,
            MeetsKAnonymity = false
        };

        return Task.FromResult(result);
    }
}
