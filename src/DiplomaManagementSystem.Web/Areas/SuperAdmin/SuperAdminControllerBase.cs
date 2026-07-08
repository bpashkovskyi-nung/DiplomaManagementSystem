using DiplomaManagementSystem.Application.Constants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin;

[Area("SuperAdmin")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public abstract class SuperAdminControllerBase : Controller;
