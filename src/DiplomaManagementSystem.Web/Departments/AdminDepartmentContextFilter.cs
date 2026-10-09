using System.Security.Claims;

using DiplomaManagementSystem.Web.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DiplomaManagementSystem.Web.Departments;

internal sealed class AdminDepartmentContextFilter(IDepartmentContextService departmentContextService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is DepartmentController)
        {
            await next();
            return;
        }

        string? userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            context.Result = new ChallengeResult();
            return;
        }

        IActionResult? redirect = await departmentContextService.EnsureAdminContextAsync(
            context.HttpContext,
            userId,
            context.HttpContext.RequestAborted);

        if (redirect is not null)
        {
            context.Result = redirect;
            return;
        }

        await next();
    }
}
