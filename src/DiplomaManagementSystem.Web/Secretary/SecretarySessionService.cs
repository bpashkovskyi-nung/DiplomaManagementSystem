using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Secretary.Contracts;

using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Web.Secretary;

internal sealed class SecretarySessionService(
    IOptions<SecretaryOptions> options,
    ISecretaryAccessService accessService) : ISecretarySessionService
{
    public Guid? GetSelectedSessionId(HttpContext httpContext)
    {
        string cookieName = options.Value.SelectedSessionCookieName;
        if (!httpContext.Request.Cookies.TryGetValue(cookieName, out string? value)
            || !Guid.TryParse(value, out Guid sessionId))
        {
            return null;
        }

        return sessionId;
    }

    public async Task SetSelectedSessionAsync(
        HttpContext httpContext,
        Guid userId,
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        if (!await accessService.CanAccessSessionAsync(userId, defenceSessionId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have access to this defence session.");
        }

        SecretaryOptions secretaryOptions = options.Value;
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(secretaryOptions.SessionCookieExpirationDays),
        };

        httpContext.Response.Cookies.Append(
            secretaryOptions.SelectedSessionCookieName,
            defenceSessionId.ToString(),
            cookieOptions);
    }
}
