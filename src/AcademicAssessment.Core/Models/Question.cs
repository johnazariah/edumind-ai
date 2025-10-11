using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a single question in an assessment
/// </summary>
public record Question
{
    /// <summary>
    /// Unique identifier for the question
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Course this question belongs to
    /// </summary>
    public required Guid CourseId { get; init; }

    /// <summary>
    /// Question text/prompt
    /// </summary>
    public required string QuestionText { get; init; }

    /// <summary>
    /// Type of question
    /// </summary>
    public required QuestionType QuestionType { get; init; }

    /// <summary>
    /// Subject area
    /// </summary>
    public required Subject Subject { get; init; }

    /// <summary>
    /// Grade level
    /// </summary>
    public required GradeLevel GradeLevel { get; init; }

    /// <summary>
    /// Difficulty level
    /// </summary>
    public required DifficultyLevel DifficultyLevel { get; init; }

    /// <summary>
    /// Topics/concepts covered by this question
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>
    /// Learning objective(s) this question assesses
    /// </summary>
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];

    /// <summary>
    /// Possible answer options (for multiple choice/select)
    /// JSON serialized array of options
    /// </summary>
    public string? AnswerOptions { get; init; }

    /// <summary>
    /// Correct answer(s)
    /// JSON serialized - format depends on question type
    /// </summary>
    public required string CorrectAnswer { get; init; }

    /// <summary>
    /// Explanation of the correct answer
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// Points awarded for correct answer
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// IRT discrimination parameter (for adaptive testing)
    /// </summary>
    public double? IrtDiscrimination { get; init; }

    /// <summary>
    /// IRT difficulty parameter (for adaptive testing)
    /// </summary>
    public double? IrtDifficulty { get; init; }

    /// <summary>
    /// IRT guessing parameter (for adaptive testing)
    /// </summary>
    public double? IrtGuessing { get; init; }

    /// <summary>
    /// Whether the question is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// How many times this question has been answered
    /// </summary>
    public int TimesAnswered { get; init; } = 0;

    /// <summary>
    /// How many times this question was answered correctly
    /// </summary>
    public int TimesCorrect { get; init; } = 0;

    /// <summary>
    /// Success rate (0-1)
    /// </summary>
    public double SuccessRate =>
        TimesAnswered == 0 ? 0 : (double)TimesCorrect / TimesAnswered;

    /// <summary>
    /// When the question was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the question was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Whether this question was AI-generated
    /// </summary>
    public bool IsAiGenerated { get; init; } = false;

    /// <summary>
    /// Hash of the question content for duplicate detection
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Creates a new question with updated properties
    /// </summary>
    public Question With(
        string? questionText = null,
        string? answerOptions = null,
        string? correctAnswer = null,
        string? explanation = null,
        DifficultyLevel? difficultyLevel = null,
        bool? isActive = null) =>
        this with
        {
            QuestionText = questionText ?? QuestionText,
            AnswerOptions = answerOptions ?? AnswerOptions,
            CorrectAnswer = correctAnswer ?? CorrectAnswer,
            Explanation = explanation ?? Explanation,
            DifficultyLevel = difficultyLevel ?? DifficultyLevel,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Records that this question was answered
    /// </summary>
    public Question RecordAnswer(bool wasCorrect) =>
        this with
        {
            TimesAnswered = TimesAnswered + 1,
            TimesCorrect = TimesCorrect + (wasCorrect ? 1 : 0),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Updates IRT parameters (after calibration)
    /// </summary>
    public Question UpdateIrtParameters(
        double discrimination,
        double difficulty,
        double guessing) =>
        this with
        {
            IrtDiscrimination = discrimination,
            IrtDifficulty = difficulty,
            IrtGuessing = guessing,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
