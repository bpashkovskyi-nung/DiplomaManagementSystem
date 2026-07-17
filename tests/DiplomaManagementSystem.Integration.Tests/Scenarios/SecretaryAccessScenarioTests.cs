using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryAccessScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task CanAccessSessionAsync_WhenSecretaryAssigned_ReturnsTrue()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryAccessService accessService = scope.ServiceProvider.GetRequiredService<ISecretaryAccessService>();

        bool canAccess = await accessService.CanAccessSessionAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            CancellationToken.None);

        Assert.True(canAccess);
    }

    [SkippableFact]
    public async Task CanAccessSessionAsync_WhenSessionNotAssigned_ReturnsFalse()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(services);

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        Guid otherSessionId = await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2025, DefenceSessionType.Bachelor, 2));

        ISecretaryAccessService accessService = services.GetRequiredService<ISecretaryAccessService>();
        bool canAccess = await accessService.CanAccessSessionAsync(
            scenario.SecretaryId,
            otherSessionId,
            CancellationToken.None);

        Assert.False(canAccess);
    }

    [SkippableFact]
    public async Task GetAccessibleSessionsAsync_IncludesAssignedSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryAccessService accessService = scope.ServiceProvider.GetRequiredService<ISecretaryAccessService>();

        IReadOnlyList<SecretarySessionOptionDto> sessions = await accessService.GetAccessibleSessionsAsync(
            scenario.SecretaryId,
            CancellationToken.None);

        Assert.Contains(sessions, session => session.Id == scenario.SessionId);
    }

    [SkippableFact]
    public async Task IsSecretaryAsync_ForStudent_ReturnsFalse()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryAccessService accessService = scope.ServiceProvider.GetRequiredService<ISecretaryAccessService>();

        bool isSecretary = await accessService.IsSecretaryAsync(scenario.StudentId, CancellationToken.None);

        Assert.False(isSecretary);
    }
}
