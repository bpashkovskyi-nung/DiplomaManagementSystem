using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class AnnualRoleAssignmentConfiguration : IEntityTypeConfiguration<AnnualRoleAssignment>
{
    public void Configure(EntityTypeBuilder<AnnualRoleAssignment> builder)
    {
        builder.ToTable("annual_role_assignments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleType)
            .HasConversion<short>();

        builder.HasIndex(e => new { e.DefenceSessionId, e.RoleType })
            .IsUnique();

        builder.HasOne(e => e.DefenceSession)
            .WithMany(session => session.RoleAssignments)
            .HasForeignKey(e => e.DefenceSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
