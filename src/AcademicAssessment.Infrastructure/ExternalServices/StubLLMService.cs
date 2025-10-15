using System.Text;
using System.Text.Json;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Infrastructure.ExternalServices;

/// <summary>
/// Stub implementation of LLM service for Phase 4
/// Returns realistic mock data for testing. Full Azure OpenAI integration to be added.
/// </summary>
public class StubLLMService : ILLMService
{
    private readonly ILogger<StubLLMService> _logger;

    public StubLLMService(ILogger<StubLLMService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("StubLLMService initialized (mock AI responses)");
    }

    public Task<Result<List<GeneratedQuestion>>> GenerateQuestionsAsync(
        Subject subject,
        GradeLevel gradeLevel,
        string topic,
        DifficultyLevel difficulty,
        int questionCount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "STUB: Generating {Count} {Difficulty} questions for {Subject}/{Topic}",
            questionCount, difficulty, subject, topic);

        // Return mock generated questions
        var questions = new List<GeneratedQuestion>();
        for (int i = 1; i <= questionCount; i++)
        {
            questions.Add(new GeneratedQuestion
            {
                QuestionText = $"{subject} {difficulty} question {i} about {topic}",
                QuestionType = i % 2 == 0 ? QuestionType.MultipleChoice : QuestionType.ShortAnswer,
                CorrectAnswer = $"Answer {i}",
                DistractorOptions = i % 2 == 0
                    ? new List<string> { $"Wrong A{i}", $"Wrong B{i}", $"Wrong C{i}" }
                    : null,
                Explanation = $"This tests understanding of {topic} concepts at {difficulty} level.",
                Topics = new List<string> { topic },
                EstimatedDifficulty = difficulty,
                EstimatedTimeMinutes = difficulty switch
                {
                    DifficultyLevel.Easy => 3,
                    DifficultyLevel.Medium => 5,
                    DifficultyLevel.Hard => 7,
                    _ => 5
                }
            });
        }

        return Task.FromResult<Result<List<GeneratedQuestion>>>(new Result<List<GeneratedQuestion>>.Success(questions));
    }

    public Task<Result<AnswerEvaluation>> EvaluateAnswerAsync(
        string question,
        string correctAnswer,
        string studentAnswer,
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: Evaluating answer for {Subject}", subject);

        // Perform semantic-like evaluation (case-insensitive, trim)
        var studentLower = studentAnswer.Trim().ToLowerInvariant();
        var correctLower = correctAnswer.Trim().ToLowerInvariant();

        var isExactMatch = studentLower == correctLower;
        var isSimilar = studentLower.Contains(correctLower) || correctLower.Contains(studentLower);

        var evaluation = new AnswerEvaluation
        {
            IsCorrect = isExactMatch,
            Score = isExactMatch ? 1.0 : (isSimilar ? 0.7 : 0.0),
            Feedback = isExactMatch
                ? "Excellent! Your answer is correct."
                : isSimilar
                    ? "Your answer shows partial understanding. The complete answer should include: " + correctAnswer
                    : "This answer needs improvement. The correct answer is: " + correctAnswer,
            Reasoning = isExactMatch
                ? "Student provided the exact correct answer."
                : isSimilar
                    ? "Student answer contains key elements but is incomplete."
                    : "Student answer does not match the expected response.",
            PartialCreditAreas = isSimilar && !isExactMatch
                ? new List<string> { "Partial credit awarded for demonstrating some understanding" }
                : null,
            MisconceptionIdentified = !isExactMatch && !isSimilar
                ? new List<string> { "May need to review core concepts" }
                : null
        };

        return Task.FromResult<Result<AnswerEvaluation>>(new Result<AnswerEvaluation>.Success(evaluation));
    }

    public Task<Result<string>> GenerateFeedbackAsync(
        string question,
        string studentAnswer,
        string correctAnswer,
        bool isCorrect,
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: Generating feedback for {Subject}", subject);

        var feedback = isCorrect
            ? $"Great job! You correctly answered: {correctAnswer}. This demonstrates your understanding of the concept."
            : $"Let's review this. The correct answer is: {correctAnswer}. " +
              $"Your answer '{studentAnswer}' shows you're thinking about the problem, " +
              $"but make sure to consider all aspects of the question.";

        return Task.FromResult<Result<string>>(new Result<string>.Success(feedback));
    }

    public Task<Result<StudyRecommendation>> GenerateStudyRecommendationsAsync(
        Guid studentId,
        Subject subject,
        List<StudentResponseSummary> recentResponses,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: Generating recommendations for student {StudentId}", studentId);

        var correctCount = recentResponses.Count(r => r.IsCorrect);
        var totalCount = recentResponses.Count;
        var successRate = totalCount > 0 ? (double)correctCount / totalCount : 0;

        var weakTopics = recentResponses
            .Where(r => !r.IsCorrect)
            .Select(r => r.Topic)
            .Distinct()
            .Take(3)
            .ToList();

        var strongTopics = recentResponses
            .Where(r => r.IsCorrect)
            .Select(r => r.Topic)
            .Distinct()
            .Take(3)
            .ToList();

        var recommendation = new StudyRecommendation
        {
            StrengthAreas = strongTopics.Any()
                ? strongTopics
                : new List<string> { "Keep working to build your strengths" },
            ImprovementAreas = weakTopics.Any()
                ? weakTopics
                : new List<string> { "Continue practicing all topics" },
            RecommendedTopics = weakTopics,
            StudyStrategies = successRate >= 0.7
                ? new List<string>
                {
                    "You're doing well! Continue practicing regularly.",
                    "Try more challenging problems to deepen understanding.",
                    "Review the topics where you made mistakes."
                }
                : new List<string>
                {
                    "Focus on understanding core concepts before moving to advanced topics.",
                    "Practice more problems on weak topics.",
                    "Consider seeking help from teachers or tutors.",
                    "Break study sessions into focused 25-minute intervals."
                },
            SummaryMessage = successRate >= 0.8
                ? $"Excellent work! You're demonstrating strong understanding of {subject}."
                : successRate >= 0.6
                    ? $"Good progress in {subject}! Focus on the areas needing improvement."
                    : $"Keep practicing {subject}. Focus on building foundational understanding."
        };

        return Task.FromResult<Result<StudyRecommendation>>(new Result<StudyRecommendation>.Success(recommendation));
    }
}
