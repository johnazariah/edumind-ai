using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents an assessment (collection of questions) for a specific course
/// </summary>
public record Assessment
{
    /// <summary>
    /// Unique identifier for the assessment
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Course this assessment belongs to
    /// </summary>
    public required Guid CourseId { get; init; }

    /// <summary>
    /// School ID (for school-specific assessments, null for global)
    /// </summary>
    public Guid? SchoolId { get; init; }

    /// <summary>
    /// Assessment title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Assessment description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Type of assessment
    /// </summary>
    public required AssessmentType AssessmentType { get; init; }

    /// <summary>
    /// Subject area
    /// </summary>
    public required Subject Subject { get; init; }

    /// <summary>
    /// Grade level
    /// </summary>
    public required GradeLevel GradeLevel { get; init; }

    /// <summary>
    /// Topics covered in this assessment
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>
    /// Question IDs in this assessment (ordered)
    /// </summary>
    public IReadOnlyList<Guid> QuestionIds { get; init; } = [];

    /// <summary>
    /// Total points possible
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Time limit in minutes (null for untimed)
    /// </summary>
    public int? TimeLimitMinutes { get; init; }

    /// <summary>
    /// Passing score percentage (0-100)
    /// </summary>
    public int PassingScorePercentage { get; init; } = 70;

    /// <summary>
    /// Whether the assessment is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Whether this is an adaptive assessment
    /// </summary>
    public bool IsAdaptive => AssessmentType == AssessmentType.Adaptive;

    /// <summary>
    /// When the assessment was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the assessment was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Number of questions in the assessment
    /// </summary>
    public int QuestionCount => QuestionIds.Count;

    /// <summary>
    /// Creates a new assessment with updated properties
    /// </summary>
    public Assessment With(
        string? title = null,
        string? description = null,
        IReadOnlyList<Guid>? questionIds = null,
        int? totalPoints = null,
        int? timeLimitMinutes = null,
        bool? isActive = null) =>
        this with
        {
            Title = title ?? Title,
            Description = description ?? Description,
            QuestionIds = questionIds ?? QuestionIds,
            TotalPoints = totalPoints ?? TotalPoints,
            TimeLimitMinutes = timeLimitMinutes ?? TimeLimitMinutes,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Adds a question to the assessment
    /// </summary>
    public Assessment AddQuestion(Guid questionId) =>
        QuestionIds.Contains(questionId)
            ? this
            : this with
            {
                QuestionIds = QuestionIds.Append(questionId).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Removes a question from the assessment
    /// </summary>
    public Assessment RemoveQuestion(Guid questionId) =>
        this with
        {
            QuestionIds = QuestionIds.Where(id => id != questionId).ToList().AsReadOnly(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Reorders questions
    /// </summary>
    public Assessment ReorderQuestions(IReadOnlyList<Guid> newOrder) =>
        newOrder.Count != QuestionIds.Count ||
        !newOrder.All(QuestionIds.Contains)
            ? this
            : this with
            {
                QuestionIds = newOrder,
                UpdatedAt = DateTimeOffset.UtcNow
            };
}
