using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Infrastructure.ExternalServices;

/// <summary>
/// OLLAMA-based LLM service for local AI-powered educational features.
/// Provides zero-cost, low-latency, privacy-focused semantic evaluation
/// using locally-hosted open-source models (Llama 3.2, Mistral, etc.).
/// </summary>
public class OllamaService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _baseUrl;
    private readonly string _modelName;

    public OllamaService(
        IConfiguration configuration,
        ILogger<OllamaService> logger,
        HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();

        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "llama3.2:3b";

        _httpClient.Timeout = TimeSpan.FromMinutes(2); // LLM inference can take time

        _logger.LogInformation("OllamaService initialized: {BaseUrl}, Model: {ModelName}", _baseUrl, _modelName);
    }

    public async Task<Result<List<GeneratedQuestion>>> GenerateQuestionsAsync(
        Subject subject,
        GradeLevel gradeLevel,
        string topic,
        DifficultyLevel difficulty,
        int questionCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"You are an expert educational content creator specializing in {subject} for Grade {gradeLevel}.

Generate {questionCount} high-quality multiple-choice questions for the topic: {topic} at {difficulty} difficulty level.

For each question, provide:
1. The question text (clear and unambiguous)
2. The correct answer
3. Three plausible distractors (incorrect but believable options)
4. A brief explanation of why the answer is correct

Format your response as JSON array with this structure:
[
  {{
    ""questionText"": ""..."",
    ""correctAnswer"": ""...,
    ""distractors"": [""..."", ""..."", ""...""],
    ""explanation"": ""...""
  }}
]

Only return the JSON array, no additional text.";

            var response = await CallOllamaAsync(prompt, cancellationToken);

            // Parse the JSON response
            var questions = ParseQuestionsFromResponse(response, topic, difficulty);

            if (questions.Count == 0)
            {
                return new Result<List<GeneratedQuestion>>.Failure(
                    new Error("OllamaService.GenerateQuestions", "Failed to parse generated questions from OLLAMA response"));
            }

            _logger.LogInformation("Generated {Count} questions for {Subject}/{Topic} Grade {GradeLevel} at {Difficulty} level",
                questions.Count, subject, topic, gradeLevel, difficulty);

            return new Result<List<GeneratedQuestion>>.Success(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating questions via OLLAMA for {Subject}/{Topic}", subject, topic);
            return new Result<List<GeneratedQuestion>>.Failure(
                new Error("OllamaService.GenerateQuestions", $"OLLAMA generation failed: {ex.Message}"));
        }
    }

    public async Task<Result<AnswerEvaluation>> EvaluateAnswerAsync(
        string question,
        string correctAnswer,
        string studentAnswer,
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"You are an expert {subject} teacher evaluating student answers with fairness and educational insight.

Question: {question}
Student Answer: {studentAnswer}
Correct Answer: {correctAnswer}
Subject: {subject}

Evaluate the student's answer considering:
1. Correctness (exact match, semantically equivalent, partially correct, or incorrect)
2. Understanding demonstrated
3. Common misconceptions (if applicable)
4. Partial credit opportunities

Provide your evaluation as JSON with this exact structure:
{{
  ""score"": 0.0 to 1.0,
  ""reasoning"": ""Explain why this score was given"",
  ""isCorrect"": true or false,
  ""hasPartialCredit"": true or false,
  ""misconceptionsIdentified"": [""...""],
  ""partialCreditAreas"": [""...""]
}}

Scoring guidelines:
- 1.0: Completely correct (exact or semantically equivalent)
- 0.7-0.9: Mostly correct with minor errors
- 0.4-0.6: Partially correct, shows some understanding
- 0.1-0.3: Incorrect but shows minimal relevant knowledge
- 0.0: Completely incorrect or irrelevant

Only return the JSON object, no additional text.";

            var response = await CallOllamaAsync(prompt, cancellationToken);

            // Parse the evaluation response
            var evaluation = ParseEvaluationFromResponse(response);

            if (evaluation == null)
            {
                // Fallback: Use basic string comparison
                _logger.LogWarning("Failed to parse OLLAMA evaluation response, using fallback logic");
                return CreateFallbackEvaluation(studentAnswer, correctAnswer);
            }


            _logger.LogInformation("Evaluated answer for {Subject}: Score={Score}, Correct={IsCorrect}",
                subject, evaluation.Score, evaluation.IsCorrect);

            return new Result<AnswerEvaluation>.Success(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating answer via OLLAMA for {Subject}", subject);            // Fallback to simple evaluation
            return CreateFallbackEvaluation(studentAnswer, correctAnswer);
        }
    }

    public async Task<Result<string>> GenerateFeedbackAsync(
        string question,
        string studentAnswer,
        string correctAnswer,
        bool isCorrect,
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"You are an encouraging and insightful {subject} teacher providing personalized feedback to a student.

Question: {question}
Student Answer: {studentAnswer}
Correct Answer: {correctAnswer}
Is Correct: {isCorrect}
Subject: {subject}

Generate supportive, actionable feedback that:
1. Acknowledges what they got right or wrong
2. Explains why the correct answer is correct
3. If incorrect, gently corrects misconceptions
4. Provides encouragement and motivation
5. Keeps a positive, growth-mindset tone

Limit feedback to 2-3 paragraphs. Be specific and practical.

Only return the feedback text, no JSON or additional formatting.";

            var feedback = await CallOllamaAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(feedback))
            {
                return new Result<string>.Failure(
                    new Error("OllamaService.GenerateFeedback", "OLLAMA returned empty feedback"));
            }

            _logger.LogInformation("Generated feedback for {Subject}, IsCorrect={IsCorrect}",
                subject, isCorrect);

            return new Result<string>.Success(feedback.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating feedback via OLLAMA for {Subject}", subject);
            return new Result<string>.Failure(
                new Error("OllamaService.GenerateFeedback", $"OLLAMA feedback generation failed: {ex.Message}"));
        }
    }

    public async Task<Result<StudyRecommendation>> GenerateStudyRecommendationsAsync(
        Guid studentId,
        Subject subject,
        List<StudentResponseSummary> recentResponses,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Analyze the responses
            var totalResponses = recentResponses.Count;
            var correctCount = recentResponses.Count(r => r.IsCorrect);
            var topics = recentResponses.Select(r => r.Topic).Distinct().ToList();
            var difficulties = recentResponses.GroupBy(r => r.Difficulty)
                .Select(g => $"{g.Key}: {g.Count(r => r.IsCorrect)}/{g.Count()}")
                .ToList();

            var prompt = $@"You are an expert educational advisor creating personalized study recommendations.

Student Performance Summary:
- Student ID: {studentId}
- Subject: {subject}
- Total Questions: {totalResponses}
- Correct Answers: {correctCount}
- Accuracy Rate: {(correctCount * 100.0 / totalResponses):F1}%
- Topics Covered: {string.Join(", ", topics)}
- Performance by Difficulty: {string.Join(", ", difficulties)}

Generate a comprehensive study recommendation as JSON with this structure:
{{
  ""strengthAreas"": [""area1"", ""area2""],
  ""improvementAreas"": [""area1"", ""area2""],
  ""recommendedTopics"": [""topic1"", ""topic2""],
  ""studyStrategies"": [""strategy1"", ""strategy2""],
  ""summaryMessage"": ""Overall assessment and encouragement""
}}

Focus on actionable, specific recommendations based on their performance data.

Only return the JSON object, no additional text.";

            var response = await CallOllamaAsync(prompt, cancellationToken);

            // Parse the recommendation response
            var recommendation = ParseRecommendationFromResponse(response);

            if (recommendation == null)
            {
                return new Result<StudyRecommendation>.Failure(
                    new Error("OllamaService.GenerateRecommendations", "Failed to parse OLLAMA recommendations"));
            }

            _logger.LogInformation("Generated study recommendations for Student {StudentId} in {Subject}",
                studentId, subject); return new Result<StudyRecommendation>.Success(recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations via OLLAMA for {Subject}", subject);
            return new Result<StudyRecommendation>.Failure(
                new Error("OllamaService.GenerateRecommendations", $"OLLAMA recommendation generation failed: {ex.Message}"));
        }
    }

    // ============================================================
    // PRIVATE HELPER METHODS
    // ============================================================

    private async Task<string> CallOllamaAsync(string prompt, CancellationToken cancellationToken)
    {
        var request = new OllamaGenerateRequest
        {
            Model = _modelName,
            Prompt = prompt,
            Stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);

        return result?.Response ?? string.Empty;
    }

    private List<GeneratedQuestion> ParseQuestionsFromResponse(string response, string topic, DifficultyLevel difficulty)
    {
        try
        {
            // Extract JSON array from response (in case OLLAMA adds extra text)
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var questionDtos = JsonSerializer.Deserialize<List<QuestionDto>>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (questionDtos != null)
                {
                    return questionDtos.Select(dto => new GeneratedQuestion
                    {
                        QuestionText = dto.QuestionText,
                        QuestionType = QuestionType.MultipleChoice,
                        CorrectAnswer = dto.CorrectAnswer,
                        DistractorOptions = dto.Distractors,
                        Explanation = dto.Explanation,
                        Topics = new List<string> { topic },
                        EstimatedDifficulty = difficulty,
                        EstimatedTimeMinutes = difficulty switch
                        {
                            DifficultyLevel.Easy => 2,
                            DifficultyLevel.Medium => 3,
                            DifficultyLevel.Hard => 5,
                            _ => 3
                        }
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse questions from OLLAMA response: {Response}", response);
        }

        return new List<GeneratedQuestion>();
    }
    private AnswerEvaluation? ParseEvaluationFromResponse(string response)
    {
        try
        {
            // Extract JSON object from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var evalDto = JsonSerializer.Deserialize<EvaluationDto>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (evalDto != null)
                {
                    return new AnswerEvaluation
                    {
                        IsCorrect = evalDto.IsCorrect,
                        Score = evalDto.Score,
                        Feedback = evalDto.Reasoning, // Using reasoning as feedback
                        Reasoning = evalDto.Reasoning,
                        PartialCreditAreas = evalDto.PartialCreditAreas,
                        MisconceptionIdentified = evalDto.MisconceptionsIdentified
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse evaluation from OLLAMA response: {Response}", response);
        }

        return null;
    }

    private StudyRecommendation? ParseRecommendationFromResponse(string response)
    {
        try
        {
            // Extract JSON object from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var recDto = JsonSerializer.Deserialize<RecommendationDto>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (recDto != null)
                {
                    return new StudyRecommendation
                    {
                        StrengthAreas = recDto.StrengthAreas ?? new List<string>(),
                        ImprovementAreas = recDto.ImprovementAreas ?? new List<string>(),
                        RecommendedTopics = recDto.RecommendedTopics ?? new List<string>(),
                        StudyStrategies = recDto.StudyStrategies ?? new List<string>(),
                        SummaryMessage = recDto.SummaryMessage ?? "Keep up the good work!"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse recommendation from OLLAMA response: {Response}", response);
        }

        return null;
    }

    private Result<AnswerEvaluation> CreateFallbackEvaluation(string studentAnswer, string correctAnswer)
    {
        var isExactMatch = string.Equals(
            studentAnswer.Trim(),
            correctAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase);

        var evaluation = new AnswerEvaluation
        {
            IsCorrect = isExactMatch,
            Score = isExactMatch ? 1.0 : 0.0,
            Feedback = isExactMatch
                ? "Correct! Your answer matches the expected answer."
                : "Incorrect. The correct answer is: " + correctAnswer,
            Reasoning = isExactMatch
                ? "Exact match with correct answer"
                : "Does not match correct answer (OLLAMA evaluation unavailable)",
            PartialCreditAreas = null,
            MisconceptionIdentified = null
        };

        return new Result<AnswerEvaluation>.Success(evaluation);
    }

    // ============================================================
    // DTOs FOR OLLAMA API
    // ============================================================

    private record OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; init; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
    }

    private record OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; init; } = string.Empty;
    }

    private record QuestionDto
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; init; } = string.Empty;

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; init; } = string.Empty;

        [JsonPropertyName("distractors")]
        public List<string> Distractors { get; init; } = new();

        [JsonPropertyName("explanation")]
        public string Explanation { get; init; } = string.Empty;
    }

    private record EvaluationDto
    {
        [JsonPropertyName("score")]
        public double Score { get; init; }

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; init; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; init; }

        [JsonPropertyName("hasPartialCredit")]
        public bool HasPartialCredit { get; init; }

        [JsonPropertyName("misconceptionsIdentified")]
        public List<string>? MisconceptionsIdentified { get; init; }

        [JsonPropertyName("partialCreditAreas")]
        public List<string>? PartialCreditAreas { get; init; }
    }

    private record RecommendationDto
    {
        [JsonPropertyName("strengthAreas")]
        public List<string>? StrengthAreas { get; init; }

        [JsonPropertyName("improvementAreas")]
        public List<string>? ImprovementAreas { get; init; }

        [JsonPropertyName("recommendedTopics")]
        public List<string>? RecommendedTopics { get; init; }

        [JsonPropertyName("studyStrategies")]
        public List<string>? StudyStrategies { get; init; }

        [JsonPropertyName("summaryMessage")]
        public string? SummaryMessage { get; init; }
    }
}
