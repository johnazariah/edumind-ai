using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Service for AI-powered operations using Large Language Models
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Generates assessment questions dynamically using LLM
    /// </summary>
    /// <param name="subject">Subject for question generation</param>
    /// <param name="gradeLevel">Grade level (8-12)</param>
    /// <param name="topic">Specific topic or learning objective</param>
    /// <param name="difficulty">Target difficulty level</param>
    /// <param name="questionCount">Number of questions to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated questions with answers and explanations</returns>
    Task<Result<List<GeneratedQuestion>>> GenerateQuestionsAsync(
        Subject subject,
        GradeLevel gradeLevel,
        string topic,
        DifficultyLevel difficulty,
        int questionCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a student's answer using semantic understanding
    /// </summary>
    /// <param name="question">The question text</param>
    /// <param name="correctAnswer">The expected correct answer</param>
    /// <param name="studentAnswer">The student's submitted answer</param>
    /// <param name="subject">Subject context for evaluation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Evaluation result with score, feedback, and reasoning</returns>
    Task<Result<AnswerEvaluation>> EvaluateAnswerAsync(
        string question,
        string correctAnswer,
        string studentAnswer,
        Subject subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates detailed feedback and explanations for student responses
    /// </summary>
    /// <param name="question">The question text</param>
    /// <param name="studentAnswer">Student's answer</param>
    /// <param name="correctAnswer">Expected answer</param>
    /// <param name="isCorrect">Whether answer is correct</param>
    /// <param name="subject">Subject context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed educational feedback</returns>
    Task<Result<string>> GenerateFeedbackAsync(
        string question,
        string studentAnswer,
        string correctAnswer,
        bool isCorrect,
        Subject subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes student's work to provide personalized learning recommendations
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <param name="subject">Subject to analyze</param>
    /// <param name="recentResponses">Recent assessment responses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Personalized study recommendations</returns>
    Task<Result<StudyRecommendation>> GenerateStudyRecommendationsAsync(
        Guid studentId,
        Subject subject,
        List<StudentResponseSummary> recentResponses,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a dynamically generated question
/// </summary>
public record GeneratedQuestion
{
    public required string QuestionText { get; init; }
    public required QuestionType QuestionType { get; init; }
    public required string CorrectAnswer { get; init; }
    public List<string>? DistractorOptions { get; init; }
    public required string Explanation { get; init; }
    public List<string>? Topics { get; init; }
    public required DifficultyLevel EstimatedDifficulty { get; init; }
    public int EstimatedTimeMinutes { get; init; }
}

/// <summary>
/// Result of semantic answer evaluation
/// </summary>
public record AnswerEvaluation
{
    public required bool IsCorrect { get; init; }
    public required double Score { get; init; } // 0.0 to 1.0
    public required string Feedback { get; init; }
    public required string Reasoning { get; init; }
    public List<string>? PartialCreditAreas { get; init; }
    public List<string>? MisconceptionIdentified { get; init; }
}

/// <summary>
/// Personalized study recommendations
/// </summary>
public record StudyRecommendation
{
    public required List<string> StrengthAreas { get; init; }
    public required List<string> ImprovementAreas { get; init; }
    public required List<string> RecommendedTopics { get; init; }
    public required List<string> StudyStrategies { get; init; }
    public required string SummaryMessage { get; init; }
}

/// <summary>
/// Summary of student response for analysis
/// </summary>
public record StudentResponseSummary
{
    public required string QuestionText { get; init; }
    public required string StudentAnswer { get; init; }
    public required string CorrectAnswer { get; init; }
    public required bool IsCorrect { get; init; }
    public required string Topic { get; init; }
    public required DifficultyLevel Difficulty { get; init; }
}
