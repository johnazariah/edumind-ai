using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Models;

public class StudentAssessmentTests
{
    #region Test Helpers

    private static StudentAssessment CreateTestStudentAssessment(
        AssessmentStatus status = AssessmentStatus.NotStarted) =>
        new()
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            AssessmentId = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            Status = status,
            MaxScore = 100,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        };

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllRequiredProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var studentAssessment = new StudentAssessment
        {
            Id = id,
            StudentId = studentId,
            AssessmentId = assessmentId,
            SchoolId = schoolId,
            ClassId = classId,
            Status = AssessmentStatus.InProgress,
            StartedAt = createdAt,
            Score = 85,
            MaxScore = 100,
            Passed = true,
            CurrentQuestionIndex = 5,
            EstimatedAbility = 1.5,
            TimeSpentSeconds = 1800,
            CorrectAnswers = 17,
            IncorrectAnswers = 3,
            SkippedQuestions = 0,
            Feedback = "Great job!",
            XpEarned = 150,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        studentAssessment.Id.Should().Be(id);
        studentAssessment.StudentId.Should().Be(studentId);
        studentAssessment.AssessmentId.Should().Be(assessmentId);
        studentAssessment.SchoolId.Should().Be(schoolId);
        studentAssessment.ClassId.Should().Be(classId);
        studentAssessment.Status.Should().Be(AssessmentStatus.InProgress);
        studentAssessment.StartedAt.Should().Be(createdAt);
        studentAssessment.Score.Should().Be(85);
        studentAssessment.MaxScore.Should().Be(100);
        studentAssessment.Passed.Should().BeTrue();
        studentAssessment.CurrentQuestionIndex.Should().Be(5);
        studentAssessment.EstimatedAbility.Should().Be(1.5);
        studentAssessment.TimeSpentSeconds.Should().Be(1800);
        studentAssessment.CorrectAnswers.Should().Be(17);
        studentAssessment.IncorrectAnswers.Should().Be(3);
        studentAssessment.SkippedQuestions.Should().Be(0);
        studentAssessment.Feedback.Should().Be("Great job!");
        studentAssessment.XpEarned.Should().Be(150);
        studentAssessment.CreatedAt.Should().Be(createdAt);
        studentAssessment.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Constructor_DefaultsCountersToZero()
    {
        // Act
        var studentAssessment = CreateTestStudentAssessment();

        // Assert
        studentAssessment.CurrentQuestionIndex.Should().Be(0);
        studentAssessment.CorrectAnswers.Should().Be(0);
        studentAssessment.IncorrectAnswers.Should().Be(0);
        studentAssessment.SkippedQuestions.Should().Be(0);
        studentAssessment.XpEarned.Should().Be(0);
    }

    [Fact]
    public void Constructor_AllowsNullableProperties()
    {
        // Act
        var studentAssessment = CreateTestStudentAssessment() with
        {
            SchoolId = null,
            ClassId = null,
            StartedAt = null,
            CompletedAt = null,
            Score = null,
            Passed = null,
            EstimatedAbility = null,
            TimeSpentSeconds = null,
            Feedback = null
        };

        // Assert
        studentAssessment.SchoolId.Should().BeNull();
        studentAssessment.ClassId.Should().BeNull();
        studentAssessment.StartedAt.Should().BeNull();
        studentAssessment.CompletedAt.Should().BeNull();
        studentAssessment.Score.Should().BeNull();
        studentAssessment.Passed.Should().BeNull();
        studentAssessment.EstimatedAbility.Should().BeNull();
        studentAssessment.TimeSpentSeconds.Should().BeNull();
        studentAssessment.Feedback.Should().BeNull();
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void PercentageScore_WhenScoreSet_CalculatesCorrectly()
    {
        // Arrange
        var studentAssessment = CreateTestStudentAssessment() with
        {
            Score = 85,
            MaxScore = 100
        };

        // Act & Assert
        studentAssessment.PercentageScore.Should().Be(85.0);
    }

    [Fact]
    public void PercentageScore_WhenScoreNotSet_ReturnsNull()
    {
        // Arrange
        var studentAssessment = CreateTestStudentAssessment() with
        {
            Score = null
        };

        // Act & Assert
        studentAssessment.PercentageScore.Should().BeNull();
    }

    [Fact]
    public void PercentageScore_WithPartialScore_CalculatesCorrectly()
    {
        // Arrange
        var studentAssessment = CreateTestStudentAssessment() with
        {
            Score = 47,
            MaxScore = 60
        };

        // Act & Assert
        studentAssessment.PercentageScore.Should().BeApproximately(78.33, 0.01);
    }

    #endregion

    #region Start Method Tests

    [Fact]
    public void Start_WhenNotStarted_StartsAssessment()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.NotStarted);
        var beforeStart = DateTimeOffset.UtcNow;

        // Act
        var started = original.Start();

        // Assert
        started.Should().NotBeSameAs(original);
        started.Status.Should().Be(AssessmentStatus.InProgress);
        started.StartedAt.Should().NotBeNull();
        started.StartedAt.Should().BeOnOrAfter(beforeStart);
        started.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void Start_WhenAlreadyStarted_ReturnsOriginal()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act
        var result = original.Start();

        // Assert
        result.Should().BeSameAs(original);
    }

    [Fact]
    public void Start_WhenCompleted_ReturnsOriginal()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.Completed);

        // Act
        var result = original.Start();

        // Assert
        result.Should().BeSameAs(original);
    }

    #endregion

    #region Complete Method Tests

    [Fact]
    public void Complete_SetsAllProperties()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);
        var recommendations = new List<string> { "Review topic A", "Practice more" }.AsReadOnly();
        var beforeComplete = DateTimeOffset.UtcNow;

        // Act
        var completed = original.Complete(
            finalScore: 85,
            passed: true,
            timeSpentSeconds: 1800,
            feedback: "Excellent work!",
            recommendations: recommendations,
            xpEarned: 150);

        // Assert
        completed.Should().NotBeSameAs(original);
        completed.Status.Should().Be(AssessmentStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();
        completed.CompletedAt.Should().BeOnOrAfter(beforeComplete);
        completed.Score.Should().Be(85);
        completed.Passed.Should().BeTrue();
        completed.TimeSpentSeconds.Should().Be(1800);
        completed.Feedback.Should().Be("Excellent work!");
        completed.Recommendations.Should().BeEquivalentTo(recommendations);
        completed.XpEarned.Should().Be(150);
        completed.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void Complete_WithoutOptionalParameters_SetsDefaults()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act
        var completed = original.Complete(
            finalScore: 60,
            passed: false,
            timeSpentSeconds: 900);

        // Assert
        completed.Score.Should().Be(60);
        completed.Passed.Should().BeFalse();
        completed.TimeSpentSeconds.Should().Be(900);
        completed.Feedback.Should().BeNull();
        completed.Recommendations.Should().BeEmpty();
        completed.XpEarned.Should().Be(0);
    }

    #endregion

    #region Navigation Methods Tests

    [Fact]
    public void NextQuestion_IncrementsIndex()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            CurrentQuestionIndex = 3
        };

        // Act
        var updated = original.NextQuestion();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.CurrentQuestionIndex.Should().Be(4);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void NextQuestion_FromZero_IncrementsToOne()
    {
        // Arrange
        var original = CreateTestStudentAssessment();

        // Act
        var updated = original.NextQuestion();

        // Assert
        updated.CurrentQuestionIndex.Should().Be(1);
    }

    #endregion

    #region Answer Recording Tests

    [Fact]
    public void RecordCorrect_IncrementsCorrectAnswers()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            CorrectAnswers = 5
        };

        // Act
        var updated = original.RecordCorrect();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.CorrectAnswers.Should().Be(6);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void RecordIncorrect_IncrementsIncorrectAnswers()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            IncorrectAnswers = 2
        };

        // Act
        var updated = original.RecordIncorrect();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.IncorrectAnswers.Should().Be(3);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void RecordSkipped_IncrementsSkippedQuestions()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            SkippedQuestions = 1
        };

        // Act
        var updated = original.RecordSkipped();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.SkippedQuestions.Should().Be(2);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    #endregion

    #region Adaptive Assessment Tests

    [Fact]
    public void UpdateAbility_SetsNewAbility()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            EstimatedAbility = 0.5
        };

        // Act
        var updated = original.UpdateAbility(1.2);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.EstimatedAbility.Should().Be(1.2);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void UpdateAbility_CanSetNegativeAbility()
    {
        // Arrange
        var original = CreateTestStudentAssessment();

        // Act
        var updated = original.UpdateAbility(-0.8);

        // Assert
        updated.EstimatedAbility.Should().Be(-0.8);
    }

    #endregion

    #region Pause/Resume Tests

    [Fact]
    public void Pause_WhenInProgress_PausesAssessment()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act
        var paused = original.Pause();

        // Assert
        paused.Should().NotBeSameAs(original);
        paused.Status.Should().Be(AssessmentStatus.Paused);
        paused.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void Pause_WhenNotInProgress_ReturnsOriginal()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.NotStarted);

        // Act
        var result = original.Pause();

        // Assert
        result.Should().BeSameAs(original);
    }

    [Fact]
    public void Resume_WhenPaused_ResumesAssessment()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.Paused);

        // Act
        var resumed = original.Resume();

        // Assert
        resumed.Should().NotBeSameAs(original);
        resumed.Status.Should().Be(AssessmentStatus.InProgress);
        resumed.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void Resume_WhenNotPaused_ReturnsOriginal()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act
        var result = original.Resume();

        // Assert
        result.Should().BeSameAs(original);
    }

    #endregion

    #region Abandon Tests

    [Fact]
    public void Abandon_SetsStatusToAbandoned()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act
        var abandoned = original.Abandon();

        // Assert
        abandoned.Should().NotBeSameAs(original);
        abandoned.Status.Should().Be(AssessmentStatus.Abandoned);
        abandoned.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void Abandon_CanAbandonFromAnyStatus()
    {
        // Arrange
        var statuses = new[]
        {
            AssessmentStatus.NotStarted,
            AssessmentStatus.InProgress,
            AssessmentStatus.Paused,
            AssessmentStatus.Completed
        };

        foreach (var status in statuses)
        {
            var original = CreateTestStudentAssessment(status);

            // Act
            var abandoned = original.Abandon();

            // Assert
            abandoned.Status.Should().Be(AssessmentStatus.Abandoned);
        }
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdatesStatus_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.NotStarted);

        // Act
        var updated = original.With(status: AssessmentStatus.InProgress);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Status.Should().Be(AssessmentStatus.InProgress);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_UpdatesMultipleProperties_ReturnsNewInstance()
    {
        // Arrange
        var original = CreateTestStudentAssessment();

        // Act
        var updated = original.With(
            status: AssessmentStatus.InProgress,
            currentQuestionIndex: 5,
            score: 85,
            passed: true,
            estimatedAbility: 1.5,
            timeSpentSeconds: 1800,
            feedback: "Good progress");

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Status.Should().Be(AssessmentStatus.InProgress);
        updated.CurrentQuestionIndex.Should().Be(5);
        updated.Score.Should().Be(85);
        updated.Passed.Should().BeTrue();
        updated.EstimatedAbility.Should().Be(1.5);
        updated.TimeSpentSeconds.Should().Be(1800);
        updated.Feedback.Should().Be("Good progress");
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    [Fact]
    public void With_NoParameters_UpdatesOnlyTimestamp()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress) with
        {
            CurrentQuestionIndex = 3,
            Score = 50
        };

        // Act
        var updated = original.With();

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Status.Should().Be(original.Status);
        updated.CurrentQuestionIndex.Should().Be(original.CurrentQuestionIndex);
        updated.Score.Should().Be(original.Score);
        updated.UpdatedAt.Should().BeAfter(original.UpdatedAt);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void StudentAssessment_IsImmutable_OriginalUnchangedAfterStart()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.NotStarted);
        var originalStatus = original.Status;
        var originalStartedAt = original.StartedAt;
        var originalUpdatedAt = original.UpdatedAt;

        // Act
        var _ = original.Start();

        // Assert
        original.Status.Should().Be(originalStatus);
        original.StartedAt.Should().Be(originalStartedAt);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void StudentAssessment_IsImmutable_OriginalUnchangedAfterComplete()
    {
        // Arrange
        var original = CreateTestStudentAssessment(AssessmentStatus.InProgress);
        var originalStatus = original.Status;
        var originalScore = original.Score;
        var originalUpdatedAt = original.UpdatedAt;

        // Act
        var _ = original.Complete(85, true, 1800);

        // Assert
        original.Status.Should().Be(originalStatus);
        original.Score.Should().Be(originalScore);
        original.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void StudentAssessment_IsImmutable_OriginalUnchangedAfterAnswerRecording()
    {
        // Arrange
        var original = CreateTestStudentAssessment() with
        {
            CorrectAnswers = 5,
            IncorrectAnswers = 2,
            SkippedQuestions = 1
        };
        var originalCorrect = original.CorrectAnswers;
        var originalIncorrect = original.IncorrectAnswers;
        var originalSkipped = original.SkippedQuestions;

        // Act
        var _ = original.RecordCorrect();
        var __ = original.RecordIncorrect();
        var ___ = original.RecordSkipped();

        // Assert
        original.CorrectAnswers.Should().Be(originalCorrect);
        original.IncorrectAnswers.Should().Be(originalIncorrect);
        original.SkippedQuestions.Should().Be(originalSkipped);
    }

    #endregion

    #region Workflow Tests

    [Fact]
    public void CompleteWorkflow_NotStartedToCompleted_WorksCorrectly()
    {
        // Arrange
        var assessment = CreateTestStudentAssessment(AssessmentStatus.NotStarted);

        // Act & Assert - Start
        var started = assessment.Start();
        started.Status.Should().Be(AssessmentStatus.InProgress);
        started.StartedAt.Should().NotBeNull();

        // Act & Assert - Answer questions
        var afterQ1 = started.RecordCorrect().NextQuestion();
        afterQ1.CorrectAnswers.Should().Be(1);
        afterQ1.CurrentQuestionIndex.Should().Be(1);

        var afterQ2 = afterQ1.RecordIncorrect().NextQuestion();
        afterQ2.IncorrectAnswers.Should().Be(1);
        afterQ2.CurrentQuestionIndex.Should().Be(2);

        // Act & Assert - Complete
        var completed = afterQ2.Complete(
            finalScore: 85,
            passed: true,
            timeSpentSeconds: 1800);
        completed.Status.Should().Be(AssessmentStatus.Completed);
        completed.Score.Should().Be(85);
        completed.Passed.Should().BeTrue();
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void PauseResumeWorkflow_WorksCorrectly()
    {
        // Arrange
        var assessment = CreateTestStudentAssessment(AssessmentStatus.InProgress);

        // Act - Pause
        var paused = assessment.Pause();
        paused.Status.Should().Be(AssessmentStatus.Paused);

        // Act - Resume
        var resumed = paused.Resume();
        resumed.Status.Should().Be(AssessmentStatus.InProgress);
    }

    [Fact]
    public void AdaptiveAssessmentWorkflow_UpdatesAbilityThroughout()
    {
        // Arrange
        var assessment = CreateTestStudentAssessment(AssessmentStatus.InProgress) with
        {
            EstimatedAbility = 0.0
        };

        // Act - Correct answer increases ability
        var afterCorrect = assessment
            .RecordCorrect()
            .UpdateAbility(0.5)
            .NextQuestion();
        afterCorrect.EstimatedAbility.Should().Be(0.5);

        // Act - Incorrect answer decreases ability
        var afterIncorrect = afterCorrect
            .RecordIncorrect()
            .UpdateAbility(0.2)
            .NextQuestion();
        afterIncorrect.EstimatedAbility.Should().Be(0.2);
    }

    #endregion
}
