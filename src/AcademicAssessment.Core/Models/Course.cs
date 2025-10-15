using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Models;

/// <summary>
/// Represents a course curriculum with learning objectives
/// </summary>
public record Course
{
    /// <summary>
    /// Unique identifier for the course
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Course name (e.g., "8th Grade Mathematics")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short course code (e.g., "MATH-8")
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Subject area
    /// </summary>
    public required Subject Subject { get; init; }

    /// <summary>
    /// Grade level
    /// </summary>
    public required GradeLevel GradeLevel { get; init; }

    /// <summary>
    /// Detailed description of the course
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Learning objectives/standards covered
    /// </summary>
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];

    /// <summary>
    /// Topics covered in the course
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>
    /// Optional: Board/Curriculum name (e.g., "CBSE", "ICSE", "IB", "State Board")
    /// Allows schools to tag content by educational board without requiring full hierarchy
    /// </summary>
    public string? BoardName { get; init; }

    /// <summary>
    /// Optional: Module name for grouping related topics (e.g., "Algebra", "Thermodynamics", "Cell Biology")
    /// Provides additional organizational flexibility without strict hierarchy
    /// </summary>
    public string? ModuleName { get; init; }

    /// <summary>
    /// Optional: Flexible metadata dictionary for custom attributes
    /// Use for board-specific codes, external LMS IDs, or custom tagging
    /// Example: { "externalId": "google-classroom-123", "boardCode": "CBSE-2024", "term": "Semester 1" }
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether the course is active
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// When the course was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the course was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Course administrator who manages this course
    /// </summary>
    public Guid? CourseAdminId { get; init; }

    /// <summary>
    /// Creates a new course with updated properties
    /// </summary>
    public Course With(
        string? name = null,
        string? description = null,
        IReadOnlyList<string>? learningObjectives = null,
        IReadOnlyList<string>? topics = null,
        bool? isActive = null) =>
        this with
        {
            Name = name ?? Name,
            Description = description ?? Description,
            LearningObjectives = learningObjectives ?? LearningObjectives,
            Topics = topics ?? Topics,
            IsActive = isActive ?? IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Adds a learning objective
    /// </summary>
    public Course AddLearningObjective(string objective) =>
        LearningObjectives.Contains(objective)
            ? this
            : this with
            {
                LearningObjectives = LearningObjectives.Append(objective).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

    /// <summary>
    /// Adds a topic
    /// </summary>
    public Course AddTopic(string topic) =>
        Topics.Contains(topic)
            ? this
            : this with
            {
                Topics = Topics.Append(topic).ToList().AsReadOnly(),
                UpdatedAt = DateTimeOffset.UtcNow
            };
}
