using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiplomaDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageFolderId",
                table: "diplomas",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "diploma_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<short>(type: "smallint", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    StorageFileId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AdmissionStepAttemptId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diploma_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_diploma_documents_diploma_admission_step_attempts_Admission~",
                        column: x => x.AdmissionStepAttemptId,
                        principalTable: "diploma_admission_step_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_diploma_documents_diplomas_DiplomaId",
                        column: x => x.DiplomaId,
                        principalTable: "diplomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_diploma_documents_users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diploma_documents_AdmissionStepAttemptId",
                table: "diploma_documents",
                column: "AdmissionStepAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_documents_DiplomaId",
                table: "diploma_documents",
                column: "DiplomaId");

            migrationBuilder.CreateIndex(
                name: "IX_diploma_documents_DiplomaId_Kind_VersionNumber",
                table: "diploma_documents",
                columns: new[] { "DiplomaId", "Kind", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diploma_documents_UploadedById",
                table: "diploma_documents",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diploma_documents");

            migrationBuilder.DropColumn(
                name: "StorageFolderId",
                table: "diplomas");
        }
    }
}
