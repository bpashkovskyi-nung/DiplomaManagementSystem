using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class SecretaryDiplomaListService(
    IDefenceSessionQueries defenceSessionQueries,
    IStudyGroupQueries studyGroupQueries,
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries) : ISecretaryDiplomaListService
{
    public async Task<DiplomaListPageDto?> GetListAsync(
        Guid sessionId,
        DiplomaListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        DefenceSessionType? sessionType = await defenceSessionQueries.GetTypeAsync(sessionId, cancellationToken);
        if (sessionType is null)
        {
            return null;
        }

        List<StudyGroupOption> studyGroupOptions = await studyGroupQueries.ListOptionsForSessionAsync(
            sessionId,
            cancellationToken);
        var studyGroups = studyGroupOptions
            .Select(option => new StudyGroupFilterOptionDto(option.Id, option.Name))
            .ToList();

        List<Diploma> diplomas = await diplomaQueries.ListForSessionReadAsync(sessionId, cancellationToken);
        if (diplomas.Count == 0)
        {
            return new DiplomaListPageDto(sessionId, sessionType.Value, [], filter, studyGroups);
        }

        var userIds = diplomas
            .Select(diploma => diploma.StudentId)
            .Concat(diplomas.Where(diploma => diploma.SupervisorId.HasValue).Select(diploma => diploma.SupervisorId!.Value))
            .ToHashSet();

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        var studyGroupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> studyGroupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            studyGroupIds,
            cancellationToken);

        IEnumerable<Diploma> filtered = DiplomaListFilterApplicator.Apply(diplomas, filter, users);

        List<DiplomaListItemDto> items = SecretaryDiplomaListProjection.MapListItems(
            filtered,
            users,
            studyGroupNames);

        return new DiplomaListPageDto(sessionId, sessionType.Value, items, filter, studyGroups);
    }
}
