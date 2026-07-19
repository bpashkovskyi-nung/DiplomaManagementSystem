using System.Security.Claims;

using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Http;

namespace DiplomaManagementSystem.Web.Tests.AdminPreview;

public sealed class AdminPreviewClaimsTransformationTests
{
    private readonly FakeAdminPreviewService _previewService = new();
    private readonly FakeAdminPreviewUserLookup _userLookup = new();
    private readonly HttpContextAccessor _httpContextAccessor = new();

    [Fact]
    public async Task TransformAsync_WhenNotAdmin_ReturnsOriginalPrincipal()
    {
        ClaimsPrincipal principal = CreatePrincipal(RoleNames.Student);
        AdminPreviewClaimsTransformation transformation = CreateTransformation(principal);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        Assert.Same(principal, result);
    }

    [Fact]
    public async Task TransformAsync_WhenAdminInEmployeeMode_AddsEmployeeRole()
    {
        ClaimsPrincipal principal = CreatePrincipal(RoleNames.Admin);
        _previewService.Mode = AdminPreviewMode.Employee;
        AdminPreviewClaimsTransformation transformation = CreateTransformation(principal);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        Assert.True(result.IsInRole(RoleNames.Employee));
        Assert.True(result.IsInRole(RoleNames.Admin));
    }

    [Fact]
    public async Task TransformAsync_WhenImpersonatingStudent_ReplacesIdentityClaims()
    {
        var originalUserId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        ClaimsPrincipal principal = CreatePrincipal(RoleNames.Admin, originalUserId);
        _previewService.Mode = AdminPreviewMode.Student;
        _previewService.ImpersonatedUserId = studentId;
        _userLookup.Profile = new AdminPreviewUserProfile(
            studentId,
            "Student One",
            "student@test.local",
            UserKind.Student);
        AdminPreviewClaimsTransformation transformation = CreateTransformation(principal);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        Assert.Equal(studentId.ToString(), result.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Student One", result.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(originalUserId.ToString(), result.FindFirstValue(AdminPreviewClaimTypes.OriginalUserId));
        Assert.True(result.IsInRole(RoleNames.Student));
    }

    [Fact]
    public async Task TransformAsync_WhenImpersonatedUserMissing_ClearsImpersonationAndAddsRoleOnly()
    {
        ClaimsPrincipal principal = CreatePrincipal(RoleNames.Admin);
        _previewService.Mode = AdminPreviewMode.Secretary;
        _previewService.ImpersonatedUserId = Guid.NewGuid();
        _userLookup.Profile = null;
        AdminPreviewClaimsTransformation transformation = CreateTransformation(principal);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        Assert.True(_previewService.ImpersonationCleared);
        Assert.True(result.IsInRole(RoleNames.Employee));
    }

    private AdminPreviewClaimsTransformation CreateTransformation(ClaimsPrincipal principal)
    {
        DefaultHttpContext httpContext = new()
        {
            User = principal,
        };
        _httpContextAccessor.HttpContext = httpContext;

        return new AdminPreviewClaimsTransformation(
            _previewService,
            _httpContextAccessor,
            _userLookup);
    }

    private static ClaimsPrincipal CreatePrincipal(string role, Guid? userId = null)
    {
        Guid id = userId ?? Guid.NewGuid();
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
        ],
        authenticationType: "test"));
    }

    private sealed class FakeAdminPreviewService : IAdminPreviewService
    {
        public AdminPreviewMode Mode { get; set; } = AdminPreviewMode.SuperAdmin;

        public Guid? ImpersonatedUserId { get; set; }

        public bool ImpersonationCleared { get; private set; }

        public bool IsAdmin(ClaimsPrincipal principal, HttpContext? httpContext) =>
            principal.IsInRole(RoleNames.Admin) || principal.IsInRole(RoleNames.SuperAdmin);

        public AdminPreviewMode GetMode(HttpContext httpContext, ClaimsPrincipal? principal = null) => Mode;

        public Guid? GetImpersonatedUserId(HttpContext httpContext, ClaimsPrincipal? principal = null) =>
            ImpersonatedUserId;

        public bool RequiresImpersonation(AdminPreviewMode mode) =>
            mode is AdminPreviewMode.Student or AdminPreviewMode.Secretary or AdminPreviewMode.Employee;

        public bool IsActivePreview(HttpContext httpContext) => Mode != AdminPreviewMode.SuperAdmin;

        public bool HasImpersonation(HttpContext httpContext, ClaimsPrincipal? principal = null) =>
            ImpersonatedUserId is not null;

        public string GetModeDisplayName(AdminPreviewMode mode) => mode.ToString();

        public void SetMode(HttpContext httpContext, AdminPreviewMode mode) => Mode = mode;

        public void SetImpersonatedUserId(HttpContext httpContext, Guid userId) => ImpersonatedUserId = userId;

        public void ClearImpersonation(HttpContext httpContext) => ImpersonationCleared = true;
    }

    private sealed class FakeAdminPreviewUserLookup : IAdminPreviewUserLookup
    {
        public AdminPreviewUserProfile? Profile { get; set; }

        public Task<AdminPreviewUserProfile?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile);
    }
}
