using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class StudyGroupConfiguration : IEntityTypeConfiguration<StudyGroup>
{
    public void Configure(EntityTypeBuilder<StudyGroup> builder)
    {
        builder.ToTable("study_groups");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Course);

        builder.Property(e => e.DefenceSessionId)
            .IsRequired();

        builder.Property(e => e.SpecialtyId)
            .IsRequired();

        builder.Property(e => e.StudyForm)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(e => new { e.DefenceSessionId, e.Name })
            .IsUnique();

        builder.HasIndex(e => e.DefenceSessionId);

        builder.HasIndex(e => e.SpecialtyId);

        builder.HasOne(e => e.DefenceSession)
            .WithMany(s => s.StudyGroups)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Specialty)
            .WithMany(s => s.StudyGroups)
            .HasForeignKey(e => e.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
