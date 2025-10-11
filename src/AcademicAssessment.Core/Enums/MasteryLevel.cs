namespace AcademicAssessment.Core.Enums;

/// <summary>
/// Student's mastery level for a particular topic or skill
/// </summary>
public enum MasteryLevel
{
    /// <summary>
    /// No mastery - needs introduction
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Beginning mastery - 0-40% proficiency
    /// </summary>
    Beginning = 1,

    /// <summary>
    /// Developing mastery - 40-60% proficiency
    /// </summary>
    Developing = 2,

    /// <summary>
    /// Proficient mastery - 60-80% proficiency
    /// </summary>
    Proficient = 3,

    /// <summary>
    /// Advanced mastery - 80-100% proficiency
    /// </summary>
    Advanced = 4,

    /// <summary>
    /// Expert mastery - consistently above grade level
    /// </summary>
    Expert = 5
}
