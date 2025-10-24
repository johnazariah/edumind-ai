using System.Net.Http.Json;
using AcademicAssessment.Core.Models.Dtos;
using Microsoft.AspNetCore.Components;

namespace AcademicAssessment.StudentApp.Components.Pages;

public partial class AssessmentDetail
{
    [Parameter]
    public Guid AssessmentId { get; set; }

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private AssessmentSummary? assessment;
    private bool isLoading = true;
    private bool isSaving;
    private string? errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            assessment = await Http.GetFromJsonAsync<AssessmentSummary>($"api/v1/assessment/{AssessmentId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching assessment details: {ex.Message}");
            errorMessage = "We couldn't load this assessment. Please try again later.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveAndExitAsync()
    {
        if (isSaving)
        {
            return;
        }

        isSaving = true;
        try
        {
            // TODO: Persist the student's current state once backend endpoints are available.
            await Task.Delay(350);
            Navigation.NavigateTo("/assessments");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void StartAssessment()
    {
        Navigation.NavigateTo($"/assessment/{AssessmentId}/session");
    }

    private static int ToPercent(double value) => (int)Math.Clamp(Math.Round(value * 100), 0, 100);

    private static string FormatLastAttempted(DateTimeOffset? timestamp) =>
        timestamp?.ToLocalTime().ToString("MMM d, yyyy") ?? string.Empty;
}
