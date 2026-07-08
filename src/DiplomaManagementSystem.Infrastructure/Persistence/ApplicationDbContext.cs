using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DiplomaManagementSystem.Infrastructure.Persistence;

public sealed partial class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Faculty> Faculties => Set<Faculty>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<DepartmentAdminAssignment> DepartmentAdminAssignments => Set<DepartmentAdminAssignment>();

    public DbSet<DepartmentEmployee> DepartmentEmployees => Set<DepartmentEmployee>();

    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();

    public DbSet<DefenceSession> DefenceSessions => Set<DefenceSession>();

    public DbSet<AnnualRoleAssignment> AnnualRoleAssignments => Set<AnnualRoleAssignment>();

    public DbSet<EmployeeSessionWorkloadLimit> EmployeeSessionWorkloadLimits => Set<EmployeeSessionWorkloadLimit>();

    public DbSet<Diploma> Diplomas => Set<Diploma>();

    public DbSet<DiplomaTopicVersion> DiplomaTopicVersions => Set<DiplomaTopicVersion>();

    public DbSet<DiplomaAdmissionStepAttempt> DiplomaAdmissionStepAttempts => Set<DiplomaAdmissionStepAttempt>();

    public DbSet<DiplomaDocument> DiplomaDocuments => Set<DiplomaDocument>();

    public DbSet<DiplomaComment> DiplomaComments => Set<DiplomaComment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public async Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfApplicationDbTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
