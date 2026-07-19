using DiplomaManagementSystem.Application.Employee.Dtos;

namespace DiplomaManagementSystem.Application.Employee.Contracts;

public interface ISupervisorProgressService
{
    Task<SupervisorProgressPageDto> GetPageAsync(
        Guid supervisorId,
        Guid? sessionId,
        CancellationToken cancellationToken = default);

    Task SetActualPercentAsync(
        Guid supervisorId,
        SetMilestoneProgressDto request,
        CancellationToken cancellationToken = default);
}

public interface IDepartmentProgressReportService
{
    Task<IReadOnlyList<(Guid SessionId, string Label)>> ListDepartmentSessionsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<DepartmentProgressReportDto?> GetReportAsync(
        Guid employeeId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface IDefenceDateRequestService
{
    Task<DefenceDateRequestFormDto?> GetFormForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<DefenceDateRequestFormDto?> GetFormForSupervisorAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default);

    Task RequestAsStudentAsync(
        Guid studentId,
        RequestDefenceDateDto request,
        CancellationToken cancellationToken = default);

    Task RequestAsSupervisorAsync(
        Guid supervisorId,
        RequestDefenceDateDto request,
        CancellationToken cancellationToken = default);
}
