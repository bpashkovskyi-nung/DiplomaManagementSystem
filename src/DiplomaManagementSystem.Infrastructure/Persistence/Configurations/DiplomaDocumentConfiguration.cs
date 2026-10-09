using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaDocumentConfiguration : IEntityTypeConfiguration<DiplomaDocument>
{
    public void Configure(EntityTypeBuilder<DiplomaDocument> builder)
    {
        builder.ToTable("diploma_documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Kind)
            .HasConversion<short>();

        builder.Property(document => document.StorageFileId)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(document => document.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(document => document.MimeType)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(document => document.DiplomaId);
        builder.HasIndex(document => new { document.DiplomaId, document.Kind, document.VersionNumber })
            .IsUnique();

        builder.HasOne(document => document.Diploma)
            .WithMany(diploma => diploma.Documents)
            .HasForeignKey(document => document.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(document => document.AdmissionStepAttempt)
            .WithMany()
            .HasForeignKey(document => document.AdmissionStepAttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(document => document.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
