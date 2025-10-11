namespace AcademicAssessment.Core.Enums;

/// <summary>
/// Types of assessments available in the system
/// </summary>
public enum AssessmentType
{
    /// <summary>
    /// Diagnostic assessment to determine current skill level
    /// </summary>
    Diagnostic = 0,

    /// <summary>
    /// Formative assessment during learning process
    /// </summary>
    Formative = 1,

    /// <summary>
    /// Summative assessment at end of learning period
    /// </summary>
    Summative = 2,

    /// <summary>
    /// Practice assessment for skill reinforcement
    /// </summary>
    Practice = 3,

    /// <summary>
    /// Adaptive assessment that adjusts difficulty based on responses
    /// </summary>
    Adaptive = 4
}
