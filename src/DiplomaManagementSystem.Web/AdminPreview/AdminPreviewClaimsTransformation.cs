using System.Security.Claims;

using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Authentication;

namespace DiplomaManagementSystem.Web.AdminPreview;

internal sealed class AdminPreviewClaimsTransformation(
    IAdminPreviewService adminPreviewService,
    IHttpContextAccessor httpContextAccessor,
    IAdminPreviewUserLookup adminPreviewUserLookup) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null || !adminPreviewService.IsAdmin(principal, httpContext))
        {
            return principal;
        }

        AdminPreviewMode mode = adminPreviewService.GetMode(httpContext, principal);
        Guid? impersonatedUserId = adminPreviewService.GetImpersonatedUserId(httpContext, principal);

        if (!adminPreviewService.RequiresImpersonation(mode) || impersonatedUserId is null)
        {
            return TransformRoleOnly(principal, mode);
        }

        AdminPreviewUserProfile? impersonatedUser = await adminPreviewUserLookup.FindAsync(impersonatedUserId.Value);
        UserKind expectedKind = mode == AdminPreviewMode.Student ? UserKind.Student : UserKind.Employee;
        if (impersonatedUser is null || impersonatedUser.UserKind != expectedKind)
        {
            adminPreviewService.ClearImpersonation(httpContext);
            return TransformRoleOnly(principal, mode);
        }

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return principal;
        }

        ClaimsIdentity clone = identity.Clone();
        string? originalUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (originalUserId is not null
            && clone.FindFirst(AdminPreviewClaimTypes.OriginalUserId) is null)
        {
            clone.AddClaim(new Claim(AdminPreviewClaimTypes.OriginalUserId, originalUserId));
        }

        ReplaceClaim(clone, ClaimTypes.NameIdentifier, impersonatedUser.Id.ToString());
        ReplaceClaim(clone, ClaimTypes.Name, impersonatedUser.FullName);
        ReplaceClaim(clone, ClaimTypes.Email, impersonatedUser.Email ?? string.Empty);

        string previewRole = mode switch
        {
            AdminPreviewMode.Student => RoleNames.Student,
            AdminPreviewMode.Secretary or AdminPreviewMode.Employee => RoleNames.Employee,
            _ => RoleNames.Employee,
        };
        if (!clone.HasClaim(claim => claim.Type == ClaimTypes.Role && claim.Value == previewRole))
        {
            clone.AddClaim(new Claim(ClaimTypes.Role, previewRole));
        }

        if (!clone.HasClaim(claim => claim.Type == ClaimTypes.Role && claim.Value == RoleNames.Admin))
        {
            clone.AddClaim(new Claim(ClaimTypes.Role, RoleNames.Admin));
        }

        return new ClaimsPrincipal(clone);
    }

    private static ClaimsPrincipal TransformRoleOnly(ClaimsPrincipal principal, AdminPreviewMode mode)
    {
        string? previewRole = mode switch
        {
            AdminPreviewMode.Employee or AdminPreviewMode.Secretary => RoleNames.Employee,
            AdminPreviewMode.Student => RoleNames.Student,
            _ => null,
        };

        if (previewRole is null || principal.IsInRole(previewRole))
        {
            return principal;
        }

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return principal;
        }

        ClaimsIdentity clone = identity.Clone();
        clone.AddClaim(new Claim(ClaimTypes.Role, previewRole));
        return new ClaimsPrincipal(clone);
    }

    private static void ReplaceClaim(ClaimsIdentity identity, string claimType, string value)
    {
        Claim? existingClaim = identity.FindFirst(claimType);
        if (existingClaim is not null)
        {
            identity.RemoveClaim(existingClaim);
        }

        identity.AddClaim(new Claim(claimType, value));
    }
}
