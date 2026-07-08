using DiplomaManagementSystem.Application.Departments.Contracts;

namespace DiplomaManagementSystem.Application.Tests.Departments;

internal sealed class TestDepartmentContext : IDepartmentContext
{
    public Guid? CurrentDepartmentId { get; set; }

    public bool IsSuperAdminImpersonating { get; set; }
}
