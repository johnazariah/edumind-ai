namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Represents the summary information students need to decide whether to start an assessment.
/// </summary>
public record AssessmentSummary
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Subject { get; init; }
    public required string Difficulty { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public int QuestionCount { get; init; }
    public double? ProgressPercentage { get; init; }
    public bool IsInProgress { get; init; }
    public DateTimeOffset? LastAttemptedAt { get; init; }
    public IReadOnlyList<string> LearningObjectives { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
}
