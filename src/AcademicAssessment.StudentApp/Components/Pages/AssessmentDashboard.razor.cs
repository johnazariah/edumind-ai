using System.Net.Http.Json;
using AcademicAssessment.Core.Models.Dtos;
using Microsoft.AspNetCore.Components;

namespace AcademicAssessment.StudentApp.Components.Pages;

public partial class AssessmentDashboard
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    private List<AssessmentSummary>? assessments;
    private string searchTerm = string.Empty;
    private string selectedSubject = string.Empty;
    private string selectedDifficulty = string.Empty;

    private IEnumerable<AssessmentSummary> FilteredAssessments =>
        assessments?
            .Where(a => string.IsNullOrWhiteSpace(searchTerm) || a.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(a => string.IsNullOrWhiteSpace(selectedSubject) || a.Subject.Equals(selectedSubject, StringComparison.OrdinalIgnoreCase))
            .Where(a => string.IsNullOrWhiteSpace(selectedDifficulty) || a.Difficulty.Equals(selectedDifficulty, StringComparison.OrdinalIgnoreCase))
        ?? Enumerable.Empty<AssessmentSummary>();

    private IEnumerable<string> allSubjects =>
        assessments?.Select(a => a.Subject).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s) ?? Enumerable.Empty<string>();

    private IEnumerable<string> allDifficulties =>
        assessments?.Select(a => a.Difficulty).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d) ?? Enumerable.Empty<string>();

    private static int ToPercent(double value) => (int)Math.Clamp(Math.Round(value * 100), 0, 100);

    private static string? FormatLastAttempted(DateTimeOffset? timestamp) =>
        timestamp?.ToLocalTime().ToString("MMM d, yyyy");

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Use relative path since base address is configured in Program.cs
            assessments = await Http.GetFromJsonAsync<List<AssessmentSummary>>("api/v1/assessment");
        }
        catch (Exception ex)
        {
            // TODO: Add proper logging
            Console.WriteLine($"Error fetching assessments: {ex.Message}");
        }
    }
}
