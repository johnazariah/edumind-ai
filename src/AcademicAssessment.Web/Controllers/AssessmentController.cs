using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models.Dtos;
using Asp.Versioning;
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

    private static readonly IReadOnlyDictionary<Guid, AssessmentSessionDto> Sessions =
        Assessments.ToDictionary(summary => summary.Id, BuildSessionForAssessment);

    [HttpGet]
    public IActionResult GetAssessments() => Ok(Assessments);

    [HttpGet("{id}")]
    public IActionResult GetAssessment(Guid id)
    {
        var assessment = Assessments.FirstOrDefault(a => a.Id == id);
        return assessment is null ? NotFound() : Ok(assessment);
    }

    [HttpGet("{id}/session")]
    public IActionResult GetAssessmentSession(Guid id)
    {
        if (!Sessions.TryGetValue(id, out var session))
        {
            return NotFound();
        }

        return Ok(session);
    }

    private static AssessmentSessionDto BuildSessionForAssessment(AssessmentSummary summary)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(summary.EstimatedDurationMinutes == 0 ? 45 : summary.EstimatedDurationMinutes);

        return new AssessmentSessionDto
        {
            AssessmentId = summary.Id,
            AssessmentTitle = summary.Title,
            EstimatedDurationMinutes = summary.EstimatedDurationMinutes == 0 ? 45 : summary.EstimatedDurationMinutes,
            StartedAt = now,
            ExpiresAt = expiresAt,
            Questions = BuildSampleQuestions(summary)
        };
    }

    private static IReadOnlyList<AssessmentQuestionDto> BuildSampleQuestions(AssessmentSummary summary)
    {
        return summary.Id switch
        {
            var id when id == Guid.Parse("6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38") => BuildAlgebraQuestions(),
            var id when id == Guid.Parse("b3f7c93a-0b18-4865-8bd9-dbbd4fd438ea") => BuildOrganicChemistryQuestions(),
            var id when id == Guid.Parse("2c20f965-d1a7-456c-9e36-820a0a64f9da") => BuildMechanicsQuestions(),
            var id when id == Guid.Parse("0c7f9505-8644-41da-8535-9d7e98f3aa4f") => BuildShakespeareQuestions(),
            _ => BuildCellBiologyQuestions()
        };
    }

    private static IReadOnlyList<AssessmentQuestionDto> BuildAlgebraQuestions() =>
    [
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("32d32f0f-8a7a-4d24-bdaa-efa201e2ee96"),
            Prompt = "Solve the equation $$2x + 5 = 17$$.",
            QuestionType = QuestionType.ShortAnswer,
            AllowMultipleSelection = false,
            MathExpression = "2x + 5 = 17",
            Points = 2,
            EstimatedTimeSeconds = 60,
            Hints = [
                "Subtract 5 from both sides first.",
                "You will be left with a single step equation."
            ]
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("3a9abc1f-f6ce-45e5-a0dc-2411aebaec72"),
            Prompt = "Which graphs represent linear functions? Select all that apply.",
            QuestionType = QuestionType.MultipleSelect,
            AllowMultipleSelection = true,
            Points = 3,
            Options =
            [
                new QuestionOptionDto { Key = "A", Label = "Line passing through (0,2) and (3,8)" },
                new QuestionOptionDto { Key = "B", Label = "Parabola opening upwards" },
                new QuestionOptionDto { Key = "C", Label = "Horizontal line $$y = -4$$" },
                new QuestionOptionDto { Key = "D", Label = "Circle centered at the origin" }
            ]
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("2ef4ae68-ca2f-4e8c-b51c-4141b51315d8"),
            Prompt = "Consider the system <br/>$$\\begin{cases}3x - 2y = 4\\\\x + y = 5\\end{cases}$$<br/>What is the value of $$x$$?",
            QuestionType = QuestionType.MultipleChoice,
            AllowMultipleSelection = false,
            Points = 4,
            Options =
            [
                new QuestionOptionDto { Key = "A", Label = "1" },
                new QuestionOptionDto { Key = "B", Label = "2" },
                new QuestionOptionDto { Key = "C", Label = "3" },
                new QuestionOptionDto { Key = "D", Label = "4" }
            ],
            Hints = ["Try solving the system using substitution or elimination."]
        }
    ];

    private static IReadOnlyList<AssessmentQuestionDto> BuildOrganicChemistryQuestions() =>
    [
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("44fc1c6c-4d4a-494f-85f0-702c081c1395"),
            Prompt = "Select the correct statements about SN1 reactions.",
            QuestionType = QuestionType.MultipleSelect,
            AllowMultipleSelection = true,
            Points = 3,
            Options =
            [
                new QuestionOptionDto { Key = "A", Label = "They proceed through a carbocation intermediate." },
                new QuestionOptionDto { Key = "B", Label = "The rate depends only on substrate concentration." },
                new QuestionOptionDto { Key = "C", Label = "They invert stereochemistry at the reaction center." },
                new QuestionOptionDto { Key = "D", Label = "Polar protic solvents slow the reaction dramatically." }
            ]
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("9e3f31bf-7996-44f0-bfb6-efadbc767c67"),
            Prompt = "Draw the major product of the following reaction: $$\\text{2-bromobutane} + \\text{NaOH (ethanol)}$$",
            QuestionType = QuestionType.Essay,
            AllowMultipleSelection = false,
            Points = 5,
            EstimatedTimeSeconds = 240,
            Hints = ["Consider whether the reaction follows an SN1 or SN2 pathway."]
        }
    ];

    private static IReadOnlyList<AssessmentQuestionDto> BuildMechanicsQuestions() =>
    [
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("98c2fd19-4788-4dbf-8056-5d3f8cf59c97"),
            Prompt = "A block slides down a frictionless incline of angle $$30^\\circ$$. What is its acceleration?",
            QuestionType = QuestionType.MultipleChoice,
            Options =
            [
                new QuestionOptionDto { Key = "A", Label = "$$g$$" },
                new QuestionOptionDto { Key = "B", Label = "$$\\frac{g}{2}$$" },
                new QuestionOptionDto { Key = "C", Label = "$$\\frac{g}{\\sqrt{2}}$$" },
                new QuestionOptionDto { Key = "D", Label = "Zero" }
            ],
            Points = 3
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("bd1c491b-420b-489f-9df1-5c2471b4b7e6"),
            Prompt = "Given the following C# simulation, why does the block slow down unexpectedly?",
            QuestionType = QuestionType.ShortAnswer,
            CodeSnippet = """
var velocity = initialVelocity;
for (var step = 0; step < steps; step++)
{
    velocity += acceleration * timeStep;
    velocity -= dragCoefficient * velocity * timeStep;
}
""",
            CodeLanguage = "csharp",
            Points = 4,
            Hints = ["Consider the effect of the drag term on each iteration."]
        }
    ];

    private static IReadOnlyList<AssessmentQuestionDto> BuildShakespeareQuestions() =>
    [
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("c056e3e8-7aee-4f97-b75f-0a1ae2a6de44"),
            Prompt = "In <em>Hamlet</em>, which themes emerge from the \"To be, or not to be\" soliloquy?",
            QuestionType = QuestionType.Essay,
            Points = 5,
            EstimatedTimeSeconds = 300,
            Hints = [
                "Focus on Hamlet's contemplation of existence.",
                "Tie your response to the broader political tension in the court."
            ]
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("5d0b7d27-52c6-41a9-9cca-b3e9f733884e"),
            Prompt = "Identify the literary devices used in this excerpt.",
            QuestionType = QuestionType.ShortAnswer,
            ImageUrl = "https://placehold.co/600x200?text=Sonnet+Excerpt",
            ImageAltText = "Excerpt from Shakespearean sonnet displayed as an image.",
            Points = 2
        }
    ];

    private static IReadOnlyList<AssessmentQuestionDto> BuildCellBiologyQuestions() =>
    [
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("b2cf9b64-b677-4d7b-b1d5-0a12c08ff031"),
            Prompt = "Which organelle is responsible for ATP production?",
            QuestionType = QuestionType.MultipleChoice,
            Options =
            [
                new QuestionOptionDto { Key = "A", Label = "Ribosome" },
                new QuestionOptionDto { Key = "B", Label = "Golgi apparatus" },
                new QuestionOptionDto { Key = "C", Label = "Mitochondrion" },
                new QuestionOptionDto { Key = "D", Label = "Endoplasmic reticulum" }
            ],
            Points = 1
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("1b053ddb-1ab5-4f33-9d88-679a6c2b0f46"),
            Prompt = "True or False: DNA replication occurs during the G2 phase of the cell cycle.",
            QuestionType = QuestionType.TrueFalse,
            Options =
            [
                new QuestionOptionDto { Key = "true", Label = "True" },
                new QuestionOptionDto { Key = "false", Label = "False" }
            ],
            Points = 1
        },
        new AssessmentQuestionDto
        {
            Id = Guid.Parse("9f285d7b-5c0c-4fbb-8e08-4d4f47fbb3fa"),
            Prompt = "Fill in the blank: The lipid bilayer is primarily composed of _____.",
            QuestionType = QuestionType.FillInBlank,
            Points = 1
        }
    ];
}
