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

public class UserRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IUserRepository _repository;
    private readonly Guid _schoolId = Guid.NewGuid();

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"UserRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext();
        _context = new AcademicContext(options, tenantContext);
        _repository = new UserRepository(_context);
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

    private User CreateTestUser(
        string? email = null,
        string? externalId = null,
        UserRole role = UserRole.Teacher,
        Guid? schoolId = null,
        bool isActive = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"user{Guid.NewGuid()}@test.com",
            FullName = $"Test User {Guid.NewGuid()}",
            ExternalId = externalId ?? $"ext_{Guid.NewGuid()}",
            Role = role,
            SchoolId = schoolId,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SeedUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUser()
    {
        var user = CreateTestUser();

        var result = await _repository.AddAsync(user);

        result.Should().BeOfType<Result<User>.Success>();
        var retrieved = await _repository.GetByIdAsync(user.Id);
        retrieved.Should().BeOfType<Result<User>.Success>();
        var actualUser = ((Result<User>.Success)retrieved).Value;
        actualUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        var user = CreateTestUser();
        await SeedUserAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);

        result.Should().BeOfType<Result<User>.Success>();
        var actualUser = ((Result<User>.Success)result).Value;
        actualUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<Result<User>.Failure>();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
    {
        var user = CreateTestUser(email: "john.doe@test.com");
        await SeedUserAsync(user);

        var result = await _repository.GetByEmailAsync("john.doe@test.com");

        result.Should().BeOfType<Result<User>.Success>();
        var actualUser = ((Result<User>.Success)result).Value;
        actualUser.Email.Should().Be("john.doe@test.com");
    }

    [Fact]
    public async Task GetByExternalIdAsync_ShouldReturnUser_WhenExists()
    {
        var user = CreateTestUser(externalId: "ext_12345");
        await SeedUserAsync(user);

        var result = await _repository.GetByExternalIdAsync("ext_12345");

        result.Should().BeOfType<Result<User>.Success>();
        var actualUser = ((Result<User>.Success)result).Value;
        actualUser.ExternalId.Should().Be("ext_12345");
    }

    [Fact]
    public async Task GetBySchoolIdAsync_ShouldReturnUsersForSchool()
    {
        var schoolUsers = new[] {
            CreateTestUser(schoolId: _schoolId),
            CreateTestUser(schoolId: _schoolId)
        };
        var otherUser = CreateTestUser(schoolId: Guid.NewGuid());

        foreach (var u in schoolUsers) await SeedUserAsync(u);
        await SeedUserAsync(otherUser);

        var result = await _repository.GetBySchoolIdAsync(_schoolId);

        result.Should().BeOfType<Result<IReadOnlyList<User>>.Success>();
        var users = ((Result<IReadOnlyList<User>>.Success)result).Value;
        users.Should().HaveCount(2);
        users.Should().AllSatisfy(u => u.SchoolId.Should().Be(_schoolId));
    }

    [Fact]
    public async Task GetByRoleAsync_ShouldReturnUsersWithRole()
    {
        var teachers = new[] {
            CreateTestUser(role: UserRole.Teacher),
            CreateTestUser(role: UserRole.Teacher)
        };
        var admin = CreateTestUser(role: UserRole.SchoolAdmin);

        foreach (var t in teachers) await SeedUserAsync(t);
        await SeedUserAsync(admin);

        var result = await _repository.GetByRoleAsync(UserRole.Teacher);

        result.Should().BeOfType<Result<IReadOnlyList<User>>.Success>();
        var users = ((Result<IReadOnlyList<User>>.Success)result).Value;
        users.Should().HaveCount(2);
        users.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Teacher));
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        var activeUsers = new[] {
            CreateTestUser(isActive: true),
            CreateTestUser(isActive: true)
        };
        var inactiveUser = CreateTestUser(isActive: false);

        foreach (var u in activeUsers) await SeedUserAsync(u);
        await SeedUserAsync(inactiveUser);

        var result = await _repository.GetActiveUsersAsync();

        result.Should().BeOfType<Result<IReadOnlyList<User>>.Success>();
        var users = ((Result<IReadOnlyList<User>>.Success)result).Value;
        users.Should().HaveCount(2);
        users.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task IsEmailInUseAsync_ShouldReturnTrue_WhenEmailExists()
    {
        var user = CreateTestUser(email: "existing@test.com");
        await SeedUserAsync(user);

        var result = await _repository.IsEmailInUseAsync("existing@test.com");

        result.Should().BeOfType<Result<bool>.Success>();
        var isInUse = ((Result<bool>.Success)result).Value;
        isInUse.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailInUseAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        var result = await _repository.IsEmailInUseAsync("nonexistent@test.com");

        result.Should().BeOfType<Result<bool>.Success>();
        var isInUse = ((Result<bool>.Success)result).Value;
        isInUse.Should().BeFalse();
    }
}
