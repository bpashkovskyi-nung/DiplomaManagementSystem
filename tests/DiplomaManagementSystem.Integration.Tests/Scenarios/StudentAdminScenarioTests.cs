using DiplomaManagementSystem.Application.Admin.Students.Contracts;
using DiplomaManagementSystem.Application.Admin.Students.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class StudentAdminScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetAllAsync_FilteredBySession_ReturnsSeededStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        IReadOnlyList<StudentListItemDto> students = await studentAdminService.GetAllAsync(
            scenario.SessionId,
            CancellationToken.None);

        StudentListItemDto student = Assert.Single(students);
        Assert.Equal("Student One", student.FullName);
        Assert.Equal(scenario.StudyGroupName, student.StudyGroupName);
    }

    [SkippableFact]
    public async Task GetDetailsAsync_ReturnsStudentWithDiplomaFlag()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        StudentDetailsDto? details = await studentAdminService.GetDetailsAsync(
            scenario.StudentId,
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("Student One", details.FullName);
        Assert.Equal(scenario.StudyGroupName, details.StudyGroupName);
        Assert.True(details.HasDiploma);
    }

    [SkippableFact]
    public async Task UpdateAsync_ChangesStudentFullName()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        StudentFormDto? form = await studentAdminService.GetForEditAsync(scenario.StudentId, CancellationToken.None);
        Assert.NotNull(form);

        string suffix = Guid.NewGuid().ToString("N")[..6];
        string updatedEmail = $"student.updated.{suffix}@test.local";
        StudentFormDto updated = form with
        {
            FullName = "Student Updated",
            Email = updatedEmail,
        };

        await studentAdminService.UpdateAsync(scenario.StudentId, updated, CancellationToken.None);

        StudentDetailsDto? details = await studentAdminService.GetDetailsAsync(
            scenario.StudentId,
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("Student Updated", details.FullName);
        Assert.Equal(updatedEmail, details.Email);
    }
}
