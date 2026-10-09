using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaMilestoneProgressConfiguration : IEntityTypeConfiguration<DiplomaMilestoneProgress>
{
    public void Configure(EntityTypeBuilder<DiplomaMilestoneProgress> builder)
    {
        builder.ToTable("diploma_milestone_progress", table =>
        {
            table.HasCheckConstraint(
                "CK_diploma_milestone_progress_actual_percent",
                "\"ActualPercent\" >= 0 AND \"ActualPercent\" <= 100");
        });

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.DiplomaId, e.MilestoneId })
            .IsUnique();

        builder.HasOne(e => e.Diploma)
            .WithMany(diploma => diploma.MilestoneProgressEntries)
            .HasForeignKey(e => e.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Milestone)
            .WithMany(milestone => milestone.ProgressEntries)
            .HasForeignKey(e => e.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
