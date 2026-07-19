using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Models.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class StudentNavViewComponent(IDefenceSessionQueries defenceSessionQueries) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole(RoleNames.Student))
        {
            return Content(string.Empty);
        }

        string? userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return Content(string.Empty);
        }

        DefenceSessionType? sessionType = (await defenceSessionQueries.FindForStudentAsync(
            userId,
            HttpContext.RequestAborted))?.Type;

        return View(new WorkNavViewModel { SessionType = sessionType });
    }
}
