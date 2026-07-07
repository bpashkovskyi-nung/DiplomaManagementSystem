using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;

namespace DiplomaManagementSystem.Application.Employee.Contracts;

public interface IReviewerDiplomaListService
{
    Task<ReviewerDiplomaListPageDto> GetListAsync(
        Guid reviewerId,
        DiplomaListFilterDto filter,
        CancellationToken cancellationToken = default);
}
