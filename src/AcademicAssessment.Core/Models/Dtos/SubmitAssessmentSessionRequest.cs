namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Request to submit a completed assessment session.
/// </summary>
public sealed class SubmitAssessmentSessionRequest
{
    /// <summary>
    /// The assessment ID.
    /// </summary>
    public required Guid AssessmentId { get; init; }

    /// <summary>
    /// Dictionary of final answers keyed by question ID.
    /// </summary>
    public required Dictionary<Guid, QuestionAnswerDto> Answers { get; init; }

    /// <summary>
    /// Total time taken to complete the assessment (in seconds).
    /// </summary>
    public required int TimeTakenSeconds { get; init; }

    /// <summary>
    /// Timestamp when the assessment was submitted.
    /// </summary>
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
}
