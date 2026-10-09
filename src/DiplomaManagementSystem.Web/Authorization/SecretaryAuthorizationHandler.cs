using System.Security.Claims;

using DiplomaManagementSystem.Application.Secretary.Contracts;

using Microsoft.AspNetCore.Authorization;

namespace DiplomaManagementSystem.Web.Authorization;

internal sealed class SecretaryAuthorizationHandler(ISecretaryAccessService accessService)
    : AuthorizationHandler<SecretaryRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SecretaryRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return;
        }

        if (await accessService.IsSecretaryAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
