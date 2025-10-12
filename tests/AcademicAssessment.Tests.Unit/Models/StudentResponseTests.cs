using AcademicAssessment.Core.Models;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Models;

public class StudentResponseTests
{
    private static StudentResponse CreateTestResponse(
        bool isCorrect = true,
        int pointsEarned = 10,
        int maxPoints = 10,
        string? feedback = null,
        string? aiExplanation = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            StudentAnswer = "{\"answer\": \"correct answer\"}",
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            MaxPoints = maxPoints,
            TimeSpentSeconds = 45,
            QuestionOrder = 0,
            AbilityAtTime = 0.5,
            Feedback = feedback,
            AiExplanation = aiExplanation,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllRequiredProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentAssessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var submittedAt = DateTimeOffset.UtcNow;
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var response = new StudentResponse
        {
            Id = id,
            StudentAssessmentId = studentAssessmentId,
            StudentId = studentId,
            QuestionId = questionId,
            SchoolId = schoolId,
            StudentAnswer = "{\"answer\": \"B\"}",
            IsCorrect = true,
            PointsEarned = 10,
            MaxPoints = 10,
            QuestionOrder = 3,
            SubmittedAt = submittedAt,
            CreatedAt = createdAt
        };

        // Assert
        response.Id.Should().Be(id);
        response.StudentAssessmentId.Should().Be(studentAssessmentId);
        response.StudentId.Should().Be(studentId);
        response.QuestionId.Should().Be(questionId);
        response.SchoolId.Should().Be(schoolId);
        response.StudentAnswer.Should().Be("{\"answer\": \"B\"}");
        response.IsCorrect.Should().BeTrue();
        response.PointsEarned.Should().Be(10);
        response.MaxPoints.Should().Be(10);
        response.QuestionOrder.Should().Be(3);
        response.SubmittedAt.Should().Be(submittedAt);
        response.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Constructor_DefaultsTimeSpentToZero()
    {
        // Act
        var response = CreateTestResponse();

        // Assert - explicitly setting in CreateTestResponse, let's test without
        var responseWithDefaults = new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            StudentAnswer = "answer",
            IsCorrect = true,
            PointsEarned = 10,
            MaxPoints = 10,
            QuestionOrder = 0,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        responseWithDefaults.TimeSpentSeconds.Should().Be(0);
    }

    [Fact]
    public void Constructor_AllowsNullableProperties()
    {
        // Act
        var response = new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            SchoolId = null,
            StudentAnswer = "answer",
            IsCorrect = false,
            PointsEarned = 0,
            MaxPoints = 10,
            QuestionOrder = 0,
            AbilityAtTime = null,
            Feedback = null,
            AiExplanation = null,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        response.SchoolId.Should().BeNull();
        response.AbilityAtTime.Should().BeNull();
        response.Feedback.Should().BeNull();
        response.AiExplanation.Should().BeNull();
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void WasSkipped_ReturnsFalse_WhenAnswerProvided()
    {
        // Arrange
        var response = CreateTestResponse();

        // Assert
        response.WasSkipped.Should().BeFalse();
    }

    [Fact]
    public void WasSkipped_ReturnsTrue_WhenAnswerIsNull()
    {
        // Arrange
        var response = new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            StudentAnswer = "",
            IsCorrect = false,
            PointsEarned = 0,
            MaxPoints = 10,
            QuestionOrder = 0,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        response.WasSkipped.Should().BeTrue();
    }

    [Fact]
    public void WasSkipped_ReturnsTrue_WhenAnswerIsWhitespace()
    {
        // Arrange
        var response = new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            StudentAnswer = "   ",
            IsCorrect = false,
            PointsEarned = 0,
            MaxPoints = 10,
            QuestionOrder = 0,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        response.WasSkipped.Should().BeTrue();
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdatesIsCorrect()
    {
        // Arrange
        var response = CreateTestResponse(isCorrect: false, pointsEarned: 0);

        // Act
        var updated = response.With(isCorrect: true, pointsEarned: 10);

        // Assert
        updated.IsCorrect.Should().BeTrue();
        updated.PointsEarned.Should().Be(10);
    }

    [Fact]
    public void With_UpdatesFeedback()
    {
        // Arrange
        var response = CreateTestResponse();

        // Act
        var updated = response.With(feedback: "Good job!");

        // Assert
        updated.Feedback.Should().Be("Good job!");
    }

    [Fact]
    public void With_UpdatesAiExplanation()
    {
        // Arrange
        var response = CreateTestResponse();

        // Act
        var updated = response.With(aiExplanation: "AI explanation here");

        // Assert
        updated.AiExplanation.Should().Be("AI explanation here");
    }

    [Fact]
    public void With_UpdatesMultipleProperties()
    {
        // Arrange
        var response = CreateTestResponse(isCorrect: false, pointsEarned: 0);

        // Act
        var updated = response.With(
            isCorrect: true,
            pointsEarned: 8,
            feedback: "Correct!",
            aiExplanation: "Well done");

        // Assert
        updated.IsCorrect.Should().BeTrue();
        updated.PointsEarned.Should().Be(8);
        updated.Feedback.Should().Be("Correct!");
        updated.AiExplanation.Should().Be("Well done");
    }

    #endregion

    #region AddAiExplanation Method Tests

    [Fact]
    public void AddAiExplanation_AddsExplanation()
    {
        // Arrange
        var response = CreateTestResponse();
        var explanation = "This is how the answer works...";

        // Act
        var updated = response.AddAiExplanation(explanation);

        // Assert
        updated.AiExplanation.Should().Be(explanation);
    }

    [Fact]
    public void AddAiExplanation_CanOverwriteExisting()
    {
        // Arrange
        var response = CreateTestResponse(aiExplanation: "Old explanation");
        var newExplanation = "New explanation";

        // Act
        var updated = response.AddAiExplanation(newExplanation);

        // Assert
        updated.AiExplanation.Should().Be(newExplanation);
    }

    #endregion

    #region AddFeedback Method Tests

    [Fact]
    public void AddFeedback_AddsFeedback()
    {
        // Arrange
        var response = CreateTestResponse();
        var feedback = "Great work!";

        // Act
        var updated = response.AddFeedback(feedback);

        // Assert
        updated.Feedback.Should().Be(feedback);
    }

    [Fact]
    public void AddFeedback_CanOverwriteExisting()
    {
        // Arrange
        var response = CreateTestResponse(feedback: "Old feedback");
        var newFeedback = "New feedback";

        // Act
        var updated = response.AddFeedback(newFeedback);

        // Assert
        updated.Feedback.Should().Be(newFeedback);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void With_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestResponse(isCorrect: false, pointsEarned: 0);

        // Act
        var updated = original.With(isCorrect: true, pointsEarned: 10);

        // Assert
        original.IsCorrect.Should().BeFalse();
        original.PointsEarned.Should().Be(0);
        updated.IsCorrect.Should().BeTrue();
        updated.PointsEarned.Should().Be(10);
    }

    [Fact]
    public void AddAiExplanation_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestResponse();
        original.AiExplanation.Should().BeNull();

        // Act
        var updated = original.AddAiExplanation("New explanation");

        // Assert
        original.AiExplanation.Should().BeNull();
        updated.AiExplanation.Should().Be("New explanation");
    }

    [Fact]
    public void AddFeedback_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateTestResponse();
        original.Feedback.Should().BeNull();

        // Act
        var updated = original.AddFeedback("New feedback");

        // Assert
        original.Feedback.Should().BeNull();
        updated.Feedback.Should().Be("New feedback");
    }

    #endregion

    #region Workflow Tests

    [Fact]
    public void CompleteWorkflow_ResponseProcessing()
    {
        // Arrange - Student submits incorrect answer initially
        var response = CreateTestResponse(isCorrect: false, pointsEarned: 0);
        response.IsCorrect.Should().BeFalse();
        response.PointsEarned.Should().Be(0);

        // Act - Teacher reviews and gives partial credit
        var reviewed = response.With(
            isCorrect: false,  // Still incorrect but...
            pointsEarned: 3,   // Gets partial credit for showing work
            feedback: "Good approach but minor calculation error");

        // Add AI explanation for student learning
        var withExplanation = reviewed.AddAiExplanation(
            "The correct approach is to...");

        // Assert - Final state
        withExplanation.IsCorrect.Should().BeFalse();
        withExplanation.PointsEarned.Should().Be(3);
        withExplanation.Feedback.Should().Contain("Good approach");
        withExplanation.AiExplanation.Should().Contain("correct approach");
    }

    [Fact]
    public void SkippedQuestionWorkflow_TrackingAndFeedback()
    {
        // Arrange - Student skips a question
        var skippedResponse = new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            StudentAnswer = "",  // Empty = skipped
            IsCorrect = false,
            PointsEarned = 0,
            MaxPoints = 10,
            TimeSpentSeconds = 5,  // Looked at it briefly
            QuestionOrder = 5,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert - Verify skipped status
        skippedResponse.WasSkipped.Should().BeTrue();
        skippedResponse.PointsEarned.Should().Be(0);

        // Act - Add feedback encouraging them to attempt it
        var withFeedback = skippedResponse.AddFeedback(
            "Try breaking this problem down into smaller steps");

        // Assert - Feedback added but still skipped
        withFeedback.WasSkipped.Should().BeTrue();
        withFeedback.Feedback.Should().Contain("breaking this problem");
    }

    [Fact]
    public void AdaptiveTestingWorkflow_TrackingAbilityEstimate()
    {
        // Arrange - Series of responses showing ability progression
        var easyCorrect = CreateTestResponse(isCorrect: true, pointsEarned: 10)
            with { AbilityAtTime = -0.5, TimeSpentSeconds = 30 };

        var mediumCorrect = CreateTestResponse(isCorrect: true, pointsEarned: 10)
            with { AbilityAtTime = 0.0, TimeSpentSeconds = 60 };

        var hardIncorrect = CreateTestResponse(isCorrect: false, pointsEarned: 0)
            with { AbilityAtTime = 0.5, TimeSpentSeconds = 120 };

        // Assert - Ability estimates tracked for adaptive algorithm
        easyCorrect.AbilityAtTime.Should().Be(-0.5);
        mediumCorrect.AbilityAtTime.Should().Be(0.0);
        hardIncorrect.AbilityAtTime.Should().Be(0.5);

        // Verify time spent increases with difficulty
        easyCorrect.TimeSpentSeconds.Should().BeLessThan(mediumCorrect.TimeSpentSeconds);
        mediumCorrect.TimeSpentSeconds.Should().BeLessThan(hardIncorrect.TimeSpentSeconds);
    }

    #endregion
}
