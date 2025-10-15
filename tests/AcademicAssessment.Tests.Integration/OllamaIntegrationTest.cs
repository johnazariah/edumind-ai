using System;
using System.Threading.Tasks;
using AcademicAssessment.Agents.Mathematics;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AcademicAssessment.Tests.Integration;

/// <summary>
/// Manual integration test for OLLAMA with MathematicsAssessmentAgent
/// Run this to verify OLLAMA is working correctly with semantic evaluation
/// </summary>
public class OllamaIntegrationTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("OLLAMA Integration Test with MathematicsAssessmentAgent");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Setup
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Ollama:BaseUrl"] = "http://localhost:11434",
                ["Ollama:ModelName"] = "llama3.2:3b",
                ["Ollama:Timeout"] = "120"
            })
            .Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var ollamaLogger = loggerFactory.CreateLogger<OllamaService>();
        var ollamaService = new OllamaService(configuration, ollamaLogger);

        var agentLogger = loggerFactory.CreateLogger<MathematicsAssessmentAgent>();
        var mathAgent = new MathematicsAssessmentAgent(ollamaService, agentLogger);

        Console.WriteLine("✅ OLLAMA service and Mathematics agent initialized");
        Console.WriteLine();

        // Test 1: Exact Match
        Console.WriteLine("Test 1: Exact Match");
        Console.WriteLine("-".PadRight(80, '-'));
        await TestEvaluation(
            mathAgent,
            question: "What is the Pythagorean theorem formula?",
            studentAnswer: "a² + b² = c²",
            correctAnswer: "a² + b² = c²",
            expectedScore: 1.0
        );

        // Test 2: Semantic Equivalent
        Console.WriteLine("\nTest 2: Semantic Equivalent (Different Formatting)");
        Console.WriteLine("-".PadRight(80, '-'));
        await TestEvaluation(
            mathAgent,
            question: "What is the Pythagorean theorem formula?",
            studentAnswer: "a squared plus b squared equals c squared",
            correctAnswer: "a² + b² = c²",
            expectedScore: 0.8 // Should recognize semantic equivalence
        );

        // Test 3: Partially Correct
        Console.WriteLine("\nTest 3: Partially Correct");
        Console.WriteLine("-".PadRight(80, '-'));
        await TestEvaluation(
            mathAgent,
            question: "What is the Pythagorean theorem formula?",
            studentAnswer: "a² + b² = c (forgot to square c)",
            correctAnswer: "a² + b² = c²",
            expectedScore: 0.5 // Should give partial credit
        );

        // Test 4: Incorrect
        Console.WriteLine("\nTest 4: Incorrect Answer");
        Console.WriteLine("-".PadRight(80, '-'));
        await TestEvaluation(
            mathAgent,
            question: "What is the Pythagorean theorem formula?",
            studentAnswer: "a + b = c",
            correctAnswer: "a² + b² = c²",
            expectedScore: 0.0
        );

        // Test 5: Complex Mathematical Answer
        Console.WriteLine("\nTest 5: Complex Mathematical Answer");
        Console.WriteLine("-".PadRight(80, '-'));
        await TestEvaluation(
            mathAgent,
            question: "What is the derivative of x²?",
            studentAnswer: "2x",
            correctAnswer: "2x",
            expectedScore: 1.0
        );

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Integration test complete!");
        Console.WriteLine("=".PadRight(80, '='));
    }

    private static async Task TestEvaluation(
        MathematicsAssessmentAgent agent,
        string question,
        string studentAnswer,
        string correctAnswer,
        double expectedScore)
    {
        Console.WriteLine($"Question: {question}");
        Console.WriteLine($"Correct Answer: {correctAnswer}");
        Console.WriteLine($"Student Answer: {studentAnswer}");
        Console.WriteLine($"Expected Score: {expectedScore:P0}");
        Console.WriteLine();

        try
        {
            var agentTask = new AcademicAssessment.Agents.Shared.Models.AgentTask(
                TaskId: Guid.NewGuid(),
                TaskType: "EVALUATE_RESPONSE",
                Payload: new Dictionary<string, object>
                {
                    ["question"] = question,
                    ["studentAnswer"] = studentAnswer,
                    ["correctAnswer"] = correctAnswer,
                    ["subject"] = Subject.Mathematics,
                    ["skill"] = "algebra"
                },
                RequesterAgentId: "test-agent",
                Priority: 1,
                CreatedAt: DateTime.UtcNow
            );

            Console.WriteLine("⏳ Evaluating with OLLAMA (this may take 15-30 seconds)...");
            var startTime = DateTime.UtcNow;

            var result = await agent.EvaluateResponseAsync(agentTask, CancellationToken.None);

            var duration = DateTime.UtcNow - startTime;

            if (result.IsSuccess && result.Value != null)
            {
                var evaluation = result.Value;
                var score = evaluation.ContainsKey("score") ? Convert.ToDouble(evaluation["score"]) : 0.0;
                var isCorrect = evaluation.ContainsKey("isCorrect") && Convert.ToBoolean(evaluation["isCorrect"]);
                var reasoning = evaluation.ContainsKey("reasoning") ? evaluation["reasoning"]?.ToString() : "No reasoning provided";

                Console.WriteLine();
                Console.WriteLine($"✅ Evaluation completed in {duration.TotalSeconds:F1} seconds");
                Console.WriteLine($"Score: {score:P0} (Expected: {expectedScore:P0})");
                Console.WriteLine($"Is Correct: {isCorrect}");
                Console.WriteLine($"Reasoning: {reasoning}");

                // Check if score is within reasonable range
                var scoreDiff = Math.Abs(score - expectedScore);
                if (scoreDiff <= 0.3) // Allow 30% tolerance
                {
                    Console.WriteLine("✅ Score is within expected range");
                }
                else
                {
                    Console.WriteLine($"⚠️  Score differs significantly from expected (diff: {scoreDiff:P0})");
                }
            }
            else
            {
                Console.WriteLine($"❌ Evaluation failed: {result.Error?.Message ?? "Unknown error"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception during evaluation: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
