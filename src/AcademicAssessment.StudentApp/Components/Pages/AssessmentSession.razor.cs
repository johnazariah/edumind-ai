using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models.Dtos;
using Microsoft.AspNetCore.Components;

namespace AcademicAssessment.StudentApp.Components.Pages;

public partial class AssessmentSession : IDisposable
{
    private readonly Dictionary<Guid, AnswerState> _answers = new();
    private readonly HashSet<int> _answeredQuestions = new();
    private readonly HashSet<int> _reviewQuestions = new();
    private readonly SemaphoreSlim _autosaveSemaphore = new(1, 1);

    [Parameter]
    public Guid AssessmentId { get; set; }

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private AssessmentSessionDto? session;
    private bool isLoading = true;
    private string? errorMessage;
    private int currentQuestionIndex;
    private TimeSpan timeRemaining;
    private DateTimeOffset? lastSavedAt;
    private bool isAutoSaving;
    private string? autoSaveError;

    // Toast notification state
    private bool showToast;
    private string toastTitle = string.Empty;
    private string? toastMessage;
    private ToastType toastType;

    private CancellationTokenSource? timerCts;
    private AssessmentQuestionDto? CurrentQuestion =>
        session is { Questions.Count: > 0 } && currentQuestionIndex >= 0 && currentQuestionIndex < session.Questions.Count
            ? session.Questions[currentQuestionIndex]
            : null;

    private int CurrentQuestionNumber => currentQuestionIndex + 1;

    private int TotalQuestions => session?.Questions.Count ?? 0;

    private int AnsweredCount => _answeredQuestions.Count;

    private int ProgressPercent => TotalQuestions == 0
        ? 0
        : (int)Math.Clamp(Math.Round((AnswerCountFraction() * 100)), 0, 100);

    private bool IsSessionExpired => timeRemaining <= TimeSpan.Zero && session is not null;

    private string TimeRemainingDisplay =>
        timeRemaining <= TimeSpan.Zero
            ? "00:00"
            : timeRemaining.TotalHours >= 1
                ? timeRemaining.ToString(@"hh\:mm\:ss")
                : timeRemaining.ToString(@"mm\:ss");

    protected override async Task OnParametersSetAsync()
    {
        await LoadSessionAsync();
    }

    private async Task LoadSessionAsync()
    {
        CancelTimers();
        isLoading = true;
        errorMessage = null;
        session = null;
        currentQuestionIndex = 0;
        _answers.Clear();
        _answeredQuestions.Clear();
        _reviewQuestions.Clear();
        autoSaveError = null;
        lastSavedAt = null;

        try
        {
            session = await Http.GetFromJsonAsync<AssessmentSessionDto>($"http://academicassessment-web/api/v1/assessment/{AssessmentId}/session");
            if (session is null)
            {
                errorMessage = "We could not start this assessment session.";
                return;
            }

            foreach (var (question, index) in session.Questions.Select((q, idx) => (q, idx)))
            {
                _answers[question.Id] = new AnswerState();
                if (question.QuestionType is QuestionType.TrueFalse && question.Options.Count == 0)
                {
                    _answers[question.Id].SelectedOptions.Add("false");
                }
            }

            timeRemaining = CalculateTimeRemaining();
            StartTimers();
        }
        catch (HttpRequestException ex)
        {
            errorMessage = "Unable to reach the assessment service. Please try again later.";
            Console.WriteLine($"Assessment session fetch failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            errorMessage = "Something went wrong while loading the assessment.";
            Console.WriteLine($"Unexpected error loading session: {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void StartTimers()
    {
        if (session is null)
        {
            return;
        }

        timerCts = new CancellationTokenSource();
        var token = timerCts.Token;

        _ = RunCountdownAsync(token);
        _ = RunAutosaveAsync(token);
    }

    private TimeSpan CalculateTimeRemaining()
    {
        if (session is null)
        {
            return TimeSpan.Zero;
        }

        var remaining = session.ExpiresAt - DateTimeOffset.UtcNow;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        if (session is null)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                timeRemaining = CalculateTimeRemaining();
                await InvokeAsync(StateHasChanged);

                if (timeRemaining <= TimeSpan.Zero)
                {
                    timeRemaining = TimeSpan.Zero;
                    await AutoSaveAsync(isBackground: false);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the user navigates away
        }
    }

    private async Task RunAutosaveAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await AutoSaveAsync(isBackground: true);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disposal
        }
    }

    private async Task AutoSaveAsync(bool isBackground)
    {
        if (session is null)
        {
            return;
        }

        if (isBackground)
        {
            var acquired = await _autosaveSemaphore.WaitAsync(TimeSpan.Zero);
            if (!acquired)
            {
                return;
            }
        }
        else
        {
            await _autosaveSemaphore.WaitAsync();
        }

        try
        {
            isAutoSaving = true;
            autoSaveError = null;

            // Build save request
            var saveRequest = new SaveAssessmentSessionRequest
            {
                AssessmentId = AssessmentId,
                Answers = _answers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new QuestionAnswerDto
                    {
                        QuestionId = kvp.Key,
                        SelectedOptions = kvp.Value.SelectedOptions,
                        FreeResponse = kvp.Value.FreeResponse
                    }),
                ReviewFlags = new HashSet<int>(_reviewQuestions)
            };

            // Call save API
            var response = await Http.PostAsJsonAsync(
                $"api/v1.0/Assessment/{AssessmentId}/session/save",
                saveRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SaveAssessmentSessionResponse>();
                lastSavedAt = result?.SavedAt ?? DateTimeOffset.UtcNow;
            }
            else
            {
                autoSaveError = "Save failed. Will retry shortly.";
            }
        }
        catch (Exception ex)
        {
            autoSaveError = "Auto-save failed. Changes will retry shortly.";
            Console.WriteLine($"Auto-save failed: {ex}");
        }
        finally
        {
            isAutoSaving = false;
            _autosaveSemaphore.Release();

            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
                // Component disposed.
            }
        }
    }

