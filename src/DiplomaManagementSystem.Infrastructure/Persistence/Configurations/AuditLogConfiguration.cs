using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.PerformedAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DefenceSession)
            .WithMany()
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
