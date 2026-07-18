using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExaminationCommissionParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "examination_commission_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_examination_commission_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_examination_commission_participants_defence_sessions_Defenc~",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_examination_commission_participants_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_examination_commission_participants_DefenceSessionId",
                table: "examination_commission_participants",
                column: "DefenceSessionId",
                unique: true,
                filter: "\"Role\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_examination_commission_participants_DefenceSessionId_Employ~",
                table: "examination_commission_participants",
                columns: new[] { "DefenceSessionId", "EmployeeId" },
                unique: true,
                filter: "\"EmployeeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_examination_commission_participants_DefenceSessionId_Role",
                table: "examination_commission_participants",
                columns: new[] { "DefenceSessionId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_examination_commission_participants_EmployeeId",
                table: "examination_commission_participants",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "examination_commission_participants");
        }
    }
}
