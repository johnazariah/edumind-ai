using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for student responses with tenant filtering
/// </summary>
public interface IStudentResponseRepository : IRepository<StudentResponse, Guid>
{
    /// <summary>
    /// Gets all responses for a specific student assessment
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentAssessmentIdAsync(
        Guid studentAssessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all responses from a specific student
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all responses to a specific question
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific response by student assessment and question
    /// </summary>
    Task<Result<StudentResponse>> GetByStudentAssessmentAndQuestionAsync(
        Guid studentAssessmentId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets correct responses for a student
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetCorrectResponsesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets incorrect responses for a student
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetIncorrectResponsesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets responses within a time range (for time-based analysis)
    /// </summary>
    Task<Result<IReadOnlyList<StudentResponse>>> GetByTimeSpentRangeAsync(
        int minSeconds,
        int maxSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregate statistics for a question (privacy-preserving - min 5 responses)
    /// </summary>
    Task<Result<QuestionStatistics>> GetQuestionStatisticsAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregate statistics for a question (privacy-preserving)
/// </summary>
public record QuestionStatistics
{
    public required Guid QuestionId { get; init; }
    public required int TotalResponses { get; init; }
    public required int CorrectResponses { get; init; }
    public required double SuccessRate { get; init; }
    public required double AverageTimeSeconds { get; init; }
    public required int MedianTimeSeconds { get; init; }

    /// <summary>
    /// Whether this data meets k-anonymity threshold (min 5 responses)
    /// </summary>
    public bool MeetsPrivacyThreshold => TotalResponses >= 5;
}
