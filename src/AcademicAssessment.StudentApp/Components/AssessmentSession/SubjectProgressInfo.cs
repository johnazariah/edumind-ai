namespace AcademicAssessment.StudentApp.Components.AssessmentSession;

public class SubjectProgressInfo
{
    public required string Subject { get; init; }
    public required int TotalCount { get; init; }
    public required int AnsweredCount { get; init; }
    public int ProgressPercentage => TotalCount == 0 ? 0 : (AnsweredCount * 100) / TotalCount;
}
