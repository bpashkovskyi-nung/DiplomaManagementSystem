namespace DiplomaManagementSystem.Web.Departments;

public sealed class DepartmentSelectorViewModel
{
    public string AreaName { get; init; } = string.Empty;

    public Guid? SelectedDepartmentId { get; init; }

    public IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Departments { get; init; } = [];
}
