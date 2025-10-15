using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.English;

/// <summary>
/// English assessment agent responsible for generating English assessments
/// and evaluating student responses for reading, writing, grammar, and literature.
/// Phase 5: Built with OLLAMA LLM integration - especially powerful for essay evaluation.
/// </summary>
public class EnglishAssessmentAgent : A2ABaseAgent
{
    private readonly ILLMService? _llmService;

    /// <summary>
    /// Initializes the English assessment agent.
    /// </summary>
    public EnglishAssessmentAgent(
        ILLMService? llmService = null,
        ILogger<EnglishAssessmentAgent>? logger = null)
        : base(CreateAgentCard(llmService != null), null!, logger, null)
    {
        _llmService = llmService;
    }

    /// <summary>
    /// Creates the agent card describing this agent's capabilities.
    /// </summary>
    private static AgentCard CreateAgentCard(bool hasLLM)
    {
        return new AgentCard
        {
            Name = "EnglishAssessmentAgent",
            Description = "Generates English assessments and evaluates student responses for reading comprehension, writing, grammar, literature, and vocabulary. Enhanced with OLLAMA for semantic evaluation.",
            Version = hasLLM ? "2.0.0" : "1.0.0",
            Subject = Subject.English,
            Skills = new List<string>
            {
                "generate_assessment",
                "evaluate_response",
                "reading_comprehension",
                "writing",
                "grammar",
                "literature",
                "vocabulary",
                "essay_evaluation",
                "creative_writing"
            },
            SupportedGradeLevels = new List<GradeLevel>
            {
                GradeLevel.Grade8,
                GradeLevel.Grade9,
                GradeLevel.Grade10,
                GradeLevel.Grade11,
                GradeLevel.Grade12
            },
            Capabilities = new Dictionary<string, object>
            {
                { "max_questions_per_assessment", 30 },
                { "supports_adaptive_difficulty", true },
                { "evaluation_method", hasLLM ? "semantic_llm" : "exact_match" },
                { "can_generate_explanations", hasLLM },
                { "can_generate_dynamic_questions", hasLLM },
                { "llm_enhanced", hasLLM },
                { "supports_open_ended_questions", hasLLM }
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger?.LogInformation("Processing English task type: {TaskType} (TaskId: {TaskId})",
            task.Type, task.TaskId);

        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            _ => throw new NotSupportedException(
                $"Task type '{task.Type}' is not supported by EnglishAssessmentAgent. " +
                $"Supported types: generate_assessment, evaluate_response")
        };
    }

    /// <summary>
    /// Generates an English assessment with curated questions.
    /// </summary>
    private async Task<AgentTask> GenerateAssessmentAsync(AgentTask task)
    {
        try
        {
            var taskData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                task.DataJson ?? "{}");

            if (taskData == null)
            {
                throw new ArgumentException("Task data is missing or invalid");
            }

            var studentId = Guid.Parse(taskData["studentId"].ToString()!);
            var gradeLevel = Enum.Parse<GradeLevel>(taskData["gradeLevel"].ToString()!);
            var questionCount = taskData.ContainsKey("questionCount")
                ? int.Parse(taskData["questionCount"].ToString()!)
                : 10;

            Logger?.LogInformation(
                "Generating English assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
                studentId, gradeLevel, questionCount);

            await BroadcastProgressAsync(
                $"English agent generating {questionCount} questions for grade {gradeLevel}...");

            var questions = GenerateSampleEnglishQuestions(gradeLevel, questionCount);

            Logger?.LogInformation("Generated {Count} English questions", questions.Count);

            await BroadcastProgressAsync(
                $"English assessment generated: {questions.Count} questions created");

            task.Result = new
            {
                questionCount = questions.Count,
                questions = questions,
                subject = "English",
                gradeLevel = gradeLevel.ToString(),
                totalPoints = questions.Count,
                passingPercentage = 70,
                recommendedTimeMinutes = questions.Count * 3,
                topics = questions.SelectMany(q => ((string[])q.topics)).Distinct().ToList(),
                difficultyDistribution = questions
                    .GroupBy(q => q.difficultyLevel)
                    .ToDictionary(g => g.Key, g => g.Count()),
                generatedBy = AgentCard.Name,
                generatedAt = DateTime.UtcNow
            };

            return task;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error generating English assessment in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Evaluates an English student response using OLLAMA-powered semantic evaluation.
    /// Particularly effective for open-ended, essay, and creative writing responses.
    /// </summary>
    private async Task<AgentTask> EvaluateResponseAsync(AgentTask task)
    {
        try
        {
            var taskData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                task.DataJson ?? "{}");

            if (taskData == null)
            {
                throw new ArgumentException("Task data is missing or invalid");
            }

            var question = taskData["question"]?.ToString() ?? "";
            var studentAnswer = taskData["studentAnswer"]?.ToString() ?? "";
            var correctAnswer = taskData["correctAnswer"]?.ToString() ?? "";
            var maxPoints = taskData.ContainsKey("maxPoints") ? int.Parse(taskData["maxPoints"].ToString()!) : 1;

            Logger?.LogInformation("Evaluating English response using {Method}",
                _llmService != null ? "OLLAMA LLM" : "exact match");

            bool isCorrect;
            int pointsEarned;
            string feedback;
            string evaluationMethod;
            double? score = null;
            string? reasoning = null;
            List<string>? partialCreditAreas = null;

            if (_llmService != null)
            {
                await BroadcastProgressAsync("Analyzing English response with OLLAMA AI...");

                var evaluationResult = await _llmService.EvaluateAnswerAsync(
                    question,
                    correctAnswer,
                    studentAnswer,
                    Subject.English);

                if (evaluationResult is Result<AnswerEvaluation>.Failure failure)
                {
                    Logger?.LogWarning("OLLAMA evaluation failed, falling back to exact match: {Error}",
                        failure.Error);
                    (isCorrect, pointsEarned, feedback) = PerformExactMatchEvaluation(
                        studentAnswer, correctAnswer, maxPoints);
                    evaluationMethod = "exact_match_fallback";
                }
                else if (evaluationResult is Result<AnswerEvaluation>.Success success)
                {
                    var evaluation = success.Value;
                    isCorrect = evaluation.IsCorrect;
                    score = evaluation.Score;
                    pointsEarned = (int)Math.Round(evaluation.Score * maxPoints);
                    feedback = evaluation.Feedback;
                    reasoning = evaluation.Reasoning;
                    partialCreditAreas = evaluation.PartialCreditAreas;
                    evaluationMethod = "semantic_llm_ollama";

                    Logger?.LogInformation(
                        "OLLAMA evaluation: IsCorrect={IsCorrect}, Score={Score:P0}, PointsEarned={Points}",
                        isCorrect, score, pointsEarned);
                }
                else
                {
                    // Unexpected result type - fall back to exact match
                    (isCorrect, pointsEarned, feedback) = PerformExactMatchEvaluation(
                        studentAnswer, correctAnswer, maxPoints);
                    evaluationMethod = "exact_match_fallback";
                }
            }
            else
            {
                (isCorrect, pointsEarned, feedback) = PerformExactMatchEvaluation(
                    studentAnswer, correctAnswer, maxPoints);
                evaluationMethod = "exact_match";
            }

            await BroadcastProgressAsync(
                $"English response evaluated: {(isCorrect ? "Correct" : "Incorrect")} ({pointsEarned}/{maxPoints} points)");

            var result = new Dictionary<string, object>
            {
                { "isCorrect", isCorrect },
                { "pointsEarned", pointsEarned },
                { "maxPoints", maxPoints },
                { "studentAnswer", studentAnswer },
                { "correctAnswer", correctAnswer },
                { "feedback", feedback },
                { "evaluationMethod", evaluationMethod },
                { "evaluatedBy", AgentCard.Name },
                { "evaluatedAt", DateTime.UtcNow }
            };

            if (score.HasValue) result["score"] = score.Value;
            if (reasoning != null) result["reasoning"] = reasoning;
            if (partialCreditAreas != null) result["partialCreditAreas"] = partialCreditAreas;

            task.Result = result;

            return task;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error evaluating English response in task {TaskId}", task.TaskId);
            throw;
        }
    }

    private (bool isCorrect, int pointsEarned, string feedback) PerformExactMatchEvaluation(
        string studentAnswer, string correctAnswer, int maxPoints)
    {
        var studentAns = studentAnswer.Trim().ToLowerInvariant();
        var correctAns = correctAnswer.Trim().ToLowerInvariant();

        var isCorrect = studentAns == correctAns;
        var pointsEarned = isCorrect ? maxPoints : 0;
        var feedback = isCorrect
            ? "Your answer is correct!"
            : $"The correct answer is: {correctAnswer}";

        return (isCorrect, pointsEarned, feedback);
    }

    private List<dynamic> GenerateSampleEnglishQuestions(GradeLevel gradeLevel, int count)
    {
        var questions = new List<dynamic>
        {
            new
            {
                id = Guid.NewGuid(),
                text = "What is the term for a comparison using 'like' or 'as'?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "literary_devices", "figurative_language" },
                points = 1,
                correctAnswer = "Simile"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the main idea or message of a literary work called?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "literature", "reading_comprehension" },
                points = 1,
                correctAnswer = "Theme"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "Which word is the verb in this sentence: 'The cat runs quickly.'?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "grammar", "parts_of_speech" },
                points = 1,
                correctAnswer = "runs"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the opposite of 'synonym'?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "vocabulary" },
                points = 1,
                correctAnswer = "Antonym"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "Who is the author of 'Romeo and Juliet'?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "literature", "drama" },
                points = 1,
                correctAnswer = "William Shakespeare"
            }
        };

        return questions.Take(Math.Min(count, questions.Count)).ToList();
    }
}
