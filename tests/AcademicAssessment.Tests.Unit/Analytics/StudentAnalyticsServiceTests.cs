using AcademicAssessment.Analytics.Services;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace AcademicAssessment.Tests.Unit.Analytics;

/// <summary>
/// Unit tests for StudentAnalyticsService
/// Tests all 7 analytics methods with various scenarios
/// </summary>
public sealed class StudentAnalyticsServiceTests
{
    private readonly Mock<IStudentAssessmentRepository> _mockStudentAssessmentRepo;
    private readonly Mock<IStudentResponseRepository> _mockStudentResponseRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IAssessmentRepository> _mockAssessmentRepo;
    private readonly Mock<ILogger<StudentAnalyticsService>> _mockLogger;
    private readonly StudentAnalyticsService _service;

    public StudentAnalyticsServiceTests()
    {
        _mockStudentAssessmentRepo = new Mock<IStudentAssessmentRepository>();
        _mockStudentResponseRepo = new Mock<IStudentResponseRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockAssessmentRepo = new Mock<IAssessmentRepository>();
        _mockLogger = new Mock<ILogger<StudentAnalyticsService>>();

        // Setup default mock behavior: return empty successful results
        _mockStudentAssessmentRepo
            .Setup(x => x.GetCompletedByStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) =>
                Result.Success<IReadOnlyList<StudentAssessment>>(Array.Empty<StudentAssessment>()));

