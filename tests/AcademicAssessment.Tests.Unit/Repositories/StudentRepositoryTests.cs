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

public class StudentRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IStudentRepository _repository;
    private readonly Guid _schoolId = Guid.NewGuid();

    public StudentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"StudentRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext(_schoolId);
        _context = new AcademicContext(options, tenantContext);
        _repository = new StudentRepository(_context);
    }

    private class MockTenantContext : ITenantContext
    {
        public MockTenantContext(Guid? schoolId = null)
        {
            SchoolId = schoolId;
            UserId = Guid.NewGuid();
            // Use BusinessAdmin to bypass tenant filters in tests
            Role = UserRole.BusinessAdmin;
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private Student CreateTestStudent(
        Guid? schoolId = null,
        GradeLevel gradeLevel = GradeLevel.Grade10,
        SubscriptionTier tier = SubscriptionTier.Free,
        DateOnly? dateOfBirth = null,
        bool parentalConsent = false,
        int xpPoints = 0,
        List<Guid>? classIds = null,
        DateTimeOffset? subscriptionExpiresAt = null)
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SchoolId = schoolId,
            GradeLevel = gradeLevel,
            SubscriptionTier = tier,
            DateOfBirth = dateOfBirth,
            ParentalConsentGranted = parentalConsent,
            XpPoints = xpPoints,
            ClassIds = classIds?.AsReadOnly() ?? new List<Guid>().AsReadOnly(),
            SubscriptionExpiresAt = subscriptionExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<Student> SeedStudentAsync(
        Guid? schoolId = null,
        GradeLevel gradeLevel = GradeLevel.Grade10,
        SubscriptionTier tier = SubscriptionTier.Free,
        DateOnly? dateOfBirth = null,
        bool parentalConsent = false,
        int xpPoints = 0,
        List<Guid>? classIds = null,
        DateTimeOffset? subscriptionExpiresAt = null)
    {
        var student = CreateTestStudent(schoolId, gradeLevel, tier, dateOfBirth, parentalConsent, xpPoints, classIds, subscriptionExpiresAt);
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        return student;
    }

    [Fact]
    public async Task AddAsync_ShouldAddStudent()
    {
        var student = CreateTestStudent();
        var result = await _repository.AddAsync(student);
        result.Should().BeOfType<Result<Student>.Success>();
        var savedStudent = ((Result<Student>.Success)result).Value;
        savedStudent.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnStudent_WhenExists()
    {
        var student = await SeedStudentAsync();
        var result = await _repository.GetByIdAsync(student.Id);
        result.Should().BeOfType<Result<Student>.Success>();
        var retrieved = ((Result<Student>.Success)result).Value;
        retrieved.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeOfType<Result<Student>.Failure>();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnStudent_WhenExists()
    {
        var student = await SeedStudentAsync();
        var result = await _repository.GetByUserIdAsync(student.UserId);
        result.Should().BeOfType<Result<Student>.Success>();
        var retrieved = ((Result<Student>.Success)result).Value;
        retrieved.UserId.Should().Be(student.UserId);
    }

    [Fact]
    public async Task GetByClassIdAsync_ShouldReturnStudentsInClass()
    {
        var classId = Guid.NewGuid();
        await SeedStudentAsync(classIds: new List<Guid> { classId });
        await SeedStudentAsync(classIds: new List<Guid> { classId });
        await SeedStudentAsync(classIds: new List<Guid> { Guid.NewGuid() });
        var result = await _repository.GetByClassIdAsync(classId);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBySchoolIdAsync_ShouldReturnSchoolStudents()
    {
        await SeedStudentAsync(schoolId: _schoolId);
        await SeedStudentAsync(schoolId: _schoolId);
        await SeedStudentAsync(schoolId: null);
        var result = await _repository.GetBySchoolIdAsync(_schoolId);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSelfServiceStudentsAsync_ShouldReturnOnlySelfServiceStudents()
    {
        await SeedStudentAsync(schoolId: null);
        await SeedStudentAsync(schoolId: null);
        await SeedStudentAsync(schoolId: _schoolId);
        var result = await _repository.GetSelfServiceStudentsAsync();
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByGradeLevelAsync_ShouldReturnMatchingGrade()
    {
        await SeedStudentAsync(gradeLevel: GradeLevel.Grade9);
        await SeedStudentAsync(gradeLevel: GradeLevel.Grade9);
        await SeedStudentAsync(gradeLevel: GradeLevel.Grade10);
        var result = await _repository.GetByGradeLevelAsync(GradeLevel.Grade9);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBySubscriptionTierAsync_ShouldReturnMatchingTier()
    {
        await SeedStudentAsync(schoolId: null, tier: SubscriptionTier.Premium);
        await SeedStudentAsync(schoolId: null, tier: SubscriptionTier.Premium);
        await SeedStudentAsync(schoolId: null, tier: SubscriptionTier.Free);
        var result = await _repository.GetBySubscriptionTierAsync(SubscriptionTier.Premium);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_ShouldReturnExpiringSoon()
    {
        var inFiveDays = DateTimeOffset.UtcNow.AddDays(5);
        await SeedStudentAsync(schoolId: null, subscriptionExpiresAt: inFiveDays);
        await SeedStudentAsync(schoolId: null, subscriptionExpiresAt: DateTimeOffset.UtcNow.AddDays(10));
        var result = await _repository.GetExpiringSubscriptionsAsync(7);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRequiringCoppaConsentAsync_ShouldReturnUnder13WithoutConsent()
    {
        var under13 = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        await SeedStudentAsync(schoolId: null, dateOfBirth: under13, parentalConsent: false);
        await SeedStudentAsync(schoolId: null, dateOfBirth: under13, parentalConsent: true);
        await SeedStudentAsync(schoolId: null, dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));
        var result = await _repository.GetRequiringCoppaConsentAsync();
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTopByXpAsync_ShouldReturnTopByXp()
    {
        await SeedStudentAsync(schoolId: null, xpPoints: 1000);
        await SeedStudentAsync(schoolId: null, xpPoints: 500);
        await SeedStudentAsync(schoolId: null, xpPoints: 2000);
        var result = await _repository.GetTopByXpAsync(2);
        result.Should().BeOfType<Result<IReadOnlyList<Student>>.Success>();
        var students = ((Result<IReadOnlyList<Student>>.Success)result).Value;
        students.Should().HaveCount(2);
        students.First().XpPoints.Should().Be(2000);
    }
}
