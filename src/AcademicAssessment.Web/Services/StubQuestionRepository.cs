using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Stub implementation of IQuestionRepository for development without database
/// </summary>
public class StubQuestionRepository : UniversalStubRepository<Question, Guid>, IQuestionRepository
{
    public Task<Result<IReadOnlyList<Question>>> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetBySubjectAsync(Subject subject, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByDifficultyAsync(DifficultyLevel difficulty, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByLearningObjectiveAsync(string learningObjective, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByQuestionTypeAsync(QuestionType questionType, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByDifficultyLevelAsync(DifficultyLevel difficulty, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetBySubjectAndGradeLevelAsync(Subject subject, GradeLevel gradeLevel, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByTopicsAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByLearningObjectivesAsync(IReadOnlyList<string> learningObjectives, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetAiGeneratedQuestionsAsync(CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetByIrtDifficultyRangeAsync(double minDifficulty, double maxDifficulty, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<IReadOnlyList<Question>>> GetBySuccessRateRangeAsync(double minRate, double maxRate, CancellationToken cancellationToken = default)
        => EmptyList<Question>();

    public Task<Result<bool>> IsDuplicateAsync(string questionText, CancellationToken cancellationToken = default)
        => FalseResult();
}
