using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using FluentAssertions;
using Xunit;

namespace AcademicAssessment.Tests.Unit.Models;

/// <summary>
/// Tests for the Class domain model including k-anonymity aggregate reporting requirements
/// </summary>
public class ClassTests
{
    private static Class CreateTestClass(
        Guid? id = null,
        Guid? schoolId = null,
        string name = "8th Grade Math - Section A",
        string code = "8M-A",
        GradeLevel gradeLevel = GradeLevel.Grade8,
        Subject subject = Subject.Mathematics,
        IReadOnlyList<Guid>? teacherIds = null,
        IReadOnlyList<Guid>? studentIds = null,
        string academicYear = "2024-2025",
        bool isActive = true)
    {
        return new Class
        {
            Id = id ?? Guid.NewGuid(),
            SchoolId = schoolId ?? Guid.NewGuid(),
            Name = name,
            Code = code,
            GradeLevel = gradeLevel,
            Subject = subject,
            TeacherIds = teacherIds ?? Array.Empty<Guid>().ToList().AsReadOnly(),
            StudentIds = studentIds ?? Array.Empty<Guid>().ToList().AsReadOnly(),
            AcademicYear = academicYear,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    #region Immutability Tests

    [Fact]
    public void Class_ShouldBeImmutable()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var originalName = classInstance.Name;

        // Act - Attempting to reassign should not compile (record type)
        // This test validates the immutability at compile time
        var newInstance = classInstance with { Name = "New Name" };

        // Assert
        classInstance.Name.Should().Be(originalName);
        newInstance.Name.Should().Be("New Name");
        newInstance.Should().NotBeSameAs(classInstance);
    }

    [Fact]
    public void StudentIds_ShouldBeReadOnly()
    {
        // Arrange & Act
        var studentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());

        // Assert
        classInstance.StudentIds.Should().HaveCount(2);
        classInstance.StudentIds.Should().BeAssignableTo<IReadOnlyList<Guid>>();
        // Verify collection is actually read-only (attempts to modify should throw)
        Action tryAdd = () => ((IList<Guid>)classInstance.StudentIds).Add(Guid.NewGuid());
        tryAdd.Should().Throw<NotSupportedException>("readonly collections should not allow modifications");
    }

    [Fact]
    public void TeacherIds_ShouldBeReadOnly()
    {
        // Arrange & Act
        var teacherIds = new List<Guid> { Guid.NewGuid() };
        var classInstance = CreateTestClass(teacherIds: teacherIds.AsReadOnly());

        // Assert
        classInstance.TeacherIds.Should().HaveCount(1);
        classInstance.TeacherIds.Should().BeAssignableTo<IReadOnlyList<Guid>>();
        // Verify collection is actually read-only (attempts to modify should throw)
        Action tryAdd = () => ((IList<Guid>)classInstance.TeacherIds).Add(Guid.NewGuid());
        tryAdd.Should().Throw<NotSupportedException>("readonly collections should not allow modifications");
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void EnrollmentCount_ShouldReflectStudentIdsCount()
    {
        // Arrange
        var studentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());

        // Act & Assert
        classInstance.EnrollmentCount.Should().Be(3);
    }

    [Fact]
    public void EnrollmentCount_WithNoStudents_ShouldBeZero()
    {
        // Arrange
        var classInstance = CreateTestClass();

        // Act & Assert
        classInstance.EnrollmentCount.Should().Be(0);
    }

    [Fact]
    public void SupportsAggregateReporting_WithFiveStudents_ShouldReturnTrue()
    {
        // Arrange - Exactly 5 students (k-anonymity threshold)
        var studentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());

