using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Repository for user entities
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    Task<Result<User>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by external ID (Azure AD B2C subject)
    /// </summary>
    Task<Result<User>> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a specific school
    /// </summary>
    Task<Result<IReadOnlyList<User>>> GetBySchoolIdAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role
    /// </summary>
    Task<Result<IReadOnlyList<User>>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users
    /// </summary>
    Task<Result<IReadOnlyList<User>>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use
    /// </summary>
    Task<Result<bool>> IsEmailInUseAsync(string email, CancellationToken cancellationToken = default);
}
