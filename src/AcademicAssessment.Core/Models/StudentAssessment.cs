using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a student's attempt at an assessment
/// </summary>
public record StudentAssessment
{
    /// <summary>
    /// Unique identifier for this assessment attempt
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Student taking the assessment
    /// </summary>
    public required Guid StudentId { get; init; }

    /// <summary>
    /// Assessment being taken
    /// </summary>
    public required Guid AssessmentId { get; init; }

    /// <summary>
    /// School ID (for tenant isolation)
    /// </summary>
    public Guid? SchoolId { get; init; }

    /// <summary>
    /// Class ID (if taken as part of a class)
    /// </summary>
    public Guid? ClassId { get; init; }

    /// <summary>
    /// Current status of the assessment
    /// </summary>
    public required AssessmentStatus Status { get; init; }

    /// <summary>
    /// When the assessment was started
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// When the assessment was completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Total score achieved
    /// </summary>
    public int? Score { get; init; }

    /// <summary>
    /// Maximum possible score
    /// </summary>
    public required int MaxScore { get; init; }

    /// <summary>
    /// Percentage score (0-100)
    /// </summary>
    public double? PercentageScore =>
        Score.HasValue ? (double)Score.Value / MaxScore * 100 : null;

    /// <summary>
    /// Whether the student passed
    /// </summary>
    public bool? Passed { get; init; }

    /// <summary>
    /// Current question index (for in-progress assessments)
    /// </summary>
    public int CurrentQuestionIndex { get; init; } = 0;

    /// <summary>
    /// Student's current estimated ability (IRT theta parameter)
    /// </summary>
    public double? EstimatedAbility { get; init; }

    /// <summary>
    /// Time spent in seconds
    /// </summary>
    public int? TimeSpentSeconds { get; init; }

    /// <summary>
    /// Number of correct answers
    /// </summary>
    public int CorrectAnswers { get; init; } = 0;

    /// <summary>
    /// Number of incorrect answers
    /// </summary>
    public int IncorrectAnswers { get; init; } = 0;

    /// <summary>
    /// Number of skipped questions
    /// </summary>
    public int SkippedQuestions { get; init; } = 0;

    /// <summary>
    /// Feedback provided by the system
    /// </summary>
    public string? Feedback { get; init; }

    /// <summary>
    /// Recommended next steps for the student
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; } = [];

    /// <summary>
    /// When the record was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the record was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// XP points earned (for self-service students)
    /// </summary>
    public int XpEarned { get; init; } = 0;

    /// <summary>
    /// Creates a new student assessment with updated properties
    /// </summary>
    public StudentAssessment With(
        AssessmentStatus? status = null,
        int? currentQuestionIndex = null,
        int? score = null,
        bool? passed = null,
        double? estimatedAbility = null,
        int? timeSpentSeconds = null,
        string? feedback = null) =>
        this with
        {
            Status = status ?? Status,
            CurrentQuestionIndex = currentQuestionIndex ?? CurrentQuestionIndex,
            Score = score ?? Score,
            Passed = passed ?? Passed,
            EstimatedAbility = estimatedAbility ?? EstimatedAbility,
            TimeSpentSeconds = timeSpentSeconds ?? TimeSpentSeconds,
            Feedback = feedback ?? Feedback,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Starts the assessment
    /// </summary>
    public StudentAssessment Start() =>
        Status != AssessmentStatus.NotStarted
            ? this
            : this with
            {
                Status = AssessmentStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Completes the assessment
    /// </summary>
    public StudentAssessment Complete(
        int finalScore,
        bool passed,
        int timeSpentSeconds,
        string? feedback = null,
        IReadOnlyList<string>? recommendations = null,
        int xpEarned = 0) =>
        this with
        {
            Status = AssessmentStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            Score = finalScore,
            Passed = passed,
            TimeSpentSeconds = timeSpentSeconds,
            Feedback = feedback,
            Recommendations = recommendations ?? [],
            XpEarned = xpEarned,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Advances to next question
    /// </summary>
    public StudentAssessment NextQuestion() =>
        this with
        {
            CurrentQuestionIndex = CurrentQuestionIndex + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Records a correct answer
    /// </summary>
    public StudentAssessment RecordCorrect() =>
        this with
        {
            CorrectAnswers = CorrectAnswers + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Records an incorrect answer
    /// </summary>
    public StudentAssessment RecordIncorrect() =>
        this with
        {
            IncorrectAnswers = IncorrectAnswers + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Records a skipped question
    /// </summary>
    public StudentAssessment RecordSkipped() =>
        this with
        {
            SkippedQuestions = SkippedQuestions + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Updates estimated ability (for adaptive assessments)
    /// </summary>
    public StudentAssessment UpdateAbility(double newAbility) =>
        this with
        {
            EstimatedAbility = newAbility,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Pauses the assessment
    /// </summary>
    public StudentAssessment Pause() =>
        Status != AssessmentStatus.InProgress
            ? this
            : this with
            {
                Status = AssessmentStatus.Paused,
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Resumes a paused assessment
    /// </summary>
    public StudentAssessment Resume() =>
        Status != AssessmentStatus.Paused
            ? this
            : this with
            {
                Status = AssessmentStatus.InProgress,
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Abandons the assessment
    /// </summary>
    public StudentAssessment Abandon() =>
        this with
        {
            Status = AssessmentStatus.Abandoned,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
