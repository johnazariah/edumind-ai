using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicAssessment.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardName",
                table: "questions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "questions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModuleName",
                table: "questions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoardName",
                table: "courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "courses",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModuleName",
                table: "courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_BoardName",
                table: "questions",
                column: "BoardName");

            migrationBuilder.CreateIndex(
                name: "IX_questions_ModuleName",
                table: "questions",
                column: "ModuleName");

            migrationBuilder.CreateIndex(
                name: "IX_courses_BoardName",
                table: "courses",
                column: "BoardName");

            migrationBuilder.CreateIndex(
                name: "IX_courses_ModuleName",
                table: "courses",
                column: "ModuleName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_questions_BoardName",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_questions_ModuleName",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_courses_BoardName",
                table: "courses");

            migrationBuilder.DropIndex(
                name: "IX_courses_ModuleName",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "BoardName",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "ModuleName",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "BoardName",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "ModuleName",
                table: "courses");
        }
    }
}
