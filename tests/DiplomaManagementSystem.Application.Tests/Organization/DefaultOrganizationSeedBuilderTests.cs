using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Organization;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Tests.Organization;

public sealed class DefaultOrganizationSeedBuilderTests
{
    [Fact]
    public void FromOptions_MapsFacultyAndDepartmentFromOrganizationOptions()
    {
        OrganizationOptions options = new()
        {
            FacultyName = "факультет інформаційних технологій",
            DepartmentName = "кафедра комп'ютерних систем і мереж",
            SpecialtyCode = "123",
            SpecialtyName = "Комп'ютерна інженерія",
            StudyForm = "очної форми навчання",
        };

        DateTimeOffset createdAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        (Faculty faculty, Department department) = DefaultOrganizationSeedBuilder.FromOptions(options, createdAt);

        Assert.Equal("факультет інформаційних технологій", faculty.Name);
        Assert.Equal("кафедра комп'ютерних систем і мереж", department.Name);
        Assert.Equal(faculty.Id, department.FacultyId);
        Assert.Equal("123", department.SpecialtyCode);
        Assert.Equal("Комп'ютерна інженерія", department.SpecialtyName);
        Assert.Equal("очної форми навчання", department.StudyForm);
        Department linked = Assert.Single(faculty.Departments);
        Assert.Equal(department.Id, linked.Id);
    }

    [Fact]
    public void FromOptions_UsesFallbackNamesWhenOrganizationOptionsEmpty()
    {
        OrganizationOptions options = new();

        (Faculty faculty, Department department) = DefaultOrganizationSeedBuilder.FromOptions(
            options,
            DateTimeOffset.UtcNow);

        Assert.Equal("Факультет", faculty.Name);
        Assert.Equal("Кафедра", department.Name);
        Assert.Equal("очної форми навчання", department.StudyForm);
    }
}
