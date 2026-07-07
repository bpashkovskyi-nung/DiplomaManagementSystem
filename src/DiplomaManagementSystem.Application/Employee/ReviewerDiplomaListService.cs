using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class ReviewerDiplomaListService(
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries) : IReviewerDiplomaListService
{
    public async Task<ReviewerDiplomaListPageDto> GetListAsync(
        Guid reviewerId,
        DiplomaListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        List<Diploma> diplomas = await diplomaQueries.ListForReviewerReadAsync(reviewerId, cancellationToken);
        if (diplomas.Count == 0)
        {
            return new ReviewerDiplomaListPageDto([], filter, []);
        }

        HashSet<Guid> userIds = diplomas
            .Select(diploma => diploma.StudentId)
            .Concat(diplomas.Where(diploma => diploma.SupervisorId.HasValue).Select(diploma => diploma.SupervisorId!.Value))
            .ToHashSet();

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        HashSet<Guid> studyGroupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> studyGroupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            studyGroupIds,
            cancellationToken);

        List<StudyGroupFilterOptionDto> studyGroups = studyGroupNames
            .OrderBy(pair => pair.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(pair => new StudyGroupFilterOptionDto(pair.Key, pair.Value))
            .ToList();

        IEnumerable<Diploma> filtered = DiplomaListFilterApplicator.Apply(diplomas, filter, users);

        List<DiplomaListItemDto> items = SecretaryDiplomaListProjection.MapListItems(
            filtered,
            users,
            studyGroupNames);

        return new ReviewerDiplomaListPageDto(items, filter, studyGroups);
    }
}
