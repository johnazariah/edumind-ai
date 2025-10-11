namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a student's response to a specific question
/// </summary>
public record StudentResponse
{
    /// <summary>
    /// Unique identifier for this response
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Student assessment this response belongs to
    /// </summary>
    public required Guid StudentAssessmentId { get; init; }

    /// <summary>
    /// Student who provided the response
    /// </summary>
    public required Guid StudentId { get; init; }

    /// <summary>
    /// Question being answered
    /// </summary>
    public required Guid QuestionId { get; init; }

    /// <summary>
    /// School ID (for tenant isolation)
    /// </summary>
    public Guid? SchoolId { get; init; }

    /// <summary>
    /// Student's answer
    /// JSON serialized - format depends on question type
    /// </summary>
    public required string StudentAnswer { get; init; }

    /// <summary>
    /// Whether the answer was correct
    /// </summary>
    public required bool IsCorrect { get; init; }

    /// <summary>
    /// Points earned for this question
    /// </summary>
    public required int PointsEarned { get; init; }

    /// <summary>
    /// Maximum points possible for this question
    /// </summary>
    public required int MaxPoints { get; init; }

    /// <summary>
    /// Time spent on this question in seconds
    /// </summary>
    public int TimeSpentSeconds { get; init; } = 0;

    /// <summary>
    /// Question order in the assessment (0-indexed)
    /// </summary>
    public required int QuestionOrder { get; init; }

    /// <summary>
    /// Student's ability estimate at time of this question (for adaptive)
    /// </summary>
    public double? AbilityAtTime { get; init; }

    /// <summary>
    /// Feedback provided for this response
    /// </summary>
    public string? Feedback { get; init; }

    /// <summary>
    /// AI-generated explanation (if applicable)
    /// </summary>
    public string? AiExplanation { get; init; }

    /// <summary>
    /// When the response was submitted
    /// </summary>
    public required DateTimeOffset SubmittedAt { get; init; }

    /// <summary>
    /// When the record was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Whether this question was skipped
    /// </summary>
    public bool WasSkipped => string.IsNullOrWhiteSpace(StudentAnswer);

    /// <summary>
    /// Creates a new student response with updated properties
    /// </summary>
    public StudentResponse With(
        bool? isCorrect = null,
        int? pointsEarned = null,
        string? feedback = null,
        string? aiExplanation = null) =>
        this with
        {
            IsCorrect = isCorrect ?? IsCorrect,
            PointsEarned = pointsEarned ?? PointsEarned,
            Feedback = feedback ?? Feedback,
            AiExplanation = aiExplanation ?? AiExplanation
        };

    /// <summary>
    /// Adds AI-generated explanation
    /// </summary>
    public StudentResponse AddAiExplanation(string explanation) =>
        this with { AiExplanation = explanation };

    /// <summary>
    /// Adds feedback
    /// </summary>
    public StudentResponse AddFeedback(string feedback) =>
        this with { Feedback = feedback };
}
