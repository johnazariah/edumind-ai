using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for school entities
/// Accessed by BusinessAdmin and SystemAdmin roles
/// </summary>
public interface ISchoolRepository : IRepository<School, Guid>
{
    /// <summary>
    /// Gets a school by its code
    /// </summary>
    Task<Result<School>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active schools
    /// </summary>
    Task<Result<IReadOnlyList<School>>> GetActiveSchoolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a school code is already in use
    /// </summary>
    Task<Result<bool>> IsCodeInUseAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schools created within a date range
    /// </summary>
    Task<Result<IReadOnlyList<School>>> GetSchoolsByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);
}
