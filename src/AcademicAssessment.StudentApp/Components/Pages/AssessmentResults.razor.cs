using System.Net.Http.Json;
using AcademicAssessment.Core.Models.Dtos;
using Microsoft.AspNetCore.Components;

namespace AcademicAssessment.StudentApp.Components.Pages;

public partial class AssessmentResults
{
    [Parameter]
    public Guid SessionId { get; set; }

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private AssessmentResultsDto? results;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        await LoadResultsAsync();
    }

    private async Task LoadResultsAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            var response = await Http.GetAsync($"api/v1.0/Assessment/results/{SessionId}");

            if (response.IsSuccessStatusCode)
            {
                results = await response.Content.ReadFromJsonAsync<AssessmentResultsDto>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = "Results not found. The assessment session may not exist or results are not yet available.";
            }
            else
            {
                errorMessage = "Failed to load results. Please try again later.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading results: {ex.Message}";
            Console.WriteLine($"Error loading results: {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private static string FormatTime(int seconds)
    {
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;

        if (minutes >= 60)
        {
            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            return $"{hours}h {remainingMinutes}m";
        }

        return $"{minutes}m {remainingSeconds}s";
    }

    private static string GetTimeEfficiencyClass(AssessmentResultsDto results)
    {
        var percentageUsed = results.TimeTakenSeconds / 60.0 / results.EstimatedDurationMinutes * 100;
        return percentageUsed switch
        {
            < 50 => "bg-success",
            < 75 => "bg-info",
            < 100 => "bg-warning",
            _ => "bg-danger"
        };
    }

    private static string GetPerformanceBadgeClass(string performanceLevel) => performanceLevel switch
    {
        "Excellent" => "bg-success",
        "Good" => "bg-primary",
        "Fair" => "bg-warning",
        "Needs Improvement" => "bg-danger",
        _ => "bg-secondary"
    };

    private static string GetPerformanceProgressClass(string performanceLevel) => performanceLevel switch
    {
        "Excellent" => "bg-success",
        "Good" => "bg-primary",
        "Fair" => "bg-warning",
        "Needs Improvement" => "bg-danger",
        _ => "bg-secondary"
    };

    private void ReviewAnswers()
    {
        // TODO: Navigate to answer review page once implemented
        Navigation.NavigateTo($"/assessment/{results?.AssessmentId}/review/{SessionId}");
    }

    private void RetakeAssessment()
    {
        if (results is not null)
        {
            Navigation.NavigateTo($"/assessment/{results.AssessmentId}");
        }
    }
}
