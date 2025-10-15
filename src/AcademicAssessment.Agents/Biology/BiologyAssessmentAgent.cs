using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Biology;

/// <summary>
/// Biology assessment agent responsible for generating biology assessments
/// and evaluating student responses for biology questions.
/// Phase 5: Built with OLLAMA LLM integration for semantic evaluation.
/// </summary>
public class BiologyAssessmentAgent : A2ABaseAgent
{
    private readonly ILLMService? _llmService;

    /// <summary>
    /// Initializes the biology assessment agent.
    /// </summary>
    public BiologyAssessmentAgent(
        ILLMService? llmService = null,
        ILogger<BiologyAssessmentAgent>? logger = null)
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
            Name = "BiologyAssessmentAgent",
            Description = "Generates biology assessments and evaluates student responses for cell biology, genetics, ecology, evolution, and anatomy",
            Version = hasLLM ? "2.0.0" : "1.0.0",
            Subject = Subject.Biology,
            Skills = new List<string>
            {
                "generate_assessment",
                "evaluate_response",
                "cell_biology",
                "genetics",
                "ecology",
                "evolution",
                "anatomy",
                "physiology",
                "molecular_biology"
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
                { "supports_diagrams", false }
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger?.LogInformation("Processing biology task type: {TaskType} (TaskId: {TaskId})",
            task.Type, task.TaskId);

        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            _ => throw new NotSupportedException(
                $"Task type '{task.Type}' is not supported by BiologyAssessmentAgent. " +
                $"Supported types: generate_assessment, evaluate_response")
        };
    }

    /// <summary>
    /// Generates a biology assessment with curated questions.
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
                "Generating biology assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
                studentId, gradeLevel, questionCount);

            await BroadcastProgressAsync(
                $"Biology agent generating {questionCount} questions for grade {gradeLevel}...");

            var questions = GenerateSampleBiologyQuestions(gradeLevel, questionCount);

            Logger?.LogInformation("Generated {Count} biology questions", questions.Count);

            await BroadcastProgressAsync(
                $"Biology assessment generated: {questions.Count} questions created");

            task.Result = new
            {
                questionCount = questions.Count,
                questions = questions,
                subject = "Biology",
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
            Logger?.LogError(ex, "Error generating biology assessment in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Evaluates a biology student response using OLLAMA-powered semantic evaluation.
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

            Logger?.LogInformation("Evaluating biology response using {Method}",
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
                await BroadcastProgressAsync("Analyzing biology response with OLLAMA AI...");

                var evaluationResult = await _llmService.EvaluateAnswerAsync(
                    question,
                    correctAnswer,
                    studentAnswer,
                    Subject.Biology);

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
                $"Biology response evaluated: {(isCorrect ? "Correct" : "Incorrect")} ({pointsEarned}/{maxPoints} points)");

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
            Logger?.LogError(ex, "Error evaluating biology response in task {TaskId}", task.TaskId);
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

    private List<dynamic> GenerateSampleBiologyQuestions(GradeLevel gradeLevel, int count)
    {
        var questions = new List<dynamic>
        {
            new
            {
                id = Guid.NewGuid(),
                text = "What is the powerhouse of the cell?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "cell_biology", "organelles" },
                points = 1,
                correctAnswer = "Mitochondria"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the process by which plants make food using sunlight?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "photosynthesis", "plant_biology" },
                points = 1,
                correctAnswer = "Photosynthesis"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the basic unit of heredity?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "genetics", "heredity" },
                points = 1,
                correctAnswer = "Gene"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is DNA an acronym for?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "molecular_biology", "genetics" },
                points = 1,
                correctAnswer = "Deoxyribonucleic acid"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What are the three main types of blood cells?",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "anatomy", "physiology" },
                points = 1,
                correctAnswer = "Red blood cells (erythrocytes), white blood cells (leukocytes), and platelets (thrombocytes)"
            }
        };

        return questions.Take(Math.Min(count, questions.Count)).ToList();
    }
}
