using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DefenceSessionMilestoneConfiguration : IEntityTypeConfiguration<DefenceSessionMilestone>
{
    public void Configure(EntityTypeBuilder<DefenceSessionMilestone> builder)
    {
        builder.ToTable("defence_session_milestones", table =>
        {
            table.HasCheckConstraint("CK_defence_session_milestones_ordinal", "\"Ordinal\" >= 1 AND \"Ordinal\" <= 3");
            table.HasCheckConstraint(
                "CK_defence_session_milestones_expected_percent",
                "\"ExpectedPercent\" >= 0 AND \"ExpectedPercent\" <= 100");
        });

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.DefenceSessionId, e.Ordinal })
            .IsUnique();

        builder.HasIndex(e => new { e.DefenceSessionId, e.DueDate })
            .IsUnique();

        builder.HasOne(e => e.DefenceSession)
            .WithMany(session => session.Milestones)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
