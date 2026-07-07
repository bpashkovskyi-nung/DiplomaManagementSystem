using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeSessionWorkloadLimitConfiguration : IEntityTypeConfiguration<EmployeeSessionWorkloadLimit>
{
    public void Configure(EntityTypeBuilder<EmployeeSessionWorkloadLimit> builder)
    {
        builder.ToTable("employee_session_workload_limits");

        builder.HasKey(limit => limit.Id);

        builder.HasIndex(limit => new { limit.DefenceSessionId, limit.EmployeeId })
            .IsUnique();

        builder.HasOne(limit => limit.DefenceSession)
            .WithMany()
            .HasForeignKey(limit => limit.DefenceSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(limit => limit.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
