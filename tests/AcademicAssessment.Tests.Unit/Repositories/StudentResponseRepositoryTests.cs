using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Data;
using AcademicAssessment.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Tests.Unit.Repositories;

public sealed class StudentResponseRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IStudentResponseRepository _repository;
    private readonly Guid _schoolId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _questionId = Guid.NewGuid();
    private readonly Guid _studentAssessmentId = Guid.NewGuid();

    public StudentResponseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase($"StudentResponseRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext(_schoolId, _studentId);
        _context = new AcademicContext(options, tenantContext);
        _repository = new StudentResponseRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private class MockTenantContext : ITenantContext
    {
        public MockTenantContext(Guid? schoolId = null, Guid? userId = null)
        {
            SchoolId = schoolId;
            UserId = userId ?? Guid.NewGuid();
            Role = UserRole.Student;
            ClassIds = new List<Guid>();
            Email = "test@example.com";
            FullName = "Test User";
        }

        public Guid UserId { get; }
        public UserRole Role { get; }
        public Guid? SchoolId { get; }
        public IReadOnlyList<Guid> ClassIds { get; }
        public string Email { get; }
        public string FullName { get; }

        public bool HasAccessToSchool(Guid schoolId) => SchoolId == schoolId;
        public bool HasAccessToClass(Guid classId) => ClassIds.Contains(classId);
        public bool HasRole(UserRole minimumRole) => Role >= minimumRole;
    }

    private StudentResponse CreateTestResponse(
        Guid? studentAssessmentId = null,
        Guid? studentId = null,
        Guid? questionId = null,
        bool isCorrect = true,
        int timeSpentSeconds = 60,
        int questionOrder = 0)
    {
        return new StudentResponse
        {
            Id = Guid.NewGuid(),
            StudentAssessmentId = studentAssessmentId ?? Guid.NewGuid(),
            StudentId = studentId ?? Guid.NewGuid(),
            QuestionId = questionId ?? Guid.NewGuid(),
            SchoolId = _schoolId,
            StudentAnswer = "Test response",
            IsCorrect = isCorrect,
            PointsEarned = isCorrect ? 10 : 0,
            MaxPoints = 10,
            TimeSpentSeconds = timeSpentSeconds,
            QuestionOrder = questionOrder,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<StudentResponse> SeedResponseAsync(
        Guid? studentAssessmentId = null,
        Guid? studentId = null,
        Guid? questionId = null,
        bool isCorrect = true,
        int timeSpentSeconds = 60,
        int questionOrder = 0)
    {
        var response = CreateTestResponse(
            studentAssessmentId: studentAssessmentId,
            studentId: studentId,
            questionId: questionId,
            isCorrect: isCorrect,
            timeSpentSeconds: timeSpentSeconds,
            questionOrder: questionOrder);

        await _context.StudentResponses.AddAsync(response);
        await _context.SaveChangesAsync();
        return response;
    }

    // GetByStudentAssessmentIdAsync Tests

    [Fact]
    public async Task GetByStudentAssessmentIdAsync_WithResponses_ShouldReturnOrderedByQuestionOrder()
    {
        // Arrange
        await SeedResponseAsync(studentAssessmentId: _studentAssessmentId, questionOrder: 1);
        var response2 = CreateTestResponse(studentAssessmentId: _studentAssessmentId, questionOrder: 2);
        var response3 = CreateTestResponse(studentAssessmentId: _studentAssessmentId, questionOrder: 3);
        await _context.StudentResponses.AddRangeAsync(response2, response3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAssessmentIdAsync(_studentAssessmentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(3);
        responses[0].QuestionOrder.Should().Be(1);
        responses[1].QuestionOrder.Should().Be(2);
        responses[2].QuestionOrder.Should().Be(3);
    }

    [Fact]
    public async Task GetByStudentAssessmentIdAsync_WithNoResponses_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByStudentAssessmentIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().BeEmpty();
    }

    // GetByStudentIdAsync Tests

    [Fact]
    public async Task GetByStudentIdAsync_WithResponses_ShouldReturnOrderedBySubmittedAtDescending()
    {
        // Arrange
        var oldResponse = CreateTestResponse(studentId: _studentId);
        oldResponse = oldResponse with { SubmittedAt = DateTime.UtcNow.AddHours(-2) };
        var middleResponse = CreateTestResponse(studentId: _studentId);
        middleResponse = middleResponse with { SubmittedAt = DateTime.UtcNow.AddHours(-1) };
        var newResponse = CreateTestResponse(studentId: _studentId);

        await _context.StudentResponses.AddRangeAsync(oldResponse, middleResponse, newResponse);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentIdAsync(_studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(3);
        responses[0].Id.Should().Be(newResponse.Id); // Most recent first
        responses[1].Id.Should().Be(middleResponse.Id);
        responses[2].Id.Should().Be(oldResponse.Id);
    }

    [Fact]
    public async Task GetByStudentIdAsync_WithDifferentStudent_ShouldNotReturnOtherStudentResponses()
    {
        // Arrange
        await SeedResponseAsync(studentId: _studentId);
        var otherStudentId = Guid.NewGuid();
        await SeedResponseAsync(studentId: otherStudentId);

        // Act
        var result = await _repository.GetByStudentIdAsync(_studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().ContainSingle();
        responses[0].StudentId.Should().Be(_studentId);
    }

    // GetByQuestionIdAsync Tests

    [Fact]
    public async Task GetByQuestionIdAsync_WithMultipleResponses_ShouldReturnAll()
    {
        // Arrange
        await SeedResponseAsync(questionId: _questionId, studentId: Guid.NewGuid());
        await SeedResponseAsync(questionId: _questionId, studentId: Guid.NewGuid());
        await SeedResponseAsync(questionId: _questionId, studentId: Guid.NewGuid());

        // Act
        var result = await _repository.GetByQuestionIdAsync(_questionId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(3);
        responses.Should().AllSatisfy(r => r.QuestionId.Should().Be(_questionId));
    }

    [Fact]
    public async Task GetByQuestionIdAsync_WithDifferentQuestion_ShouldNotReturnOtherQuestionResponses()
    {
        // Arrange
        await SeedResponseAsync(questionId: _questionId);
        var otherQuestionId = Guid.NewGuid();
        await SeedResponseAsync(questionId: otherQuestionId);

        // Act
        var result = await _repository.GetByQuestionIdAsync(_questionId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().ContainSingle();
        responses[0].QuestionId.Should().Be(_questionId);
    }

    // GetByStudentAssessmentAndQuestionAsync Tests

    [Fact]
    public async Task GetByStudentAssessmentAndQuestionAsync_WithMatchingResponse_ShouldReturnResponse()
    {
        // Arrange
        var seededResponse = await SeedResponseAsync(
            studentAssessmentId: _studentAssessmentId,
            questionId: _questionId);

        // Act
        var result = await _repository.GetByStudentAssessmentAndQuestionAsync(
            _studentAssessmentId, _questionId);

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Success>();
        var response = ((Result<StudentResponse>.Success)result).Value;
        response.Id.Should().Be(seededResponse.Id);
    }

    [Fact]
    public async Task GetByStudentAssessmentAndQuestionAsync_WithNoMatch_ShouldReturnNotFound()
    {
        // Act
        var result = await _repository.GetByStudentAssessmentAndQuestionAsync(
            Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Failure>();
        var failure = ((Result<StudentResponse>.Failure)result);
        failure.Error.Code.Should().Be("NOT_FOUND");
    }

    // GetCorrectResponsesByStudentAsync Tests

    [Fact]
    public async Task GetCorrectResponsesByStudentAsync_WithMixedResponses_ShouldReturnOnlyCorrect()
    {
        // Arrange
        await SeedResponseAsync(studentId: _studentId, isCorrect: true);
        await SeedResponseAsync(studentId: _studentId, isCorrect: true);
        await SeedResponseAsync(studentId: _studentId, isCorrect: false);
        await SeedResponseAsync(studentId: _studentId, isCorrect: false);

        // Act
        var result = await _repository.GetCorrectResponsesByStudentAsync(_studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(2);
        responses.Should().AllSatisfy(r => r.IsCorrect.Should().BeTrue());
    }

    [Fact]
    public async Task GetCorrectResponsesByStudentAsync_WithNoCorrectResponses_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedResponseAsync(isCorrect: false);
        await SeedResponseAsync(isCorrect: false);

        // Act
        var result = await _repository.GetCorrectResponsesByStudentAsync(_studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().BeEmpty();
    }

    // GetIncorrectResponsesByStudentAsync Tests

    [Fact]
    public async Task GetIncorrectResponsesByStudentAsync_WithMixedResponses_ShouldReturnOnlyIncorrect()
    {
        // Arrange
        await SeedResponseAsync(studentId: _studentId, isCorrect: true);
        await SeedResponseAsync(studentId: _studentId, isCorrect: false);
        await SeedResponseAsync(studentId: _studentId, isCorrect: false);
        await SeedResponseAsync(studentId: _studentId, isCorrect: false);

        // Act
        var result = await _repository.GetIncorrectResponsesByStudentAsync(_studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(3);
        responses.Should().AllSatisfy(r => r.IsCorrect.Should().BeFalse());
    }

    // GetByTimeSpentRangeAsync Tests

    [Fact]
    public async Task GetByTimeSpentRangeAsync_WithinRange_ShouldReturnMatchingResponses()
    {
        // Arrange
        await SeedResponseAsync(timeSpentSeconds: 30);  // Below range
        await SeedResponseAsync(timeSpentSeconds: 60);  // In range
        await SeedResponseAsync(timeSpentSeconds: 90);  // In range
        await SeedResponseAsync(timeSpentSeconds: 150); // Above range

        // Act
        var result = await _repository.GetByTimeSpentRangeAsync(50, 100);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(2);
        responses.Should().AllSatisfy(r =>
        {
            r.TimeSpentSeconds.Should().BeGreaterOrEqualTo(50);
            r.TimeSpentSeconds.Should().BeLessOrEqualTo(100);
        });
    }

    [Fact]
    public async Task GetByTimeSpentRangeAsync_NoneInRange_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedResponseAsync(timeSpentSeconds: 30);
        await SeedResponseAsync(timeSpentSeconds: 200);

        // Act
        var result = await _repository.GetByTimeSpentRangeAsync(50, 100);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().BeEmpty();
    }

    // GetQuestionStatisticsAsync Tests - K-Anonymity Focus

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithFiveResponses_ShouldReturnStatistics()
    {
        // Arrange - Exactly 5 responses (k-anonymity threshold)
        var questionId = Guid.NewGuid();
        await SeedResponseAsync(questionId: questionId, isCorrect: true, timeSpentSeconds: 60);
        await SeedResponseAsync(questionId: questionId, isCorrect: true, timeSpentSeconds: 80);
        await SeedResponseAsync(questionId: questionId, isCorrect: false, timeSpentSeconds: 100);
        await SeedResponseAsync(questionId: questionId, isCorrect: true, timeSpentSeconds: 120);
        await SeedResponseAsync(questionId: questionId, isCorrect: false, timeSpentSeconds: 140);

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.QuestionId.Should().Be(questionId);
        stats.TotalResponses.Should().Be(5);
        stats.CorrectResponses.Should().Be(3);
        stats.SuccessRate.Should().BeApproximately(0.6, 0.01); // 3/5 = 60%
        stats.AverageTimeSeconds.Should().BeApproximately(100, 0.1); // (60+80+100+120+140)/5
        stats.MedianTimeSeconds.Should().Be(100); // Middle value
        stats.MeetsPrivacyThreshold.Should().BeTrue();
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithTenResponses_ShouldReturnStatistics()
    {
        // Arrange - Above threshold (10 responses)
        var questionId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            await SeedResponseAsync(
                questionId: questionId,
                isCorrect: i < 7, // 7 correct, 3 incorrect
                timeSpentSeconds: (i + 1) * 10); // 10, 20, 30, ..., 100
        }

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.TotalResponses.Should().Be(10);
        stats.CorrectResponses.Should().Be(7);
        stats.SuccessRate.Should().BeApproximately(0.7, 0.01); // 70%
        stats.AverageTimeSeconds.Should().BeApproximately(55, 0.1); // (10+20+...+100)/10
        stats.MedianTimeSeconds.Should().Be(55); // Average of 50 and 60
        stats.MeetsPrivacyThreshold.Should().BeTrue();
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithFourResponses_ShouldReturnForbidden()
    {
        // Arrange - Below k-anonymity threshold
        var questionId = Guid.NewGuid();
        await SeedResponseAsync(questionId: questionId);
        await SeedResponseAsync(questionId: questionId);
        await SeedResponseAsync(questionId: questionId);
        await SeedResponseAsync(questionId: questionId);

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Failure>();
        var failure = ((Result<QuestionStatistics>.Failure)result);
        failure.Error.Code.Should().Be("FORBIDDEN");
        failure.Error.Message.Should().Contain("minimum 5 responses required");
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithZeroResponses_ShouldReturnForbidden()
    {
        // Arrange - No responses
        var questionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Failure>();
        var failure = ((Result<QuestionStatistics>.Failure)result);
        failure.Error.Code.Should().Be("FORBIDDEN");
        failure.Error.Message.Should().Contain("minimum 5 responses required");
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithOddNumberOfResponses_ShouldCalculateMedianCorrectly()
    {
        // Arrange - 7 responses (odd number)
        var questionId = Guid.NewGuid();
        int[] times = { 20, 40, 60, 80, 100, 120, 140 }; // Median should be 80
        foreach (var time in times)
        {
            await SeedResponseAsync(questionId: questionId, timeSpentSeconds: time);
        }

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.MedianTimeSeconds.Should().Be(80); // Middle value in sorted list
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_WithEvenNumberOfResponses_ShouldAverageMiddleTwo()
    {
        // Arrange - 6 responses (even number)
        var questionId = Guid.NewGuid();
        int[] times = { 20, 40, 60, 80, 100, 120 }; // Median should be (60+80)/2 = 70
        foreach (var time in times)
        {
            await SeedResponseAsync(questionId: questionId, timeSpentSeconds: time);
        }

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.MedianTimeSeconds.Should().Be(70); // Average of 60 and 80
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_AllCorrectResponses_ShouldReturn100PercentSuccess()
    {
        // Arrange - All correct
        var questionId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            await SeedResponseAsync(questionId: questionId, isCorrect: true);
        }

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.SuccessRate.Should().Be(1.0); // 100%
        stats.CorrectResponses.Should().Be(5);
    }

    [Fact]
    public async Task GetQuestionStatisticsAsync_AllIncorrectResponses_ShouldReturn0PercentSuccess()
    {
        // Arrange - All incorrect
        var questionId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            await SeedResponseAsync(questionId: questionId, isCorrect: false);
        }

        // Act
        var result = await _repository.GetQuestionStatisticsAsync(questionId);

        // Assert
        result.Should().BeOfType<Result<QuestionStatistics>.Success>();
        var stats = ((Result<QuestionStatistics>.Success)result).Value;
        stats.SuccessRate.Should().Be(0.0); // 0%
        stats.CorrectResponses.Should().Be(0);
    }

    // CRUD Operations Tests

    [Fact]
    public async Task AddAsync_ValidResponse_ShouldAddSuccessfully()
    {
        // Arrange
        var newResponse = CreateTestResponse();

        // Act
        var result = await _repository.AddAsync(newResponse);

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Success>();
        var addedResponse = ((Result<StudentResponse>.Success)result).Value;
        addedResponse.Id.Should().Be(newResponse.Id);

        // Verify in database
        var dbResponse = await _context.StudentResponses.FindAsync(newResponse.Id);
        dbResponse.Should().NotBeNull();
        dbResponse!.StudentAnswer.Should().Be("Test response");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingResponse_ShouldReturnResponse()
    {
        // Arrange
        var seededResponse = await SeedResponseAsync();

        // Act
        var result = await _repository.GetByIdAsync(seededResponse.Id);

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Success>();
        var response = ((Result<StudentResponse>.Success)result).Value;
        response.Id.Should().Be(seededResponse.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentResponse_ShouldReturnNotFound()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Failure>();
        var failure = ((Result<StudentResponse>.Failure)result);
        failure.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_ExistingResponse_ShouldUpdateSuccessfully()
    {
        // Arrange
        var seededResponse = await SeedResponseAsync();
        _context.Entry(seededResponse).State = EntityState.Detached;
        var updatedResponse = seededResponse with { StudentAnswer = "Updated response" };

        // Act
        var result = await _repository.UpdateAsync(updatedResponse);

        // Assert
        result.Should().BeOfType<Result<StudentResponse>.Success>();

        // Verify in database
        var dbResponse = await _context.StudentResponses.FindAsync(seededResponse.Id);
        dbResponse!.StudentAnswer.Should().Be("Updated response");
    }

    [Fact]
    public async Task DeleteAsync_ExistingResponse_ShouldDeleteSuccessfully()
    {
        // Arrange
        var seededResponse = await SeedResponseAsync();

        // Act
        var result = await _repository.DeleteAsync(seededResponse.Id);

        // Assert
        result.Should().BeOfType<Result<Core.Common.Unit>.Success>();

        // Verify removed from database
        var dbResponse = await _context.StudentResponses.FindAsync(seededResponse.Id);
        dbResponse.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentResponse_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeOfType<Result<Core.Common.Unit>.Failure>();
        var failure = ((Result<Core.Common.Unit>.Failure)result);
        failure.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleResponses_ShouldReturnAll()
    {
        // Arrange
        await SeedResponseAsync();
        await SeedResponseAsync();
        await SeedResponseAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<StudentResponse>>.Success>();
        var responses = ((Result<IReadOnlyList<StudentResponse>>.Success)result).Value;
        responses.Should().HaveCount(3);
    }
}
