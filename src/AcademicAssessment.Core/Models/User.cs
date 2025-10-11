using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Base user entity representing all types of users in the system
/// </summary>
public record User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Email address (unique identifier for login)
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Full name of the user
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// User's role in the system
    /// </summary>
    public required UserRole Role { get; init; }

    /// <summary>
    /// School ID for school-based users (null for self-service)
    /// </summary>
    public Guid? SchoolId { get; init; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Azure AD B2C subject identifier
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Creates a new user with updated properties
    /// </summary>
    public User With(
        string? fullName = null,
        UserRole? role = null,
        bool? isActive = null) =>
        this with
        {
            FullName = fullName ?? FullName,
            Role = role ?? Role,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
