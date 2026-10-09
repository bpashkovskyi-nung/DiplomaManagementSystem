using System.Security.Claims;
using System.Text.Encodings.Web;

using DiplomaManagementSystem.Application.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Integration.Tests.Web;

internal sealed class IntegrationTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";
    public const string UserIdHeaderName = "X-Test-UserId";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out Microsoft.Extensions.Primitives.StringValues userIdValues)
            || !Guid.TryParse(userIdValues.FirstOrDefault(), out Guid userId))
        {
            return AuthenticateResult.NoResult();
        }

        UserManager<ApplicationUser> userManager = Context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthenticateResult.Fail("Test user was not found.");
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
        ];

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        ClaimsIdentity identity = new(claims, Scheme.Name);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = "/Account/Login";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = "/Account/AccessDenied";
        return Task.CompletedTask;
    }
}
