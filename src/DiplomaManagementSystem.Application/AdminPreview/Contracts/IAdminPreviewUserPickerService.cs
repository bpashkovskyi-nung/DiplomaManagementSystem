
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.AdminPreview.Contracts;

public interface IAdminPreviewUserPickerService
{
    Task<IReadOnlyList<AdminPreviewUserOption>> GetUsersAsync(
        UserKind userKind,
        CancellationToken cancellationToken = default);
}

public sealed record AdminPreviewUserOption(
    Guid Id,
    string FullName,
    string Email,
    string? Subtitle);
