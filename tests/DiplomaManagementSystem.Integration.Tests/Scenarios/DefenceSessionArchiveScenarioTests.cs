using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DefenceSessionArchiveScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task ArchiveAsync_SetsArchivedStatusAndWritesAuditLog()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        await defenceSessionService.ArchiveAsync(scenario.SessionId, scenario.SecretaryId);

        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        DefenceSessionStatus status = await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.Status)
            .SingleAsync();

        Assert.Equal(DefenceSessionStatus.Archived, status);

        await IntegrationScenarioAssertions.AssertAuditLogExistsAsync(
            services,
            scenario.SessionId,
            "Archive",
            scenario.SecretaryId);
    }
}
