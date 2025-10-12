using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Models;

public class AssessmentTests
{
    #region Test Helpers

    private static Assessment CreateTestAssessment() =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            Title = "Algebra I Final Exam",
            Description = "Comprehensive final exam covering all topics",
            AssessmentType = AssessmentType.Summative,
            Subject = Subject.Mathematics,
            GradeLevel = GradeLevel.Grade9,
            Topics = new List<string> { "Linear Equations", "Quadratic Functions" }.AsReadOnly(),
            QuestionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }.AsReadOnly(),
            TotalPoints = 100,
            TimeLimitMinutes = 90,
            PassingScorePercentage = 70,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var assessment = new Assessment
        {
            Id = id,
            CourseId = courseId,
            SchoolId = schoolId,
            Title = "Math Final",
            Description = "Final exam",
            AssessmentType = AssessmentType.Summative,
            Subject = Subject.Mathematics,
            GradeLevel = GradeLevel.Grade9,
            Topics = new List<string> { "Algebra", "Geometry" }.AsReadOnly(),
            QuestionIds = new List<Guid> { questionId1, questionId2 }.AsReadOnly(),
            TotalPoints = 100,
            TimeLimitMinutes = 60,
            PassingScorePercentage = 75,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        assessment.Id.Should().Be(id);
        assessment.CourseId.Should().Be(courseId);
        assessment.SchoolId.Should().Be(schoolId);
        assessment.Title.Should().Be("Math Final");
        assessment.Description.Should().Be("Final exam");
        assessment.AssessmentType.Should().Be(AssessmentType.Summative);
        assessment.Subject.Should().Be(Subject.Mathematics);
        assessment.GradeLevel.Should().Be(GradeLevel.Grade9);
        assessment.Topics.Should().BeEquivalentTo(new[] { "Algebra", "Geometry" });
        assessment.QuestionIds.Should().BeEquivalentTo(new[] { questionId1, questionId2 });
        assessment.TotalPoints.Should().Be(100);
        assessment.TimeLimitMinutes.Should().Be(60);
        assessment.PassingScorePercentage.Should().Be(75);
        assessment.IsActive.Should().BeTrue();
        assessment.CreatedAt.Should().Be(createdAt);
        assessment.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Constructor_AllowsNullSchoolId()
    {
        // Act
        var assessment = CreateTestAssessment() with { SchoolId = null };

        // Assert
        assessment.SchoolId.Should().BeNull();
    }

    [Fact]
    public void Constructor_AllowsNullTimeLimitMinutes()
    {
        // Act
        var assessment = CreateTestAssessment() with { TimeLimitMinutes = null };

        // Assert
        assessment.TimeLimitMinutes.Should().BeNull();
    }

    [Fact]
    public void Constructor_PassingScorePercentageDefaultsTo70()
    {
        // Act
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Title = "Test",
            Description = "Test",
            AssessmentType = AssessmentType.Formative,
            Subject = Subject.Mathematics,
            GradeLevel = GradeLevel.Grade9,
            TotalPoints = 100,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        assessment.PassingScorePercentage.Should().Be(70);
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void IsAdaptive_WhenAdaptiveType_ReturnsTrue()
    {
        // Arrange
        var assessment = CreateTestAssessment() with
        {
            AssessmentType = AssessmentType.Adaptive
        };

        // Act & Assert
        assessment.IsAdaptive.Should().BeTrue();
    }

    [Theory]
    [InlineData(AssessmentType.Diagnostic)]
    [InlineData(AssessmentType.Formative)]
    [InlineData(AssessmentType.Summative)]
    [InlineData(AssessmentType.Practice)]
    public void IsAdaptive_WhenNonAdaptiveType_ReturnsFalse(AssessmentType type)
    {
        // Arrange
        var assessment = CreateTestAssessment() with
        {
            AssessmentType = type
        };

        // Act & Assert
        assessment.IsAdaptive.Should().BeFalse();
    }

    [Fact]
    public void QuestionCount_ReturnsQuestionIdsCount()
    {
        // Arrange
        var questionIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        }.AsReadOnly();

        var assessment = CreateTestAssessment() with
        {
            QuestionIds = questionIds
        };

        // Act & Assert
        assessment.QuestionCount.Should().Be(3);
    }

    [Fact]
    public void QuestionCount_WhenNoQuestions_ReturnsZero()
    {
        // Arrange
        var assessment = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid>().AsReadOnly()
        };

        // Act & Assert
        assessment.QuestionCount.Should().Be(0);
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdatesTitle_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();
        var newTitle = "Updated Title";

        // Act
        var updated = original.With(title: newTitle);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Title.Should().Be(newTitle);
        updated.Description.Should().Be(original.Description);
        updated.QuestionIds.Should().BeEquivalentTo(original.QuestionIds);
        updated.TotalPoints.Should().Be(original.TotalPoints);
        updated.TimeLimitMinutes.Should().Be(original.TimeLimitMinutes);
        updated.IsActive.Should().Be(original.IsActive);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesDescription_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();
        var newDescription = "Updated Description";

        // Act
        var updated = original.With(description: newDescription);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Description.Should().Be(newDescription);
        updated.Title.Should().Be(original.Title);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesQuestionIds_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();
        var newQuestionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.AsReadOnly();

        // Act
        var updated = original.With(questionIds: newQuestionIds);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().BeEquivalentTo(newQuestionIds);
        updated.QuestionCount.Should().Be(3);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesTotalPoints_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();
        var newTotalPoints = 150;

        // Act
        var updated = original.With(totalPoints: newTotalPoints);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.TotalPoints.Should().Be(newTotalPoints);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesTimeLimitMinutes_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();
        var newTimeLimit = 120;

        // Act
        var updated = original.With(timeLimitMinutes: newTimeLimit);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.TimeLimitMinutes.Should().Be(newTimeLimit);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesIsActive_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestAssessment();

        // Act
        var updated = original.With(isActive: false);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.IsActive.Should().BeFalse();
        original.IsActive.Should().BeTrue();
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_NoParameters_UpdatesOnlyUpdatedAt()
    {
        // Arrange
        var original = CreateTestAssessment();

        // Act
        var updated = original.With();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Title.Should().Be(original.Title);
        updated.Description.Should().Be(original.Description);
        updated.QuestionIds.Should().BeEquivalentTo(original.QuestionIds);
        updated.TotalPoints.Should().Be(original.TotalPoints);
        updated.TimeLimitMinutes.Should().Be(original.TimeLimitMinutes);
        updated.IsActive.Should().Be(original.IsActive);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    #endregion

    #region AddQuestion Tests

    [Fact]
    public void AddQuestion_NewQuestion_AddsToList()
    {
        // Arrange
        var original = CreateTestAssessment();
        var originalCount = original.QuestionCount;
        var newQuestionId = Guid.NewGuid();

        // Act
        var updated = original.AddQuestion(newQuestionId);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().Contain(newQuestionId);
        updated.QuestionCount.Should().Be(originalCount + 1);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void AddQuestion_DuplicateQuestion_DoesNotAdd()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId }.AsReadOnly()
        };

        // Act
        var result = original.AddQuestion(questionId);

        // Assert
        result.Should().BeSameAs(original); // Returns same instance when no change
        result.QuestionIds.Should().ContainSingle();
        result.QuestionIds.Should().Contain(questionId);
    }

    [Fact]
    public void AddQuestion_ToEmptyList_AddsFirstQuestion()
    {
        // Arrange
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid>().AsReadOnly()
        };
        var newQuestionId = Guid.NewGuid();

        // Act
        var updated = original.AddQuestion(newQuestionId);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().ContainSingle();
        updated.QuestionIds.Should().Contain(newQuestionId);
        updated.QuestionCount.Should().Be(1);
    }

    #endregion

    #region RemoveQuestion Tests

    [Fact]
    public void RemoveQuestion_ExistingQuestion_RemovesFromList()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var otherQuestionId = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId, otherQuestionId }.AsReadOnly()
        };

        // Act
        var updated = original.RemoveQuestion(questionId);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().NotContain(questionId);
        updated.QuestionIds.Should().Contain(otherQuestionId);
        updated.QuestionCount.Should().Be(1);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void RemoveQuestion_NonExistentQuestion_ReturnsNewInstanceWithSameQuestions()
    {
        // Arrange
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId1, questionId2 }.AsReadOnly()
        };
        var nonExistentId = Guid.NewGuid();

        // Act
        var updated = original.RemoveQuestion(nonExistentId);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().BeEquivalentTo(original.QuestionIds);
        updated.QuestionCount.Should().Be(2);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void RemoveQuestion_LastQuestion_LeavesEmptyList()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId }.AsReadOnly()
        };

        // Act
        var updated = original.RemoveQuestion(questionId);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().BeEmpty();
        updated.QuestionCount.Should().Be(0);
    }

    #endregion

    #region ReorderQuestions Tests

    [Fact]
    public void ReorderQuestions_ValidOrder_ReordersQuestions()
    {
        // Arrange
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var questionId3 = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId1, questionId2, questionId3 }.AsReadOnly()
        };
        var newOrder = new List<Guid> { questionId3, questionId1, questionId2 }.AsReadOnly();

        // Act
        var updated = original.ReorderQuestions(newOrder);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().ContainInOrder(questionId3, questionId1, questionId2);
        updated.QuestionCount.Should().Be(3);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void ReorderQuestions_DifferentCount_ReturnsOriginalInstance()
    {
        // Arrange
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId1, questionId2 }.AsReadOnly()
        };
        var newOrder = new List<Guid> { questionId1 }.AsReadOnly(); // Wrong count

        // Act
        var result = original.ReorderQuestions(newOrder);

        // Assert
        result.Should().BeSameAs(original); // Returns same instance when invalid
        result.QuestionIds.Should().ContainInOrder(questionId1, questionId2);
    }

    [Fact]
    public void ReorderQuestions_MissingQuestion_ReturnsOriginalInstance()
    {
        // Arrange
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId1, questionId2 }.AsReadOnly()
        };
        var differentId = Guid.NewGuid();
        var newOrder = new List<Guid> { questionId1, differentId }.AsReadOnly(); // Contains ID not in original

        // Act
        var result = original.ReorderQuestions(newOrder);

        // Assert
        result.Should().BeSameAs(original); // Returns same instance when invalid
        result.QuestionIds.Should().ContainInOrder(questionId1, questionId2);
    }

    [Fact]
    public void ReorderQuestions_SameOrder_UpdatesTimestamp()
    {
        // Arrange
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var original = CreateTestAssessment() with
        {
            QuestionIds = new List<Guid> { questionId1, questionId2 }.AsReadOnly()
        };
        var sameOrder = new List<Guid> { questionId1, questionId2 }.AsReadOnly();

        // Act
        var updated = original.ReorderQuestions(sameOrder);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.QuestionIds.Should().ContainInOrder(questionId1, questionId2);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void Assessment_IsImmutable_OriginalUnchangedAfterWith()
    {
        // Arrange
        var original = CreateTestAssessment();
        var originalTitle = original.Title;
        var originalDescription = original.Description;
        var originalTotalPoints = original.TotalPoints;
        var originalIsActive = original.IsActive;
        var originalUpdatedAt = original.UpdatedAt;

        // Act
        var _ = original.With(
            title: "New Title",
            description: "New Description",
            totalPoints: 200,
            isActive: false);

        // Assert - Original unchanged
        original.Title.Should().Be(originalTitle);
        original.Description.Should().Be(originalDescription);
        original.TotalPoints.Should().Be(originalTotalPoints);
        original.IsActive.Should().Be(originalIsActive);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Assessment_IsImmutable_OriginalUnchangedAfterAddQuestion()
    {
        // Arrange
        var original = CreateTestAssessment();
        var originalQuestionIds = original.QuestionIds.ToList();
        var originalCount = original.QuestionCount;
        var originalUpdatedAt = original.UpdatedAt;

        // Act
        var _ = original.AddQuestion(Guid.NewGuid());

        // Assert - Original unchanged
        original.QuestionIds.Should().BeEquivalentTo(originalQuestionIds);
        original.QuestionCount.Should().Be(originalCount);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Assessment_IsImmutable_OriginalUnchangedAfterRemoveQuestion()
    {
        // Arrange
        var original = CreateTestAssessment();
        var originalQuestionIds = original.QuestionIds.ToList();
        var originalCount = original.QuestionCount;
        var originalUpdatedAt = original.UpdatedAt;
        var questionToRemove = originalQuestionIds.First();

        // Act
        var _ = original.RemoveQuestion(questionToRemove);

        // Assert - Original unchanged
        original.QuestionIds.Should().BeEquivalentTo(originalQuestionIds);
        original.QuestionCount.Should().Be(originalCount);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Assessment_IsImmutable_OriginalUnchangedAfterReorderQuestions()
    {
        // Arrange
        var questionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var original = CreateTestAssessment() with
        {
            QuestionIds = questionIds.AsReadOnly()
        };
        var originalQuestionIds = original.QuestionIds.ToList();
        var originalUpdatedAt = original.UpdatedAt;
        var reversedOrder = questionIds.AsEnumerable().Reverse().ToList().AsReadOnly();

        // Act
        var _ = original.ReorderQuestions(reversedOrder);

        // Assert - Original unchanged
        original.QuestionIds.Should().ContainInOrder(originalQuestionIds);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    #endregion
}
