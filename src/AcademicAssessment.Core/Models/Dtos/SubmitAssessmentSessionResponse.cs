namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Response from submitting an assessment session.
/// </summary>
public sealed class SubmitAssessmentSessionResponse
{
    /// <summary>
    /// Indicates whether the submission was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The session ID for the submitted assessment (for tracking and results).
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Timestamp when the session was submitted.
    /// </summary>
    public required DateTimeOffset SubmittedAt { get; init; }

    /// <summary>
    /// Number of questions answered.
    /// </summary>
    public required int QuestionsAnswered { get; init; }

    /// <summary>
    /// Total number of questions in the assessment.
    /// </summary>
    public required int TotalQuestions { get; init; }

    /// <summary>
    /// Optional message (e.g., "Assessment submitted successfully").
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Optional error message if submission failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
