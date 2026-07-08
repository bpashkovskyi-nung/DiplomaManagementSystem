using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.SuperAdmin.Departments;

internal sealed class DepartmentAdminService(IApplicationDbContext dbContext) : IDepartmentAdminService
{
    public async Task<IReadOnlyList<DepartmentListItemDto>> GetAllAsync(
        Guid? facultyId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Department> query = dbContext.Departments
            .AsNoTracking()
            .Include(department => department.Faculty);

        if (facultyId.HasValue)
        {
            query = query.Where(department => department.FacultyId == facultyId.Value);
        }

        return await query
            .OrderBy(department => department.Faculty!.Name)
            .ThenBy(department => department.Name)
            .Select(department => new DepartmentListItemDto(
                department.Id,
                department.FacultyId,
                department.Faculty!.Name,
                department.Name,
                dbContext.Specialties.Count(specialty => specialty.DepartmentId == department.Id && specialty.IsActive),
                department.IsActive,
                department.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Department? department = await dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return department is null
            ? null
            : new DepartmentFormDto(department.Id, department.FacultyId, department.Name);
    }

    public async Task<Guid> CreateAsync(DepartmentFormDto form, CancellationToken cancellationToken = default)
    {
        await EnsureFacultyExistsAsync(form.FacultyId, cancellationToken);

        string name = form.Name.Trim();
        await EnsureUniqueNameInFacultyAsync(form.FacultyId, name, excludeId: null, cancellationToken);

        Department department = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = form.FacultyId,
            Name = name,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return department.Id;
    }

    public async Task UpdateAsync(Guid id, DepartmentFormDto form, CancellationToken cancellationToken = default)
    {
        Department department = await dbContext.Departments
                                    .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                                ?? throw new DomainException(DepartmentMessages.DepartmentNotFound);

        await EnsureFacultyExistsAsync(form.FacultyId, cancellationToken);

        string name = form.Name.Trim();
        await EnsureUniqueNameInFacultyAsync(form.FacultyId, name, id, cancellationToken);

        department.FacultyId = form.FacultyId;
        department.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Department department = await dbContext.Departments
                                    .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                                ?? throw new DomainException(DepartmentMessages.DepartmentNotFound);

        department.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFacultyExistsAsync(Guid facultyId, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Faculties
            .AsNoTracking()
            .AnyAsync(faculty => faculty.Id == facultyId, cancellationToken);

        if (!exists)
        {
            throw new DomainException(DepartmentMessages.FacultyNotFound);
        }
    }

    private async Task EnsureUniqueNameInFacultyAsync(
        Guid facultyId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(
                department => department.FacultyId == facultyId
                              && department.Name == name
                              && (excludeId == null || department.Id != excludeId),
                cancellationToken);

        if (exists)
        {
            throw new DomainException(DepartmentMessages.DuplicateDepartmentName);
        }
    }
}
