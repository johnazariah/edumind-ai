namespace AcademicAssessment.Core.Enums;

/// <summary>
/// Types of questions that can appear in assessments
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Multiple choice with single correct answer
    /// </summary>
    MultipleChoice = 0,

    /// <summary>
    /// Multiple choice with multiple correct answers
    /// </summary>
    MultipleSelect = 1,

    /// <summary>
    /// True or false question
    /// </summary>
    TrueFalse = 2,

    /// <summary>
    /// Short answer requiring text input
    /// </summary>
    ShortAnswer = 3,

    /// <summary>
    /// Essay or long-form written response
    /// </summary>
    Essay = 4,

    /// <summary>
    /// Mathematical expression or equation
    /// </summary>
    MathExpression = 5,

    /// <summary>
    /// Fill in the blank
    /// </summary>
    FillInBlank = 6,

    /// <summary>
    /// Matching items between two lists
    /// </summary>
    Matching = 7
}
