using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using AcademicAssessment.Infrastructure.Context;
using AcademicAssessment.Infrastructure.Data;
using AcademicAssessment.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AcademicAssessment.Tests.Unit.Repositories;

/// <summary>
/// Simplified tests for StudentAssessmentRepository focusing on k-anonymity enforcement
/// </summary>
public class StudentAssessmentRepositoryTests_Simple : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IStudentAssessmentRepository _repository;
    private readonly Guid _testSchoolId = Guid.NewGuid();

    public StudentAssessmentRepositoryTests_Simple()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new TenantContext
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.SystemAdmin,
            SchoolId = _testSchoolId
        };

        _context = new AcademicContext(options, tenantContext);
        _repository = new StudentAssessmentRepository(_context);
    }

    private StudentAssessment CreateCompletedAssessment(Guid assessmentId, int score)
    {
        return new StudentAssessment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            AssessmentId = assessmentId,
            SchoolId = _testSchoolId,
            Status = AssessmentStatus.Completed,
            Score = score,
            MaxScore = 100,
            Passed = score >= 70,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task GetAverageScoreAsync_With4Students_ReturnsError()
    {
        // Arrange - 4 students (below 5-record threshold)
        var assessmentId = Guid.NewGuid();
        for (int i = 0; i < 4; i++)
        {
            await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, 80 + i));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAverageScoreAsync(assessmentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Result<double>.Failure>();
        var failure = (Result<double>.Failure)result;
        failure.Error.Code.Should().Be("FORBIDDEN");
        failure.Error.Message.Should().Contain("minimum 5 students required");
    }

    [Fact]
    public async Task GetAverageScoreAsync_With5Students_ReturnsAverage()
    {
        // Arrange - Exactly 5 students (at threshold)
        var assessmentId = Guid.NewGuid();
        var scores = new[] { 80, 85, 90, 95, 100 };
        foreach (var score in scores)
        {
            await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, score));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAverageScoreAsync(assessmentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeOfType<Result<double>.Success>();
        var success = (Result<double>.Success)result;
        success.Value.Should().Be(90.0);
    }

    [Fact]
    public async Task GetPassRateAsync_With4Students_ReturnsError()
    {
        // Arrange - 4 students (below threshold)
        var assessmentId = Guid.NewGuid();
        for (int i = 0; i < 4; i++)
        {
            await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, 80));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPassRateAsync(assessmentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        var failure = (Result<double>.Failure)result;
        failure.Error.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task GetPassRateAsync_With5Students_ReturnsRate()
    {
        // Arrange - 5 students, 4 passed
        var assessmentId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            var score = i < 4 ? 80 : 60; // First 4 pass (score >= 70), last one fails
            await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, score));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPassRateAsync(assessmentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var success = (Result<double>.Success)result;
        success.Value.Should().Be(80.0); // 4/5 = 80%
    }

    [Fact]
    public async Task RealWorld_Adding5thStudent_EnablesAggregates()
    {
        // Arrange - Start with 4 students
        var assessmentId = Guid.NewGuid();
        for (int i = 0; i < 4; i++)
        {
            await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, 85));
        }
        await _context.SaveChangesAsync();

        // Act & Assert - Should fail with 4 students
        var result1 = await _repository.GetAverageScoreAsync(assessmentId);
        result1.IsFailure.Should().BeTrue();

        // Add 5th student
        await _context.StudentAssessments.AddAsync(CreateCompletedAssessment(assessmentId, 90));
        await _context.SaveChangesAsync();

        // Act & Assert - Should succeed with 5 students
        var result2 = await _repository.GetAverageScoreAsync(assessmentId);
        result2.IsSuccess.Should().BeTrue();
        var success = (Result<double>.Success)result2;
        success.Value.Should().BeApproximately(86.0, 0.1); // (85*4 + 90) / 5
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
