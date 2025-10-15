using AcademicAssessment.Agents.Shared;
using AcademicAssessment.Agents.Shared.Interfaces;
using AcademicAssessment.Agents.Shared.Models;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Agents.Mathematics;

/// <summary>
/// Mathematics assessment agent responsible for generating math assessments
/// and evaluating student responses for mathematics questions.
/// </summary>
public class MathematicsAssessmentAgent : A2ABaseAgent
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IStudentResponseRepository _responseRepository;
    private readonly IAssessmentRepository _assessmentRepository;

    /// <summary>
    /// Initializes the mathematics assessment agent.
    /// </summary>
    public MathematicsAssessmentAgent(
        ITaskService taskService,
        IQuestionRepository questionRepository,
        IStudentResponseRepository responseRepository,
        IAssessmentRepository assessmentRepository,
        ILogger<MathematicsAssessmentAgent> logger,
        string? signalRHubUrl = "https://localhost:5001/hubs/agent-progress")
        : base(CreateAgentCard(), taskService, logger, signalRHubUrl)
    {
        _questionRepository = questionRepository;
        _responseRepository = responseRepository;
        _assessmentRepository = assessmentRepository;
    }

    /// <summary>
    /// Creates the agent card describing this agent's capabilities.
    /// </summary>
    private static AgentCard CreateAgentCard()
    {
        return new AgentCard
        {
            Name = "MathematicsAssessmentAgent",
            Description = "Generates mathematics assessments and evaluates student responses for algebra, geometry, calculus, and statistics",
            Version = "1.0.0",
            Subject = Subject.Mathematics,
            Skills = new List<string>
            {
                "generate_assessment",
                "evaluate_response",
                "algebra",
                "geometry",
                "calculus",
                "statistics",
                "trigonometry"
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
                { "evaluation_method", "exact_match" },
                { "can_generate_explanations", false } // Phase 2: no LLM yet
            }
        };
    }

    /// <summary>
    /// Process incoming tasks based on task type.
    /// </summary>
    protected override async Task<AgentTask> ProcessTaskAsync(AgentTask task)
    {
        Logger.LogInformation("Processing mathematics task type: {TaskType} (TaskId: {TaskId})",
            task.Type, task.TaskId);

        return task.Type switch
        {
            "generate_assessment" => await GenerateAssessmentAsync(task),
            "evaluate_response" => await EvaluateResponseAsync(task),
            _ => throw new NotSupportedException(
                $"Task type '{task.Type}' is not supported by MathematicsAssessmentAgent. " +
                $"Supported types: generate_assessment, evaluate_response")
        };
    }

    /// <summary>
    /// Generates a mathematics assessment by selecting questions from the database.
    /// </summary>
    private async Task<AgentTask> GenerateAssessmentAsync(AgentTask task)
    {
        try
        {
            // Extract parameters from task data
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

            Logger.LogInformation(
                "Generating mathematics assessment for student {StudentId}, grade {GradeLevel}, {QuestionCount} questions",
                studentId, gradeLevel, questionCount);

            // Broadcast progress
            await BroadcastProgressAsync(
                $"Mathematics agent selecting {questionCount} questions for grade {gradeLevel}...");

            // Load mathematics questions from database for this grade level
            var questionsResult = await _questionRepository.GetBySubjectAndGradeLevelAsync(Subject.Mathematics, gradeLevel);

            if (questionsResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to load mathematics questions for grade {gradeLevel}");
            }

            var suitableQuestions = ((AcademicAssessment.Core.Common.Result<IReadOnlyList<AcademicAssessment.Core.Models.Question>>.Success)questionsResult).Value.ToList();

            if (suitableQuestions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No mathematics questions available for grade level {gradeLevel}");
            }

            Logger.LogInformation("Found {Count} suitable mathematics questions for grade {GradeLevel}",
                suitableQuestions.Count, gradeLevel);

            // Select random questions (or all if fewer than requested)
            var random = new Random();
            var selectedQuestions = suitableQuestions
                .OrderBy(_ => random.Next())
                .Take(Math.Min(questionCount, suitableQuestions.Count))
                .ToList();

            Logger.LogInformation("Selected {Count} mathematics questions", selectedQuestions.Count);

            // Prepare question details for result
            var questionDetails = selectedQuestions.Select(q => new
            {
                id = q.Id,
                text = q.QuestionText,
                type = q.QuestionType.ToString(),
                difficultyLevel = q.DifficultyLevel.ToString(),
                topics = q.Topics,
                points = 1 // Each question worth 1 point in Phase 3
            }).ToList();

            // Broadcast completion
            await BroadcastProgressAsync(
                $"Mathematics assessment generated: {selectedQuestions.Count} questions selected");

            Logger.LogInformation(
                "Mathematics assessment generated successfully with {QuestionCount} questions",
                selectedQuestions.Count);

            // Return result with question data
            // The orchestrator or calling service will create the actual Assessment entity
            task.Result = new
            {
                questionCount = selectedQuestions.Count,
                questions = questionDetails,
                subject = "Mathematics",
                gradeLevel = gradeLevel.ToString(),
                totalPoints = selectedQuestions.Count,
                passingPercentage = 70,
                recommendedTimeMinutes = selectedQuestions.Count * 3, // 3 minutes per question
                topics = selectedQuestions.SelectMany(q => q.Topics).Distinct().ToList(),
                difficultyDistribution = selectedQuestions
                    .GroupBy(q => q.DifficultyLevel)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                generatedBy = AgentCard.Name,
                generatedAt = DateTime.UtcNow
            };

            return task;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating mathematics assessment in task {TaskId}", task.TaskId);
            throw;
        }
    }

    /// <summary>
    /// Evaluates a student response using exact match comparison.
    /// Phase 2: Simple exact match (case-insensitive).
    /// Phase 4: Will use LLM for semantic evaluation.
    /// </summary>
    private async Task<AgentTask> EvaluateResponseAsync(AgentTask task)
    {
        try
        {
            // Extract parameters from task data
            var taskData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                task.DataJson ?? "{}");

            if (taskData == null || !taskData.ContainsKey("responseId"))
            {
                throw new ArgumentException("Task data must contain 'responseId'");
            }

            var responseId = Guid.Parse(taskData["responseId"].ToString()!);

            Logger.LogInformation("Evaluating mathematics response: {ResponseId}", responseId);

            // Load response from database
            var responseResult = await _responseRepository.GetByIdAsync(responseId);

            if (responseResult.IsFailure)
            {
                throw new InvalidOperationException($"Response {responseId} not found");
            }

            var response = ((AcademicAssessment.Core.Common.Result<AcademicAssessment.Core.Models.StudentResponse>.Success)responseResult).Value;

            // Load question to get correct answer
            var questionResult = await _questionRepository.GetByIdAsync(response.QuestionId);

            if (questionResult.IsFailure)
            {
                throw new InvalidOperationException($"Question {response.QuestionId} not found");
            }

            var question = ((AcademicAssessment.Core.Common.Result<AcademicAssessment.Core.Models.Question>.Success)questionResult).Value;

            // Perform exact match evaluation (case-insensitive, trim whitespace)
            var studentAnswer = (response.StudentAnswer ?? "").Trim().ToLowerInvariant();
            var correctAnswer = (question.CorrectAnswer ?? "").Trim().ToLowerInvariant();

            var isCorrect = studentAnswer == correctAnswer;
            var pointsEarned = isCorrect ? response.MaxPoints : 0;

            Logger.LogInformation(
                "Response {ResponseId} evaluated: {IsCorrect} (Student: '{StudentAnswer}', Correct: '{CorrectAnswer}')",
                responseId, isCorrect, studentAnswer, correctAnswer);

            // Broadcast progress
            await BroadcastProgressAsync(
                $"Mathematics response {responseId} evaluated: {(isCorrect ? "Correct" : "Incorrect")}");

            // Return evaluation result
            // The calling service will update the StudentResponse entity
            task.Result = new
            {
                responseId = responseId,
                questionId = question.Id,
                isCorrect = isCorrect,
                pointsEarned = pointsEarned,
                maxPoints = response.MaxPoints,
                studentAnswer = response.StudentAnswer,
                correctAnswer = question.CorrectAnswer,
                feedback = isCorrect
                    ? "Your answer is correct!"
                    : $"The correct answer is: {question.CorrectAnswer}",
                evaluationMethod = "exact_match",
                evaluatedBy = AgentCard.Name,
                evaluatedAt = DateTime.UtcNow
            };

            return task;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error evaluating mathematics response in task {TaskId}", task.TaskId);
            throw;
        }
    }
}
