using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }

    DbSet<StudyGroup> StudyGroups { get; }

    DbSet<DefenceSession> DefenceSessions { get; }

    DbSet<AnnualRoleAssignment> AnnualRoleAssignments { get; }

    DbSet<EmployeeSessionWorkloadLimit> EmployeeSessionWorkloadLimits { get; }

    DbSet<Diploma> Diplomas { get; }

    DbSet<DiplomaTopicVersion> DiplomaTopicVersions { get; }

    DbSet<DiplomaAdmissionStepAttempt> DiplomaAdmissionStepAttempts { get; }

    DbSet<DiplomaDocument> DiplomaDocuments { get; }

    DbSet<DiplomaComment> DiplomaComments { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
