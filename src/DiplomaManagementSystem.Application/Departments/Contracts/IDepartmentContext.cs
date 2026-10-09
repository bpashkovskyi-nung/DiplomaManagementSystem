namespace DiplomaManagementSystem.Application.Departments.Contracts;

public interface IDepartmentContext
{
    Guid? CurrentDepartmentId { get; }

    bool IsSuperAdminImpersonating { get; }
}
