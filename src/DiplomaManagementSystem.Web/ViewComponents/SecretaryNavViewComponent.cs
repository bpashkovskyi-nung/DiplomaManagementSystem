using System.Security.Claims;

using DiplomaManagementSystem.Application.Secretary.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class SecretaryNavViewComponent(ISecretaryAccessService accessService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        string? userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return Content(string.Empty);
        }

        bool isSecretary = await accessService.IsSecretaryAsync(userId, HttpContext.RequestAborted);
        if (!isSecretary)
        {
            return Content(string.Empty);
        }

        return View();
    }
}
