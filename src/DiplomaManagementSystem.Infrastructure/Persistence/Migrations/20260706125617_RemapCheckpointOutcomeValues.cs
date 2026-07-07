using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemapCheckpointOutcomeValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE diploma_admission_step_attempts
                SET "Outcome" = 1
                WHERE "Outcome" = 2;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_diploma_admission_step_attempts_outcome",
                table: "diploma_admission_step_attempts",
                sql: "\"Outcome\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_diploma_admission_step_attempts_outcome",
                table: "diploma_admission_step_attempts");
        }
    }
}
