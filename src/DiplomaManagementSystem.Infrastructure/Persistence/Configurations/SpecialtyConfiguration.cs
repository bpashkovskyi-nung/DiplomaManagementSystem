using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => new { e.DepartmentId, e.Code })
            .IsUnique();

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Specialties)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
