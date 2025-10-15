using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Chemistry;

/// <summary>
/// Chemistry assessment agent responsible for generating chemistry assessments
/// and evaluating student responses for chemistry questions.
/// Phase 5: Built with OLLAMA LLM integration for semantic evaluation.
/// </summary>
public class ChemistryAssessmentAgent : A2ABaseAgent
{
    private readonly ILLMService? _llmService;

    /// <summary>
    /// Initializes the chemistry assessment agent.
    /// </summary>
    public ChemistryAssessmentAgent(
        ILLMService? llmService = null,
        ILogger<ChemistryAssessmentAgent>? logger = null)
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
            Name = "ChemistryAssessmentAgent",
            Description = "Generates chemistry assessments and evaluates student responses for organic, inorganic, physical, analytical chemistry, and biochemistry",
            Version = hasLLM ? "2.0.0" : "1.0.0",
            Subject = Subject.Chemistry,
            Skills = new List<string>
            {
                "generate_assessment",
                "evaluate_response",
                "organic_chemistry",
                "inorganic_chemistry",
                "physical_chemistry",
                "analytical_chemistry",
                "biochemistry",
                "chemical_reactions"
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
                { "supports_chemical_formulas", true },
                { "supports_equations", true }
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger?.LogInformation("Processing chemistry task type: {TaskType} (TaskId: {TaskId})",
            task.Type, task.TaskId);

        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            _ => throw new NotSupportedException(
                $"Task type '{task.Type}' is not supported by ChemistryAssessmentAgent. " +
                $"Supported types: generate_assessment, evaluate_response")
        };
    }

    /// <summary>
    /// Generates a chemistry assessment with curated questions.
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
                "Generating chemistry assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
                studentId, gradeLevel, questionCount);

            await BroadcastProgressAsync(
                $"Chemistry agent generating {questionCount} questions for grade {gradeLevel}...");

            var questions = GenerateSampleChemistryQuestions(gradeLevel, questionCount);

            Logger?.LogInformation("Generated {Count} chemistry questions", questions.Count);

            await BroadcastProgressAsync(
                $"Chemistry assessment generated: {questions.Count} questions created");

            task.Result = new
            {
                questionCount = questions.Count,
                questions = questions,
                subject = "Chemistry",
                gradeLevel = gradeLevel.ToString(),
                totalPoints = questions.Count,
                passingPercentage = 70,
                recommendedTimeMinutes = questions.Count * 4,
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
            Logger?.LogError(ex, "Error generating chemistry assessment in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Evaluates a chemistry student response using OLLAMA-powered semantic evaluation.
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

            Logger?.LogInformation("Evaluating chemistry response using {Method}",
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
                await BroadcastProgressAsync("Analyzing chemistry response with OLLAMA AI...");

                var evaluationResult = await _llmService.EvaluateAnswerAsync(
                    question,
                    correctAnswer,
                    studentAnswer,
                    Subject.Chemistry);

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
                $"Chemistry response evaluated: {(isCorrect ? "Correct" : "Incorrect")} ({pointsEarned}/{maxPoints} points)");

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
            Logger?.LogError(ex, "Error evaluating chemistry response in task {TaskId}", task.TaskId);
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

    private List<dynamic> GenerateSampleChemistryQuestions(GradeLevel gradeLevel, int count)
    {
        var questions = new List<dynamic>
        {
            new
            {
                id = Guid.NewGuid(),
                text = "What is the chemical formula for water?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "chemical_formulas", "inorganic_chemistry" },
                points = 1,
                correctAnswer = "H2O"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the pH of a neutral solution at 25°C?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "acids_bases", "physical_chemistry" },
                points = 1,
                correctAnswer = "7"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is Avogadro's number?",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "mole_concept", "physical_chemistry" },
                points = 1,
                correctAnswer = "6.022 × 10^23 or 6.022 x 10^23"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "Balance the equation: H2 + O2 → H2O",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "chemical_reactions", "balancing_equations" },
                points = 1,
                correctAnswer = "2H2 + O2 → 2H2O"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What are the products of combustion of a hydrocarbon in excess oxygen?",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "organic_chemistry", "chemical_reactions" },
                points = 1,
                correctAnswer = "Carbon dioxide (CO2) and water (H2O)"
            }
        };

        return questions.Take(Math.Min(count, questions.Count)).ToList();
    }
}
