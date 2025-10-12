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

/// <summary>
/// Tests for ClassRepository with focus on k-anonymity privacy features
/// </summary>
public class ClassRepositoryTests : IDisposable
{
    private readonly AcademicContext _context;
    private readonly IClassRepository _repository;
    private readonly Guid _schoolId = Guid.NewGuid();

    public ClassRepositoryTests()
    {
        // In-memory database for testing
        var options = new DbContextOptionsBuilder<AcademicContext>()
            .UseInMemoryDatabase(databaseName: $"ClassRepositoryTests_{Guid.NewGuid()}")
            .Options;

        // Create a mock tenant context
        var tenantContext = new MockTenantContext(_schoolId);
        _context = new AcademicContext(options, tenantContext);
        _repository = new ClassRepository(_context);
    }

    // Mock tenant context for testing
    private class MockTenantContext : ITenantContext
    {
        public MockTenantContext(Guid? schoolId = null, Guid? userId = null)
        {
            SchoolId = schoolId;
            UserId = userId ?? Guid.NewGuid();
            Role = UserRole.Teacher;
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

    #region Helper Methods

    private Class CreateTestClass(
        string name = "Test Class",
        Subject subject = Subject.Mathematics,
        GradeLevel gradeLevel = GradeLevel.Grade9,
        string code = "MATH9A",
        string academicYear = "2024-2025",
        int studentCount = 0,
        Guid? teacherId = null)
    {
        var studentIds = Enumerable.Range(0, studentCount)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var teacherIds = teacherId.HasValue
            ? new List<Guid> { teacherId.Value }
            : new List<Guid> { Guid.NewGuid() };

        return new Class
        {
            Id = Guid.NewGuid(),
            SchoolId = _schoolId,
            Name = name,
            Code = code,
            Subject = subject,
            GradeLevel = gradeLevel,
            AcademicYear = academicYear,
            TeacherIds = teacherIds.AsReadOnly(),
            StudentIds = studentIds.AsReadOnly(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<Class> SeedClassAsync(
        string name = "Test Class",
        Subject subject = Subject.Mathematics,
        GradeLevel gradeLevel = GradeLevel.Grade9,
        string code = "MATH9A",
        string academicYear = "2024-2025",
        int studentCount = 0,
        Guid? teacherId = null)
    {
        var classEntity = CreateTestClass(name, subject, gradeLevel, code, academicYear, studentCount, teacherId);
        await _context.Classes.AddAsync(classEntity);
        await _context.SaveChangesAsync();
        return classEntity;
    }

    #endregion

    #region GetBySchoolIdAsync Tests

    [Fact]
    public async Task GetBySchoolIdAsync_WithExistingClasses_ShouldReturnAll()
    {
        // Arrange
        await SeedClassAsync("Math 9A", code: "MATH9A");
        await SeedClassAsync("Math 9B", code: "MATH9B");
        await SeedClassAsync("Physics 10A", Subject.Physics, GradeLevel.Grade10, code: "PHYS10A");

        // Act
        var result = await _repository.GetBySchoolIdAsync(_schoolId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(3);
        classes.Should().AllSatisfy(c => c.SchoolId.Should().Be(_schoolId));
    }

    [Fact]
    public async Task GetBySchoolIdAsync_WithNoClasses_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetBySchoolIdAsync(_schoolId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySchoolIdAsync_WithDifferentSchool_ShouldNotReturnOtherSchoolClasses()
    {
        // Arrange
        var otherSchoolId = Guid.NewGuid();
        await SeedClassAsync("Math 9A");

        // Seed class for different school
        var otherClass = CreateTestClass("Other School Class");
        var otherSchoolClass = otherClass with { SchoolId = otherSchoolId };
        await _context.Classes.AddAsync(otherSchoolClass);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySchoolIdAsync(_schoolId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().ContainSingle();
        classes[0].SchoolId.Should().Be(_schoolId);
    }

    #endregion

    #region GetByTeacherIdAsync Tests

    [Fact]
    public async Task GetByTeacherIdAsync_WithAssignedClasses_ShouldReturnTeacherClasses()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        await SeedClassAsync("Math 9A", teacherId: teacherId);
        await SeedClassAsync("Math 9B", teacherId: teacherId);
        await SeedClassAsync("Physics 10A", Subject.Physics, teacherId: Guid.NewGuid()); // Different teacher

        // Act
        var result = await _repository.GetByTeacherIdAsync(teacherId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(2);
        classes.Should().AllSatisfy(c => c.TeacherIds.Should().Contain(teacherId));
    }

    [Fact]
    public async Task GetByTeacherIdAsync_WithNoAssignedClasses_ShouldReturnEmptyList()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        await SeedClassAsync("Math 9A", teacherId: Guid.NewGuid());

        // Act
        var result = await _repository.GetByTeacherIdAsync(teacherId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    #endregion

    #region GetByStudentIdAsync Tests

    [Fact]
    public async Task GetByStudentIdAsync_WithEnrolledClasses_ShouldReturnStudentClasses()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var class1 = CreateTestClass("Math 9A", code: "MATH9A", studentCount: 0);
        var class1WithStudent = class1.AddStudent(studentId);

        var class2 = CreateTestClass("Physics 10A", Subject.Physics, GradeLevel.Grade10, code: "PHYS10A", studentCount: 0);
        var class2WithStudent = class2.AddStudent(studentId);

        var class3 = CreateTestClass("Chemistry 10A", Subject.Chemistry, GradeLevel.Grade10, code: "CHEM10A", studentCount: 0);

        await _context.Classes.AddAsync(class1WithStudent);
        await _context.Classes.AddAsync(class2WithStudent);
        await _context.Classes.AddAsync(class3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentIdAsync(studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(2);
        classes.Should().AllSatisfy(c => c.StudentIds.Should().Contain(studentId));
    }

    [Fact]
    public async Task GetByStudentIdAsync_WithNoEnrollment_ShouldReturnEmptyList()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        await SeedClassAsync("Math 9A", studentCount: 5); // Students but not our studentId

        // Act
        var result = await _repository.GetByStudentIdAsync(studentId);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    #endregion

    #region GetBySubjectAndGradeLevelAsync Tests

    [Fact]
    public async Task GetBySubjectAndGradeLevelAsync_WithMatchingClasses_ShouldReturnFiltered()
    {
        // Arrange
        await SeedClassAsync("Math 9A", Subject.Mathematics, GradeLevel.Grade9, code: "MATH9A");
        await SeedClassAsync("Math 9B", Subject.Mathematics, GradeLevel.Grade9, code: "MATH9B");
        await SeedClassAsync("Math 10A", Subject.Mathematics, GradeLevel.Grade10, code: "MATH10A");
        await SeedClassAsync("Physics 9A", Subject.Physics, GradeLevel.Grade9, code: "PHYS9A");

        // Act
        var result = await _repository.GetBySubjectAndGradeLevelAsync(Subject.Mathematics, GradeLevel.Grade9);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(2);
        classes.Should().AllSatisfy(c =>
        {
            c.Subject.Should().Be(Subject.Mathematics);
            c.GradeLevel.Should().Be(GradeLevel.Grade9);
        });
    }

    [Fact]
    public async Task GetBySubjectAndGradeLevelAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedClassAsync("Math 9A", Subject.Mathematics, GradeLevel.Grade9);

        // Act
        var result = await _repository.GetBySubjectAndGradeLevelAsync(Subject.Chemistry, GradeLevel.Grade12);

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    #endregion

    #region GetByAcademicYearAsync Tests

    [Fact]
    public async Task GetByAcademicYearAsync_WithMatchingYear_ShouldReturnClasses()
    {
        // Arrange
        await SeedClassAsync("Math 9A", academicYear: "2024-2025");
        await SeedClassAsync("Physics 10A", Subject.Physics, academicYear: "2024-2025");
        await SeedClassAsync("Chemistry 11A", Subject.Chemistry, academicYear: "2023-2024");

        // Act
        var result = await _repository.GetByAcademicYearAsync("2024-2025");

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(2);
        classes.Should().AllSatisfy(c => c.AcademicYear.Should().Be("2024-2025"));
    }

    [Fact]
    public async Task GetByAcademicYearAsync_WithNoMatchingYear_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedClassAsync("Math 9A", academicYear: "2024-2025");

        // Act
        var result = await _repository.GetByAcademicYearAsync("2025-2026");

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    #endregion

    #region GetBySchoolAndCodeAsync Tests

    [Fact]
    public async Task GetBySchoolAndCodeAsync_WithMatchingClass_ShouldReturnClass()
    {
        // Arrange
        var seededClass = await SeedClassAsync("Math 9A", code: "MATH9A");

        // Act
        var result = await _repository.GetBySchoolAndCodeAsync(_schoolId, "MATH9A");

        // Assert
        result.Should().BeOfType<Result<Class>.Success>();
        var foundClass = ((Result<Class>.Success)result).Value;
        foundClass.Id.Should().Be(seededClass.Id);
        foundClass.Code.Should().Be("MATH9A");
        foundClass.SchoolId.Should().Be(_schoolId);
    }

    [Fact]
    public async Task GetBySchoolAndCodeAsync_WithNonExistentCode_ShouldReturnNotFound()
    {
        // Arrange
        await SeedClassAsync("Math 9A", code: "MATH9A");

        // Act
        var result = await _repository.GetBySchoolAndCodeAsync(_schoolId, "NONEXISTENT");

        // Assert
        result.Should().BeOfType<Result<Class>.Failure>();
        var failure = ((Result<Class>.Failure)result);
        failure.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetBySchoolAndCodeAsync_WithDifferentSchool_ShouldReturnNotFound()
    {
        // Arrange
        await SeedClassAsync("Math 9A", code: "MATH9A");
        var differentSchoolId = Guid.NewGuid();

        // Act
        var result = await _repository.GetBySchoolAndCodeAsync(differentSchoolId, "MATH9A");

        // Assert
        result.Should().BeOfType<Result<Class>.Failure>();
    }

    #endregion

    #region GetClassesWithAggregateReportingAsync Tests - K-Anonymity Privacy

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_WithFiveStudents_ShouldIncludeClass()
    {
        // Arrange - Exactly 5 students (k-anonymity threshold)
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 5);
        await SeedClassAsync("Physics 10A", Subject.Physics, code: "PHYS10A", studentCount: 4); // Below threshold

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().ContainSingle();
        classes[0].Code.Should().Be("MATH9A");
        classes[0].EnrollmentCount.Should().Be(5);
        classes[0].SupportsAggregateReporting.Should().BeTrue();
    }

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_WithTenStudents_ShouldIncludeClass()
    {
        // Arrange - Well above threshold
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 10);

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().ContainSingle();
        classes[0].EnrollmentCount.Should().Be(10);
        classes[0].SupportsAggregateReporting.Should().BeTrue();
    }

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_WithFourStudents_ShouldExcludeClass()
    {
        // Arrange - Below k-anonymity threshold
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 4);

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty("classes with < 5 students cannot support aggregate reporting");
    }

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_WithZeroStudents_ShouldExcludeClass()
    {
        // Arrange - Empty class
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 0);

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_MixedEnrollments_ShouldFilterCorrectly()
    {
        // Arrange - Mixed class sizes
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 5);  // Include
        await SeedClassAsync("Math 9B", code: "MATH9B", studentCount: 10); // Include
        await SeedClassAsync("Physics 10A", Subject.Physics, code: "PHYS10A", studentCount: 4);  // Exclude
        await SeedClassAsync("Chemistry 10A", Subject.Chemistry, code: "CHEM10A", studentCount: 3); // Exclude
        await SeedClassAsync("Biology 11A", Subject.Biology, code: "BIO11A", studentCount: 7);  // Include
        await SeedClassAsync("English 12A", Subject.English, code: "ENG12A", studentCount: 0);  // Exclude

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(3);
        classes.Should().AllSatisfy(c => c.EnrollmentCount.Should().BeGreaterOrEqualTo(5));
        classes.Should().AllSatisfy(c => c.SupportsAggregateReporting.Should().BeTrue());

        var codes = classes.Select(c => c.Code).ToList();
        codes.Should().Contain(new[] { "MATH9A", "MATH9B", "BIO11A" });
        codes.Should().NotContain(new[] { "PHYS10A", "CHEM10A", "ENG12A" });
    }

    [Fact]
    public async Task GetClassesWithAggregateReportingAsync_AllBelowThreshold_ShouldReturnEmptyList()
    {
        // Arrange - All classes below k-anonymity threshold
        await SeedClassAsync("Math 9A", code: "MATH9A", studentCount: 1);
        await SeedClassAsync("Physics 10A", Subject.Physics, code: "PHYS10A", studentCount: 2);
        await SeedClassAsync("Chemistry 11A", Subject.Chemistry, code: "CHEM11A", studentCount: 4);

        // Act
        var result = await _repository.GetClassesWithAggregateReportingAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().BeEmpty("no classes meet k-anonymity threshold");
    }

    #endregion

    #region CRUD Operations Tests

    [Fact]
    public async Task AddAsync_ValidClass_ShouldAddSuccessfully()
    {
        // Arrange
        var newClass = CreateTestClass("New Class", code: "NEW01");

        // Act
        var result = await _repository.AddAsync(newClass);

        // Assert
        result.Should().BeOfType<Result<Class>.Success>();
        var addedClass = ((Result<Class>.Success)result).Value;
        addedClass.Id.Should().Be(newClass.Id);

        // Verify in database
        var dbClass = await _context.Classes.FindAsync(newClass.Id);
        dbClass.Should().NotBeNull();
        dbClass!.Name.Should().Be("New Class");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingClass_ShouldReturnClass()
    {
        // Arrange
        var seededClass = await SeedClassAsync("Test Class");

        // Act
        var result = await _repository.GetByIdAsync(seededClass.Id);

        // Assert
        result.Should().BeOfType<Result<Class>.Success>();
        var foundClass = ((Result<Class>.Success)result).Value;
        foundClass.Id.Should().Be(seededClass.Id);
        foundClass.Name.Should().Be("Test Class");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentClass_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeOfType<Result<Class>.Failure>();
        var failure = ((Result<Class>.Failure)result);
        failure.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_ExistingClass_ShouldUpdateSuccessfully()
    {
        // Arrange
        var seededClass = await SeedClassAsync("Original Name");
        _context.Entry(seededClass).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        var updatedClass = seededClass with { Name = "Updated Name" };

        // Act
        var result = await _repository.UpdateAsync(updatedClass);

        // Assert
        result.Should().BeOfType<Result<Class>.Success>();

        // Verify in database
        var dbClass = await _context.Classes.FindAsync(seededClass.Id);
        dbClass!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_ExistingClass_ShouldDeleteSuccessfully()
    {
        // Arrange
        var seededClass = await SeedClassAsync("To Delete");

        // Act
        var result = await _repository.DeleteAsync(seededClass.Id);

        // Assert
        result.Should().BeOfType<Result<Core.Common.Unit>.Success>();

        // Verify removed from database
        var dbClass = await _context.Classes.FindAsync(seededClass.Id);
        dbClass.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentClass_ShouldReturnNotFound()
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
    public async Task GetAllAsync_WithMultipleClasses_ShouldReturnAll()
    {
        // Arrange
        await SeedClassAsync("Class 1", code: "C1");
        await SeedClassAsync("Class 2", code: "C2");
        await SeedClassAsync("Class 3", code: "C3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Class>>.Success>();
        var classes = ((Result<IReadOnlyList<Class>>.Success)result).Value;
        classes.Should().HaveCount(3);
    }

    #endregion
}
