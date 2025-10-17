using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models.Dtos;

public record AssessmentSessionDto
{
    public Guid AssessmentId { get; init; }
    public string AssessmentTitle { get; init; } = string.Empty;
    public int EstimatedDurationMinutes { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public IReadOnlyList<AssessmentQuestionDto> Questions { get; init; } = Array.Empty<AssessmentQuestionDto>();
}

public record AssessmentQuestionDto
{
    public Guid Id { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string PromptFormat { get; init; } = "markdown";
    public QuestionType QuestionType { get; init; }
    public bool AllowMultipleSelection { get; init; }
    public IReadOnlyList<QuestionOptionDto> Options { get; init; } = Array.Empty<QuestionOptionDto>();
    public string? CodeSnippet { get; init; }
    public string? CodeLanguage { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAltText { get; init; }
    public string? MathExpression { get; init; }
    public int Points { get; init; }
    public double EstimatedTimeSeconds { get; init; } = 60;
    public IReadOnlyList<string> Hints { get; init; } = Array.Empty<string>();
}

public record QuestionOptionDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
}
