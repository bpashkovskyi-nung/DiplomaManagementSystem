using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.SuperAdmin;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Validation;
using DiplomaManagementSystem.Web.Tests.Support;

using FluentValidation.TestHelper;

namespace DiplomaManagementSystem.Web.Tests.SuperAdmin;

public sealed class SuperAdminNavigationTests
{
    [Fact]
    public void SuperAdminNavigation_Global_ContainsExpectedLinksInOrder()
    {
        IReadOnlyList<SuperAdminNavLink> links = SuperAdminNavigation.Global();

        Assert.Equal(5, links.Count);
        Assert.Equal(SuperAdminPageTitles.Home, links[0].Text);
        Assert.Equal("Home", links[0].Controller);
        Assert.Equal(SuperAdminPageTitles.Faculties, links[1].Text);
        Assert.Equal(SuperAdminPageTitles.Departments, links[2].Text);
        Assert.Equal(SuperAdminPageTitles.DepartmentAdmins, links[3].Text);
        Assert.Equal(SuperAdminPageTitles.OrganizationImport, links[4].Text);
    }

    [Fact]
    public void SuperAdminNavigation_DepartmentAdminsBack_IncludesDepartmentRoute()
    {
        Guid departmentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        IReadOnlyList<SuperAdminNavLink> links = SuperAdminNavigation.DepartmentAdminsBack(departmentId);

        Assert.Equal(departmentId.ToString(), links[0].RouteValues!["departmentId"]);
    }
}

public sealed class OrganizationImportViewModelValidatorTests
{
    private readonly OrganizationImportViewModelValidator _validator = new();

    [Fact]
    public void Validate_WhenFileMissing_HasError()
    {
        OrganizationImportViewModel model = new();

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(viewModel => viewModel.File);
    }

    [Fact]
    public void Validate_WhenFileIsNotJson_HasError()
    {
        OrganizationImportViewModel model = new()
        {
            File = new FakeFormFile("structure.txt", System.Text.Encoding.UTF8.GetBytes("[]"), "text/plain"),
        };

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(viewModel => viewModel.File);
    }

    [Fact]
    public void Validate_WhenJsonFileProvided_IsValid()
    {
        OrganizationImportViewModel model = new()
        {
            File = new FakeFormFile("structure.json", System.Text.Encoding.UTF8.GetBytes("[]"), "application/json"),
        };

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
