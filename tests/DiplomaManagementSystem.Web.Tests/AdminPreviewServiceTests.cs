using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Http;

namespace DiplomaManagementSystem.Web.Tests;

public sealed class AdminPreviewServiceTests
{
    private readonly AdminPreviewService _service = new();

    [Fact]
    public void SetMode_StoresSelectedModeForSuperAdmin()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();

        _service.SetMode(httpContext, AdminPreviewMode.Secretary);

        Assert.Equal(AdminPreviewMode.Secretary, _service.GetMode(httpContext));
        Assert.True(_service.IsActivePreview(httpContext));
    }

    [Fact]
    public void SetMode_ThrowsForNonSuperAdmin()
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RoleNames.Admin)],
            authenticationType: "test"));

        Assert.Throws<UnauthorizedAccessException>(() =>
            _service.SetMode(httpContext, AdminPreviewMode.Secretary));
    }

    [Fact]
    public void GetMode_ReturnsSuperAdminWhenSessionMissing()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();

        Assert.Equal(AdminPreviewMode.SuperAdmin, _service.GetMode(httpContext));
        Assert.False(_service.IsActivePreview(httpContext));
    }

    [Fact]
    public void SetImpersonatedUserId_StoresUserForSuperAdmin()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        var userId = Guid.NewGuid();

        _service.SetImpersonatedUserId(httpContext, userId);

        Assert.Equal(userId, _service.GetImpersonatedUserId(httpContext));
        Assert.True(_service.HasImpersonation(httpContext));
    }

    [Fact]
    public void SetMode_ToEmployee_ClearsImpersonationWhenSwitchingFromSecretary()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        _service.SetMode(httpContext, AdminPreviewMode.Secretary);
        _service.SetImpersonatedUserId(httpContext, Guid.NewGuid());

        _service.SetMode(httpContext, AdminPreviewMode.Employee);

        Assert.Null(_service.GetImpersonatedUserId(httpContext));
    }

    [Fact]
    public void GetMode_MapsStoredSecretaryValue()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        httpContext.Session.SetInt32("AdminPreviewMode", (int)AdminPreviewMode.Secretary);

        Assert.Equal(AdminPreviewMode.Secretary, _service.GetMode(httpContext));
    }

    [Fact]
    public void RequiresImpersonation_IsTrueForSecretaryEmployeeAndStudent()
    {
        Assert.True(_service.RequiresImpersonation(AdminPreviewMode.Student));
        Assert.True(_service.RequiresImpersonation(AdminPreviewMode.Secretary));
        Assert.True(_service.RequiresImpersonation(AdminPreviewMode.Employee));
        Assert.False(_service.RequiresImpersonation(AdminPreviewMode.SuperAdmin));
        Assert.False(_service.RequiresImpersonation(AdminPreviewMode.Admin));
    }

    [Fact]
    public void GetMode_UsesPrincipal_WhenHttpContextUserIsAnonymous()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        _service.SetMode(httpContext, AdminPreviewMode.Secretary);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        ClaimsPrincipal superAdminPrincipal = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)],
            authenticationType: "test"));

        Assert.Equal(AdminPreviewMode.Secretary, _service.GetMode(httpContext, superAdminPrincipal));
    }

    [Fact]
    public void GetImpersonatedUserId_UsesPrincipal_WhenHttpContextUserIsAnonymous()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        var userId = Guid.NewGuid();
        _service.SetImpersonatedUserId(httpContext, userId);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        ClaimsPrincipal superAdminPrincipal = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)],
            authenticationType: "test"));

        Assert.Equal(userId, _service.GetImpersonatedUserId(httpContext, superAdminPrincipal));
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_WhenOriginalUserIdClaimPresent()
    {
        DefaultHttpContext httpContext = CreateSuperAdminContext();
        ClaimsPrincipal impersonatedPrincipal = new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, RoleNames.Employee),
                new Claim(AdminPreviewClaimTypes.OriginalUserId, Guid.NewGuid().ToString()),
            ],
            authenticationType: "test"));

        Assert.True(_service.IsAdmin(impersonatedPrincipal, httpContext));
    }

    private static DefaultHttpContext CreateSuperAdminContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)],
            authenticationType: "test"));
        httpContext.Session = new TestSession();
        return httpContext;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;

        public string Id { get; } = Guid.NewGuid().ToString();

        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
