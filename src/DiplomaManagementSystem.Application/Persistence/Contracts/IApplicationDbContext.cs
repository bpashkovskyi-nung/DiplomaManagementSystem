using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }

    DbSet<Faculty> Faculties { get; }

    DbSet<Department> Departments { get; }

    DbSet<Specialty> Specialties { get; }

    DbSet<DepartmentAdminAssignment> DepartmentAdminAssignments { get; }

    DbSet<DepartmentEmployee> DepartmentEmployees { get; }

    DbSet<StudyGroup> StudyGroups { get; }

    DbSet<DefenceSession> DefenceSessions { get; }

    DbSet<AnnualRoleAssignment> AnnualRoleAssignments { get; }

    DbSet<ExaminationCommissionParticipant> ExaminationCommissionParticipants { get; }

    DbSet<EmployeeSessionWorkloadLimit> EmployeeSessionWorkloadLimits { get; }

    DbSet<DefenceSessionMilestone> DefenceSessionMilestones { get; }

    DbSet<DefenceDateOption> DefenceDateOptions { get; }

    DbSet<Diploma> Diplomas { get; }

    DbSet<DiplomaTopicVersion> DiplomaTopicVersions { get; }

    DbSet<DiplomaAdmissionStepAttempt> DiplomaAdmissionStepAttempts { get; }

    DbSet<DiplomaDocument> DiplomaDocuments { get; }

    DbSet<DiplomaComment> DiplomaComments { get; }

    DbSet<DiplomaMilestoneProgress> DiplomaMilestoneProgressEntries { get; }

    DbSet<DefenceDatePreference> DefenceDatePreferences { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
