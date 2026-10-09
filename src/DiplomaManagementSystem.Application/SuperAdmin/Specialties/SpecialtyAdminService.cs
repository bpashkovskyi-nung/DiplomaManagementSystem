using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.SuperAdmin.Specialties;

internal sealed class SpecialtyAdminService(IApplicationDbContext dbContext) : ISpecialtyAdminService
{
    public async Task<IReadOnlyList<SpecialtyListItemDto>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentExistsAsync(departmentId, cancellationToken);

        return await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.DepartmentId == departmentId)
            .OrderBy(specialty => specialty.Code)
            .ThenBy(specialty => specialty.Name)
            .Select(specialty => new SpecialtyListItemDto(
                specialty.Id,
                specialty.Code,
                specialty.Name,
                specialty.IsActive,
                dbContext.StudyGroups.Count(group => group.SpecialtyId == specialty.Id),
                specialty.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpecialtyOptionDto>> GetActiveOptionsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.DepartmentId == departmentId && specialty.IsActive)
            .OrderBy(specialty => specialty.Code)
            .ThenBy(specialty => specialty.Name)
            .Select(specialty => new SpecialtyOptionDto(
                specialty.Id,
                specialty.Code + " — " + specialty.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpecialtyOptionDto>> GetActiveOptionsForSessionAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        Guid? departmentId = await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Id == defenceSessionId)
            .Select(session => (Guid?)session.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (departmentId is not Guid id)
        {
            throw new DomainException("Defence session not found.");
        }

        return await GetActiveOptionsForDepartmentAsync(id, cancellationToken);
    }

    public async Task<Guid> CreateAsync(SpecialtyFormDto form, CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentExistsAsync(form.DepartmentId, cancellationToken);

        string code = form.Code.Trim();
        string name = form.Name.Trim();
        await EnsureUniqueCodeInDepartmentAsync(form.DepartmentId, code, excludeId: null, cancellationToken);

        Specialty specialty = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = form.DepartmentId,
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Specialties.Add(specialty);
        await dbContext.SaveChangesAsync(cancellationToken);

        return specialty.Id;
    }

    public async Task UpdateAsync(Guid id, SpecialtyFormDto form, CancellationToken cancellationToken = default)
    {
        Specialty specialty = await dbContext.Specialties
                                    .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                                ?? throw new DomainException("Спеціальність не знайдено.");

        if (specialty.DepartmentId != form.DepartmentId)
        {
            throw new DomainException("Неможливо змінити кафедру для спеціальності.");
        }

        string code = form.Code.Trim();
        string name = form.Name.Trim();
        await EnsureUniqueCodeInDepartmentAsync(form.DepartmentId, code, id, cancellationToken);

        specialty.Code = code;
        specialty.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Specialty specialty = await dbContext.Specialties
                                    .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                                ?? throw new DomainException("Спеціальність не знайдено.");

        bool hasStudyGroups = await dbContext.StudyGroups
            .AnyAsync(group => group.SpecialtyId == id, cancellationToken);

        if (hasStudyGroups)
        {
            throw new DomainException("Неможливо деактивувати спеціальність, яка використовується групами.");
        }

        specialty.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Id == departmentId, cancellationToken);

        if (!exists)
        {
            throw new DomainException(DepartmentMessages.DepartmentNotFound);
        }
    }

    private async Task EnsureUniqueCodeInDepartmentAsync(
        Guid departmentId,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Specialties
            .AsNoTracking()
            .AnyAsync(
                specialty => specialty.DepartmentId == departmentId
                             && specialty.Code == code
                             && (excludeId == null || specialty.Id != excludeId),
                cancellationToken);

        if (exists)
        {
            throw new DomainException("Спеціальність з таким кодом уже існує на цій кафедрі.");
        }
    }
}
