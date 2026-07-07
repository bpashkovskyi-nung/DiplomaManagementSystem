using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEmployeeSessionWorkloadLimits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "employee_session_workload_limits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                MaxSupervisorStudents = table.Column<int>(type: "integer", nullable: true),
                MaxReviewerStudents = table.Column<int>(type: "integer", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_session_workload_limits", x => x.Id);
                table.ForeignKey(
                    name: "FK_employee_session_workload_limits_users_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_employee_session_workload_limits_defence_sessions_DefenceSe~",
                    column: x => x.DefenceSessionId,
                    principalTable: "defence_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_employee_session_workload_limits_DefenceSessionId_EmployeeId",
            table: "employee_session_workload_limits",
            columns: new[] { "DefenceSessionId", "EmployeeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_employee_session_workload_limits_EmployeeId",
            table: "employee_session_workload_limits",
            column: "EmployeeId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "employee_session_workload_limits");
    }
}
