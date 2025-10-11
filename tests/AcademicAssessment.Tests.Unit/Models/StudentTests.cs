using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Models;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Models;

/// <summary>
/// Unit tests for Student model
/// Tests immutability, computed properties, XP/level calculations, and business logic
/// </summary>
public class StudentTests
{
    #region Test Data Helpers

    private static Student CreateTestStudent(
        Guid? schoolId = null,
        GradeLevel gradeLevel = GradeLevel.Grade9,
        DateOnly? dateOfBirth = null,
        bool parentalConsentGranted = false,
        int xpPoints = 0,
        int level = 1,
        int dailyStreak = 0,
        DateOnly? lastActivityDate = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SchoolId = schoolId,
            GradeLevel = gradeLevel,
            DateOfBirth = dateOfBirth,
            ParentalConsentGranted = parentalConsentGranted,
            XpPoints = xpPoints,
            Level = level,
            DailyStreak = dailyStreak,
            LastActivityDate = lastActivityDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    #endregion

    #region Immutability Tests

    [Fact]
    public void Student_ShouldBeImmutable()
    {
        // Arrange
        var student = CreateTestStudent();
        var originalGrade = student.GradeLevel;

        // Act - trying to modify should create new instance
        var updatedStudent = student with { GradeLevel = GradeLevel.Grade10 };

        // Assert - original unchanged, new instance created
        student.GradeLevel.Should().Be(originalGrade);
        updatedStudent.GradeLevel.Should().Be(GradeLevel.Grade10);
        updatedStudent.Should().NotBeSameAs(student);
    }

    [Fact]
    public void ClassIds_ShouldBeReadOnly()
    {
        // Arrange
        var student = CreateTestStudent();

        // Assert
        student.ClassIds.Should().BeAssignableTo<IReadOnlyList<Guid>>();
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void RequiresCoppaCompliance_WhenUnder13_ShouldReturnTrue()
    {
        // Arrange - Student born 10 years ago
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        var student = CreateTestStudent(dateOfBirth: birthDate);

        // Assert
        student.RequiresCoppaCompliance.Should().BeTrue();
    }

    [Fact]
    public void RequiresCoppaCompliance_WhenOver13_ShouldReturnFalse()
    {
        // Arrange - Student born 15 years ago
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15));
        var student = CreateTestStudent(dateOfBirth: birthDate);

        // Assert
        student.RequiresCoppaCompliance.Should().BeFalse();
    }

    [Fact]
    public void RequiresCoppaCompliance_WhenNoBirthDate_ShouldReturnFalse()
    {
        // Arrange
        var student = CreateTestStudent(dateOfBirth: null);

        // Assert
        student.RequiresCoppaCompliance.Should().BeFalse();
    }

    [Fact]
    public void IsSelfService_WhenNoSchoolId_ShouldReturnTrue()
    {
        // Arrange
        var student = CreateTestStudent(schoolId: null);

        // Assert
        student.IsSelfService.Should().BeTrue();
    }

    [Fact]
    public void IsSelfService_WhenHasSchoolId_ShouldReturnFalse()
    {
        // Arrange
        var student = CreateTestStudent(schoolId: Guid.NewGuid());

        // Assert
        student.IsSelfService.Should().BeFalse();
    }

    #endregion

    #region XP and Level Tests

    [Fact]
    public void AddXp_WithZeroPoints_ShouldStayAtLevel1()
    {
        // Arrange
        var student = CreateTestStudent(xpPoints: 0, level: 1);

        // Act
        var updated = student.AddXp(50);

        // Assert
        updated.XpPoints.Should().Be(50);
        updated.Level.Should().Be(1); // Still level 1 (need 100 XP for level 2)
    }

    [Fact]
    public void AddXp_With100Points_ShouldReachLevel2()
    {
        // Arrange
        var student = CreateTestStudent(xpPoints: 0, level: 1);

        // Act
        var updated = student.AddXp(100);

        // Assert
        updated.XpPoints.Should().Be(100);
        updated.Level.Should().Be(2);
    }

