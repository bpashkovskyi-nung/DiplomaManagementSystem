using DiplomaManagementSystem.Application.Constants;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class EmployeeNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole(RoleNames.Employee))
        {
            return Content(string.Empty);
        }

        return View();
    }
}
