using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicAssessment.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssessmentType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    GradeLevel = table.Column<int>(type: "integer", nullable: false),
                    Topics = table.Column<string>(type: "text", nullable: false),
                    QuestionIds = table.Column<string>(type: "text", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "integer", nullable: true),
                    PassingScorePercentage = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GradeLevel = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    TeacherIds = table.Column<string>(type: "text", nullable: false),
                    StudentIds = table.Column<string>(type: "text", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    GradeLevel = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LearningObjectives = table.Column<string>(type: "text", nullable: false),
                    Topics = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CourseAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    GradeLevel = table.Column<int>(type: "integer", nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    Topics = table.Column<string>(type: "text", nullable: false),
                    LearningObjectives = table.Column<string>(type: "text", nullable: false),
                    AnswerOptions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CorrectAnswer = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    IrtDiscrimination = table.Column<double>(type: "double precision", nullable: true),
                    IrtDifficulty = table.Column<double>(type: "double precision", nullable: true),
                    IrtGuessing = table.Column<double>(type: "double precision", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TimesAnswered = table.Column<int>(type: "integer", nullable: false),
                    TimesCorrect = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: true),
                    CurrentQuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    EstimatedAbility = table.Column<double>(type: "double precision", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: true),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    IncorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    SkippedQuestions = table.Column<int>(type: "integer", nullable: false),
                    Feedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    XpEarned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentAnswer = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: false),
                    QuestionOrder = table.Column<int>(type: "integer", nullable: false),
                    AbilityAtTime = table.Column<double>(type: "double precision", nullable: true),
                    Feedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AiExplanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_responses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassIds = table.Column<string>(type: "text", nullable: false),
                    GradeLevel = table.Column<int>(type: "integer", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    ParentalConsentGranted = table.Column<bool>(type: "boolean", nullable: false),
                    ParentEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SubscriptionTier = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    XpPoints = table.Column<int>(type: "integer", nullable: false),
                    DailyStreak = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_AssessmentType",
                table: "assessments",
                column: "AssessmentType");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_CourseId",
                table: "assessments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_GradeLevel",
                table: "assessments",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_SchoolId",
                table: "assessments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_Subject",
                table: "assessments",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_classes_GradeLevel",
                table: "classes",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_classes_SchoolId",
                table: "classes",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_classes_SchoolId_Code",
                table: "classes",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_classes_Subject",
                table: "classes",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_courses_Code",
                table: "courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_courses_CourseAdminId",
                table: "courses",
                column: "CourseAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_courses_GradeLevel",
                table: "courses",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_courses_Subject",
                table: "courses",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_questions_ContentHash",
                table: "questions",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_questions_CourseId",
                table: "questions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_DifficultyLevel",
                table: "questions",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_questions_GradeLevel",
                table: "questions",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_questions_IsAiGenerated",
                table: "questions",
                column: "IsAiGenerated");

            migrationBuilder.CreateIndex(
                name: "IX_questions_QuestionType",
                table: "questions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_questions_Subject",
                table: "questions",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_schools_Code",
                table: "schools",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schools_IsActive",
                table: "schools",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_AssessmentId",
                table: "student_assessments",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_ClassId",
                table: "student_assessments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_CompletedAt",
                table: "student_assessments",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_SchoolId",
                table: "student_assessments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_Status",
                table: "student_assessments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_StudentId",
                table: "student_assessments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_assessments_StudentId_AssessmentId",
                table: "student_assessments",
                columns: new[] { "StudentId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_IsCorrect",
                table: "student_responses",
                column: "IsCorrect");

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_QuestionId",
                table: "student_responses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_SchoolId",
                table: "student_responses",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_StudentAssessmentId",
                table: "student_responses",
                column: "StudentAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_StudentAssessmentId_QuestionId",
                table: "student_responses",
                columns: new[] { "StudentAssessmentId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_responses_StudentId",
                table: "student_responses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_students_GradeLevel",
                table: "students",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_students_Level",
                table: "students",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_students_SchoolId",
                table: "students",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_students_SubscriptionTier",
                table: "students",
                column: "SubscriptionTier");

            migrationBuilder.CreateIndex(
                name: "IX_students_UserId",
                table: "students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_ExternalId",
                table: "users",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_users_SchoolId",
                table: "users",
                column: "SchoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "classes");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "schools");

            migrationBuilder.DropTable(
                name: "student_assessments");

            migrationBuilder.DropTable(
                name: "student_responses");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
