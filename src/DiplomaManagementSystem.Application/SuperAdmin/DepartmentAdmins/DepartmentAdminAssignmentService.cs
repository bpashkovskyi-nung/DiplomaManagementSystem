using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins;

internal sealed class DepartmentAdminAssignmentService(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IDepartmentAdminAssignmentService
{
    public async Task<IReadOnlyList<DepartmentAdminListItemDto>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DepartmentAdminAssignments
            .AsNoTracking()
            .Where(assignment => assignment.DepartmentId == departmentId)
            .Join(
                dbContext.Users.AsNoTracking(),
                assignment => assignment.UserId,
                user => user.Id,
                (assignment, user) => new { assignment, user })
            .OrderBy(pair => pair.user.FullName)
            .Select(pair => new DepartmentAdminListItemDto(
                pair.assignment.Id,
                pair.user.Id,
                pair.user.FullName,
                pair.user.Email ?? string.Empty,
                pair.assignment.AssignedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentEmployeeOptionDto>> GetAssignableEmployeesAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> assignedUserIds = await dbContext.DepartmentAdminAssignments
            .AsNoTracking()
            .Where(assignment => assignment.DepartmentId == departmentId)
            .Select(assignment => assignment.UserId)
            .ToListAsync(cancellationToken);

        return await dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(link => link.DepartmentId == departmentId)
            .Join(
                dbContext.Users.AsNoTracking(),
                link => link.UserId,
                user => user.Id,
                (link, user) => user)
            .Where(user => !assignedUserIds.Contains(user.Id))
            .OrderBy(user => user.FullName)
            .Select(user => new DepartmentEmployeeOptionDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignAsync(DepartmentAdminAssignDto dto, CancellationToken cancellationToken = default)
    {
        bool departmentExists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Id == dto.DepartmentId, cancellationToken);

        if (!departmentExists)
        {
            throw new DomainException(DepartmentMessages.DepartmentNotFound);
        }

        bool isDepartmentEmployee = await dbContext.DepartmentEmployees
            .AnyAsync(
                link => link.DepartmentId == dto.DepartmentId && link.UserId == dto.UserId,
                cancellationToken);

        if (!isDepartmentEmployee)
        {
            throw new DomainException("Обраного викладача не знайдено на цій кафедрі.");
        }

        ApplicationUser? user = await userManager.FindByIdAsync(dto.UserId.ToString());
        if (user is null)
        {
            throw new DomainException("Користувача не знайдено.");
        }

        bool alreadyAssigned = await dbContext.DepartmentAdminAssignments
            .AnyAsync(
                assignment => assignment.DepartmentId == dto.DepartmentId && assignment.UserId == user.Id,
                cancellationToken);

        if (alreadyAssigned)
        {
            throw new DomainException("Користувач уже призначений адміністратором цієї кафедри.");
        }

        dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
        {
            Id = Guid.NewGuid(),
            DepartmentId = dto.DepartmentId,
            UserId = user.Id,
            AssignedAt = DateTimeOffset.UtcNow,
        });

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, RoleNames.Admin);
            if (!roleResult.Succeeded)
            {
                string details = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                throw new DomainException($"Не вдалося призначити роль Admin: {details}");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        DepartmentAdminAssignment? assignment = await dbContext.DepartmentAdminAssignments
            .FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            throw new DomainException("Призначення адміністратора не знайдено.");
        }

        dbContext.DepartmentAdminAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