        // Act & Assert
        classInstance.SupportsAggregateReporting.Should().BeTrue(
            "k-anonymity requires minimum 5 students for aggregate reporting");
    }

    [Fact]
    public void SupportsAggregateReporting_WithFourStudents_ShouldReturnFalse()
    {
        // Arrange - Just below k-anonymity threshold
        var studentIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());

        // Act & Assert
        classInstance.SupportsAggregateReporting.Should().BeFalse(
            "k-anonymity requires minimum 5 students, class has only 4");
    }

    [Fact]
    public void SupportsAggregateReporting_WithTenStudents_ShouldReturnTrue()
    {
        // Arrange - Well above k-anonymity threshold
        var studentIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());

        // Act & Assert
        classInstance.SupportsAggregateReporting.Should().BeTrue();
    }

    [Fact]
    public void SupportsAggregateReporting_WithNoStudents_ShouldReturnFalse()
    {
        // Arrange
        var classInstance = CreateTestClass();

        // Act & Assert
        classInstance.SupportsAggregateReporting.Should().BeFalse(
            "empty class cannot support aggregate reporting");
    }

    #endregion

    #region Student Management Tests

    [Fact]
    public void AddStudent_NewStudent_ShouldAddToStudentIds()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var studentId = Guid.NewGuid();

        // Act
        var updatedClass = classInstance.AddStudent(studentId);

        // Assert
        updatedClass.StudentIds.Should().Contain(studentId);
        updatedClass.EnrollmentCount.Should().Be(1);
    }

    [Fact]
    public void AddStudent_AlreadyEnrolled_ShouldNotDuplicate()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classInstance = CreateTestClass(studentIds: new[] { studentId }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.AddStudent(studentId);

        // Assert
        updatedClass.StudentIds.Should().ContainSingle();
        updatedClass.StudentIds.Should().Contain(studentId);
    }

    [Fact]
    public void AddStudent_MultipleStudents_ShouldAddAll()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();
        var student3 = Guid.NewGuid();

        // Act
        var updatedClass = classInstance
            .AddStudent(student1)
            .AddStudent(student2)
            .AddStudent(student3);

        // Assert
        updatedClass.StudentIds.Should().HaveCount(3);
        updatedClass.StudentIds.Should().Contain(new[] { student1, student2, student3 });
    }

    [Fact]
    public void AddStudent_ShouldUpdateTimestamp()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var originalTimestamp = classInstance.UpdatedAt;
        Thread.Sleep(10); // Ensure time passes

        // Act
        var updatedClass = classInstance.AddStudent(Guid.NewGuid());

        // Assert
        updatedClass.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public void RemoveStudent_ExistingStudent_ShouldRemove()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classInstance = CreateTestClass(studentIds: new[] { studentId }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.RemoveStudent(studentId);

        // Assert
        updatedClass.StudentIds.Should().BeEmpty();
        updatedClass.EnrollmentCount.Should().Be(0);
    }

    [Fact]
    public void RemoveStudent_NonExistentStudent_ShouldNotError()
    {
        // Arrange
        var classInstance = CreateTestClass(
            studentIds: new[] { Guid.NewGuid() }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.RemoveStudent(Guid.NewGuid());

        // Assert
        updatedClass.StudentIds.Should().ContainSingle();
    }

    [Fact]
    public void RemoveStudent_OneOfMany_ShouldOnlyRemoveOne()
    {
        // Arrange
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();
        var student3 = Guid.NewGuid();
        var classInstance = CreateTestClass(
            studentIds: new[] { student1, student2, student3 }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.RemoveStudent(student2);

        // Assert
        updatedClass.StudentIds.Should().HaveCount(2);
        updatedClass.StudentIds.Should().Contain(new[] { student1, student3 });
        updatedClass.StudentIds.Should().NotContain(student2);
    }

    #endregion

    #region Teacher Management Tests

    [Fact]
    public void AddTeacher_NewTeacher_ShouldAddToTeacherIds()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var teacherId = Guid.NewGuid();

        // Act
        var updatedClass = classInstance.AddTeacher(teacherId);

        // Assert
        updatedClass.TeacherIds.Should().Contain(teacherId);
        updatedClass.TeacherIds.Should().ContainSingle();
    }

    [Fact]
    public void AddTeacher_AlreadyAssigned_ShouldNotDuplicate()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var classInstance = CreateTestClass(teacherIds: new[] { teacherId }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.AddTeacher(teacherId);

        // Assert
        updatedClass.TeacherIds.Should().ContainSingle();
        updatedClass.TeacherIds.Should().Contain(teacherId);
    }

    [Fact]
    public void AddTeacher_MultipleTeachers_ShouldAddAll()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var teacher1 = Guid.NewGuid();
        var teacher2 = Guid.NewGuid();

        // Act
        var updatedClass = classInstance
            .AddTeacher(teacher1)
            .AddTeacher(teacher2);

        // Assert
        updatedClass.TeacherIds.Should().HaveCount(2);
        updatedClass.TeacherIds.Should().Contain(new[] { teacher1, teacher2 });
    }

    [Fact]
    public void RemoveTeacher_ExistingTeacher_ShouldRemove()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var classInstance = CreateTestClass(teacherIds: new[] { teacherId }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.RemoveTeacher(teacherId);

        // Assert
        updatedClass.TeacherIds.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTeacher_NonExistentTeacher_ShouldNotError()
    {
        // Arrange
        var classInstance = CreateTestClass(
            teacherIds: new[] { Guid.NewGuid() }.ToList().AsReadOnly());

        // Act
        var updatedClass = classInstance.RemoveTeacher(Guid.NewGuid());

        // Assert
        updatedClass.TeacherIds.Should().ContainSingle();
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdateName_ShouldCreateNewInstance()
    {
        // Arrange
        var classInstance = CreateTestClass(name: "Original Name");

        // Act
        var updatedClass = classInstance.With(name: "Updated Name");

        // Assert
        updatedClass.Name.Should().Be("Updated Name");
        updatedClass.Should().NotBeSameAs(classInstance);
        classInstance.Name.Should().Be("Original Name");
    }

    [Fact]
    public void With_UpdateIsActive_ShouldWork()
    {
        // Arrange
        var classInstance = CreateTestClass(isActive: true);

        // Act
        var updatedClass = classInstance.With(isActive: false);

        // Assert
        updatedClass.IsActive.Should().BeFalse();
        classInstance.IsActive.Should().BeTrue();
    }

    [Fact]
    public void With_NullParameters_ShouldKeepOriginalValues()
    {
        // Arrange
        var classInstance = CreateTestClass(name: "Original");

        // Act
        var updatedClass = classInstance.With();

        // Assert
        updatedClass.Name.Should().Be("Original");
        updatedClass.UpdatedAt.Should().BeAfter(classInstance.UpdatedAt);
    }

    [Fact]
    public void With_ShouldUpdateTimestamp()
    {
        // Arrange
        var classInstance = CreateTestClass();
        var originalTimestamp = classInstance.UpdatedAt;
        Thread.Sleep(10);

        // Act
        var updatedClass = classInstance.With(name: "New Name");

        // Assert
        updatedClass.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    #endregion

    #region K-Anonymity Integration Tests

    [Fact]
    public void AddingStudentsToReachThreshold_ShouldEnableAggregateReporting()
    {
        // Arrange - Start with 4 students (below threshold)
        var studentIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());
        classInstance.SupportsAggregateReporting.Should().BeFalse();

        // Act - Add 5th student to reach threshold
        var updatedClass = classInstance.AddStudent(Guid.NewGuid());

        // Assert
        updatedClass.EnrollmentCount.Should().Be(5);
        updatedClass.SupportsAggregateReporting.Should().BeTrue(
            "adding 5th student should enable aggregate reporting for k-anonymity");
    }

    [Fact]
    public void RemovingStudentsBelowThreshold_ShouldDisableAggregateReporting()
    {
        // Arrange - Start with 5 students (at threshold)
        var studentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var classInstance = CreateTestClass(studentIds: studentIds.AsReadOnly());
        classInstance.SupportsAggregateReporting.Should().BeTrue();

        // Act - Remove one student to fall below threshold
        var updatedClass = classInstance.RemoveStudent(studentIds[0]);

        // Assert
        updatedClass.EnrollmentCount.Should().Be(4);
        updatedClass.SupportsAggregateReporting.Should().BeFalse(
            "removing student to fall below 5 should disable aggregate reporting for k-anonymity");
    }

    [Fact]
    public void RealWorldScenario_GrowingClass_ShouldTrackAggregateReportingEligibility()
    {
        // Arrange - Start empty class
        var classInstance = CreateTestClass();
        classInstance.SupportsAggregateReporting.Should().BeFalse();

        // Act & Assert - Add students one by one
        var class1 = classInstance.AddStudent(Guid.NewGuid());
        class1.EnrollmentCount.Should().Be(1);
        class1.SupportsAggregateReporting.Should().BeFalse();

        var class2 = class1.AddStudent(Guid.NewGuid());
        class2.EnrollmentCount.Should().Be(2);
        class2.SupportsAggregateReporting.Should().BeFalse();

        var class3 = class2.AddStudent(Guid.NewGuid());
        class3.EnrollmentCount.Should().Be(3);
        class3.SupportsAggregateReporting.Should().BeFalse();

        var class4 = class3.AddStudent(Guid.NewGuid());
        class4.EnrollmentCount.Should().Be(4);
        class4.SupportsAggregateReporting.Should().BeFalse("still one short of k-anonymity threshold");

        var class5 = class4.AddStudent(Guid.NewGuid());
        class5.EnrollmentCount.Should().Be(5);
        class5.SupportsAggregateReporting.Should().BeTrue("reached k-anonymity threshold");
    }

    #endregion
}
