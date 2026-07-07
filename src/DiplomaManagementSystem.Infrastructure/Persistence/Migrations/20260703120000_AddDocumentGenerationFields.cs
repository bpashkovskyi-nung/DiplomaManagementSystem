using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentGenerationFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "AcademicRank",
            table: "users",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ShortDisplayName",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Course",
            table: "study_groups",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AcademicRank",
            table: "users");

        migrationBuilder.DropColumn(
            name: "ShortDisplayName",
            table: "users");

        migrationBuilder.DropColumn(
            name: "Course",
            table: "study_groups");
    }
}
