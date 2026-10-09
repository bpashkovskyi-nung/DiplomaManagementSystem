using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupervisorPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supervisor_pool_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supervisor_pool_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supervisor_pool_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supervisor_pool_entries_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supervisor_pool_entries_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_pool_entries_DefenceSessionId_EmployeeId",
                table: "supervisor_pool_entries",
                columns: new[] { "DefenceSessionId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_pool_entries_EmployeeId",
                table: "supervisor_pool_entries",
                column: "EmployeeId");
        }
    }
}
