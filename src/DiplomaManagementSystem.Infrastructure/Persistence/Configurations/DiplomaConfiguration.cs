using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaConfiguration : IEntityTypeConfiguration<Diploma>
{
    public void Configure(EntityTypeBuilder<Diploma> builder)
    {
        builder.ToTable("diplomas");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SupervisorAssignmentStatus)
            .HasConversion<short>();

        builder.Property(e => e.ReviewAssignmentStatus)
            .HasConversion<short>();

        builder.Property(e => e.LifecycleStatus)
            .HasConversion<short>();

        builder.Property(e => e.AdmissionStatus)
            .HasConversion<short>();

        builder.Property(e => e.CurrentAdmissionStep)
            .HasConversion<short>();

        builder.Property(e => e.StorageFolderId)
            .HasMaxLength(1024);

        builder.Property(e => e.RowVersion);

        builder.HasIndex(e => e.CurrentAdmissionStep);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => new { e.StudentId, e.DefenceSessionId })
            .IsUnique();

        builder.HasOne(e => e.DefenceSession)
            .WithMany(s => s.Diplomas)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.SupervisorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.ReviewerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
