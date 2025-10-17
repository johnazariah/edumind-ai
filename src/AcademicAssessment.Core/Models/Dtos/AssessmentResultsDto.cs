namespace AcademicAssessment.Core.Models.Dtos;

/// <summary>
/// Assessment results data for display to students.
/// </summary>
public sealed class AssessmentResultsDto
{
    /// <summary>
    /// The session ID for this assessment attempt.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The assessment ID.
    /// </summary>
    public required Guid AssessmentId { get; init; }

    /// <summary>
    /// Assessment title.
    /// </summary>
    public required string AssessmentTitle { get; init; }

    /// <summary>
    /// Overall score as a percentage (0-100).
    /// </summary>
    public required double ScorePercentage { get; init; }

    /// <summary>
    /// Total points earned.
    /// </summary>
    public required int PointsEarned { get; init; }

    /// <summary>
    /// Total points possible.
    /// </summary>
    public required int TotalPoints { get; init; }

    /// <summary>
    /// Number of questions answered correctly.
    /// </summary>
    public required int CorrectAnswers { get; init; }

    /// <summary>
    /// Total number of questions.
    /// </summary>
    public required int TotalQuestions { get; init; }

    /// <summary>
    /// Time taken to complete the assessment (in seconds).
    /// </summary>
    public required int TimeTakenSeconds { get; init; }

    /// <summary>
    /// Estimated duration for the assessment (in minutes).
    /// </summary>
    public required int EstimatedDurationMinutes { get; init; }

    /// <summary>
    /// When the assessment was submitted.
    /// </summary>
    public required DateTimeOffset SubmittedAt { get; init; }

    /// <summary>
    /// Performance level (Excellent, Good, Fair, Needs Improvement).
    /// </summary>
    public required string PerformanceLevel { get; init; }

    /// <summary>
    /// Subject-wise breakdown of performance.
    /// </summary>
    public required IReadOnlyList<SubjectPerformanceDto> SubjectBreakdown { get; init; }

    /// <summary>
    /// Areas where the student performed well.
    /// </summary>
    public required IReadOnlyList<string> Strengths { get; init; }

    /// <summary>
    /// Areas that need improvement.
    /// </summary>
    public required IReadOnlyList<string> AreasForImprovement { get; init; }

    /// <summary>
    /// Recommended next steps or topics to study.
    /// </summary>
    public required IReadOnlyList<string> Recommendations { get; init; }

    /// <summary>
    /// Whether the student can review their answers.
    /// </summary>
    public bool CanReviewAnswers { get; init; } = true;
}

/// <summary>
/// Performance breakdown for a specific subject.
/// </summary>
public sealed class SubjectPerformanceDto
{
    /// <summary>
    /// Subject name (e.g., "Algebra", "Cell Biology").
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Number of questions for this subject.
    /// </summary>
    public required int QuestionCount { get; init; }

    /// <summary>
    /// Number of correct answers for this subject.
    /// </summary>
    public required int CorrectCount { get; init; }

    /// <summary>
    /// Score percentage for this subject (0-100).
    /// </summary>
    public required double ScorePercentage { get; init; }

    /// <summary>
    /// Performance level for this subject.
    /// </summary>
    public required string PerformanceLevel { get; init; }
}
