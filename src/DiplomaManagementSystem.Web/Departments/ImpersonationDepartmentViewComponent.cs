using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Web.Departments;

public sealed class ImpersonationDepartmentViewComponent(
    IDepartmentContext departmentContext,
    IApplicationDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true
            || !UserClaimsPrincipal.IsInRole(RoleNames.SuperAdmin)
            || !departmentContext.IsSuperAdminImpersonating
            || departmentContext.CurrentDepartmentId is not Guid departmentId)
        {
            return Content(string.Empty);
        }

        string? label = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.Id == departmentId)
            .Join(
                dbContext.Faculties.AsNoTracking(),
                department => department.FacultyId,
                faculty => faculty.Id,
                (department, faculty) => faculty.Name + " — " + department.Name)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        if (label is null)
        {
            return Content(string.Empty);
        }

        return View((object)label);
    }
}
