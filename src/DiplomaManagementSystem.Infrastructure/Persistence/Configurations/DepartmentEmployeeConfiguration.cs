using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentEmployeeConfiguration : IEntityTypeConfiguration<DepartmentEmployee>
{
    public void Configure(EntityTypeBuilder<DepartmentEmployee> builder)
    {
        builder.ToTable("department_employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.AcademicRank)
            .HasConversion<short>();

        builder.Property(e => e.ShortDisplayName)
            .HasMaxLength(64);

        builder.HasIndex(e => new { e.DepartmentId, e.UserId })
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
