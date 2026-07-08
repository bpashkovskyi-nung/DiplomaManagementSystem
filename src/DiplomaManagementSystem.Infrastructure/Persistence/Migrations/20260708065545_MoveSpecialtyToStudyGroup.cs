using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveSpecialtyToStudyGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "specialties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_specialties_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_specialties_DepartmentId_Code",
                table: "specialties",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpecialtyId",
                table: "study_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudyForm",
                table: "study_groups",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO specialties ("Id", "DepartmentId", "Code", "Name", "IsActive", "CreatedAt")
                SELECT gen_random_uuid(), d."Id",
                       COALESCE(NULLIF(TRIM(d."SpecialtyCode"), ''), '000'),
                       COALESCE(NULLIF(TRIM(d."SpecialtyName"), ''), d."Name"),
                       TRUE, NOW()
                FROM departments d;
                """);

            migrationBuilder.Sql(
                """
                UPDATE study_groups sg
                SET "SpecialtyId" = sp."Id",
                    "StudyForm" = COALESCE(NULLIF(TRIM(d."StudyForm"), ''), 'очної форми навчання')
                FROM defence_sessions ds
                JOIN departments d ON d."Id" = ds."DepartmentId"
                JOIN specialties sp ON sp."DepartmentId" = d."Id"
                WHERE sg."DefenceSessionId" = ds."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE study_groups
                SET "SpecialtyId" = (
                        SELECT s."Id"
                        FROM specialties s
                        ORDER BY s."CreatedAt", s."Id"
                        LIMIT 1
                    ),
                    "StudyForm" = COALESCE("StudyForm", 'очної форми навчання')
                WHERE "SpecialtyId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SpecialtyId",
                table: "study_groups",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StudyForm",
                table: "study_groups",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "SpecialtyCode",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "SpecialtyName",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "StudyForm",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_SpecialtyId",
                table: "study_groups",
                column: "SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_study_groups_specialties_SpecialtyId",
                table: "study_groups",
                column: "SpecialtyId",
                principalTable: "specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_study_groups_specialties_SpecialtyId",
                table: "study_groups");

            migrationBuilder.DropIndex(
                name: "IX_study_groups_SpecialtyId",
                table: "study_groups");

            migrationBuilder.AddColumn<string>(
                name: "SpecialtyCode",
                table: "departments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecialtyName",
                table: "departments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudyForm",
                table: "departments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE departments d
                SET "SpecialtyCode" = s."Code",
                    "SpecialtyName" = s."Name",
                    "StudyForm" = COALESCE(
                        (
                            SELECT sg."StudyForm"
                            FROM study_groups sg
                            JOIN defence_sessions ds ON ds."Id" = sg."DefenceSessionId"
                            WHERE ds."DepartmentId" = d."Id"
                            ORDER BY sg."CreatedAt", sg."Id"
                            LIMIT 1
                        ),
                        'очної форми навчання')
                FROM specialties s
                WHERE s."DepartmentId" = d."Id"
                  AND s."Id" = (
                      SELECT s2."Id"
                      FROM specialties s2
                      WHERE s2."DepartmentId" = d."Id"
                      ORDER BY s2."CreatedAt", s2."Id"
                      LIMIT 1);
                """);

            migrationBuilder.DropColumn(
                name: "SpecialtyId",
                table: "study_groups");

            migrationBuilder.DropColumn(
                name: "StudyForm",
                table: "study_groups");

            migrationBuilder.DropTable(
                name: "specialties");
        }
    }
}
