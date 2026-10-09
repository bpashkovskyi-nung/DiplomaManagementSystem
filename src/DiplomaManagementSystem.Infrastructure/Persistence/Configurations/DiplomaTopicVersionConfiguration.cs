using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaTopicVersionConfiguration : IEntityTypeConfiguration<DiplomaTopicVersion>
{
    public void Configure(EntityTypeBuilder<DiplomaTopicVersion> builder)
    {
        builder.ToTable("diploma_topic_versions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<short>();

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.DiplomaId);

        builder.HasIndex(e => new { e.DiplomaId, e.VersionNumber })
            .IsUnique();

        builder.HasOne(e => e.Diploma)
            .WithMany(d => d.TopicVersions)
            .HasForeignKey(e => e.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.SupervisorReviewedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
