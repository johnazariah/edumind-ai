using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Stub implementation of IAssessmentRepository for development without database
/// </summary>
public class StubAssessmentRepository : UniversalStubRepository<Assessment, Guid>, IAssessmentRepository
{
    public Task<Result<IReadOnlyList<Assessment>>> GetBySubjectAsync(Subject subject, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByGradeLevelAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByDifficultyAsync(DifficultyLevel difficulty, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetActiveAssessmentsAsync(CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByTypeAsync(AssessmentType assessmentType, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetBySubjectAndGradeLevelAsync(Subject subject, GradeLevel gradeLevel, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetBySchoolIdAsync(Guid schoolId, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetGlobalAssessmentsAsync(CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetAdaptiveAssessmentsAsync(CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();

    public Task<Result<IReadOnlyList<Assessment>>> GetByTopicsAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken = default)
        => EmptyList<Assessment>();
}
