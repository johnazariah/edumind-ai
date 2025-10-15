using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Physics;

/// <summary>
/// Physics assessment agent responsible for generating physics assessments
/// and evaluating student responses for physics questions.
/// Phase 5: Built with OLLAMA LLM integration for semantic evaluation.
/// </summary>
public class PhysicsAssessmentAgent : A2ABaseAgent
{
    private readonly ILLMService? _llmService;

    /// <summary>
    /// Initializes the physics assessment agent.
    /// </summary>
    public PhysicsAssessmentAgent(
        ILLMService? llmService = null,
        ILogger<PhysicsAssessmentAgent>? logger = null)
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
            Name = "PhysicsAssessmentAgent",
            Description = "Generates physics assessments and evaluates student responses for mechanics, thermodynamics, electromagnetism, waves, and optics",
            Version = hasLLM ? "2.0.0" : "1.0.0",
            Subject = Subject.Physics,
            Skills = new List<string>
            {
                "generate_assessment",
                "evaluate_response",
                "mechanics",
                "thermodynamics",
                "electromagnetism",
                "waves",
                "optics",
                "quantum_physics"
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
                { "supports_calculations", true },
                { "supports_diagrams", false }
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger?.LogInformation("Processing physics task type: {TaskType} (TaskId: {TaskId})",
            task.Type, task.TaskId);

        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            _ => throw new NotSupportedException(
                $"Task type '{task.Type}' is not supported by PhysicsAssessmentAgent. " +
                $"Supported types: generate_assessment, evaluate_response")
        };
    }

    /// <summary>
    /// Generates a physics assessment with curated questions.
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
                "Generating physics assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
                studentId, gradeLevel, questionCount);

            await BroadcastProgressAsync(
                $"Physics agent generating {questionCount} questions for grade {gradeLevel}...");

            // Phase 5: Generate sample questions (database integration pending)
            var questions = GenerateSamplePhysicsQuestions(gradeLevel, questionCount);

            Logger?.LogInformation("Generated {Count} physics questions", questions.Count);

            await BroadcastProgressAsync(
                $"Physics assessment generated: {questions.Count} questions created");

            task.Result = new
            {
                questionCount = questions.Count,
                questions = questions,
                subject = "Physics",
                gradeLevel = gradeLevel.ToString(),
                totalPoints = questions.Count,
                passingPercentage = 70,
                recommendedTimeMinutes = questions.Count * 4, // 4 minutes per physics question
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
            Logger?.LogError(ex, "Error generating physics assessment in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Evaluates a physics student response using OLLAMA-powered semantic evaluation.
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

            Logger?.LogInformation("Evaluating physics response using {Method}",
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
                // Use OLLAMA for semantic evaluation
                await BroadcastProgressAsync("Analyzing physics response with OLLAMA AI...");

                var evaluationResult = await _llmService.EvaluateAnswerAsync(
                    question,
                    correctAnswer,
                    studentAnswer,
                    Subject.Physics);

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
                // Fall back to exact match
                (isCorrect, pointsEarned, feedback) = PerformExactMatchEvaluation(
                    studentAnswer, correctAnswer, maxPoints);
                evaluationMethod = "exact_match";
            }

            await BroadcastProgressAsync(
                $"Physics response evaluated: {(isCorrect ? "Correct" : "Incorrect")} ({pointsEarned}/{maxPoints} points)");

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
            Logger?.LogError(ex, "Error evaluating physics response in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Performs exact match evaluation (fallback).
    /// </summary>
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

    /// <summary>
    /// Generates sample physics questions (placeholder for database integration).
    /// </summary>
    private List<dynamic> GenerateSamplePhysicsQuestions(GradeLevel gradeLevel, int count)
    {
        var questions = new List<dynamic>
        {
            new
            {
                id = Guid.NewGuid(),
                text = "What is Newton's Second Law of Motion?",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "mechanics", "newton_laws" },
                points = 1,
                correctAnswer = "F = ma (Force equals mass times acceleration)"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the speed of light in a vacuum?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "electromagnetism", "optics" },
                points = 1,
                correctAnswer = "3 × 10^8 m/s or 300,000,000 m/s"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "State the First Law of Thermodynamics.",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "thermodynamics", "energy" },
                points = 1,
                correctAnswer = "Energy cannot be created or destroyed, only converted from one form to another"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is the formula for kinetic energy?",
                type = "ShortAnswer",
                difficultyLevel = "Easy",
                topics = new[] { "mechanics", "energy" },
                points = 1,
                correctAnswer = "KE = (1/2)mv² or KE = 0.5 × mass × velocity²"
            },
            new
            {
                id = Guid.NewGuid(),
                text = "What is Ohm's Law?",
                type = "ShortAnswer",
                difficultyLevel = "Medium",
                topics = new[] { "electromagnetism", "circuits" },
                points = 1,
                correctAnswer = "V = IR (Voltage equals current times resistance)"
            }
        };

        return questions.Take(Math.Min(count, questions.Count)).ToList();
    }
}
