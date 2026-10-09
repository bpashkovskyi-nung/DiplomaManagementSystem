using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentScopedToDefenceSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefenceSessionId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE users AS u
                SET "DefenceSessionId" = d."DefenceSessionId"
                FROM diplomas AS d
                WHERE u."Id" = d."StudentId"
                  AND u."UserKind" = 0
                  AND u."DefenceSessionId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE users AS u
                SET "DefenceSessionId" = sg."DefenceSessionId"
                FROM study_groups AS sg
                WHERE u."StudyGroupId" = sg."Id"
                  AND u."UserKind" = 0
                  AND u."DefenceSessionId" IS NULL
                  AND sg."DefenceSessionId" IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_study_groups_Name",
                table: "study_groups");

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_DefenceSessionId_Name",
                table: "study_groups",
                columns: new[] { "DefenceSessionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_DefenceSessionId",
                table: "users",
                column: "DefenceSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_defence_sessions_DefenceSessionId",
                table: "users",
                column: "DefenceSessionId",
                principalTable: "defence_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_defence_sessions_DefenceSessionId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_DefenceSessionId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_study_groups_DefenceSessionId_Name",
                table: "study_groups");

            migrationBuilder.DropColumn(
                name: "DefenceSessionId",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_Name",
                table: "study_groups",
                column: "Name",
                unique: true);
        }
    }
}