    private void NavigateToQuestion(int questionNumber)
    {
        if (session is null)
        {
            return;
        }

        var clamped = Math.Clamp(questionNumber - 1, 0, session.Questions.Count - 1);
        currentQuestionIndex = clamped;
        StateHasChanged();
    }

    private void NextQuestion()
    {
        if (session is null)
        {
            return;
        }

        if (currentQuestionIndex < session.Questions.Count - 1)
        {
            currentQuestionIndex++;
            StateHasChanged();
        }
    }

    private void PreviousQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            StateHasChanged();
        }
    }

    private HashSet<string> GetSelectedOptions(Guid questionId)
    {
        if (!_answers.TryGetValue(questionId, out var state))
        {
            state = new AnswerState();
            _answers[questionId] = state;
        }

        return state.SelectedOptions;
    }

    private string GetFreeResponse(Guid questionId)
    {
        if (!_answers.TryGetValue(questionId, out var state))
        {
            state = new AnswerState();
            _answers[questionId] = state;
        }

        return state.FreeResponse;
    }

    private async Task UpdateSelectedOptionsAsync(Guid questionId, HashSet<string> selected)
    {
        if (IsSessionExpired)
        {
            return;
        }

        if (!_answers.TryGetValue(questionId, out var state))
        {
            state = new AnswerState();
            _answers[questionId] = state;
        }

        state.SelectedOptions = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        UpdateAnsweredState(questionId, HasAnswer(state));
        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateFreeResponseAsync(Guid questionId, string value)
    {
        if (IsSessionExpired)
        {
            return;
        }

        if (!_answers.TryGetValue(questionId, out var state))
        {
            state = new AnswerState();
            _answers[questionId] = state;
        }

        state.FreeResponse = value;
        UpdateAnsweredState(questionId, HasAnswer(state));
        await InvokeAsync(StateHasChanged);
    }

    private void ClearCurrentAnswer()
    {
        if (IsSessionExpired)
        {
            return;
        }

        if (CurrentQuestion is null)
        {
            return;
        }

        if (_answers.TryGetValue(CurrentQuestion.Id, out var state))
        {
            state.SelectedOptions.Clear();
            state.FreeResponse = string.Empty;
            UpdateAnsweredState(CurrentQuestion.Id, false);
            StateHasChanged();
        }
    }

    private void ToggleReview()
    {
        if (IsSessionExpired)
        {
            return;
        }

        if (CurrentQuestion is null)
        {
            return;
        }

        var questionNumber = CurrentQuestionNumber;
        if (!_reviewQuestions.Add(questionNumber))
        {
            _reviewQuestions.Remove(questionNumber);
        }

        StateHasChanged();
    }

    private bool IsMarkedForReview() => _reviewQuestions.Contains(CurrentQuestionNumber);

    private void UpdateAnsweredState(Guid questionId, bool answered)
    {
        if (session is null)
        {
            return;
        }

        var questionNumber = GetQuestionNumber(questionId);
        if (questionNumber <= 0)
        {
            return;
        }

        if (answered)
        {
            _answeredQuestions.Add(questionNumber);
        }
        else
        {
            _answeredQuestions.Remove(questionNumber);
        }
    }

    private bool HasAnswer(AnswerState state) =>
        state.SelectedOptions.Count > 0 || !string.IsNullOrWhiteSpace(state.FreeResponse);

    private double AnswerCountFraction() =>
        TotalQuestions == 0 ? 0 : (double)AnsweredCount / TotalQuestions;

    private int GetQuestionNumber(Guid questionId)
    {
        if (session is null)
        {
            return -1;
        }

        for (var i = 0; i < session.Questions.Count; i++)
        {
            if (session.Questions[i].Id == questionId)
            {
                return i + 1;
            }
        }

        return -1;
    }

    private async Task SaveProgressAndExitAsync()
    {
        await AutoSaveAsync(isBackground: false);
        Navigation.NavigateTo("/assessments");
    }

    private async Task SaveProgressAsync()
    {
        await AutoSaveAsync(isBackground: false);
        if (string.IsNullOrEmpty(autoSaveError))
        {
            ShowToast("Progress Saved", $"Your answers have been saved. ({AnsweredCount}/{TotalQuestions} answered)", ToastType.Success);
        }
        else
        {
            ShowToast("Save Failed", autoSaveError, ToastType.Error);
        }
    }

    private async Task SubmitAssessmentAsync()
    {
        if (session is null)
        {
            return;
        }

        try
        {
            isAutoSaving = true;
            autoSaveError = null;

            // Calculate time taken
            var timeTakenSeconds = session.DurationMinutes * 60 - (int)timeRemaining.TotalSeconds;

            // Build submit request
            var submitRequest = new SubmitAssessmentSessionRequest
            {
                AssessmentId = AssessmentId,
                Answers = _answers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new QuestionAnswerDto
                    {
                        QuestionId = kvp.Key,
                        SelectedOptions = kvp.Value.SelectedOptions,
                        FreeResponse = kvp.Value.FreeResponse
                    }),
                TimeTakenSeconds = timeTakenSeconds
            };

            // Call submit API
            var response = await Http.PostAsJsonAsync(
                $"api/v1.0/Assessment/{AssessmentId}/session/submit",
                submitRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();
                if (result?.Success == true)
                {
                    ShowToast("Assessment Submitted", 
                        $"Successfully submitted {result.QuestionsAnswered} of {result.TotalQuestions} answers. Redirecting...", 
                        ToastType.Success);
                    
                    // Give user time to see the toast before navigating
                    await Task.Delay(2000);
                    
                    // Navigate to results page once it's implemented
                    // For now, go back to detail page
                    Navigation.NavigateTo($"/assessment/{AssessmentId}");
                }
                else
                {
                    autoSaveError = result?.ErrorMessage ?? "Submission failed. Please try again.";
                    ShowToast("Submission Failed", autoSaveError, ToastType.Error);
                }
            }
            else
            {
                autoSaveError = "Submission failed. Please try again.";
                ShowToast("Submission Failed", autoSaveError, ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            autoSaveError = $"Submission error: {ex.Message}";
            Console.WriteLine($"Submit failed: {ex}");
        }
        finally
        {
            isAutoSaving = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private IReadOnlySet<int> GetAnsweredQuestions() => _answeredQuestions;

    private IReadOnlySet<int> GetReviewQuestions() => _reviewQuestions;

    private string BuildAutoSaveStatus()
    {
        if (isAutoSaving)
        {
            return "Saving...";
        }

        if (!string.IsNullOrEmpty(autoSaveError))
        {
            return autoSaveError;
        }

        return lastSavedAt is null
            ? "Not yet saved"
            : $"Last saved {FormatRelativeTime(lastSavedAt.Value)}";
    }

    private static string FormatRelativeTime(DateTimeOffset timestamp)
    {
        var elapsed = DateTimeOffset.UtcNow - timestamp;
        return elapsed switch
        {
            var span when span.TotalSeconds < 5 => "just now",
            var span when span.TotalSeconds < 60 => "a few seconds ago",
            var span when span.TotalMinutes < 2 => "a minute ago",
            var span when span.TotalMinutes < 60 => $"{Math.Floor(span.TotalMinutes)} minutes ago",
            var span when span.TotalHours < 2 => "an hour ago",
            var span when span.TotalHours < 24 => $"{Math.Floor(span.TotalHours)} hours ago",
            var span when span.TotalDays < 2 => "yesterday",
            _ => timestamp.LocalDateTime.ToString("MMM d, h:mm tt")
        };
    }

    private void ShowToast(string title, string? message, ToastType type)
    {
        toastTitle = title;
        toastMessage = message;
        toastType = type;
        showToast = true;
        StateHasChanged();
    }

    private Task HandleToastVisibilityChanged(bool visible)
    {
        showToast = visible;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        CancelTimers();
        _autosaveSemaphore.Dispose();
    }

    private void CancelTimers()
    {
        try
        {
            timerCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // no-op
        }

        timerCts?.Dispose();
        timerCts = null;
    }

    private sealed class AnswerState
    {
        public HashSet<string> SelectedOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string FreeResponse { get; set; } = string.Empty;
    }
}
