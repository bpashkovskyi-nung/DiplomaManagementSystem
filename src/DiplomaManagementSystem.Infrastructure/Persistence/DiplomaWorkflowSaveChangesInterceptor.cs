using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DiplomaManagementSystem.Infrastructure.Persistence;

internal sealed class DiplomaWorkflowSaveChangesInterceptor(
    DiplomaWorkflowInvariantValidator validator) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Validate(DbContext? context)
    {
        if (context is ApplicationDbContext dbContext)
        {
            dbContext.ValidateWorkflowInvariants(validator);
        }
    }
}
