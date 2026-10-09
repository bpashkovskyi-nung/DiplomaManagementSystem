using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaAdmissionStepAttemptConfiguration : IEntityTypeConfiguration<DiplomaAdmissionStepAttempt>
{
    public void Configure(EntityTypeBuilder<DiplomaAdmissionStepAttempt> builder)
    {
        builder.ToTable("diploma_admission_step_attempts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Step)
            .HasConversion<short>();

        builder.Property(e => e.Outcome)
            .HasConversion<short>();

        builder.Property(e => e.Comment)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.DiplomaId);

        builder.HasIndex(e => new { e.DiplomaId, e.Step, e.AttemptNumber })
            .IsUnique();

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_diploma_admission_step_attempts_outcome",
            "\"Outcome\" IN (0, 1)"));

        builder.HasOne(e => e.Diploma)
            .WithMany(d => d.AdmissionStepAttempts)
            .HasForeignKey(e => e.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
