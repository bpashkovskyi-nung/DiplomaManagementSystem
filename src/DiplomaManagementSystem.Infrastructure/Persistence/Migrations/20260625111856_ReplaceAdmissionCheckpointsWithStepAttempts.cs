using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAdmissionCheckpointsWithStepAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "CurrentAdmissionStep",
                table: "diplomas",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "diploma_admission_step_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Step = table.Column<short>(type: "smallint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<short>(type: "smallint", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecordedById = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSecretaryOverride = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_admission_step_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diploma_admission_step_attempts_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_admission_step_attempts_users_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_CurrentAdmissionStep",
                table: "diplomas",
                column: "CurrentAdmissionStep");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_step_attempts_DiplomaId",
                table: "diploma_admission_step_attempts",
                column: "DiplomaId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_step_attempts_DiplomaId_Step_AttemptNumber",
                table: "diploma_admission_step_attempts",
                columns: new[] { "DiplomaId", "Step", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_step_attempts_RecordedById",
                table: "diploma_admission_step_attempts",
                column: "RecordedById");

            migrationBuilder.Sql(
                """
                INSERT INTO diploma_admission_step_attempts (
                    "Id", "DiplomaId", "Step", "AttemptNumber", "Outcome", "Comment",
                    "RecordedById", "RecordedAt", "IsSecretaryOverride")
                SELECT
                    gen_random_uuid(),
                    cp."DiplomaId",
                    CASE cp."Type"
                        WHEN 0 THEN 0
                        WHEN 1 THEN 4
                        WHEN 2 THEN 2
                        WHEN 3 THEN 1
                    END,
                    1,
                    COALESCE(cp."FormattingReviewOutcome", 0),
                    cp."Comment",
                    cp."CompletedById",
                    COALESCE(cp."CompletedAt", now()),
                    cp."IsSecretaryOverride"
                FROM diploma_admission_checkpoints cp
                WHERE cp."IsCompleted" = true
                  AND cp."CompletedById" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE diplomas d
                SET "CurrentAdmissionStep" = sub.current_step
                FROM (
                    SELECT
                        d2."Id" AS diploma_id,
                        CASE
                            WHEN NOT EXISTS (
                                SELECT 1 FROM diploma_admission_checkpoints x
                                WHERE x."DiplomaId" = d2."Id")
                                THEN NULL::smallint
                            WHEN NOT EXISTS (
                                SELECT 1 FROM diploma_admission_checkpoints x
                                WHERE x."DiplomaId" = d2."Id"
                                  AND x."Type" = 0
                                  AND x."IsCompleted" = true
                                  AND COALESCE(x."FormattingReviewOutcome", 0) IN (0, 1))
                                THEN 0
                            WHEN NOT EXISTS (
                                SELECT 1 FROM diploma_admission_checkpoints x
                                WHERE x."DiplomaId" = d2."Id"
                                  AND x."Type" = 3
                                  AND x."IsCompleted" = true
                                  AND COALESCE(x."FormattingReviewOutcome", 0) IN (0, 1))
                                THEN 1
                            WHEN NOT EXISTS (
                                SELECT 1 FROM diploma_admission_checkpoints x
                                WHERE x."DiplomaId" = d2."Id"
                                  AND x."Type" = 2
                                  AND x."IsCompleted" = true
                                  AND COALESCE(x."FormattingReviewOutcome", 0) IN (0, 1))
                                THEN 2
                            WHEN d2."ReviewAssignmentStatus" = 0 THEN 3
                            WHEN NOT EXISTS (
                                SELECT 1 FROM diploma_admission_checkpoints x
                                WHERE x."DiplomaId" = d2."Id"
                                  AND x."Type" = 1
                                  AND x."IsCompleted" = true
                                  AND COALESCE(x."FormattingReviewOutcome", 0) IN (0, 1))
                                THEN 4
                            ELSE NULL::smallint
                        END AS current_step
                    FROM diplomas d2
                    WHERE EXISTS (
                        SELECT 1 FROM diploma_admission_checkpoints cp
                        WHERE cp."DiplomaId" = d2."Id")
                ) sub
                WHERE d."Id" = sub.diploma_id;
                """);

            migrationBuilder.Sql(
                """
                UPDATE diplomas d
                SET "CurrentAdmissionStep" = 0
                WHERE EXISTS (
                    SELECT 1 FROM diploma_admission_checkpoints cp
                    WHERE cp."DiplomaId" = d."Id")
                  AND d."CurrentAdmissionStep" IS NULL
                  AND d."LifecycleStatus" = 5;
                """);

            migrationBuilder.DropTable(
                name: "diploma_admission_checkpoints");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diploma_admission_step_attempts");

            migrationBuilder.DropIndex(
                name: "IX_diplomas_CurrentAdmissionStep",
                table: "diplomas");

            migrationBuilder.DropColumn(
                name: "CurrentAdmissionStep",
                table: "diplomas");

            migrationBuilder.CreateTable(
                name: "diploma_admission_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecretaryOverride = table.Column<bool>(type: "boolean", nullable: false),
                    Outcome = table.Column<short>(type: "smallint", nullable: true),
                    Type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_admission_checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diploma_admission_checkpoints_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_admission_checkpoints_users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_checkpoints_CompletedById",
                table: "diploma_admission_checkpoints",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_checkpoints_DiplomaId",
                table: "diploma_admission_checkpoints",
                column: "DiplomaId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_admission_checkpoints_DiplomaId_Type",
                table: "diploma_admission_checkpoints",
                columns: new[] { "DiplomaId", "Type" },
                unique: true);
        }
    }
}
