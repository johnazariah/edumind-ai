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

public class CourseRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly ICourseRepository _repository;
    private readonly Guid _courseAdminId = Guid.NewGuid();

    public CourseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"CourseRepositoryTests_{Guid.NewGuid()}")
            .Options;

        var tenantContext = new MockTenantContext();
        _context = new AcademicContext(options, tenantContext);
        _repository = new CourseRepository(_context);
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

    private Course CreateTestCourse(
        string? code = null,
        Subject subject = Subject.Mathematics,
        GradeLevel gradeLevel = GradeLevel.Grade10,
        bool isActive = true,
        Guid? courseAdminId = null,
        List<string>? topics = null,
        List<string>? learningObjectives = null)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Name = $"Course {Guid.NewGuid()}",
            Code = code ?? $"MATH-{Guid.NewGuid().ToString()[..4]}",
            Subject = subject,
            GradeLevel = gradeLevel,
            Description = "Test course description",
            IsActive = isActive,
            CourseAdminId = courseAdminId,
            Topics = topics ?? new List<string>(),
            LearningObjectives = learningObjectives ?? new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SeedCourseAsync(Course course)
    {
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAddCourse()
    {
        var course = CreateTestCourse();

        var result = await _repository.AddAsync(course);

        result.Should().BeOfType<Result<Course>.Success>();
        var retrieved = await _repository.GetByIdAsync(course.Id);
        retrieved.Should().BeOfType<Result<Course>.Success>();
        var actualCourse = ((Result<Course>.Success)retrieved).Value;
        actualCourse.Name.Should().Be(course.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCourse_WhenExists()
    {
        var course = CreateTestCourse();
        await SeedCourseAsync(course);

        var result = await _repository.GetByIdAsync(course.Id);

        result.Should().BeOfType<Result<Course>.Success>();
        var actualCourse = ((Result<Course>.Success)result).Value;
        actualCourse.Name.Should().Be(course.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeOfType<Result<Course>.Failure>();
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnCourse_WhenExists()
    {
        var course = CreateTestCourse(code: "MATH-101");
        await SeedCourseAsync(course);

        var result = await _repository.GetByCodeAsync("MATH-101");

        result.Should().BeOfType<Result<Course>.Success>();
        var actualCourse = ((Result<Course>.Success)result).Value;
        actualCourse.Code.Should().Be("MATH-101");
    }

    [Fact]
    public async Task GetBySubjectAsync_ShouldReturnCoursesForSubject()
    {
        var mathCourses = new[] {
            CreateTestCourse(subject: Subject.Mathematics),
            CreateTestCourse(subject: Subject.Mathematics)
        };
        var physicsCourse = CreateTestCourse(subject: Subject.Physics);

        foreach (var c in mathCourses) await SeedCourseAsync(c);
        await SeedCourseAsync(physicsCourse);

        var result = await _repository.GetBySubjectAsync(Subject.Mathematics);

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c => c.Subject.Should().Be(Subject.Mathematics));
    }

    [Fact]
    public async Task GetByGradeLevelAsync_ShouldReturnCoursesForGrade()
    {
        var grade10Courses = new[] {
            CreateTestCourse(gradeLevel: GradeLevel.Grade10),
            CreateTestCourse(gradeLevel: GradeLevel.Grade10)
        };
        var grade11Course = CreateTestCourse(gradeLevel: GradeLevel.Grade11);

        foreach (var c in grade10Courses) await SeedCourseAsync(c);
        await SeedCourseAsync(grade11Course);

        var result = await _repository.GetByGradeLevelAsync(GradeLevel.Grade10);

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c => c.GradeLevel.Should().Be(GradeLevel.Grade10));
    }

    [Fact]
    public async Task GetBySubjectAndGradeLevelAsync_ShouldReturnMatchingCourses()
    {
        var matchingCourses = new[] {
            CreateTestCourse(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade10),
            CreateTestCourse(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade10)
        };
        var nonMatchingCourses = new[] {
            CreateTestCourse(subject: Subject.Physics, gradeLevel: GradeLevel.Grade10),
            CreateTestCourse(subject: Subject.Mathematics, gradeLevel: GradeLevel.Grade11)
        };

        foreach (var c in matchingCourses) await SeedCourseAsync(c);
        foreach (var c in nonMatchingCourses) await SeedCourseAsync(c);

        var result = await _repository.GetBySubjectAndGradeLevelAsync(Subject.Mathematics, GradeLevel.Grade10);

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c =>
        {
            c.Subject.Should().Be(Subject.Mathematics);
            c.GradeLevel.Should().Be(GradeLevel.Grade10);
        });
    }

    [Fact]
    public async Task GetActiveCoursesAsync_ShouldReturnOnlyActiveCourses()
    {
        var activeCourses = new[] {
            CreateTestCourse(isActive: true),
            CreateTestCourse(isActive: true)
        };
        var inactiveCourse = CreateTestCourse(isActive: false);

        foreach (var c in activeCourses) await SeedCourseAsync(c);
        await SeedCourseAsync(inactiveCourse);

        var result = await _repository.GetActiveCoursesAsync();

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetByCourseAdminAsync_ShouldReturnCoursesForAdmin()
    {
        var adminCourses = new[] {
            CreateTestCourse(courseAdminId: _courseAdminId),
            CreateTestCourse(courseAdminId: _courseAdminId)
        };
        var otherCourse = CreateTestCourse(courseAdminId: Guid.NewGuid());

        foreach (var c in adminCourses) await SeedCourseAsync(c);
        await SeedCourseAsync(otherCourse);

        var result = await _repository.GetByCourseAdminAsync(_courseAdminId);

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c => c.CourseAdminId.Should().Be(_courseAdminId));
    }

    [Fact(Skip = "EF Core InMemory provider doesn't support querying JSON-serialized collections")]
    public async Task SearchByTopicAsync_ShouldReturnCoursesWithTopic()
    {
        var algebraCourses = new[] {
            CreateTestCourse(topics: new List<string> { "Algebra", "Equations" }),
            CreateTestCourse(topics: new List<string> { "Algebra", "Functions" })
        };
        var geometryCourse = CreateTestCourse(topics: new List<string> { "Geometry", "Triangles" });

        foreach (var c in algebraCourses) await SeedCourseAsync(c);
        await SeedCourseAsync(geometryCourse);

        var result = await _repository.SearchByTopicAsync("Algebra");

        result.Should().BeOfType<Result<IReadOnlyList<Course>>.Success>();
        var courses = ((Result<IReadOnlyList<Course>>.Success)result).Value;
        courses.Should().HaveCount(2);
        courses.Should().AllSatisfy(c => c.Topics.Should().Contain("Algebra"));
    }
}
