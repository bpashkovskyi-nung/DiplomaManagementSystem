using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.SpecialtyCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.SpecialtyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.StudyForm)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(e => new { e.FacultyId, e.Name })
            .IsUnique();

        builder.HasOne(e => e.Faculty)
            .WithMany(f => f.Departments)
            .HasForeignKey(e => e.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
