using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for question entities
/// </summary>
public interface IQuestionRepository : IRepository<Question, Guid>
{
    /// <summary>
    /// Gets questions for a specific course
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions by difficulty level
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByDifficultyLevelAsync(
        DifficultyLevel difficultyLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions by subject and grade level
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions by type
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByQuestionTypeAsync(
        QuestionType questionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions covering specific topics
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByTopicsAsync(
        IReadOnlyList<string> topics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions covering specific learning objectives
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByLearningObjectivesAsync(
        IReadOnlyList<string> learningObjectives,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI-generated questions
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetAiGeneratedQuestionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions by IRT difficulty range (for adaptive testing)
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetByIrtDifficultyRangeAsync(
        double minDifficulty,
        double maxDifficulty,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets questions with success rate in a specific range
    /// </summary>
    Task<Result<IReadOnlyList<Question>>> GetBySuccessRateRangeAsync(
        double minSuccessRate,
        double maxSuccessRate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a question with the same content hash already exists (duplicate detection)
    /// </summary>
    Task<Result<bool>> IsDuplicateAsync(
        string contentHash,
        CancellationToken cancellationToken = default);
}
