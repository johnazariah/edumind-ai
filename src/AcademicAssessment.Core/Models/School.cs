namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a school or educational institution
/// Each school has its own physically isolated database
/// </summary>
public record School
{
    /// <summary>
    /// Unique identifier for the school
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Official name of the school
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short code or abbreviation for the school
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Physical address of the school
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Contact email for the school
    /// </summary>
    public required string ContactEmail { get; init; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? ContactPhone { get; init; }

    /// <summary>
    /// Whether the school is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// When the school was onboarded
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the school record was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Connection string key in Azure Key Vault for this school's database
    /// Format: "School-{SchoolId}-ConnectionString"
    /// </summary>
    public string ConnectionStringKey => $"School-{Id}-ConnectionString";

    /// <summary>
    /// Database name for this school
    /// Format: "edumind_school_{schoolcode}_{schoolid}"
    /// </summary>
    public string DatabaseName => $"edumind_school_{Code.ToLowerInvariant()}_{Id:N}";

    /// <summary>
    /// Creates a new school with updated properties
    /// </summary>
    public School With(
        string? name = null,
        string? address = null,
        string? contactEmail = null,
        string? contactPhone = null,
        bool? isActive = null) =>
        this with
        {
            Name = name ?? Name,
            Address = address ?? Address,
            ContactEmail = contactEmail ?? ContactEmail,
            ContactPhone = contactPhone ?? ContactPhone,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
