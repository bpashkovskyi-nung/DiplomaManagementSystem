using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.Employees;

internal sealed class EmployeeAdminService(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IUserProvisioningService userProvisioningService) : IEmployeeAdminService
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Employee)
            .OrderBy(user => user.FullName)
            .Select(user => new EmployeeListItemDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.AcademicRank,
                user.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? employee = await FindEmployeeAsync(id, asNoTracking: true, cancellationToken);

        return employee is null
            ? null
            : new EmployeeFormDto(
                employee.Id,
                employee.FullName,
                employee.Email ?? string.Empty,
                employee.AcademicRank,
                employee.ShortDisplayName);
    }

    public async Task<EmployeeDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? employee = await FindEmployeeAsync(id, asNoTracking: true, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        bool hasAssignments = await HasBlockingAssignmentsAsync(id, cancellationToken);

        return new EmployeeDetailsDto(
            employee.Id,
            employee.FullName,
            employee.Email ?? string.Empty,
            employee.AcademicRank,
            hasAssignments,
            employee.CreatedAt);
    }

    public async Task<Guid> CreateAsync(EmployeeFormDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationUser user = await userProvisioningService.CreateEmployeeAsync(
            dto.FullName.Trim(),
            dto.Email.Trim(),
            cancellationToken);

        user.AcademicRank = dto.AcademicRank;
        user.ShortDisplayName = NormalizeShortDisplayName(dto.ShortDisplayName);
        await userManager.UpdateAsync(user);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, EmployeeFormDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationUser? employee = await FindEmployeeAsync(id, asNoTracking: false, cancellationToken);
        if (employee is null)
        {
            throw new DomainException("Employee not found.");
        }

        string email = dto.Email.Trim();
        if (!string.Equals(employee.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            await userProvisioningService.EnsureEmailAvailableAsync(email, id, cancellationToken);

            employee.Email = email;
            employee.UserName = email;
            employee.NormalizedEmail = email.ToUpperInvariant();
            employee.NormalizedUserName = email.ToUpperInvariant();
        }

        employee.FullName = dto.FullName.Trim();
        employee.AcademicRank = dto.AcademicRank;
        employee.ShortDisplayName = NormalizeShortDisplayName(dto.ShortDisplayName);

        IdentityResult result = await userManager.UpdateAsync(employee);
        if (!result.Succeeded)
        {
            string details = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new DomainException($"Failed to update employee: {details}");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? employee = await FindEmployeeAsync(id, asNoTracking: false, cancellationToken);
        if (employee is null)
        {
            throw new DomainException("Employee not found.");
        }

        if (await HasBlockingAssignmentsAsync(id, cancellationToken))
        {
            throw new DomainException("Cannot delete an employee linked to diplomas, roles, or audit records.");
        }

        IdentityResult result = await userManager.DeleteAsync(employee);
        if (!result.Succeeded)
        {
            string details = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new DomainException($"Failed to delete employee: {details}");
        }
    }

    private async Task<ApplicationUser?> FindEmployeeAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        IQueryable<ApplicationUser> query = dbContext.Users
            .Where(user => user.Id == id && user.UserKind == UserKind.Employee);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> HasBlockingAssignmentsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (await dbContext.Diplomas.AnyAsync(
                diploma => diploma.SupervisorId == employeeId || diploma.ReviewerId == employeeId,
                cancellationToken))
        {
            return true;
        }

        if (await dbContext.AnnualRoleAssignments.AnyAsync(
                assignment => assignment.EmployeeId == employeeId,
                cancellationToken))
        {
            return true;
        }

        if (await dbContext.DiplomaAdmissionStepAttempts.AnyAsync(
                attempt => attempt.RecordedById == employeeId,
                cancellationToken))
        {
            return true;
        }

        if (await dbContext.DiplomaComments.AnyAsync(
                comment => comment.AuthorId == employeeId,
                cancellationToken))
        {
            return true;
        }

        if (await dbContext.AuditLogs.AnyAsync(
                log => log.PerformedById == employeeId,
                cancellationToken))
        {
            return true;
        }

        return await dbContext.DiplomaTopicVersions.AnyAsync(
            version => version.ReviewedById == employeeId,
            cancellationToken);
    }

    private static string? NormalizeShortDisplayName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
