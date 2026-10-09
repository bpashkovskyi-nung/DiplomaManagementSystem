using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;

namespace DiplomaManagementSystem.Application.Admin.ExaminationCommission.Contracts;

public interface IExaminationCommissionService
{
    Task<ExaminationCommissionEditorDto?> GetEditorAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(SaveExaminationCommissionDto request, CancellationToken cancellationToken = default);
}
