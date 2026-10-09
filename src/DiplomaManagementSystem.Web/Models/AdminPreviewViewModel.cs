using DiplomaManagementSystem.Web.AdminPreview;

namespace DiplomaManagementSystem.Web.Models;

public sealed class AdminPreviewViewModel
{
    public AdminPreviewMode CurrentMode { get; init; }

    public bool IsActivePreview { get; init; }

    public string CurrentModeDisplay { get; init; } = string.Empty;

    public string? ImpersonatedUserDisplay { get; init; }

    public string? ImpersonatedUserEmail { get; init; }

    public bool RequiresUserSelection { get; init; }

    public bool NeedsDepartmentSelection { get; init; }

    public AdminPreviewMode? SelectUserMode { get; init; }

    public IReadOnlyList<AdminPreviewOptionViewModel> Options { get; init; } = [];

    public string ReturnUrl { get; init; } = "/";
}

public sealed class AdminPreviewOptionViewModel
{
    public AdminPreviewMode Mode { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool IsSelected { get; init; }
}

public sealed class AdminPreviewSelectUserViewModel
{
    public AdminPreviewMode Mode { get; init; }

    public string ModeDisplay { get; init; } = string.Empty;

    public string ReturnUrl { get; init; } = "/";

    public IReadOnlyList<AdminPreviewUserListItemViewModel> Users { get; init; } = [];
}

public sealed class AdminPreviewUserListItemViewModel
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Subtitle { get; init; }
}
