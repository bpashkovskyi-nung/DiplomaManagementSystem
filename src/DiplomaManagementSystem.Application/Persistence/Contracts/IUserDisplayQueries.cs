using DiplomaManagementSystem.Application.Identity;

namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IUserDisplayQueries
{
    Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string>> LoadFullNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string>> LoadStudyGroupNamesAsync(
        IReadOnlyCollection<Guid> studyGroupIds,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, StudentDisplayInfo>> LoadStudentDisplaysAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken = default);

    Task<List<UserOption>> LoadEmployeeOptionsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveDepartmentEmployeeAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<StudentStorageContext?> GetStudentStorageContextAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
