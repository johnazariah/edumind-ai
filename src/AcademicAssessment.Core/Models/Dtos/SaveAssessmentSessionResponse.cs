namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Response from saving assessment session progress.
/// </summary>
public sealed class SaveAssessmentSessionResponse
{
    /// <summary>
    /// Indicates whether the save was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Timestamp when the session was saved.
    /// </summary>
    public required DateTimeOffset SavedAt { get; init; }

    /// <summary>
    /// Number of answers saved.
    /// </summary>
    public required int AnswersSaved { get; init; }

    /// <summary>
    /// Optional error message if save failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
