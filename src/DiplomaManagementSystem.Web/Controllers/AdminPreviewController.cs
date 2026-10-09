using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.AdminPreview;
using DiplomaManagementSystem.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Web.Controllers;

[Authorize(Roles = RoleNames.SuperAdmin)]
public sealed class AdminPreviewController(
    IAdminPreviewService adminPreviewService,
    IAdminPreviewUserPickerService userPickerService,
    IAdminPreviewUserLookup adminPreviewUserLookup,
    ISecretaryAccessService secretaryAccessService,
    IUserDisplayQueries userDisplayQueries,
    IDepartmentContext departmentContext,
    IApplicationDbContext dbContext) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(
        AdminPreviewMode mode,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        AdminPreviewMode normalizedMode = AdminPreviewModeRules.Normalize(mode);
        adminPreviewService.SetMode(HttpContext, normalizedMode);
        string? safeReturnUrl = ResolveReturnUrl(returnUrl, normalizedMode);

        if (adminPreviewService.RequiresImpersonation(normalizedMode))
        {
            return RedirectToAction(nameof(SelectUser), new { mode = normalizedMode, returnUrl = safeReturnUrl });
        }

        return await RedirectToLocalAsync(safeReturnUrl, normalizedMode, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> SelectUser(
        AdminPreviewMode mode,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        AdminPreviewMode normalizedMode = AdminPreviewModeRules.Normalize(mode);
        if (!adminPreviewService.RequiresImpersonation(normalizedMode))
        {
            return RedirectToAction("Index", "Home");
        }

        AdminPreviewMode currentMode = adminPreviewService.GetMode(HttpContext);
        if (currentMode != normalizedMode)
        {
            adminPreviewService.SetMode(HttpContext, normalizedMode);
        }

        UserKind userKind = normalizedMode == AdminPreviewMode.Student ? UserKind.Student : UserKind.Employee;
        IReadOnlyList<AdminPreviewUserOption> users = await userPickerService.GetUsersAsync(userKind, cancellationToken);

        if (normalizedMode == AdminPreviewMode.Secretary)
        {
            users = users
                .Where(user => user.Subtitle is not null
                               && user.Subtitle.Contains("Секретар", StringComparison.Ordinal))
                .ToList();
        }
        else if (normalizedMode == AdminPreviewMode.Employee)
        {
            users = users
                .Where(user => user.Subtitle is null
                               || !user.Subtitle.Contains("Секретар", StringComparison.Ordinal))
                .ToList();
        }

        AdminPreviewSelectUserViewModel model = new()
        {
            Mode = normalizedMode,
            ModeDisplay = adminPreviewService.GetModeDisplayName(normalizedMode),
            ReturnUrl = returnUrl ?? Url.Content("~/"),
            Users = users
                .Select(user => new AdminPreviewUserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Subtitle = user.Subtitle,
                })
                .ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUser(
        Guid userId,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        AdminPreviewMode mode = adminPreviewService.GetMode(HttpContext);
        if (!adminPreviewService.RequiresImpersonation(mode))
        {
            return RedirectToAction("Index", "Home");
        }

        UserKind expectedKind = mode == AdminPreviewMode.Student ? UserKind.Student : UserKind.Employee;
        AdminPreviewUserProfile? user = await adminPreviewUserLookup.FindAsync(userId, cancellationToken);

        if (user is null || user.UserKind != expectedKind)
        {
            TempData["Error"] = "Обрано недійсного користувача для цього режиму перегляду.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        Guid? scopedDepartmentId = departmentContext.CurrentDepartmentId;
        if (scopedDepartmentId is not Guid departmentId)
        {
            TempData["Error"] = "Оберіть кафедру перед impersonation.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        if (expectedKind == UserKind.Employee
            && !await userDisplayQueries.IsActiveDepartmentEmployeeAsync(userId, departmentId, cancellationToken))
        {
            TempData["Error"] = "Обрано викладача з іншої кафедри.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        if (expectedKind == UserKind.Student
            && !await dbContext.Users.AsNoTracking().AnyAsync(
                item => item.Id == userId
                        && item.UserKind == UserKind.Student
                        && item.DefenceSession != null
                        && item.DefenceSession.DepartmentId == departmentId,
                cancellationToken))
        {
            TempData["Error"] = "Обрано студента з іншої кафедри.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        if (mode == AdminPreviewMode.Secretary
            && !await secretaryAccessService.IsSecretaryAsync(userId, cancellationToken))
        {
            TempData["Error"] = "Обрано користувача без ролі секретаря для цього режиму перегляду.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        if (mode == AdminPreviewMode.Employee
            && await secretaryAccessService.IsSecretaryAsync(userId, cancellationToken))
        {
            TempData["Error"] = "Обрано секретаря для режиму перегляду викладача.";
            return RedirectToAction(nameof(SelectUser), new { mode, returnUrl });
        }

        adminPreviewService.SetImpersonatedUserId(HttpContext, userId);
        return await RedirectToLocalAsync(ResolveReturnUrl(returnUrl, mode), mode, cancellationToken);
    }

    private string? ResolveReturnUrl(string? returnUrl, AdminPreviewMode mode)
    {
        if (returnUrl is null || !Url.IsLocalUrl(returnUrl))
        {
            return null;
        }

        return AdminPreviewRedirectRules.IsReturnUrlValidForMode(returnUrl, mode)
            ? returnUrl
            : null;
    }

    private async Task<IActionResult> RedirectToLocalAsync(
        string? returnUrl,
        AdminPreviewMode? mode = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }

        AdminPreviewMode effectiveMode = mode ?? adminPreviewService.GetMode(HttpContext);
        if (AdminPreviewModeRules.IsSecretaryPreviewMode(effectiveMode)
            && adminPreviewService.GetImpersonatedUserId(HttpContext) is Guid secretaryId
            && await secretaryAccessService.IsSecretaryAsync(secretaryId, cancellationToken))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Secretary" });
        }

        return AdminPreviewModeRules.Normalize(effectiveMode) switch
        {
            AdminPreviewMode.SuperAdmin => RedirectToAction("Index", "Home", new { area = "SuperAdmin" }),
            AdminPreviewMode.Student => RedirectToAction("Index", "Diploma", new { area = "Student" }),
            AdminPreviewMode.Secretary => RedirectToAction("Index", "Dashboard", new { area = "Secretary" }),
            AdminPreviewMode.Employee => RedirectToAction("Index", "Home", new { area = "Employee" }),
            _ => RedirectToAction("Index", "Home", new { area = "Admin" }),
        };
    }
}
