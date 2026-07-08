using DiplomaManagementSystem.Application.Constants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Tests.SuperAdmin;

public sealed class SuperAdminAuthorizationTests
{
    [Fact]
    public void SuperAdminControllerBase_RequiresSuperAdminRole()
    {
        AuthorizeAttribute? attribute = typeof(Areas.SuperAdmin.SuperAdminControllerBase)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(RoleNames.SuperAdmin, attribute!.Roles);
    }

    [Fact]
    public void AdminControllerBase_RequiresAuthenticationOnly()
    {
        AuthorizeAttribute? attribute = typeof(Areas.Admin.AdminControllerBase)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Null(attribute!.Roles);
    }

    [Fact]
    public void SuperAdminControllers_UseSuperAdminArea()
    {
        Type[] controllers =
        [
            typeof(Areas.SuperAdmin.Controllers.HomeController),
            typeof(Areas.SuperAdmin.Controllers.FacultiesController),
            typeof(Areas.SuperAdmin.Controllers.DepartmentsController),
            typeof(Areas.SuperAdmin.Controllers.DepartmentAdminsController),
            typeof(Areas.SuperAdmin.Controllers.OrganizationImportController),
        ];

        foreach (Type controller in controllers)
        {
            AreaAttribute? area = controller.GetCustomAttributes(typeof(AreaAttribute), inherit: true)
                .Cast<AreaAttribute>()
                .FirstOrDefault();

            Assert.NotNull(area);
            Assert.Equal("SuperAdmin", area!.RouteValue);
        }
    }
}
