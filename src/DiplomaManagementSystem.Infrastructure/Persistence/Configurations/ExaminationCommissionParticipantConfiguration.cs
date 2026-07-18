using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class ExaminationCommissionParticipantConfiguration
    : IEntityTypeConfiguration<ExaminationCommissionParticipant>
{
    public void Configure(EntityTypeBuilder<ExaminationCommissionParticipant> builder)
    {
        builder.ToTable("examination_commission_participants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Role)
            .HasConversion<short>();

        builder.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Position)
            .HasMaxLength(300)
            .IsRequired();

        builder.HasIndex(e => new { e.DefenceSessionId, e.Role });

        builder.HasIndex(e => new { e.DefenceSessionId, e.EmployeeId })
            .IsUnique()
            .HasFilter("\"EmployeeId\" IS NOT NULL");

        builder.HasIndex(e => e.DefenceSessionId)
            .IsUnique()
            .HasFilter($"\"Role\" = {(short)ExaminationCommissionRole.Chair}");

        builder.HasOne(e => e.DefenceSession)
            .WithMany(session => session.ExaminationCommissionParticipants)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
