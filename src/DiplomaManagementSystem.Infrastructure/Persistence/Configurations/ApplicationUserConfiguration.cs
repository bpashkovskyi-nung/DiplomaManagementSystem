using DiplomaManagementSystem.Application.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.AcademicRank)
            .HasConversion<short>();

        builder.Property(e => e.ShortDisplayName)
            .HasMaxLength(64);

        builder.Property(e => e.UserKind)
            .HasConversion<short>();

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.HasIndex(e => e.DefenceSessionId);

        builder.HasOne(e => e.StudyGroup)
            .WithMany()
            .HasForeignKey(e => e.StudyGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.DefenceSession)
            .WithMany()
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
