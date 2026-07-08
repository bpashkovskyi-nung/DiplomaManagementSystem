using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.SuperAdmin.Faculties;

internal sealed class FacultyAdminService(IApplicationDbContext dbContext) : IFacultyAdminService
{
    public async Task<IReadOnlyList<FacultyListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Faculties
            .AsNoTracking()
            .OrderBy(faculty => faculty.Name)
            .Select(faculty => new FacultyListItemDto(
                faculty.Id,
                faculty.Name,
                faculty.IsActive,
                faculty.Departments.Count,
                faculty.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<FacultyFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Faculty? faculty = await dbContext.Faculties
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return faculty is null ? null : new FacultyFormDto(faculty.Id, faculty.Name);
    }

    public async Task<Guid> CreateAsync(FacultyFormDto form, CancellationToken cancellationToken = default)
    {
        string name = form.Name.Trim();
        await EnsureUniqueNameAsync(name, excludeId: null, cancellationToken);

        Faculty faculty = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Faculties.Add(faculty);
        await dbContext.SaveChangesAsync(cancellationToken);

        return faculty.Id;
    }

    public async Task UpdateAsync(Guid id, FacultyFormDto form, CancellationToken cancellationToken = default)
    {
        Faculty faculty = await dbContext.Faculties
                              .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                          ?? throw new DomainException(DepartmentMessages.FacultyNotFound);

        string name = form.Name.Trim();
        await EnsureUniqueNameAsync(name, id, cancellationToken);

        faculty.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Faculty faculty = await dbContext.Faculties
                              .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                          ?? throw new DomainException(DepartmentMessages.FacultyNotFound);

        faculty.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Faculties
            .AsNoTracking()
            .AnyAsync(
                faculty => faculty.Name == name && (excludeId == null || faculty.Id != excludeId),
                cancellationToken);

        if (exists)
        {
            throw new DomainException(DepartmentMessages.DuplicateFacultyName);
        }
    }
}
