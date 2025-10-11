using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entities
/// </summary>
public sealed class UserRepository : RepositoryBase<User, Guid>, IUserRepository
{
    public UserRepository(AcademicContext context) : base(context) { }

    protected override Guid GetEntityId(User entity) => entity.Id;

    public Task<Result<User>> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(u => u.Email == email),
            cancellationToken);

    public Task<Result<User>> GetByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default) =>
        FindSingleAsync(
            query => query.Where(u => u.ExternalId == externalId),
            cancellationToken);

    public Task<Result<IReadOnlyList<User>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(u => u.SchoolId == schoolId),
            cancellationToken);

    public Task<Result<IReadOnlyList<User>>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(u => u.Role == role),
            cancellationToken);

    public Task<Result<IReadOnlyList<User>>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default) =>
        FindManyAsync(
            query => query.Where(u => u.IsActive),
            cancellationToken);

    public async Task<Result<bool>> IsEmailInUseAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await ExecuteQueryAsync(
            async () => await DbSet.AnyAsync(u => u.Email == email, cancellationToken),
            cancellationToken);
}