    [Fact]
    public void AddXp_With250Points_ShouldReachLevel3()
    {
        // Arrange
        var student = CreateTestStudent(xpPoints: 0, level: 1);

        // Act
        var updated = student.AddXp(250);

        // Assert
        updated.XpPoints.Should().Be(250);
        updated.Level.Should().Be(3); // (250 / 100) + 1 = 3
    }

    [Fact]
    public void AddXp_Multiple_ShouldAccumulateCorrectly()
    {
        // Arrange
        var student = CreateTestStudent(xpPoints: 50, level: 1);

        // Act - Add XP in multiple steps
        var step1 = student.AddXp(30);  // Total: 80, Level 1
        var step2 = step1.AddXp(40);    // Total: 120, Level 2
        var step3 = step2.AddXp(80);    // Total: 200, Level 3

        // Assert
        step1.XpPoints.Should().Be(80);
        step1.Level.Should().Be(1);

        step2.XpPoints.Should().Be(120);
        step2.Level.Should().Be(2);

        step3.XpPoints.Should().Be(200);
        step3.Level.Should().Be(3);
    }

    [Fact]
    public void AddXp_ShouldUpdateTimestamp()
    {
        // Arrange
        var student = CreateTestStudent();
        var originalTimestamp = student.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure time passes
        var updated = student.AddXp(50);

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    #endregion

    #region Streak Tests

    [Fact]
    public void UpdateStreak_FirstTime_ShouldSetStreak1()
    {
        // Arrange
        var student = CreateTestStudent(dailyStreak: 0, lastActivityDate: null);

        // Act
        var updated = student.UpdateStreak();

        // Assert
        updated.DailyStreak.Should().Be(1);
        updated.LastActivityDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void UpdateStreak_SameDay_ShouldNotChange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var student = CreateTestStudent(dailyStreak: 5, lastActivityDate: today);

        // Act
        var updated = student.UpdateStreak();

        // Assert
        updated.DailyStreak.Should().Be(5); // Unchanged
        updated.LastActivityDate.Should().Be(today);
        updated.Should().BeSameAs(student); // Same instance returned
    }

    [Fact]
    public void UpdateStreak_ConsecutiveDay_ShouldIncrement()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var student = CreateTestStudent(dailyStreak: 5, lastActivityDate: yesterday);

        // Act
        var updated = student.UpdateStreak();

        // Assert
        updated.DailyStreak.Should().Be(6);
        updated.LastActivityDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void UpdateStreak_SkippedDay_ShouldReset()
    {
        // Arrange
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var student = CreateTestStudent(dailyStreak: 10, lastActivityDate: twoDaysAgo);

        // Act
        var updated = student.UpdateStreak();

        // Assert
        updated.DailyStreak.Should().Be(1); // Reset to 1
        updated.LastActivityDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    #endregion

    #region Class Enrollment Tests

    [Fact]
    public void EnrollInClass_NewClass_ShouldAddToClassIds()
    {
        // Arrange
        var student = CreateTestStudent();
        var classId = Guid.NewGuid();

        // Act
        var updated = student.EnrollInClass(classId);

        // Assert
        updated.ClassIds.Should().Contain(classId);
        updated.ClassIds.Should().HaveCount(1);
    }

    [Fact]
    public void EnrollInClass_AlreadyEnrolled_ShouldNotDuplicate()
    {
        // Arrange
        var classId = Guid.NewGuid();
        var student = CreateTestStudent();
        var enrolled = student.EnrollInClass(classId);

        // Act - Enroll again
        var duplicate = enrolled.EnrollInClass(classId);

        // Assert
        duplicate.ClassIds.Should().HaveCount(1);
        duplicate.Should().BeSameAs(enrolled); // Same instance
    }

    [Fact]
    public void EnrollInClass_MultipleClasses_ShouldAddAll()
    {
        // Arrange
        var student = CreateTestStudent();
        var class1 = Guid.NewGuid();
        var class2 = Guid.NewGuid();
        var class3 = Guid.NewGuid();

        // Act
        var updated = student
            .EnrollInClass(class1)
            .EnrollInClass(class2)
            .EnrollInClass(class3);

        // Assert
        updated.ClassIds.Should().HaveCount(3);
        updated.ClassIds.Should().Contain(new[] { class1, class2, class3 });
    }

    [Fact]
    public void UnenrollFromClass_ExistingClass_ShouldRemove()
    {
        // Arrange
        var classId = Guid.NewGuid();
        var student = CreateTestStudent();
        var enrolled = student.EnrollInClass(classId);

        // Act
        var unenrolled = enrolled.UnenrollFromClass(classId);

        // Assert
        unenrolled.ClassIds.Should().NotContain(classId);
        unenrolled.ClassIds.Should().BeEmpty();
    }

    [Fact]
    public void UnenrollFromClass_NonExistentClass_ShouldNotError()
    {
        // Arrange
        var student = CreateTestStudent();
        var nonExistentClassId = Guid.NewGuid();

        // Act
        var updated = student.UnenrollFromClass(nonExistentClassId);

        // Assert
        updated.ClassIds.Should().BeEmpty();
    }

    [Fact]
    public void UnenrollFromClass_OneOfMany_ShouldOnlyRemoveOne()
    {
        // Arrange
        var class1 = Guid.NewGuid();
        var class2 = Guid.NewGuid();
        var class3 = Guid.NewGuid();
        var student = CreateTestStudent()
            .EnrollInClass(class1)
            .EnrollInClass(class2)
            .EnrollInClass(class3);

        // Act
        var updated = student.UnenrollFromClass(class2);

        // Assert
        updated.ClassIds.Should().HaveCount(2);
        updated.ClassIds.Should().Contain(new[] { class1, class3 });
        updated.ClassIds.Should().NotContain(class2);
    }

    #endregion

    #region With Method Tests

    [Fact]
    public void With_UpdateGradeLevel_ShouldCreateNewInstance()
    {
        // Arrange
        var student = CreateTestStudent(gradeLevel: GradeLevel.Grade9);

        // Act
        var updated = student.With(gradeLevel: GradeLevel.Grade10);

        // Assert
        updated.GradeLevel.Should().Be(GradeLevel.Grade10);
        student.GradeLevel.Should().Be(GradeLevel.Grade9); // Original unchanged
    }

    [Fact]
    public void With_UpdateSubscriptionTier_ShouldWork()
    {
        // Arrange
        var student = CreateTestStudent();

        // Act
        var updated = student.With(subscriptionTier: SubscriptionTier.Premium);

        // Assert
        updated.SubscriptionTier.Should().Be(SubscriptionTier.Premium);
    }

    [Fact]
    public void With_NullParameters_ShouldKeepOriginalValues()
    {
        // Arrange
        var student = CreateTestStudent(
            gradeLevel: GradeLevel.Grade9,
            xpPoints: 150,
            level: 2);

        // Act - Call with all nulls
        var updated = student.With();

        // Assert
        updated.GradeLevel.Should().Be(GradeLevel.Grade9);
        updated.XpPoints.Should().Be(150);
        updated.Level.Should().Be(2);
    }

    [Fact]
    public void With_ShouldUpdateTimestamp()
    {
        // Arrange
        var student = CreateTestStudent();
        var originalTimestamp = student.UpdatedAt;

        // Act
        Thread.Sleep(10);
        var updated = student.With(level: 5);

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    #endregion

    #region Business Logic Integration Tests

    [Fact]
    public void SchoolStudent_WithCoppaCompliance_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        var student = CreateTestStudent(
            schoolId: Guid.NewGuid(),
            dateOfBirth: birthDate,
            parentalConsentGranted: true);

        // Assert
        student.IsSelfService.Should().BeFalse();
        student.RequiresCoppaCompliance.Should().BeTrue();
        student.ParentalConsentGranted.Should().BeTrue();
    }

    [Fact]
    public void SelfServiceStudent_WithGamification_ShouldTrackProgress()
    {
        // Arrange
        var student = CreateTestStudent(schoolId: null);

        // Act - Simulate student activity
        var day1 = student.UpdateStreak().AddXp(50);
        Thread.Sleep(10);
        var day2 = day1 with { LastActivityDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) };
        var day3 = day2.UpdateStreak().AddXp(75);

        // Assert
        day3.IsSelfService.Should().BeTrue();
        day3.DailyStreak.Should().Be(2);
        day3.XpPoints.Should().Be(125);
        day3.Level.Should().Be(2);
    }

    #endregion
}
