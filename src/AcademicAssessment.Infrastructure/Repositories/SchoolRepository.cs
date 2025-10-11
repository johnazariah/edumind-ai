using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for School entities
/// </summary>
public sealed class SchoolRepository : RepositoryBase<School, Guid>, ISchoolRepository
{
    public SchoolRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(School entity) => entity.Id;

    public Task<Result<School>> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(s => s.Code == code),
            cancellationToken);

    public Task<Result<IReadOnlyList<School>>> GetActiveSchoolsAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.IsActive),
            cancellationToken);

    public async Task<Result<bool>> IsCodeInUseAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        await ExecuteQueryAsync(
            async () => await DbSet.AnyAsync(s => s.Code == code, cancellationToken),
            cancellationToken);

    public Task<Result<IReadOnlyList<School>>> GetSchoolsByDateRangeAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                          .OrderBy(s => s.CreatedAt),
            cancellationToken);
}
