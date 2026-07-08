using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class DepartmentSelectViewModel
{
    public IReadOnlyList<SelectListItem> Departments { get; init; } = [];
}
