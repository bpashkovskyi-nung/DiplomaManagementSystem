using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RequireReviewerBeforeWork : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM diploma_admission_step_attempts a
            USING diplomas d
            WHERE a."DiplomaId" = d."Id"
              AND d."AdmissionStatus" = 0
              AND (d."ReviewerId" IS NULL OR d."ReviewAssignmentStatus" = 0);

            UPDATE diplomas
            SET "CurrentAdmissionStep" = NULL,
                "LifecycleStatus" = 3,
                "ReviewAssignmentStatus" = 0,
                "ReviewerId" = NULL,
                "UpdatedAt" = NOW()
            WHERE "AdmissionStatus" = 0
              AND ("ReviewerId" IS NULL OR "ReviewAssignmentStatus" = 0)
              AND (
                    "LifecycleStatus" > 3
                    OR "CurrentAdmissionStep" IS NOT NULL
                  );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
