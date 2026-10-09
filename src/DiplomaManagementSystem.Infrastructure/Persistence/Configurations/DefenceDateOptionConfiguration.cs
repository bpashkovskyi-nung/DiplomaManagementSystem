using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DefenceDateOptionConfiguration : IEntityTypeConfiguration<DefenceDateOption>
{
    public void Configure(EntityTypeBuilder<DefenceDateOption> builder)
    {
        builder.ToTable("defence_date_options");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.DefenceSessionId, e.Date })
            .IsUnique();

        builder.HasOne(e => e.DefenceSession)
            .WithMany(session => session.DefenceDateOptions)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
