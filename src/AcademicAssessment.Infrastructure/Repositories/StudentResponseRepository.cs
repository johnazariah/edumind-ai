using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for StudentResponse entities
/// Includes privacy-preserving aggregate statistics
/// </summary>
public sealed class StudentResponseRepository :
    RepositoryBase<StudentResponse, Guid>,
    IStudentResponseRepository
{
    public StudentResponseRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(StudentResponse entity) => entity.Id;

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentAssessmentIdAsync(
        Guid studentAssessmentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr => sr.StudentAssessmentId == studentAssessmentId)
                          .OrderBy(sr => sr.QuestionOrder),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr => sr.StudentId == studentId)
                          .OrderByDescending(sr => sr.SubmittedAt),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr => sr.QuestionId == questionId),
            cancellationToken);

    public Task<Result<StudentResponse>> GetByStudentAssessmentAndQuestionAsync(
        Guid studentAssessmentId,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(sr =>
                sr.StudentAssessmentId == studentAssessmentId &&
                sr.QuestionId == questionId),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentResponse>>> GetCorrectResponsesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr => sr.StudentId == studentId && sr.IsCorrect),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentResponse>>> GetIncorrectResponsesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr => sr.StudentId == studentId && !sr.IsCorrect),
            cancellationToken);

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByTimeSpentRangeAsync(
        int minSeconds,
        int maxSeconds,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(sr =>
                sr.TimeSpentSeconds >= minSeconds &&
                sr.TimeSpentSeconds <= maxSeconds),
            cancellationToken);

    /// <summary>
    /// Gets aggregate statistics with k-anonymity check (min 5 responses)
    /// </summary>
    public async Task<Result<QuestionStatistics>> GetQuestionStatisticsAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var responses = await DbSet
                .Where(sr => sr.QuestionId == questionId)
                .ToListAsync(cancellationToken);

            // K-anonymity check: minimum 5 responses
            if (responses.Count < 5)
            {
                return Error.Forbidden(
                    "Insufficient data to provide question statistics (minimum 5 responses required)");
            }

            var correctCount = responses.Count(r => r.IsCorrect);
            var successRate = (double)correctCount / responses.Count;
            var averageTime = responses.Average(r => r.TimeSpentSeconds);

            // Calculate median time
            var sortedTimes = responses.Select(r => r.TimeSpentSeconds).OrderBy(t => t).ToList();
            var medianTime = sortedTimes.Count % 2 == 0
                ? (sortedTimes[sortedTimes.Count / 2 - 1] + sortedTimes[sortedTimes.Count / 2]) / 2
                : sortedTimes[sortedTimes.Count / 2];

            var statistics = new QuestionStatistics
            {
                QuestionId = questionId,
                TotalResponses = responses.Count,
                CorrectResponses = correctCount,
                SuccessRate = successRate,
                AverageTimeSeconds = averageTime,
                MedianTimeSeconds = medianTime
            };

            return new Result<QuestionStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }
}
