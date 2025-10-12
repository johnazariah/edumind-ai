using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using AcademicAssessment.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AcademicAssessment.Tests.Unit.Repositories;

public class QuestionRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IQuestionRepository _repository;
    private readonly Guid _courseId = Guid.NewGuid();

    public QuestionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"QuestionRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext();
        _context = new AcademicContext(options, tenantContext);
        _repository = new QuestionRepository(_context);
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public UserRole Role { get; } = UserRole.BusinessAdmin;
        public Guid? SchoolId { get; } = null;
        public IReadOnlyList<Guid> ClassIds { get; } = new List<Guid>();
        public string Email { get; } = "test@example.com";
        public string FullName { get; } = "Test User";

        public bool HasAccessToSchool(Guid schoolId) => true;
        public bool HasAccessToClass(Guid classId) => true;
        public bool HasRole(UserRole minimumRole) => true;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private Question CreateTestQuestion(
        Guid? courseId = null,
        Subject subject = Subject.Mathematics,
        GradeLevel gradeLevel = GradeLevel.Grade10,
        DifficultyLevel difficultyLevel = DifficultyLevel.Medium,
        QuestionType questionType = QuestionType.MultipleChoice,
        List<string>? topics = null,
        List<string>? learningObjectives = null,
        bool isAiGenerated = false,
        double? irtDifficulty = null,
        double? successRate = null,
        string? contentHash = null)
    {
        var timesAnswered = successRate.HasValue ? 10 : 0;
        var timesCorrect = successRate.HasValue ? (int)(timesAnswered * successRate.Value) : 0;

        return new Question
        {
            Id = Guid.NewGuid(),
            CourseId = courseId ?? _courseId,
            QuestionText = $"Question {Guid.NewGuid()}",
            QuestionType = questionType,
            Subject = subject,
            GradeLevel = gradeLevel,
            DifficultyLevel = difficultyLevel,
            Topics = topics ?? new List<string>(),
            LearningObjectives = learningObjectives ?? new List<string>(),
            AnswerOptions = "[\"A\", \"B\", \"C\", \"D\"]",
            CorrectAnswer = "A",
            Points = 1,
            IsActive = true,
            IsAiGenerated = isAiGenerated,
            IrtDifficulty = irtDifficulty,
            ContentHash = contentHash,
            TimesAnswered = timesAnswered,
            TimesCorrect = timesCorrect,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SeedQuestionAsync(Question question)
    {
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAddQuestion()
    {
        var question = CreateTestQuestion();

        var result = await _repository.AddAsync(question);

        result.Should().BeOfType<Result<Question>.Success>();
        var retrieved = await _repository.GetByIdAsync(question.Id);
        retrieved.Should().BeOfType<Result<Question>.Success>();
        var actualQuestion = ((Result<Question>.Success)retrieved).Value;
        actualQuestion.QuestionText.Should().Be(question.QuestionText);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnQuestion_WhenExists()
    {
        var question = CreateTestQuestion();
        await SeedQuestionAsync(question);

        var result = await _repository.GetByIdAsync(question.Id);

        result.Should().BeOfType<Result<Question>.Success>();
        var actualQuestion = ((Result<Question>.Success)result).Value;
        actualQuestion.QuestionText.Should().Be(question.QuestionText);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<Result<Question>.Failure>();
    }

    [Fact]
    public async Task GetByCourseIdAsync_ShouldReturnCourseQuestions()
    {
        var course1Questions = new[] {
            CreateTestQuestion(courseId: _courseId),
            CreateTestQuestion(courseId: _courseId)
        };
        var course2Question = CreateTestQuestion(courseId: Guid.NewGuid());

        foreach (var q in course1Questions) await SeedQuestionAsync(q);
        await SeedQuestionAsync(course2Question);

        var result = await _repository.GetByCourseIdAsync(_courseId);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Select(q => q.Id).Should().BeEquivalentTo(course1Questions.Select(q => q.Id));
    }

    [Fact]
    public async Task GetByDifficultyLevelAsync_ShouldReturnMatchingQuestions()
    {
        var easyQuestions = new[] {
            CreateTestQuestion(difficultyLevel: DifficultyLevel.Easy),
            CreateTestQuestion(difficultyLevel: DifficultyLevel.Easy)
        };
        var hardQuestion = CreateTestQuestion(difficultyLevel: DifficultyLevel.Hard);

        foreach (var q in easyQuestions) await SeedQuestionAsync(q);
        await SeedQuestionAsync(hardQuestion);

        var result = await _repository.GetByDifficultyLevelAsync(DifficultyLevel.Easy);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q => q.DifficultyLevel.Should().Be(DifficultyLevel.Easy));
    }

    [Fact]
    public async Task GetBySubjectAndGradeLevelAsync_ShouldReturnMatchingQuestions()
    {
        var matchingQuestions = new[] {
            CreateTestQuestion(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade10),
            CreateTestQuestion(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade10)
        };
        var nonMatchingQuestions = new[] {
            CreateTestQuestion(subject: Subject.Physics, gradeLevel: GradeLevel.Grade10),
            CreateTestQuestion(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade11)
        };

        foreach (var q in matchingQuestions) await SeedQuestionAsync(q);
        foreach (var q in nonMatchingQuestions) await SeedQuestionAsync(q);

        var result = await _repository.GetBySubjectAndGradeLevelAsync(Subject.Mathematics, GradeLevel.Grade10);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q =>
        {
            q.Subject.Should().Be(Subject.Mathematics);
            q.GradeLevel.Should().Be(GradeLevel.Grade10);
        });
    }

    [Fact]
    public async Task GetByQuestionTypeAsync_ShouldReturnMatchingQuestions()
    {
        var mcQuestions = new[] {
            CreateTestQuestion(questionType: QuestionType.MultipleChoice),
            CreateTestQuestion(questionType: QuestionType.MultipleChoice)
        };
        var essayQuestion = CreateTestQuestion(questionType: QuestionType.Essay);

        foreach (var q in mcQuestions) await SeedQuestionAsync(q);
        await SeedQuestionAsync(essayQuestion);

        var result = await _repository.GetByQuestionTypeAsync(QuestionType.MultipleChoice);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q => q.QuestionType.Should().Be(QuestionType.MultipleChoice));
    }

    [Fact(Skip = "EF Core InMemory provider doesn't support querying JSON-serialized collections")]
    public async Task GetByTopicsAsync_ShouldReturnQuestionsWithMatchingTopics()
    {
        var algebra1 = CreateTestQuestion(topics: new List<string> { "Algebra", "Equations" });
        var algebra2 = CreateTestQuestion(topics: new List<string> { "Algebra", "Functions" });
        var geometry = CreateTestQuestion(topics: new List<string> { "Geometry", "Triangles" });

        await SeedQuestionAsync(algebra1);
        await SeedQuestionAsync(algebra2);
        await SeedQuestionAsync(geometry);

        var result = await _repository.GetByTopicsAsync(new List<string> { "Algebra" });

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q => q.Topics.Should().Contain("Algebra"));
    }

    [Fact(Skip = "EF Core InMemory provider doesn't support querying JSON-serialized collections")]
    public async Task GetByLearningObjectivesAsync_ShouldReturnMatchingQuestions()
    {
        var obj1 = CreateTestQuestion(learningObjectives: new List<string> { "Solve equations", "Graph functions" });
        var obj2 = CreateTestQuestion(learningObjectives: new List<string> { "Solve equations", "Factor polynomials" });
        var obj3 = CreateTestQuestion(learningObjectives: new List<string> { "Calculate areas" });

        await SeedQuestionAsync(obj1);
        await SeedQuestionAsync(obj2);
        await SeedQuestionAsync(obj3);

        var result = await _repository.GetByLearningObjectivesAsync(new List<string> { "Solve equations" });

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q => q.LearningObjectives.Should().Contain("Solve equations"));
    }

    [Fact]
    public async Task GetAiGeneratedQuestionsAsync_ShouldReturnOnlyAiGenerated()
    {
        var aiQuestions = new[] {
            CreateTestQuestion(isAiGenerated: true),
            CreateTestQuestion(isAiGenerated: true)
        };
        var humanQuestion = CreateTestQuestion(isAiGenerated: false);

        foreach (var q in aiQuestions) await SeedQuestionAsync(q);
        await SeedQuestionAsync(humanQuestion);

        var result = await _repository.GetAiGeneratedQuestionsAsync();

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var questions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        questions.Should().HaveCount(2);
        questions.Should().AllSatisfy(q => q.IsAiGenerated.Should().BeTrue());
    }

    [Fact]
    public async Task GetByIrtDifficultyRangeAsync_ShouldReturnQuestionsInRange()
    {
        var questions = new[] {
            CreateTestQuestion(irtDifficulty: 0.5),
            CreateTestQuestion(irtDifficulty: 1.0),
            CreateTestQuestion(irtDifficulty: 1.5),
            CreateTestQuestion(irtDifficulty: 2.0)
        };

        foreach (var q in questions) await SeedQuestionAsync(q);

        var result = await _repository.GetByIrtDifficultyRangeAsync(0.8, 1.7);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var matchingQuestions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        matchingQuestions.Should().HaveCount(2);
        matchingQuestions.Should().AllSatisfy(q =>
        {
            q.IrtDifficulty.Should().BeGreaterOrEqualTo(0.8);
            q.IrtDifficulty.Should().BeLessOrEqualTo(1.7);
        });
    }

    [Fact]
    public async Task GetBySuccessRateRangeAsync_ShouldReturnQuestionsInRange()
    {
        var questions = new[] {
            CreateTestQuestion(successRate: 0.3),
            CreateTestQuestion(successRate: 0.5),
            CreateTestQuestion(successRate: 0.7),
            CreateTestQuestion(successRate: 0.9)
        };

        foreach (var q in questions) await SeedQuestionAsync(q);

        var result = await _repository.GetBySuccessRateRangeAsync(0.4, 0.8);

        result.Should().BeOfType<Result<IReadOnlyList<Question>>.Success>();
        var matchingQuestions = ((Result<IReadOnlyList<Question>>.Success)result).Value;
        matchingQuestions.Should().HaveCount(2);
        matchingQuestions.Should().AllSatisfy(q =>
        {
            q.SuccessRate.Should().BeGreaterOrEqualTo(0.4);
            q.SuccessRate.Should().BeLessOrEqualTo(0.8);
        });
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnTrue_WhenHashExists()
    {
        var contentHash = "abc123def456";
        var existingQuestion = CreateTestQuestion(contentHash: contentHash);
        await SeedQuestionAsync(existingQuestion);

        var result = await _repository.IsDuplicateAsync(contentHash);

        result.Should().BeOfType<Result<bool>.Success>();
        var isDuplicate = ((Result<bool>.Success)result).Value;
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_WhenHashDoesNotExist()
    {
        var result = await _repository.IsDuplicateAsync("nonexistent hash");

        result.Should().BeOfType<Result<bool>.Success>();
        var isDuplicate = ((Result<bool>.Success)result).Value;
        isDuplicate.Should().BeFalse();
    }
}
