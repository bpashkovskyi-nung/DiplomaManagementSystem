using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DefenceDatePreferenceConfiguration : IEntityTypeConfiguration<DefenceDatePreference>
{
    public void Configure(EntityTypeBuilder<DefenceDatePreference> builder)
    {
        builder.ToTable("defence_date_preferences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequesterType)
            .HasConversion<short>();

        builder.HasIndex(e => e.DiplomaId)
            .IsUnique();

        builder.HasOne(e => e.Diploma)
            .WithOne(diploma => diploma.DefenceDatePreference)
            .HasForeignKey<DefenceDatePreference>(e => e.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.DefenceDateOption)
            .WithMany(option => option.Preferences)
            .HasForeignKey(e => e.DefenceDateOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