        _mockStudentResponseRepo
            .Setup(x => x.GetByStudentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) =>
                Result.Success<IReadOnlyList<StudentResponse>>(Array.Empty<StudentResponse>()));

        _mockQuestionRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) =>
                Result.Failure<Question>(new Error("NotFound", "Question not found")));

        _mockAssessmentRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) =>
                Result.Failure<Assessment>(new Error("NotFound", "Assessment not found")));

        _service = new StudentAnalyticsService(
            _mockStudentAssessmentRepo.Object,
            _mockStudentResponseRepo.Object,
            _mockQuestionRepo.Object,
            _mockAssessmentRepo.Object,
            _mockLogger.Object
        );
    }

    #region GetStudentPerformanceSummaryAsync Tests

    [Fact]
    public async Task GetStudentPerformanceSummaryAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetStudentPerformanceSummaryAsync(studentId);

        // Assert
        Assert.IsType<Result<StudentPerformanceSummary>.Success>(result);
        var summary = ((Result<StudentPerformanceSummary>.Success)result).Value;
        Assert.Equal(studentId, summary.StudentId);
    }

    [Fact]
    public async Task GetStudentPerformanceSummaryAsync_ReturnsEmptyDataForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetStudentPerformanceSummaryAsync(studentId);

        // Assert
        Assert.IsType<Result<StudentPerformanceSummary>.Success>(result);
        var summary = ((Result<StudentPerformanceSummary>.Success)result).Value;
        Assert.Equal(0, summary.TotalAssessmentsTaken);
        Assert.Equal(0.0, summary.AverageScore);
        Assert.Equal(0.0, summary.OverallMastery);
        Assert.Empty(summary.SubjectScores);
        Assert.Equal(TimeSpan.Zero, summary.TotalTimeSpent);
        Assert.Equal(0, summary.CurrentStreak);
    }

    [Fact]
    public async Task GetStudentPerformanceSummaryAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetStudentPerformanceSummaryAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStudentPerformanceSummaryAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetStudentPerformanceSummaryAsync(studentId, cts.Token);

        // Assert
        Assert.IsType<Result<StudentPerformanceSummary>.Success>(result);
    }

    #endregion

    #region GetSubjectPerformanceAsync Tests

    [Fact]
    public async Task GetSubjectPerformanceAsync_WithValidInputs_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var subject = Subject.Mathematics;

        // Act
        var result = await _service.GetSubjectPerformanceAsync(studentId, subject);

        // Assert
        Assert.IsType<Result<SubjectPerformance>.Success>(result);
        var performance = ((Result<SubjectPerformance>.Success)result).Value;
        Assert.Equal(subject, performance.Subject);
    }

    [Theory]
    [InlineData(Subject.Mathematics)]
    [InlineData(Subject.English)]
    [InlineData(Subject.Physics)]
    [InlineData(Subject.Chemistry)]
    [InlineData(Subject.Biology)]
    public async Task GetSubjectPerformanceAsync_WithDifferentSubjects_ReturnsCorrectSubject(Subject subject)
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetSubjectPerformanceAsync(studentId, subject);

        // Assert
        Assert.IsType<Result<SubjectPerformance>.Success>(result);
        var performance = ((Result<SubjectPerformance>.Success)result).Value;
        Assert.Equal(subject, performance.Subject);
    }

    [Fact]
    public async Task GetSubjectPerformanceAsync_ReturnsEmptyDataForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var subject = Subject.Mathematics;

        // Act
        var result = await _service.GetSubjectPerformanceAsync(studentId, subject);

        // Assert
        Assert.IsType<Result<SubjectPerformance>.Success>(result);
        var performance = ((Result<SubjectPerformance>.Success)result).Value;
        Assert.Equal(0, performance.AssessmentsTaken);
        Assert.Equal(0.0, performance.AverageScore);
        Assert.Equal(0.0, performance.MasteryLevel);
        Assert.Equal(0.0, performance.AbilityEstimate);
        Assert.Equal(0, performance.QuestionsAnswered);
        Assert.Equal(0, performance.QuestionsCorrect);
        Assert.Equal(0.0, performance.AccuracyRate);
        Assert.Equal(TimeSpan.Zero, performance.AverageTimePerQuestion);
        Assert.Empty(performance.StrongTopics);
        Assert.Empty(performance.WeakTopics);
    }

    [Fact]
    public async Task GetSubjectPerformanceAsync_LogsInformationWithSubject()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var subject = Subject.Biology;

        // Act
        await _service.GetSubjectPerformanceAsync(studentId, subject);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains(studentId.ToString()) &&
                    v.ToString()!.Contains(subject.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSubjectPerformanceAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var subject = Subject.Physics;
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetSubjectPerformanceAsync(studentId, subject, cts.Token);

        // Assert
        Assert.IsType<Result<SubjectPerformance>.Success>(result);
    }

    #endregion

    #region GetLearningObjectiveMasteryAsync Tests

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetLearningObjectiveMasteryAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyList<LearningObjectiveMastery>>.Success>(result);
    }

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_ReturnsEmptyListForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetLearningObjectiveMasteryAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyList<LearningObjectiveMastery>>.Success>(result);
        var mastery = ((Result<IReadOnlyList<LearningObjectiveMastery>>.Success)result).Value;
        Assert.Empty(mastery);
    }

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_WithSubjectFilter_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var subject = Subject.Chemistry;

        // Act
        var result = await _service.GetLearningObjectiveMasteryAsync(studentId, subject);

        // Assert
        Assert.IsType<Result<IReadOnlyList<LearningObjectiveMastery>>.Success>(result);
    }

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_WithNullSubject_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetLearningObjectiveMasteryAsync(studentId, null);

        // Assert
        Assert.IsType<Result<IReadOnlyList<LearningObjectiveMastery>>.Success>(result);
    }

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetLearningObjectiveMasteryAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLearningObjectiveMasteryAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetLearningObjectiveMasteryAsync(studentId, null, cts.Token);

        // Assert
        Assert.IsType<Result<IReadOnlyList<LearningObjectiveMastery>>.Success>(result);
    }

    #endregion

    #region GetAbilityEstimatesAsync Tests

    [Fact]
    public async Task GetAbilityEstimatesAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetAbilityEstimatesAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyDictionary<Subject, double>>.Success>(result);
    }

    [Fact]
    public async Task GetAbilityEstimatesAsync_ReturnsEmptyDictionaryForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetAbilityEstimatesAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyDictionary<Subject, double>>.Success>(result);
        var estimates = ((Result<IReadOnlyDictionary<Subject, double>>.Success)result).Value;
        Assert.Empty(estimates);
    }

    [Fact]
    public async Task GetAbilityEstimatesAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetAbilityEstimatesAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAbilityEstimatesAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetAbilityEstimatesAsync(studentId, cts.Token);

        // Assert
        Assert.IsType<Result<IReadOnlyDictionary<Subject, double>>.Success>(result);
    }

    #endregion

    #region GetImprovementAreasAsync Tests

    [Fact]
    public async Task GetImprovementAreasAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetImprovementAreasAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyList<ImprovementArea>>.Success>(result);
    }

    [Fact]
    public async Task GetImprovementAreasAsync_ReturnsEmptyListForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetImprovementAreasAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyList<ImprovementArea>>.Success>(result);
        var areas = ((Result<IReadOnlyList<ImprovementArea>>.Success)result).Value;
        Assert.Empty(areas);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetImprovementAreasAsync_WithDifferentTopN_ReturnsSuccess(int topN)
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetImprovementAreasAsync(studentId, topN);

        // Assert
        Assert.IsType<Result<IReadOnlyList<ImprovementArea>>.Success>(result);
    }

    [Fact]
    public async Task GetImprovementAreasAsync_WithDefaultTopN_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetImprovementAreasAsync(studentId);

        // Assert
        Assert.IsType<Result<IReadOnlyList<ImprovementArea>>.Success>(result);
    }

    [Fact]
    public async Task GetImprovementAreasAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetImprovementAreasAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetImprovementAreasAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetImprovementAreasAsync(studentId, 5, cts.Token);

        // Assert
        Assert.IsType<Result<IReadOnlyList<ImprovementArea>>.Success>(result);
    }

    #endregion

    #region GetProgressTimelineAsync Tests

    [Fact]
    public async Task GetProgressTimelineAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId);

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
        var timeline = ((Result<ProgressTimeline>.Success)result).Value;
        Assert.Equal(studentId, timeline.StudentId);
    }

    [Fact]
    public async Task GetProgressTimelineAsync_ReturnsEmptyDataForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId);

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
        var timeline = ((Result<ProgressTimeline>.Success)result).Value;
        Assert.Empty(timeline.DataPoints);
        Assert.Equal(0.0, timeline.AverageGrowthRate);
        Assert.Empty(timeline.SubjectGrowthRates);
    }

    [Fact]
    public async Task GetProgressTimelineAsync_WithDateRange_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var startDate = DateTimeOffset.UtcNow.AddMonths(-3);
        var endDate = DateTimeOffset.UtcNow;

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId, startDate, endDate);

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
        var timeline = ((Result<ProgressTimeline>.Success)result).Value;
        Assert.Equal(startDate, timeline.StartDate);
        Assert.True(timeline.EndDate >= endDate.AddSeconds(-1)); // Allow small timing difference
    }

    [Fact]
    public async Task GetProgressTimelineAsync_WithNullStartDate_UsesMinValue()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId, null, null);

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
        var timeline = ((Result<ProgressTimeline>.Success)result).Value;
        Assert.Equal(DateTimeOffset.MinValue, timeline.StartDate);
    }

    [Fact]
    public async Task GetProgressTimelineAsync_WithNullEndDate_UsesCurrentTime()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var beforeCall = DateTimeOffset.UtcNow;

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId, null, null);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
        var timeline = ((Result<ProgressTimeline>.Success)result).Value;
        Assert.True(timeline.EndDate >= beforeCall);
        Assert.True(timeline.EndDate <= afterCall.AddSeconds(1)); // Small buffer
    }

    [Fact]
    public async Task GetProgressTimelineAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetProgressTimelineAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProgressTimelineAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetProgressTimelineAsync(studentId, null, null, cts.Token);

        // Assert
        Assert.IsType<Result<ProgressTimeline>.Success>(result);
    }

    #endregion

    #region GetPeerComparisonAsync Tests

    [Fact]
    public async Task GetPeerComparisonAsync_WithValidStudentId_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
        var comparison = ((Result<PeerComparison>.Success)result).Value;
        Assert.Equal(studentId, comparison.StudentId);
    }

    [Fact]
    public async Task GetPeerComparisonAsync_ReturnsEmptyDataForStub()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
        var comparison = ((Result<PeerComparison>.Success)result).Value;
        Assert.Equal(0.0, comparison.StudentScore);
        Assert.Equal(0.0, comparison.PeerAverageScore);
        Assert.Equal(0.0, comparison.PeerMedianScore);
        Assert.Equal(0, comparison.Percentile);
        Assert.Equal(0, comparison.PeerGroupSize);
        Assert.False(comparison.MeetsKAnonymity);
    }

    [Theory]
    [InlineData(GradeLevel.Grade9)]
    [InlineData(GradeLevel.Grade10)]
    [InlineData(GradeLevel.Grade11)]
    [InlineData(GradeLevel.Grade12)]
    public async Task GetPeerComparisonAsync_WithDifferentGradeLevels_ReturnsCorrectGradeLevel(GradeLevel gradeLevel)
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId, gradeLevel);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
        var comparison = ((Result<PeerComparison>.Success)result).Value;
        Assert.Equal(gradeLevel, comparison.GradeLevel);
    }

    [Fact]
    public async Task GetPeerComparisonAsync_WithNullGradeLevel_UsesDefaultGrade9()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId, null);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
        var comparison = ((Result<PeerComparison>.Success)result).Value;
        Assert.Equal(GradeLevel.Grade9, comparison.GradeLevel);
    }

    [Theory]
    [InlineData(Subject.Mathematics)]
    [InlineData(Subject.English)]
    [InlineData(null)]
    public async Task GetPeerComparisonAsync_WithDifferentSubjects_ReturnsCorrectSubject(Subject? subject)
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId, null, subject);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
        var comparison = ((Result<PeerComparison>.Success)result).Value;
        Assert.Equal(subject, comparison.Subject);
    }

    [Fact]
    public async Task GetPeerComparisonAsync_LogsInformation()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        await _service.GetPeerComparisonAsync(studentId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(studentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPeerComparisonAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _service.GetPeerComparisonAsync(studentId, null, null, cts.Token);

        // Assert
        Assert.IsType<Result<PeerComparison>.Success>(result);
    }

    #endregion

    #region Service Constructor Tests

    [Fact]
    public void Constructor_WithAllDependencies_CreatesServiceSuccessfully()
    {
        // Arrange & Act
        var service = new StudentAnalyticsService(
            _mockStudentAssessmentRepo.Object,
            _mockStudentResponseRepo.Object,
            _mockQuestionRepo.Object,
            _mockAssessmentRepo.Object,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullStudentAssessmentRepo_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new StudentAnalyticsService(
                null!,
                _mockStudentResponseRepo.Object,
                _mockQuestionRepo.Object,
                _mockAssessmentRepo.Object,
                _mockLogger.Object
            ));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new StudentAnalyticsService(
                _mockStudentAssessmentRepo.Object,
                _mockStudentResponseRepo.Object,
                _mockQuestionRepo.Object,
                _mockAssessmentRepo.Object,
                null!
            ));
    }

    #endregion
}
