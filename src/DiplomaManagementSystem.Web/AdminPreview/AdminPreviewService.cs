using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;

namespace DiplomaManagementSystem.Web.AdminPreview;

internal sealed class AdminPreviewService : IAdminPreviewService
{
    private const string ModeSessionKey = "AdminPreviewMode";
    private const string ImpersonationSessionKey = "AdminPreviewImpersonatedUserId";
    private const string OperatorSessionKey = "AdminPreviewOperatorUserId";

    public AdminPreviewMode GetMode(HttpContext httpContext, ClaimsPrincipal? user = null)
    {
        ClaimsPrincipal effectiveUser = user ?? httpContext.User;
        if (!IsAdmin(effectiveUser, httpContext))
        {
            return AdminPreviewMode.Admin;
        }

        AdminPreviewMode mode = httpContext.Session.GetInt32(ModeSessionKey) switch
        {
            int value => AdminPreviewModeRules.FromStoredValue(value),
            _ => AdminPreviewMode.SuperAdmin,
        };

        return mode;
    }

    public void SetMode(HttpContext httpContext, AdminPreviewMode mode)
    {
        if (!IsAdmin(httpContext.User, httpContext))
        {
            throw new UnauthorizedAccessException("Only super administrators can switch preview mode.");
        }

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        string? operatorUserId = GetOperatorUserId(httpContext.User);
        if (operatorUserId is not null)
        {
            httpContext.Session.SetString(OperatorSessionKey, operatorUserId);
        }

        AdminPreviewMode modeToStore = AdminPreviewModeRules.Normalize(mode);

        AdminPreviewMode previousMode = GetMode(httpContext);
        httpContext.Session.SetInt32(ModeSessionKey, (int)modeToStore);

        if (AdminPreviewModeRules.IsHomeMode(modeToStore) || modeToStore != previousMode)
        {
            ClearImpersonation(httpContext);
        }

        if (AdminPreviewModeRules.IsHomeMode(modeToStore))
        {
            httpContext.Session.Remove(OperatorSessionKey);
        }
    }

    public Guid? GetImpersonatedUserId(HttpContext httpContext, ClaimsPrincipal? user = null)
    {
        ClaimsPrincipal effectiveUser = user ?? httpContext.User;
        if (!IsAdmin(effectiveUser, httpContext))
        {
            return null;
        }

        return httpContext.Session.GetString(ImpersonationSessionKey) is string value
               && Guid.TryParse(value, out Guid userId)
            ? userId
            : null;
    }

    public void SetImpersonatedUserId(HttpContext httpContext, Guid userId)
    {
        if (!IsAdmin(httpContext.User, httpContext))
        {
            throw new UnauthorizedAccessException("Only administrators can impersonate users.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        httpContext.Session.SetString(ImpersonationSessionKey, userId.ToString());
    }

    public void ClearImpersonation(HttpContext httpContext)
    {
        httpContext.Session.Remove(ImpersonationSessionKey);
    }

    public bool RequiresImpersonation(AdminPreviewMode mode) =>
        mode is AdminPreviewMode.Student
            or AdminPreviewMode.Secretary
            or AdminPreviewMode.Employee;

    public bool HasImpersonation(HttpContext httpContext, ClaimsPrincipal? user = null) =>
        GetImpersonatedUserId(httpContext, user).HasValue;

    public bool IsAdmin(ClaimsPrincipal user, HttpContext? httpContext = null)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(RoleNames.SuperAdmin))
        {
            return true;
        }

        if (user.HasClaim(claim => claim.Type == AdminPreviewClaimTypes.OriginalUserId))
        {
            return true;
        }

        if (httpContext is null)
        {
            return false;
        }

        ISession? session = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session;
        if (session is null)
        {
            return false;
        }

        return session.GetString(OperatorSessionKey) is string operatorUserId
               && GetOperatorUserId(user) == operatorUserId;
    }

    public bool IsActivePreview(HttpContext httpContext) =>
        IsAdmin(httpContext.User, httpContext) && !AdminPreviewModeRules.IsHomeMode(GetMode(httpContext));

    public string GetModeDisplayName(AdminPreviewMode mode) => AdminPreviewModeRules.Normalize(mode) switch
    {
        AdminPreviewMode.SuperAdmin => "Супер Адміністратор",
        AdminPreviewMode.Admin => "Адміністратор",
        AdminPreviewMode.Secretary => "Секретар",
        AdminPreviewMode.Student => "Студент",
        AdminPreviewMode.Employee => "Викладач",
        _ => mode.ToString(),
    };

    private static string? GetOperatorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(AdminPreviewClaimTypes.OriginalUserId)
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
}
