using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Stub implementation of IStudentResponseRepository for development without database
/// </summary>
public class StubStudentResponseRepository : UniversalStubRepository<StudentResponse, Guid>, IStudentResponseRepository
{
    public Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentAssessmentIdAsync(Guid studentAssessmentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<StudentResponse>> GetByStudentAssessmentAndQuestionAsync(Guid studentAssessmentId, Guid questionId, CancellationToken cancellationToken = default)
        => NotFound<StudentResponse>("StudentResponse", $"{studentAssessmentId}/{questionId}");

    public Task<Result<IReadOnlyList<StudentResponse>>> GetCorrectResponsesAsync(Guid studentAssessmentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetIncorrectResponsesAsync(Guid studentAssessmentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<double>> GetAccuracyRateAsync(Guid studentAssessmentId, CancellationToken cancellationToken = default)
        => ZeroDouble();

    public Task<Result<TimeSpan>> GetAverageResponseTimeAsync(Guid studentAssessmentId, CancellationToken cancellationToken = default)
        => ZeroTimeSpan();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetCorrectResponsesByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetIncorrectResponsesByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<IReadOnlyList<StudentResponse>>> GetByTimeSpentRangeAsync(int minSeconds, int maxSeconds, CancellationToken cancellationToken = default)
        => EmptyList<StudentResponse>();

    public Task<Result<QuestionStatistics>> GetQuestionStatisticsAsync(Guid questionId, CancellationToken cancellationToken = default)
        => NotFound<QuestionStatistics>("QuestionStatistics", questionId);
}
