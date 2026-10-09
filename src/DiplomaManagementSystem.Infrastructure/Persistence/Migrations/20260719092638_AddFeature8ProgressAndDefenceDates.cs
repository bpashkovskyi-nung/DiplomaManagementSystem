using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeature8ProgressAndDefenceDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "defence_date_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defence_date_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_defence_date_options_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "defence_session_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedPercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defence_session_milestones", x => x.Id);
                    table.CheckConstraint("CK_defence_session_milestones_expected_percent", "\"ExpectedPercent\" >= 0 AND \"ExpectedPercent\" <= 100");
                    table.CheckConstraint("CK_defence_session_milestones_ordinal", "\"Ordinal\" >= 1 AND \"Ordinal\" <= 3");
                    table.ForeignKey(
                        name: "FK_defence_session_milestones_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "defence_date_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceDateOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterType = table.Column<short>(type: "smallint", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defence_date_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_defence_date_preferences_defence_date_options_DefenceDateOp~",
                        column: x => x.DefenceDateOptionId,
                        principalTable: "defence_date_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_defence_date_preferences_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_defence_date_preferences_users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diploma_milestone_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualPercent = table.Column<int>(type: "integer", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_milestone_progress", x => x.Id);
                    table.CheckConstraint("CK_diploma_milestone_progress_actual_percent", "\"ActualPercent\" >= 0 AND \"ActualPercent\" <= 100");
                    table.ForeignKey(
                        name: "FK_diploma_milestone_progress_defence_session_milestones_Miles~",
                        column: x => x.MilestoneId,
                        principalTable: "defence_session_milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_diploma_milestone_progress_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_milestone_progress_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_defence_date_options_DefenceSessionId_Date",
                table: "defence_date_options",
                columns: new[] { "DefenceSessionId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defence_date_preferences_DefenceDateOptionId",
                table: "defence_date_preferences",
                column: "DefenceDateOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_defence_date_preferences_DiplomaId",
                table: "defence_date_preferences",
                column: "DiplomaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defence_date_preferences_RequesterUserId",
                table: "defence_date_preferences",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_defence_session_milestones_DefenceSessionId_DueDate",
                table: "defence_session_milestones",
                columns: new[] { "DefenceSessionId", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defence_session_milestones_DefenceSessionId_Ordinal",
                table: "defence_session_milestones",
                columns: new[] { "DefenceSessionId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diploma_milestone_progress_DiplomaId_MilestoneId",
                table: "diploma_milestone_progress",
                columns: new[] { "DiplomaId", "MilestoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diploma_milestone_progress_MilestoneId",
                table: "diploma_milestone_progress",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_milestone_progress_RecordedByUserId",
                table: "diploma_milestone_progress",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "defence_date_preferences");

            migrationBuilder.DropTable(
                name: "diploma_milestone_progress");

            migrationBuilder.DropTable(
                name: "defence_date_options");

            migrationBuilder.DropTable(
                name: "defence_session_milestones");
        }
    }
}
