namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Request to save assessment session progress (answers and review flags).
/// </summary>
public sealed class SaveAssessmentSessionRequest
{
    /// <summary>
    /// The assessment ID.
    /// </summary>
    public required Guid AssessmentId { get; init; }

    /// <summary>
    /// Dictionary of answers keyed by question ID.
    /// </summary>
    public required Dictionary<Guid, QuestionAnswerDto> Answers { get; init; }

    /// <summary>
    /// Set of question numbers marked for review.
    /// </summary>
    public required HashSet<int> ReviewFlags { get; init; }
}

/// <summary>
/// Represents a student's answer to a single question.
/// </summary>
public sealed class QuestionAnswerDto
{
    /// <summary>
    /// Question ID.
    /// </summary>
    public required Guid QuestionId { get; init; }

    /// <summary>
    /// Selected options for multiple choice/select questions.
    /// </summary>
    public HashSet<string> SelectedOptions { get; init; } = new();

    /// <summary>
    /// Free-form text response for short answer, essay, etc.
    /// </summary>
    public string? FreeResponse { get; init; }

    /// <summary>
    /// Timestamp when this answer was last modified.
    /// </summary>
    public DateTimeOffset LastModified { get; init; } = DateTimeOffset.UtcNow;
}
