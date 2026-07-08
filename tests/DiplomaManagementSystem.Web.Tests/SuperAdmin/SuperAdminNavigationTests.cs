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

        Assert.Equal(2, links.Count);
        Assert.Equal(SuperAdminPageTitles.Faculties, links[0].Text);
        Assert.Equal("Faculties", links[0].Controller);
        Assert.Equal(SuperAdminPageTitles.OrganizationImport, links[1].Text);
    }

    [Fact]
    public void SuperAdminNavigation_FacultyDepartmentsBack_ReturnsFacultiesLinkOnly()
    {
        IReadOnlyList<SuperAdminNavLink> links = SuperAdminNavigation.FacultyDepartmentsBack();

        Assert.Single(links);
        Assert.Equal(SuperAdminPageTitles.Faculties, links[0].Text);
        Assert.Equal("Faculties", links[0].Controller);
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
