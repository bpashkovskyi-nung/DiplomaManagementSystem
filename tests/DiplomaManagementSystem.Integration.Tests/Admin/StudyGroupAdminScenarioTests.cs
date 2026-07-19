using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Admin;

[Collection(nameof(IntegrationCollection))]
public sealed class StudyGroupAdminScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task DuplicateGroupNameInSession_Throws()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IStudyGroupAdminService studyGroupAdminService = scope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studyGroupAdminService.CreateAsync(
                new StudyGroupFormDto(
                    null,
                    scenario.SessionId,
                    scenario.StudyGroupName,
                    scenario.SpecialtyId,
                    "очної форми навчання")));

        Assert.Contains("already in use", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task UpdateAsync_ChangesGroupName()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IStudyGroupAdminService studyGroupAdminService = scope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..6];
        Guid extraGroupId = await studyGroupAdminService.CreateAsync(
            new StudyGroupFormDto(null, scenario.SessionId, $"КН-99-{suffix}", scenario.SpecialtyId, "очної форми навчання"));

        await studyGroupAdminService.UpdateAsync(
            extraGroupId,
            new StudyGroupFormDto(
                extraGroupId,
                scenario.SessionId,
                $"КН-UPD-{suffix}",
                scenario.SpecialtyId,
                "очної форми навчання"),
            CancellationToken.None);

        StudyGroupListItemDto? item = await studyGroupAdminService.GetListItemAsync(extraGroupId, CancellationToken.None);
        Assert.NotNull(item);
        Assert.Equal($"КН-UPD-{suffix}", item.Name);
    }

    [SkippableFact]
    public async Task DeleteAsync_WhenEmpty_RemovesGroup()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IStudyGroupAdminService studyGroupAdminService = scope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..6];
        Guid extraGroupId = await studyGroupAdminService.CreateAsync(
            new StudyGroupFormDto(null, scenario.SessionId, $"КН-DEL-{suffix}", scenario.SpecialtyId, "очної форми навчання"));

        await studyGroupAdminService.DeleteAsync(extraGroupId, CancellationToken.None);

        StudyGroupListItemDto? item = await studyGroupAdminService.GetListItemAsync(extraGroupId, CancellationToken.None);
        Assert.Null(item);
    }
}
