using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class SupervisorDiplomaListService(
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries) : ISupervisorDiplomaListService
{
    public async Task<SupervisorDiplomaListPageDto> GetListAsync(
        Guid supervisorId,
        DiplomaListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        List<Diploma> diplomas = await diplomaQueries.ListForSupervisorReadAsync(supervisorId, cancellationToken);
        if (diplomas.Count == 0)
        {
            return new SupervisorDiplomaListPageDto([], filter, []);
        }

        var studentIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(studentIds, cancellationToken);

        var studyGroupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> studyGroupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            studyGroupIds,
            cancellationToken);

        var studyGroups = studyGroupNames
            .OrderBy(pair => pair.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(pair => new StudyGroupFilterOptionDto(pair.Key, pair.Value))
            .ToList();

        IEnumerable<Diploma> filtered = DiplomaListFilterApplicator.Apply(diplomas, filter, users);

        List<DiplomaListItemDto> items = SecretaryDiplomaListProjection.MapListItems(
            filtered,
            users,
            studyGroupNames);

        return new SupervisorDiplomaListPageDto(items, filter, studyGroups);
    }
}
