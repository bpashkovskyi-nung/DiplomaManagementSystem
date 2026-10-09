using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentAdminAssignmentConfiguration : IEntityTypeConfiguration<DepartmentAdminAssignment>
{
    public void Configure(EntityTypeBuilder<DepartmentAdminAssignment> builder)
    {
        builder.ToTable("department_admin_assignments");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.DepartmentId, e.UserId })
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.AdminAssignments)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
