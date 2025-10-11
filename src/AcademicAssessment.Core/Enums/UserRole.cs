namespace AcademicAssessment.Core.Enums;

/// <summary>
/// User roles in the system with hierarchical access levels
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Student taking assessments
    /// </summary>
    Student = 0,

    /// <summary>
    /// Teacher managing classes and viewing student progress
    /// </summary>
    Teacher = 1,

    /// <summary>
    /// School administrator managing school-level settings and users
    /// </summary>
    SchoolAdmin = 2,

    /// <summary>
    /// Course administrator managing curriculum and course content
    /// </summary>
    CourseAdmin = 3,

    /// <summary>
    /// Business administrator managing billing, schools, and onboarding
    /// </summary>
    BusinessAdmin = 4,

    /// <summary>
    /// System administrator with full system access
    /// </summary>
    SystemAdmin = 5
}
