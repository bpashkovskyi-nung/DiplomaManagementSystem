using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Web.Departments;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Admin;

[Area("Admin")]
[Authorize]
[ServiceFilter(typeof(AdminDepartmentContextFilter))]
public abstract class AdminControllerBase : Controller
{
    protected IDefenceSessionService DefenceSessionService { get; }

    protected AdminControllerBase(IDefenceSessionService defenceSessionService)
    {
        DefenceSessionService = defenceSessionService;
    }

    protected async Task<string?> GetSessionLabelAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSessionFormDto? session = await DefenceSessionService.GetForEditAsync(defenceSessionId, cancellationToken);
        return session is null
            ? null
            : SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);
    }
}
