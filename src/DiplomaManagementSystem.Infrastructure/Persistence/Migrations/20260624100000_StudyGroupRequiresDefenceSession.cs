using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudyGroupRequiresDefenceSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE study_groups AS sg
                SET "DefenceSessionId" = sub."DefenceSessionId"
                FROM (
                    SELECT DISTINCT u."StudyGroupId", u."DefenceSessionId"
                    FROM users AS u
                    WHERE u."StudyGroupId" IS NOT NULL
                      AND u."DefenceSessionId" IS NOT NULL
                ) AS sub
                WHERE sg."Id" = sub."StudyGroupId"
                  AND sg."DefenceSessionId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM study_groups
                WHERE "DefenceSessionId" IS NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_study_groups_defence_sessions_DefenceSessionId",
                table: "study_groups");

            migrationBuilder.AlterColumn<Guid>(
                name: "DefenceSessionId",
                table: "study_groups",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_study_groups_defence_sessions_DefenceSessionId",
                table: "study_groups",
                column: "DefenceSessionId",
                principalTable: "defence_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_study_groups_defence_sessions_DefenceSessionId",
                table: "study_groups");

            migrationBuilder.AlterColumn<Guid>(
                name: "DefenceSessionId",
                table: "study_groups",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_study_groups_defence_sessions_DefenceSessionId",
                table: "study_groups",
                column: "DefenceSessionId",
                principalTable: "defence_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
