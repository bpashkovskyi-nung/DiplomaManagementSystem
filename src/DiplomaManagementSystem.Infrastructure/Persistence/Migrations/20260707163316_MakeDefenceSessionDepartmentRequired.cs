using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeDefenceSessionDepartmentRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill legacy sessions created before multi-tenancy. The default organization
            // (faculty + department) is seeded at application startup, so this migration must be
            // applied only after the app has run its seeder at least once.
            migrationBuilder.Sql(
                """
                UPDATE defence_sessions
                SET "DepartmentId" = (
                    SELECT "Id" FROM departments
                    WHERE "IsActive" = TRUE
                    ORDER BY "CreatedAt", "Id"
                    LIMIT 1)
                WHERE "DepartmentId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "defence_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "defence_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
