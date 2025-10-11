namespace AcademicAssessment.Core.Enums;

/// <summary>
/// Status of a student's assessment attempt
/// </summary>
public enum AssessmentStatus
{
    /// <summary>
    /// Assessment has not been started
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Assessment is currently in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Assessment has been completed
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Assessment was abandoned before completion
    /// </summary>
    Abandoned = 3,

    /// <summary>
    /// Assessment is paused (can be resumed)
    /// </summary>
    Paused = 4,

    /// <summary>
    /// Assessment is being graded
    /// </summary>
    Grading = 5,

    /// <summary>
    /// Assessment has been graded
    /// </summary>
    Graded = 6
}
