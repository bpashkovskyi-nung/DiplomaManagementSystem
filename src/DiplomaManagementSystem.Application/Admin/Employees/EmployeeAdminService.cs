using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.Employees;

internal sealed class EmployeeAdminService(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IUserProvisioningService userProvisioningService,
    CurrentDepartmentResolver currentDepartmentResolver) : IEmployeeAdminService
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        return await dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(employee => employee.DepartmentId == departmentId && employee.IsActive)
            .Join(
                dbContext.Users.AsNoTracking(),
                employee => employee.UserId,
                user => user.Id,
                (employee, user) => new { employee, user })
            .OrderBy(pair => pair.employee.FullName)
            .Select(pair => new EmployeeListItemDto(
                pair.user.Id,
                pair.employee.FullName,
                pair.user.Email ?? string.Empty,
                pair.employee.AcademicRank,
                pair.employee.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (DepartmentEmployee Employee, ApplicationUser User)? pair =
            await FindDepartmentEmployeePairAsync(id, asNoTracking: true, cancellationToken);

        return pair is null
            ? null
            : new EmployeeFormDto(
                pair.Value.User.Id,
                pair.Value.Employee.FullName,
                pair.Value.User.Email ?? string.Empty,
                pair.Value.Employee.AcademicRank,
                pair.Value.Employee.ShortDisplayName);
    }

    public async Task<EmployeeDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (DepartmentEmployee Employee, ApplicationUser User)? pair =
            await FindDepartmentEmployeePairAsync(id, asNoTracking: true, cancellationToken);

        if (pair is null)
        {
            return null;
        }

        bool hasAssignments = await HasBlockingAssignmentsAsync(id, cancellationToken);

        return new EmployeeDetailsDto(
            pair.Value.User.Id,
            pair.Value.Employee.FullName,
            pair.Value.User.Email ?? string.Empty,
            pair.Value.Employee.AcademicRank,
            hasAssignments,
            pair.Value.Employee.CreatedAt);
    }

    public async Task<Guid> CreateAsync(EmployeeFormDto dto, CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        ApplicationUser user = await userProvisioningService.CreateEmployeeAsync(
            dto.FullName.Trim(),
            dto.Email.Trim(),
            cancellationToken);

        bool alreadyInDepartment = await dbContext.DepartmentEmployees
            .AnyAsync(
                employee => employee.DepartmentId == departmentId && employee.UserId == user.Id,
                cancellationToken);

        if (alreadyInDepartment)
        {
            throw new DomainException("Викладач уже доданий до цієї кафедри.");
        }

        DepartmentEmployee departmentEmployee = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = user.Id,
            FullName = dto.FullName.Trim(),
            AcademicRank = dto.AcademicRank,
            ShortDisplayName = NormalizeShortDisplayName(dto.ShortDisplayName),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        user.AcademicRank = dto.AcademicRank;
        user.ShortDisplayName = departmentEmployee.ShortDisplayName;
        user.FullName = departmentEmployee.FullName;

        dbContext.DepartmentEmployees.Add(departmentEmployee);
        await userManager.UpdateAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, EmployeeFormDto dto, CancellationToken cancellationToken = default)
    {
        (DepartmentEmployee Employee, ApplicationUser User)? pair =
            await FindDepartmentEmployeePairAsync(id, asNoTracking: false, cancellationToken);

        if (pair is null)
        {
            throw new DomainException("Employee not found.");
        }

        DepartmentEmployee employee = pair.Value.Employee;
        ApplicationUser applicationUser = pair.Value.User;

        string email = dto.Email.Trim();
        if (!string.Equals(applicationUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            await userProvisioningService.EnsureEmailAvailableAsync(email, id, cancellationToken);

            applicationUser.Email = email;
            applicationUser.UserName = email;
            applicationUser.NormalizedEmail = email.ToUpperInvariant();
            applicationUser.NormalizedUserName = email.ToUpperInvariant();
        }

        employee.FullName = dto.FullName.Trim();
        employee.AcademicRank = dto.AcademicRank;
        employee.ShortDisplayName = NormalizeShortDisplayName(dto.ShortDisplayName);

        applicationUser.FullName = employee.FullName;
        applicationUser.AcademicRank = employee.AcademicRank;
        applicationUser.ShortDisplayName = employee.ShortDisplayName;

        IdentityResult result = await userManager.UpdateAsync(applicationUser);
        if (!result.Succeeded)
        {
            string details = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new DomainException($"Failed to update employee: {details}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        DepartmentEmployee? employee = await dbContext.DepartmentEmployees
            .FirstOrDefaultAsync(
                item => item.UserId == id && item.DepartmentId == departmentId,
                cancellationToken);

        if (employee is null)
        {
            throw new DomainException("Employee not found.");
        }

        if (await HasBlockingAssignmentsAsync(id, cancellationToken))
        {
            throw new DomainException("Cannot delete an employee linked to diplomas, roles, or audit records.");
        }

        dbContext.DepartmentEmployees.Remove(employee);

        bool hasOtherDepartments = await dbContext.DepartmentEmployees
            .AnyAsync(item => item.UserId == id && item.DepartmentId != departmentId, cancellationToken);

        if (!hasOtherDepartments)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(id.ToString());
            if (user is not null)
            {
                IdentityResult result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    string details = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new DomainException($"Failed to delete employee: {details}");
                }
            }
        }
        else
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(DepartmentEmployee Employee, ApplicationUser User)?> FindDepartmentEmployeePairAsync(
        Guid userId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        IQueryable<DepartmentEmployee> employeeQuery = dbContext.DepartmentEmployees
            .Where(employee => employee.UserId == userId && employee.DepartmentId == departmentId);

        if (asNoTracking)
        {
            employeeQuery = employeeQuery.AsNoTracking();
        }

        DepartmentEmployee? employee = await employeeQuery.FirstOrDefaultAsync(cancellationToken);
        if (employee is null)
        {
            return null;
        }

        ApplicationUser? user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!asNoTracking)
        {
            user = await dbContext.Users.FirstAsync(item => item.Id == userId, cancellationToken);
        }

        return (employee, user);
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
