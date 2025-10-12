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

public class SchoolRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly ISchoolRepository _repository;

    public SchoolRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"SchoolRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext();
        _context = new AcademicContext(options, tenantContext);
        _repository = new SchoolRepository(_context);
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

    private School CreateTestSchool(
        string? code = null,
        string? name = null,
        bool isActive = true,
        DateTimeOffset? createdAt = null)
    {
        return new School
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Test School {Guid.NewGuid()}",
            Code = code ?? $"SCH{Guid.NewGuid().ToString()[..4]}",
            Address = "123 Test St",
            ContactEmail = "contact@testschool.edu",
            ContactPhone = "555-0100",
            IsActive = isActive,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SeedSchoolAsync(School school)
    {
        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAddSchool()
    {
        var school = CreateTestSchool();

        var result = await _repository.AddAsync(school);

        result.Should().BeOfType<Result<School>.Success>();
        var retrieved = await _repository.GetByIdAsync(school.Id);
        retrieved.Should().BeOfType<Result<School>.Success>();
        var actualSchool = ((Result<School>.Success)retrieved).Value;
        actualSchool.Name.Should().Be(school.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSchool_WhenExists()
    {
        var school = CreateTestSchool();
        await SeedSchoolAsync(school);

        var result = await _repository.GetByIdAsync(school.Id);

        result.Should().BeOfType<Result<School>.Success>();
        var actualSchool = ((Result<School>.Success)result).Value;
        actualSchool.Name.Should().Be(school.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<Result<School>.Failure>();
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnSchool_WhenExists()
    {
        var school = CreateTestSchool(code: "SCH001");
        await SeedSchoolAsync(school);

        var result = await _repository.GetByCodeAsync("SCH001");

        result.Should().BeOfType<Result<School>.Success>();
        var actualSchool = ((Result<School>.Success)result).Value;
        actualSchool.Code.Should().Be("SCH001");
    }

    [Fact]
    public async Task GetActiveSchoolsAsync_ShouldReturnOnlyActiveSchools()
    {
        var activeSchools = new[] {
            CreateTestSchool(isActive: true),
            CreateTestSchool(isActive: true)
        };
        var inactiveSchool = CreateTestSchool(isActive: false);

        foreach (var s in activeSchools) await SeedSchoolAsync(s);
        await SeedSchoolAsync(inactiveSchool);

        var result = await _repository.GetActiveSchoolsAsync();

        result.Should().BeOfType<Result<IReadOnlyList<School>>.Success>();
        var schools = ((Result<IReadOnlyList<School>>.Success)result).Value;
        schools.Should().HaveCount(2);
        schools.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task IsCodeInUseAsync_ShouldReturnTrue_WhenCodeExists()
    {
        var school = CreateTestSchool(code: "SCH123");
        await SeedSchoolAsync(school);

        var result = await _repository.IsCodeInUseAsync("SCH123");

        result.Should().BeOfType<Result<bool>.Success>();
        var isInUse = ((Result<bool>.Success)result).Value;
        isInUse.Should().BeTrue();
    }

    [Fact]
    public async Task IsCodeInUseAsync_ShouldReturnFalse_WhenCodeDoesNotExist()
    {
        var result = await _repository.IsCodeInUseAsync("NONEXISTENT");

        result.Should().BeOfType<Result<bool>.Success>();
        var isInUse = ((Result<bool>.Success)result).Value;
        isInUse.Should().BeFalse();
    }

    [Fact]
    public async Task GetSchoolsByDateRangeAsync_ShouldReturnSchoolsInRange()
    {
        var now = DateTimeOffset.UtcNow;
        var schools = new[] {
            CreateTestSchool(createdAt: now.AddDays(-10)),
            CreateTestSchool(createdAt: now.AddDays(-5)),
            CreateTestSchool(createdAt: now.AddDays(-1))
        };

        foreach (var s in schools) await SeedSchoolAsync(s);

        var result = await _repository.GetSchoolsByDateRangeAsync(
            now.AddDays(-7),
            now);

        result.Should().BeOfType<Result<IReadOnlyList<School>>.Success>();
        var matchingSchools = ((Result<IReadOnlyList<School>>.Success)result).Value;
        matchingSchools.Should().HaveCount(2);
    }
}
