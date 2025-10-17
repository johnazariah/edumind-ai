using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Models;

public class QuestionTests
{
    private static Question CreateTestQuestion(bool withIrtParameters = false) => new()
    {
        Id = Guid.NewGuid(),
        CourseId = Guid.NewGuid(),
        QuestionText = "What is 2 + 2?",
        QuestionType = QuestionType.MultipleChoice,
        Subject = Subject.Mathematics,
        GradeLevel = GradeLevel.Grade6,
        DifficultyLevel = DifficultyLevel.Easy,
        Topics = ["Addition", "Arithmetic"],
        LearningObjectives = ["Perform basic addition"],
        AnswerOptions = "[\"2\", \"3\", \"4\", \"5\"]",
        CorrectAnswer = "4",
        Explanation = "2 + 2 equals 4",
        Points = 1,
        IrtDiscrimination = withIrtParameters ? 1.5 : null,
        IrtDifficulty = withIrtParameters ? -0.5 : null,
        IrtGuessing = withIrtParameters ? 0.25 : null,
        IsActive = true,
        TimesAnswered = 0,
        TimesCorrect = 0,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        IsAiGenerated = false,
        ContentHash = "abc123"
    };

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllRequiredProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var question = new Question
        {
            Id = id,
            CourseId = courseId,
            QuestionText = "Test question?",
            QuestionType = QuestionType.ShortAnswer,
            Subject = Subject.Physics,
            GradeLevel = GradeLevel.Grade10,
            DifficultyLevel = DifficultyLevel.Medium,
            CorrectAnswer = "Test answer",
            Points = 5,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        question.Id.Should().Be(id);
        question.CourseId.Should().Be(courseId);
        question.QuestionText.Should().Be("Test question?");
        question.QuestionType.Should().Be(QuestionType.ShortAnswer);
        question.Subject.Should().Be(Subject.Physics);
        question.GradeLevel.Should().Be(GradeLevel.Grade10);
        question.DifficultyLevel.Should().Be(DifficultyLevel.Medium);
        question.CorrectAnswer.Should().Be("Test answer");
        question.Points.Should().Be(5);
        question.IsActive.Should().BeTrue();
        question.CreatedAt.Should().Be(now);
        question.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Constructor_DefaultsCountersToZero()
    {
        // Arrange & Act
        var question = CreateTestQuestion();

        // Assert
        question.TimesAnswered.Should().Be(0);
        question.TimesCorrect.Should().Be(0);
        question.IsAiGenerated.Should().BeFalse();
    }

    [Fact]
    public void Constructor_AllowsNullableProperties()
    {
        // Arrange & Act
        var question = new Question
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            QuestionText = "Test",
            QuestionType = QuestionType.TrueFalse,
            Subject = Subject.Biology,
            GradeLevel = GradeLevel.Grade7,
            DifficultyLevel = DifficultyLevel.Easy,
            CorrectAnswer = "True",
            Points = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AnswerOptions = null,
            Explanation = null,
            IrtDiscrimination = null,
            IrtDifficulty = null,
            IrtGuessing = null,
            ContentHash = null
        };

        // Assert
        question.AnswerOptions.Should().BeNull();
        question.Explanation.Should().BeNull();
        question.IrtDiscrimination.Should().BeNull();
        question.IrtDifficulty.Should().BeNull();
        question.IrtGuessing.Should().BeNull();
        question.ContentHash.Should().BeNull();
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        // Arrange & Act
        var question = CreateTestQuestion();

        // Assert
        question.Topics.Should().NotBeNull();
        question.LearningObjectives.Should().NotBeNull();
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void SuccessRate_ReturnsZero_WhenNeverAnswered()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act & Assert
        question.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void SuccessRate_CalculatesCorrectly_WhenPartiallyCorrect()
    {
        // Arrange
        var question = CreateTestQuestion() with
        {
            TimesAnswered = 10,
            TimesCorrect = 7
        };

        // Act & Assert
        question.SuccessRate.Should().BeApproximately(0.7, 0.001);
    }

    [Fact]
    public void SuccessRate_Returns100Percent_WhenAllCorrect()
    {
        // Arrange
        var question = CreateTestQuestion() with
        {
            TimesAnswered = 5,
            TimesCorrect = 5
        };

        // Act & Assert
        question.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void SuccessRate_ReturnsZeroPercent_WhenNoneCorrect()
    {
        // Arrange
        var question = CreateTestQuestion() with
        {
            TimesAnswered = 8,
            TimesCorrect = 0
        };

        // Act & Assert
        question.SuccessRate.Should().Be(0.0);
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdatesQuestionText()
    {
        // Arrange
        var original = CreateTestQuestion();
        var newText = "What is 3 + 3?";

        // Act
        var updated = original.With(questionText: newText);

        // Assert
        updated.QuestionText.Should().Be(newText);
        updated.UpdatedAt.Should().BeOnOrAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesMultipleProperties()
    {
        // Arrange
        var original = CreateTestQuestion();
        var newOptions = "[\"4\", \"5\", \"6\", \"7\"]";
        var newAnswer = "6";
        var newExplanation = "3 + 3 equals 6";

        // Act
        var updated = original.With(
            answerOptions: newOptions,
            correctAnswer: newAnswer,
            explanation: newExplanation);

        // Assert
        updated.AnswerOptions.Should().Be(newOptions);
        updated.CorrectAnswer.Should().Be(newAnswer);
        updated.Explanation.Should().Be(newExplanation);
    }

    [Fact]
    public void With_UpdatesDifficultyLevel()
    {
        // Arrange
        var original = CreateTestQuestion();

        // Act
        var updated = original.With(difficultyLevel: DifficultyLevel.Hard);

        // Assert
        updated.DifficultyLevel.Should().Be(DifficultyLevel.Hard);
    }

    [Fact]
    public void With_DeactivatesQuestion()
    {
        // Arrange
        var original = CreateTestQuestion();

        // Act
        var updated = original.With(isActive: false);

        // Assert
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task With_UpdatesOnlyUpdatedAtTimestamp_WhenNoParametersProvided()
    {
        // Arrange
        var original = CreateTestQuestion();
        var originalUpdatedAt = original.UpdatedAt;

        // Act - Add small delay to ensure timestamp changes
        await Task.Delay(10);
        var updated = original.With();

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        updated.QuestionText.Should().Be(original.QuestionText);
    }

    #endregion

    #region RecordAnswer Method Tests

    [Fact]
    public void RecordAnswer_IncrementsTimesAnswered_WhenCorrect()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act
        var updated = question.RecordAnswer(wasCorrect: true);

        // Assert
        updated.TimesAnswered.Should().Be(1);
        updated.TimesCorrect.Should().Be(1);
        updated.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void RecordAnswer_IncrementsTimesAnswered_WhenIncorrect()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act
        var updated = question.RecordAnswer(wasCorrect: false);

        // Assert
        updated.TimesAnswered.Should().Be(1);
        updated.TimesCorrect.Should().Be(0);
        updated.SuccessRate.Should().Be(0.0);
    }

    [Fact]
    public void RecordAnswer_UpdatesSuccessRate_AfterMultipleAnswers()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act
        var after1 = question.RecordAnswer(true);      // 1/1 = 100%
        var after2 = after1.RecordAnswer(true);        // 2/2 = 100%
        var after3 = after2.RecordAnswer(false);       // 2/3 = 66.7%
        var after4 = after3.RecordAnswer(true);        // 3/4 = 75%

        // Assert
        after4.TimesAnswered.Should().Be(4);
        after4.TimesCorrect.Should().Be(3);
        after4.SuccessRate.Should().BeApproximately(0.75, 0.001);
    }

    [Fact]
    public void RecordAnswer_UpdatesTimestamp()
    {
        // Arrange
        var question = CreateTestQuestion();
        var originalUpdatedAt = question.UpdatedAt;

        // Act
        var updated = question.RecordAnswer(true);

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region UpdateIrtParameters Method Tests

    [Fact]
    public void UpdateIrtParameters_SetsAllThreeParameters()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act
        var updated = question.UpdateIrtParameters(
            discrimination: 1.8,
            difficulty: 0.5,
            guessing: 0.2);

        // Assert
        updated.IrtDiscrimination.Should().Be(1.8);
        updated.IrtDifficulty.Should().Be(0.5);
        updated.IrtGuessing.Should().Be(0.2);
    }

    [Fact]
    public void UpdateIrtParameters_AllowsNegativeDifficulty()
    {
        // Arrange
        var question = CreateTestQuestion();

        // Act
        var updated = question.UpdateIrtParameters(
            discrimination: 1.5,
            difficulty: -1.2,  // Negative means easier
            guessing: 0.25);

        // Assert
        updated.IrtDifficulty.Should().Be(-1.2);
    }

    [Fact]
    public async Task UpdateIrtParameters_UpdatesTimestamp()
    {
        // Arrange
        var question = CreateTestQuestion();
        var originalUpdatedAt = question.UpdatedAt;

        // Act - Add small delay to ensure timestamp changes
        await Task.Delay(10);
        var updated = question.UpdateIrtParameters(1.5, 0.0, 0.25);

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void With_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestQuestion();
        var originalText = original.QuestionText;

        // Act
        var updated = original.With(questionText: "New question text");

        // Assert
        original.QuestionText.Should().Be(originalText);
        updated.QuestionText.Should().NotBe(originalText);
    }

    [Fact]
    public void RecordAnswer_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestQuestion();

        // Act
        var updated = original.RecordAnswer(true);

        // Assert
        original.TimesAnswered.Should().Be(0);
        original.TimesCorrect.Should().Be(0);
        updated.TimesAnswered.Should().Be(1);
        updated.TimesCorrect.Should().Be(1);
    }

    [Fact]
    public void UpdateIrtParameters_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestQuestion();

        // Act
        var updated = original.UpdateIrtParameters(2.0, 1.0, 0.3);

        // Assert
        original.IrtDiscrimination.Should().BeNull();
        original.IrtDifficulty.Should().BeNull();
        original.IrtGuessing.Should().BeNull();
        updated.IrtDiscrimination.Should().Be(2.0);
        updated.IrtDifficulty.Should().Be(1.0);
        updated.IrtGuessing.Should().Be(0.3);
    }

    #endregion

    #region Workflow Tests

    [Fact]
    public void CompleteWorkflow_QuestionLifecycle()
    {
        // Arrange - Create new question
        var question = CreateTestQuestion();
        question.IsActive.Should().BeTrue();
        question.SuccessRate.Should().Be(0);

        // Act - Record several answers
        var after1 = question.RecordAnswer(true);
        var after2 = after1.RecordAnswer(false);
        var after3 = after2.RecordAnswer(true);

        // Calibrate IRT parameters based on performance
        var calibrated = after3.UpdateIrtParameters(
            discrimination: 1.5,
            difficulty: -0.3,  // Slightly easier since 2/3 got it right
            guessing: 0.25);

        // Update question content
        var updated = calibrated.With(
            explanation: "Enhanced explanation with more detail",
            difficultyLevel: DifficultyLevel.Medium);

        // Assert - Verify final state
        updated.TimesAnswered.Should().Be(3);
        updated.TimesCorrect.Should().Be(2);
        updated.SuccessRate.Should().BeApproximately(0.667, 0.001);
        updated.IrtDiscrimination.Should().Be(1.5);
        updated.IrtDifficulty.Should().Be(-0.3);
        updated.IrtGuessing.Should().Be(0.25);
        updated.DifficultyLevel.Should().Be(DifficultyLevel.Medium);
        updated.Explanation.Should().Contain("Enhanced explanation");
    }

    [Fact]
    public void AdaptiveTestingWorkflow_IrtParametersUsedForQuestionSelection()
    {
        // Arrange - Create questions with different IRT parameters
        var easyQuestion = CreateTestQuestion().UpdateIrtParameters(
            discrimination: 1.2,
            difficulty: -1.5,  // Very negative = easy
            guessing: 0.25);

        var mediumQuestion = CreateTestQuestion().UpdateIrtParameters(
            discrimination: 1.5,
            difficulty: 0.0,   // Neutral difficulty
            guessing: 0.25);

        var hardQuestion = CreateTestQuestion().UpdateIrtParameters(
            discrimination: 1.8,
            difficulty: 2.0,   // Very positive = hard
            guessing: 0.25);

        // Assert - IRT parameters set correctly for adaptive selection
        easyQuestion.IrtDifficulty.Should().BeLessThan(mediumQuestion.IrtDifficulty!.Value);
        mediumQuestion.IrtDifficulty.Should().BeLessThan(hardQuestion.IrtDifficulty!.Value);
        hardQuestion.IrtDiscrimination.Should().BeGreaterThan(easyQuestion.IrtDiscrimination!.Value);
    }

    #endregion
}
