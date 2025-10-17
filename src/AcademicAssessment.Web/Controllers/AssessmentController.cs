using AcademicAssessment.Core.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AcademicAssessment.Web.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AssessmentController : ControllerBase
{
    private static readonly IReadOnlyList<AssessmentSummary> Assessments =
    [
        new AssessmentSummary
        {
            Id = Guid.Parse("6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38"),
            Title = "Introduction to Algebra",
            Subject = "Mathematics",
            Difficulty = "Beginner",
            EstimatedDurationMinutes = 45,
            QuestionCount = 15,
            ProgressPercentage = 0.4,
            IsInProgress = true,
            LastAttemptedAt = DateTimeOffset.UtcNow.AddDays(-2),
            Description = "Build a foundation in linear equations, inequalities, and functions before advancing to more complex topics.",
            LearningObjectives = new[]
            {
                "Solve single-variable linear equations",
                "Interpret slope and intercept in functional relationships",
                "Model word problems using algebraic expressions"
            }
        },
        new AssessmentSummary
        {
            Id = Guid.Parse("b3f7c93a-0b18-4865-8bd9-dbbd4fd438ea"),
            Title = "Organic Chemistry Fundamentals",
            Subject = "Chemistry",
            Difficulty = "Intermediate",
            EstimatedDurationMinutes = 60,
            QuestionCount = 20,
            ProgressPercentage = null,
            IsInProgress = false,
            LastAttemptedAt = null,
            Description = "Assess your mastery of functional groups, stereochemistry, and foundational reaction mechanisms.",
            LearningObjectives = new[]
            {
                "Recognize and classify common functional groups",
                "Predict stereochemical outcomes of reactions",
                "Differentiate between substitution and elimination pathways"
            }
        },
        new AssessmentSummary
        {
            Id = Guid.Parse("2c20f965-d1a7-456c-9e36-820a0a64f9da"),
            Title = "Classical Mechanics",
            Subject = "Physics",
            Difficulty = "Advanced",
            EstimatedDurationMinutes = 75,
            QuestionCount = 25,
            ProgressPercentage = 0.72,
            IsInProgress = true,
            LastAttemptedAt = DateTimeOffset.UtcNow.AddHours(-5),
            Description = "Demonstrate proficiency with Newtonian mechanics, work-energy theorems, and rotational dynamics.",
            LearningObjectives = new[]
            {
                "Apply Newton's laws to multi-body systems",
                "Analyze energy conservation in closed systems",
                "Compute torque and angular momentum in rigid bodies"
            }
        },
        new AssessmentSummary
        {
            Id = Guid.Parse("0c7f9505-8644-41da-8535-9d7e98f3aa4f"),
            Title = "Shakespearean Literature",
            Subject = "English",
            Difficulty = "Intermediate",
            EstimatedDurationMinutes = 50,
            QuestionCount = 18,
            ProgressPercentage = null,
            IsInProgress = false,
            LastAttemptedAt = DateTimeOffset.UtcNow.AddMonths(-1),
            Description = "Explore major themes, character arcs, and historical context across Shakespeare's tragedies and comedies.",
            LearningObjectives = new[]
            {
                "Analyze soliloquies for character motivation",
                "Compare thematic elements across plays",
                "Evaluate historical influences on narrative structure"
            }
        },
        new AssessmentSummary
        {
            Id = Guid.Parse("a3d3fb97-19d6-4959-a075-4ca0b8b57d81"),
            Title = "Cellular Biology",
            Subject = "Biology",
            Difficulty = "Beginner",
            EstimatedDurationMinutes = 40,
            QuestionCount = 12,
            ProgressPercentage = 0.15,
            IsInProgress = true,
            LastAttemptedAt = DateTimeOffset.UtcNow.AddDays(-7),
            Description = "Review the structure and function of cellular organelles, signaling pathways, and cell cycle regulation.",
            LearningObjectives = new[]
            {
                "Identify organelles and their roles",
                "Explain cell membrane transport mechanisms",
                "Outline the stages of the cell cycle"
            }
        }
    ];

    [HttpGet]
    public IActionResult GetAssessments() => Ok(Assessments);

    [HttpGet("{id}")]
    public IActionResult GetAssessment(Guid id)
    {
        var assessment = Assessments.FirstOrDefault(a => a.Id == id);
        return assessment is null ? NotFound() : Ok(assessment);
    }
}
