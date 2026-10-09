using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_years", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "defence_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defence_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_defence_sessions_academic_years_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "academic_years",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_study_groups_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserKind = table.Column<short>(type: "smallint", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_study_groups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalTable: "study_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "annual_role_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleType = table.Column<short>(type: "smallint", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_role_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_annual_role_assignments_academic_years_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "academic_years",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_annual_role_assignments_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diplomas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupervisorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupervisorAssignmentStatus = table.Column<short>(type: "smallint", nullable: false),
                    ReviewAssignmentStatus = table.Column<short>(type: "smallint", nullable: false),
                    LifecycleStatus = table.Column<short>(type: "smallint", nullable: false),
                    AdmissionStatus = table.Column<short>(type: "smallint", nullable: false),
                    DefenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diplomas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diplomas_defence_sessions_DefenceSessionId",
                        column: x => x.DefenceSessionId,
                        principalTable: "defence_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_diplomas_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_diplomas_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_diplomas_users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "supervisor_pool_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "diploma_admission_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    FormattingReviewOutcome = table.Column<short>(type: "smallint", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsSecretaryOverride = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "diploma_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diploma_comments_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diploma_topic_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_topic_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diploma_topic_versions_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_topic_versions_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_Label",
                table: "academic_years",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_role_assignments_AcademicYearId_RoleType",
                table: "annual_role_assignments",
                columns: new[] { "AcademicYearId", "RoleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_role_assignments_EmployeeId",
                table: "annual_role_assignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_DefenceSessionId",
                table: "audit_logs",
                column: "DefenceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_PerformedAt",
                table: "audit_logs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_PerformedById",
                table: "audit_logs",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_defence_sessions_AcademicYearId",
                table: "defence_sessions",
                column: "AcademicYearId");

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

            migrationBuilder.CreateIndex(
                name: "IX_diploma_comments_AuthorId",
                table: "diploma_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_comments_DiplomaId",
                table: "diploma_comments",
                column: "DiplomaId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_topic_versions_DiplomaId",
                table: "diploma_topic_versions",
                column: "DiplomaId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_topic_versions_DiplomaId_VersionNumber",
                table: "diploma_topic_versions",
                columns: new[] { "DiplomaId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diploma_topic_versions_ReviewedById",
                table: "diploma_topic_versions",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_DefenceSessionId",
                table: "diplomas",
                column: "DefenceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_ReviewerId",
                table: "diplomas",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_StudentId",
                table: "diplomas",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_StudentId_DefenceSessionId",
                table: "diplomas",
                columns: new[] { "StudentId", "DefenceSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diplomas_SupervisorId",
                table: "diplomas",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_DefenceSessionId",
                table: "study_groups",
                column: "DefenceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_Name",
                table: "study_groups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_pool_entries_DefenceSessionId_EmployeeId",
                table: "supervisor_pool_entries",
                columns: new[] { "DefenceSessionId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_pool_entries_EmployeeId",
                table: "supervisor_pool_entries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_StudyGroupId",
                table: "users",
                column: "StudyGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annual_role_assignments");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "diploma_admission_checkpoints");

            migrationBuilder.DropTable(
                name: "diploma_comments");

            migrationBuilder.DropTable(
                name: "diploma_topic_versions");

            migrationBuilder.DropTable(
                name: "supervisor_pool_entries");

            migrationBuilder.DropTable(
                name: "diplomas");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "study_groups");

            migrationBuilder.DropTable(
                name: "defence_sessions");

            migrationBuilder.DropTable(
                name: "academic_years");
        }
    }
}
