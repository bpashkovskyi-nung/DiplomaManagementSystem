using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApprovedWithRemarksAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM diploma_admission_step_attempts
                WHERE "Outcome" = 1;
                """);

            migrationBuilder.Sql(
                """
                WITH step_state AS (
                    SELECT
                        d."Id",
                        d."AdmissionStatus",
                        d."ReviewAssignmentStatus",
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 0
                              AND a."Outcome" = 0) AS supervisor_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 1
                              AND a."Outcome" = 0) AS formatting_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 2
                              AND a."Outcome" = 0) AS antiplagiarism_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 4
                              AND a."Outcome" = 0) AS review_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id") AS has_attempts
                    FROM diplomas d
                ),
                resolved_step AS (
                    SELECT
                        "Id",
                        CASE
                            WHEN "AdmissionStatus" = 1 THEN NULL
                            WHEN NOT supervisor_passing THEN 0
                            WHEN NOT formatting_passing THEN 1
                            WHEN NOT antiplagiarism_passing THEN 2
                            WHEN "ReviewAssignmentStatus" = 0 THEN 3
                            WHEN "ReviewAssignmentStatus" = 1 AND NOT review_passing THEN 4
                            ELSE NULL
                        END AS new_step,
                        supervisor_passing,
                        formatting_passing,
                        antiplagiarism_passing,
                        review_passing,
                        has_attempts,
                        "AdmissionStatus"
                    FROM step_state
                )
                UPDATE diplomas d
                SET "CurrentAdmissionStep" = r.new_step,
                    "UpdatedAt" = NOW()
                FROM resolved_step r
                WHERE d."Id" = r."Id"
                  AND r."AdmissionStatus" = 0
                  AND d."CurrentAdmissionStep" IS DISTINCT FROM r.new_step;
                """);

            migrationBuilder.Sql(
                """
                WITH step_state AS (
                    SELECT
                        d."Id",
                        d."AdmissionStatus",
                        d."ReviewAssignmentStatus",
                        d."CurrentAdmissionStep",
                        d."LifecycleStatus",
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 0
                              AND a."Outcome" = 0) AS supervisor_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 1
                              AND a."Outcome" = 0) AS formatting_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 2
                              AND a."Outcome" = 0) AS antiplagiarism_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id"
                              AND a."Step" = 4
                              AND a."Outcome" = 0) AS review_passing,
                        EXISTS (
                            SELECT 1
                            FROM diploma_admission_step_attempts a
                            WHERE a."DiplomaId" = d."Id") AS has_attempts
                    FROM diplomas d
                ),
                resolved_step AS (
                    SELECT
                        "Id",
                        supervisor_passing,
                        formatting_passing,
                        antiplagiarism_passing,
                        review_passing,
                        has_attempts,
                        "AdmissionStatus",
                        "ReviewAssignmentStatus",
                        "CurrentAdmissionStep",
                        "LifecycleStatus"
                    FROM step_state
                )
                UPDATE diplomas d
                SET "LifecycleStatus" = CASE
                        WHEN r."AdmissionStatus" = 1 THEN 7
                        WHEN r.supervisor_passing
                             AND r.formatting_passing
                             AND r.antiplagiarism_passing
                             AND r.review_passing
                             AND r."ReviewAssignmentStatus" = 2 THEN 6
                        WHEN r.has_attempts OR d."CurrentAdmissionStep" IS NOT NULL THEN 5
                        ELSE d."LifecycleStatus"
                    END,
                    "UpdatedAt" = NOW()
                FROM resolved_step r
                WHERE d."Id" = r."Id"
                  AND (
                        (r."AdmissionStatus" = 1 AND d."LifecycleStatus" <> 7)
                        OR (r."AdmissionStatus" = 0
                            AND r.supervisor_passing
                            AND r.formatting_passing
                            AND r.antiplagiarism_passing
                            AND r.review_passing
                            AND r."ReviewAssignmentStatus" = 2
                            AND d."LifecycleStatus" <> 6)
                        OR (r."AdmissionStatus" = 0
                            AND (r.has_attempts OR d."CurrentAdmissionStep" IS NOT NULL)
                            AND NOT (
                                r.supervisor_passing
                                AND r.formatting_passing
                                AND r.antiplagiarism_passing
                                AND r.review_passing
                                AND r."ReviewAssignmentStatus" = 2)
                            AND d."LifecycleStatus" NOT IN (5, 0, 1, 2, 3, 4)));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
