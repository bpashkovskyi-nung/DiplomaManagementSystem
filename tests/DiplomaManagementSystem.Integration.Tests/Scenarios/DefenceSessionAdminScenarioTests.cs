using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DefenceSessionAdminScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDetailsAsync_ReturnsGroupsAndDiplomaCount()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IDefenceSessionService defenceSessionService = scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();

        DefenceSessionDetailsDto? details = await defenceSessionService.GetDetailsAsync(
            scenario.SessionId,
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(scenario.SessionId, details.Id);
        Assert.Equal(DefenceSessionStatus.Active, details.Status);
        Assert.Equal(1, details.DiplomaCount);
        StudyGroupItemDto group = Assert.Single(details.Groups);
        Assert.Equal(scenario.StudyGroupName, group.Name);
        Assert.Equal(1, group.StudentCount);
    }

    [SkippableFact]
    public async Task GetAllAsync_IncludesSeededSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IDefenceSessionService defenceSessionService = scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();

        IReadOnlyList<DefenceSessionListItemDto> sessions = await defenceSessionService.GetAllAsync(CancellationToken.None);

        DefenceSessionListItemDto item = Assert.Single(
            sessions,
            session => session.Id == scenario.SessionId);
        Assert.Equal(2026, item.Year);
        Assert.Equal(DefenceSessionType.Bachelor, item.Type);
        Assert.True(item.GroupCount >= 1);
        Assert.True(item.DiplomaCount >= 1);
    }

    [SkippableFact]
    public async Task UpdateAsync_OnActiveSession_UpdatesFields()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IDefenceSessionService defenceSessionService = scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();

        await defenceSessionService.UpdateAsync(
            scenario.SessionId,
            new DefenceSessionFormDto(scenario.SessionId, 2027, DefenceSessionType.Master, 2),
            CancellationToken.None);

        DefenceSessionFormDto? form = await defenceSessionService.GetForEditAsync(scenario.SessionId, CancellationToken.None);

        Assert.NotNull(form);
        Assert.Equal(2027, form.Year);
        Assert.Equal(DefenceSessionType.Master, form.Type);
        Assert.Equal(2, form.Semester);
    }

    [SkippableFact]
    public async Task UpdateAsync_OnArchivedSession_Throws()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();

        await defenceSessionService.ArchiveAsync(scenario.SessionId, scenario.SecretaryId);

        await Assert.ThrowsAsync<DomainException>(() =>
            defenceSessionService.UpdateAsync(
                scenario.SessionId,
                new DefenceSessionFormDto(scenario.SessionId, 2027, DefenceSessionType.Bachelor, 1),
                CancellationToken.None));
    }
}
