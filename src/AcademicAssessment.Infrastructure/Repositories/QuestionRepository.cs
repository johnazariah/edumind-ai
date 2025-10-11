using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Question entities
/// </summary>
public sealed class QuestionRepository : RepositoryBase<Question, Guid>, IQuestionRepository
{
    public QuestionRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(Question entity) => entity.Id;

    public Task<Result<IReadOnlyList<Question>>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.CourseId == courseId),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetByDifficultyLevelAsync(
        DifficultyLevel difficultyLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.DifficultyLevel == difficultyLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetBySubjectAndGradeLevelAsync(
        Subject subject,
        GradeLevel gradeLevel,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.Subject == subject && q.GradeLevel == gradeLevel),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetByQuestionTypeAsync(
        QuestionType questionType,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.QuestionType == questionType),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetByTopicsAsync(
        IReadOnlyList<string> topics,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.Topics.Any(t => topics.Contains(t))),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetByLearningObjectivesAsync(
        IReadOnlyList<string> learningObjectives,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.LearningObjectives.Any(lo => learningObjectives.Contains(lo))),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetAiGeneratedQuestionsAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q => q.IsAiGenerated),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetByIrtDifficultyRangeAsync(
        double minDifficulty,
        double maxDifficulty,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q =>
                q.IrtDifficulty.HasValue &&
                q.IrtDifficulty >= minDifficulty &&
                q.IrtDifficulty <= maxDifficulty),
            cancellationToken);

    public Task<Result<IReadOnlyList<Question>>> GetBySuccessRateRangeAsync(
        double minSuccessRate,
        double maxSuccessRate,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(q =>
                q.TimesAnswered > 0 &&
                (double)q.TimesCorrect / q.TimesAnswered >= minSuccessRate &&
                (double)q.TimesCorrect / q.TimesAnswered <= maxSuccessRate),
            cancellationToken);

    public async Task<Result<bool>> IsDuplicateAsync(
        string contentHash,
        CancellationToken cancellationToken = default) =>
        await ExecuteQueryAsync(
            async () => await DbSet.AnyAsync(q => q.ContentHash == contentHash, cancellationToken),
            cancellationToken);
}
