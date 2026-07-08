using System.Security.Claims;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Web.AdminPreview;
using DiplomaManagementSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Controllers;

public class HomeController(
    ISecretaryAccessService secretaryAccessService,
    IAdminPreviewService adminPreviewService) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View();
        }

        if (adminPreviewService.IsAdmin(User))
        {
            AdminPreviewMode mode = adminPreviewService.GetMode(HttpContext);
            if (adminPreviewService.RequiresImpersonation(mode)
                && !adminPreviewService.HasImpersonation(HttpContext))
            {
                return RedirectToAction("SelectUser", "AdminPreview", new { mode });
            }

            return mode switch
            {
                AdminPreviewMode.SuperAdmin => RedirectToAction("Index", "Home", new { area = "SuperAdmin" }),
                AdminPreviewMode.Student => RedirectToAction("Index", "Diploma", new { area = "Student" }),
                AdminPreviewMode.Secretary => RedirectToAction("Index", "Dashboard", new { area = "Secretary" }),
                AdminPreviewMode.Employee => await RedirectEmployeePreviewAsync(),
                _ => RedirectToAction("Index", "Home", new { area = "Admin" }),
            };
        }

        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is not null
            && Guid.TryParse(userIdValue, out Guid userId)
            && await secretaryAccessService.IsSecretaryAsync(userId, cancellationToken))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Secretary" });
        }

        if (User.IsInRole(RoleNames.Student))
        {
            return RedirectToAction("Index", "Diploma", new { area = "Student" });
        }

        if (User.IsInRole(RoleNames.Employee))
        {
            return RedirectToAction("Index", "Home", new { area = "Employee" });
        }

        return View();
    }

    private Task<IActionResult> RedirectEmployeePreviewAsync()
    {
        return Task.FromResult<IActionResult>(RedirectToAction("Index", "Home", new { area = "Employee" }));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
