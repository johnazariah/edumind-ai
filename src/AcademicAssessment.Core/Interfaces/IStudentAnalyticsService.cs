using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Service for analyzing student performance and progress
/// </summary>
public interface IStudentAnalyticsService
{
    /// <summary>
    /// Calculates overall student performance across all subjects
    /// </summary>
    Task<Result<StudentPerformanceSummary>> GetStudentPerformanceSummaryAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates student performance for a specific subject
    /// </summary>
    Task<Result<SubjectPerformance>> GetSubjectPerformanceAsync(
        Guid studentId,
        Subject subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes mastery of learning objectives for a student
    /// </summary>
    Task<Result<IReadOnlyList<LearningObjectiveMastery>>> GetLearningObjectiveMasteryAsync(
        Guid studentId,
        Subject? subject = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates student's ability estimates using IRT for adaptive testing
    /// </summary>
    Task<Result<IReadOnlyDictionary<Subject, double>>> GetAbilityEstimatesAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies areas where the student needs improvement
    /// </summary>
    Task<Result<IReadOnlyList<ImprovementArea>>> GetImprovementAreasAsync(
        Guid studentId,
        int topN = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates student's progress over time
    /// </summary>
    Task<Result<ProgressTimeline>> GetProgressTimelineAsync(
        Guid studentId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares student performance against peers (with k-anonymity)
    /// </summary>
    Task<Result<PeerComparison>> GetPeerComparisonAsync(
        Guid studentId,
        GradeLevel? gradeLevel = null,
        Subject? subject = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Overall performance summary for a student
/// </summary>
public record StudentPerformanceSummary
{
    public required Guid StudentId { get; init; }
    public required int TotalAssessmentsTaken { get; init; }
    public required double AverageScore { get; init; }
    public required double OverallMastery { get; init; }
    public required IReadOnlyDictionary<Subject, double> SubjectScores { get; init; }
    public required TimeSpan TotalTimeSpent { get; init; }
    public required DateTimeOffset LastAssessmentDate { get; init; }
    public required int CurrentStreak { get; init; }
}

/// <summary>
/// Performance metrics for a specific subject
/// </summary>
public record SubjectPerformance
{
    public required Subject Subject { get; init; }
    public required int AssessmentsTaken { get; init; }
    public required double AverageScore { get; init; }
    public required double MasteryLevel { get; init; }
    public required double AbilityEstimate { get; init; }
    public required int QuestionsAnswered { get; init; }
    public required int QuestionsCorrect { get; init; }
    public required double AccuracyRate { get; init; }
    public required TimeSpan AverageTimePerQuestion { get; init; }
    public required IReadOnlyList<string> StrongTopics { get; init; }
    public required IReadOnlyList<string> WeakTopics { get; init; }
}

/// <summary>
/// Mastery level for a specific learning objective
/// </summary>
public record LearningObjectiveMastery
{
    public required string LearningObjective { get; init; }
    public required Subject Subject { get; init; }
    public required double MasteryLevel { get; init; }
    public required int TimesAssessed { get; init; }
    public required int TimesCorrect { get; init; }
    public required DateTimeOffset LastAssessedAt { get; init; }
    public required MasteryStatus Status { get; init; }
}

/// <summary>
/// Area identified for student improvement
/// </summary>
public record ImprovementArea
{
    public required Subject Subject { get; init; }
    public required string Topic { get; init; }
    public required string LearningObjective { get; init; }
    public required double CurrentMastery { get; init; }
    public required double TargetMastery { get; init; }
    public required int QuestionsAttempted { get; init; }
    public required double AccuracyRate { get; init; }
    public required string RecommendedAction { get; init; }
    public required PriorityLevel Priority { get; init; }
}

/// <summary>
/// Student progress over time
/// </summary>
public record ProgressTimeline
{
    public required Guid StudentId { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public required DateTimeOffset EndDate { get; init; }
    public required IReadOnlyList<ProgressDataPoint> DataPoints { get; init; }
    public required double AverageGrowthRate { get; init; }
    public required IReadOnlyDictionary<Subject, double> SubjectGrowthRates { get; init; }
}

/// <summary>
/// Single data point in progress timeline
/// </summary>
public record ProgressDataPoint
{
    public required DateTimeOffset Date { get; init; }
    public required Subject Subject { get; init; }
    public required double Score { get; init; }
    public required double MasteryLevel { get; init; }
    public required AssessmentType AssessmentType { get; init; }
}

/// <summary>
/// Comparison against peer group
/// </summary>
public record PeerComparison
{
    public required Guid StudentId { get; init; }
    public required double StudentScore { get; init; }
    public required double PeerAverageScore { get; init; }
    public required double PeerMedianScore { get; init; }
    public required int Percentile { get; init; }
    public required int PeerGroupSize { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public required Subject? Subject { get; init; }
    public required bool MeetsKAnonymity { get; init; }
}

/// <summary>
/// Mastery status categories
/// </summary>
public enum MasteryStatus
{
    NotStarted,
    Beginning,
    Developing,
    Proficient,
    Advanced,
    Mastered
}

/// <summary>
/// Priority levels for improvement areas
/// </summary>
public enum PriorityLevel
{
    Low,
    Medium,
    High,
    Critical
}
