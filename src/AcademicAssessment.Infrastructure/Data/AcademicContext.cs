using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Data;

/// <summary>
/// Main database context for the academic assessment system
/// Implements row-level security through query filters based on tenant context
/// </summary>
public sealed class AcademicContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AcademicContext(
        DbContextOptions<AcademicContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // DbSets for all entities
    public DbSet<User> Users => Set<User>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<StudentAssessment> StudentAssessments => Set<StudentAssessment>();
    public DbSet<StudentResponse> StudentResponses => Set<StudentResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity mappings
        ConfigureUsers(modelBuilder);
        ConfigureSchools(modelBuilder);
        ConfigureClasses(modelBuilder);
        ConfigureStudents(modelBuilder);
        ConfigureCourses(modelBuilder);
        ConfigureAssessments(modelBuilder);
        ConfigureQuestions(modelBuilder);
        ConfigureStudentAssessments(modelBuilder);
        ConfigureStudentResponses(modelBuilder);

        // Apply row-level security filters
        ApplyTenantFilters(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ExternalId)
                .HasMaxLength(255);

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => e.Role);
        });
    }

    private static void ConfigureSchools(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("schools");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ContactEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ContactPhone)
                .HasMaxLength(50);

            // Computed properties are not mapped
            entity.Ignore(e => e.ConnectionStringKey);
            entity.Ignore(e => e.DatabaseName);

            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureClasses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.AcademicYear)
                .IsRequired()
                .HasMaxLength(20);

            // Store lists as JSON
            entity.Property(e => e.TeacherIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            entity.Property(e => e.StudentIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Computed properties
            entity.Ignore(e => e.EnrollmentCount);
            entity.Ignore(e => e.SupportsAggregateReporting);

            // Indexes
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => new { e.SchoolId, e.Code }).IsUnique();
            entity.HasIndex(e => e.GradeLevel);
            entity.HasIndex(e => e.Subject);
        });
    }

    private static void ConfigureStudents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ParentEmail)
                .HasMaxLength(255);

            // Store lists as JSON
            entity.Property(e => e.ClassIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Computed properties
            entity.Ignore(e => e.RequiresCoppaCompliance);
            entity.Ignore(e => e.IsSelfService);

            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => e.GradeLevel);
            entity.HasIndex(e => e.SubscriptionTier);
            entity.HasIndex(e => e.Level);
        });
    }

    private static void ConfigureCourses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("courses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            // Store lists as JSON
            entity.Property(e => e.LearningObjectives)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            entity.Property(e => e.Topics)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Metadata fields (Phase 5 enhancement)
            entity.Property(e => e.BoardName)
                .HasMaxLength(100);

            entity.Property(e => e.ModuleName)
                .HasMaxLength(200);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) as IReadOnlyDictionary<string, string> ?? new Dictionary<string, string>()
                )
                .HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.GradeLevel);
            entity.HasIndex(e => e.CourseAdminId);
            entity.HasIndex(e => e.BoardName); // Index for board filtering
            entity.HasIndex(e => e.ModuleName); // Index for module filtering
        });
    }

    private static void ConfigureAssessments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            // Store lists as JSON
            entity.Property(e => e.Topics)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            entity.Property(e => e.QuestionIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Computed properties
            entity.Ignore(e => e.IsAdaptive);
            entity.Ignore(e => e.QuestionCount);

            // Indexes
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => e.AssessmentType);
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.GradeLevel);
        });
    }

    private static void ConfigureQuestions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuestionText)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.AnswerOptions)
                .HasMaxLength(4000);

            entity.Property(e => e.CorrectAnswer)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.Explanation)
                .HasMaxLength(4000);

            entity.Property(e => e.ContentHash)
                .HasMaxLength(64);

            // Store lists as JSON
            entity.Property(e => e.Topics)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            entity.Property(e => e.LearningObjectives)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Metadata fields (Phase 5 enhancement)
            entity.Property(e => e.BoardName)
                .HasMaxLength(100);

            entity.Property(e => e.ModuleName)
                .HasMaxLength(200);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) as IReadOnlyDictionary<string, string> ?? new Dictionary<string, string>()
                )
                .HasColumnType("jsonb");

            // Computed properties
            entity.Ignore(e => e.SuccessRate);

            // Indexes
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.QuestionType);
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.GradeLevel);
            entity.HasIndex(e => e.DifficultyLevel);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.IsAiGenerated);
            entity.HasIndex(e => e.BoardName); // Index for board filtering
            entity.HasIndex(e => e.ModuleName); // Index for module filtering
        });
    }

    private static void ConfigureStudentAssessments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentAssessment>(entity =>
        {
            entity.ToTable("student_assessments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Feedback)
                .HasMaxLength(4000);

            // Store lists as JSON
            entity.Property(e => e.Recommendations)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly()
                );

            // Computed properties
            entity.Ignore(e => e.PercentageScore);

            // Indexes
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => new { e.StudentId, e.AssessmentId });
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CompletedAt);
        });
    }

    private static void ConfigureStudentResponses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentResponse>(entity =>
        {
            entity.ToTable("student_responses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StudentAnswer)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.Feedback)
                .HasMaxLength(4000);

            entity.Property(e => e.AiExplanation)
                .HasMaxLength(4000);

            // Computed properties
            entity.Ignore(e => e.WasSkipped);

            // Indexes
            entity.HasIndex(e => e.StudentAssessmentId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => new { e.StudentAssessmentId, e.QuestionId }).IsUnique();
            entity.HasIndex(e => e.SchoolId);
            entity.HasIndex(e => e.IsCorrect);
        });
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // Apply row-level security filters based on tenant context
        // System admins bypass all filters
        // Business admins see all schools
        // School admins/teachers/students see only their school's data

        // Users - filter by school (or self-service users with no school)
        modelBuilder.Entity<User>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == _tenantContext.SchoolId ||
            (e.SchoolId == null && _tenantContext.SchoolId == null));

        // Students - filter by school (or self-service)
        modelBuilder.Entity<Student>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == _tenantContext.SchoolId ||
            (e.SchoolId == null && _tenantContext.SchoolId == null && e.UserId == _tenantContext.UserId));

        // Classes - filter by school
        modelBuilder.Entity<Class>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == _tenantContext.SchoolId);

        // Assessments - filter by school (or global assessments)
        modelBuilder.Entity<Assessment>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == null ||
            e.SchoolId == _tenantContext.SchoolId);

        // Student Assessments - filter by school
        modelBuilder.Entity<StudentAssessment>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == _tenantContext.SchoolId ||
            (e.SchoolId == null && e.StudentId == _tenantContext.UserId));

        // Student Responses - filter by school
        modelBuilder.Entity<StudentResponse>().HasQueryFilter(e =>
            _tenantContext.Role >= Core.Enums.UserRole.BusinessAdmin ||
            e.SchoolId == _tenantContext.SchoolId ||
            (e.SchoolId == null && e.StudentId == _tenantContext.UserId));

        // Schools - business admins and system admins see all
        // Note: No filter needed as only admins can access schools
    }
}
