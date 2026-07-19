using DiplomaManagementSystem.Web.Areas.Admin;
using DiplomaManagementSystem.Web.Tests.Support;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DiplomaManagementSystem.Web.Tests.Admin;

public sealed class AdminFlashMessagesTests
{
    // TC-WEB-MAP-013
    [Fact]
    public void SetSuccess_WritesTempData()
    {
        TestController controller = CreateController();

        AdminFlashMessages.SetSuccess(controller, "Збережено");

        Assert.Equal("Збережено", controller.TempData["Success"]);
    }

    // TC-WEB-MAP-013b
    [Fact]
    public void SetError_WritesTempData()
    {
        TestController controller = CreateController();

        AdminFlashMessages.SetError(controller, "Помилка");

        Assert.Equal("Помилка", controller.TempData["Error"]);
    }

    // TC-WEB-MAP-013c
    [Fact]
    public void SetInfo_WritesTempData()
    {
        TestController controller = CreateController();

        AdminFlashMessages.SetInfo(controller, "Підказка");

        Assert.Equal("Підказка", controller.TempData["Info"]);
    }

    private static TestController CreateController()
    {
        DefaultHttpContext httpContext = new();
        TestController controller = new()
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider()),
        };

        return controller;
    }

    private sealed class TestController : Controller;
}
