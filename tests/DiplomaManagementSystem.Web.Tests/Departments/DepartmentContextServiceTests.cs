using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Web.Departments;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Web.Tests.Departments;

public sealed class DepartmentContextServiceTests
{
    [Fact]
    public async Task EnsureAdminContext_MultipleAssignments_RedirectsToSelect()
    {
        TestContext context = await CreateContextAsync();
        Guid userId = Guid.NewGuid();
        context.Authorization.SetAdminDepartments(userId, context.DepartmentAId, context.DepartmentBId);
        DefaultHttpContext httpContext = CreateAdminHttpContext(userId);

        IActionResult? result = await context.Service.EnsureAdminContextAsync(httpContext, userId);

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Select", redirect.ActionName);
        Assert.Equal("Department", redirect.ControllerName);
        Assert.Equal("Admin", redirect.RouteValues?["area"]);
    }

    [Fact]
    public async Task EnsureAdminContext_SingleAssignment_AutoSetsDepartmentCookie()
    {
        TestContext context = await CreateContextAsync();
        Guid userId = Guid.NewGuid();
        context.Authorization.SetAdminDepartments(userId, context.DepartmentAId);
        DefaultHttpContext httpContext = CreateAdminHttpContext(userId);

        IActionResult? result = await context.Service.EnsureAdminContextAsync(httpContext, userId);

        Assert.Null(result);
        string setCookie = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("dms.dept=", setCookie, StringComparison.Ordinal);
        Assert.Contains(context.DepartmentAId.ToString(), setCookie, StringComparison.Ordinal);
    }

    private static DefaultHttpContext CreateAdminHttpContext(Guid userId)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier,
                    userId.ToString()),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role,
                    RoleNames.Admin),
            ],
            authenticationType: "test"));
        return httpContext;
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        FakeDepartmentAuthorizationService authorization = new();
        services.AddSingleton(authorization);
        services.AddSingleton<IDepartmentAuthorizationService>(authorization);
        services.AddSingleton<TestDepartmentContext>();
        services.AddSingleton<IDepartmentContext>(provider => provider.GetRequiredService<TestDepartmentContext>());
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IOptions<DepartmentOptions>>(Options.Create(new DepartmentOptions
        {
            SelectedDepartmentCookieName = "dms.dept",
            DepartmentCookieExpirationDays = 30,
        }));
        services.AddScoped<IDepartmentSessionService, DepartmentSessionService>();
        services.AddScoped<IDepartmentContextService, DepartmentContextService>();

        ServiceProvider provider = services.BuildServiceProvider();
        ApplicationDbContext dbContext = provider.GetRequiredService<ApplicationDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Faculty facultyA = new()
        {
            Id = Guid.NewGuid(),
            Name = "Факультет A",
            IsActive = true,
            CreatedAt = now,
        };
        Faculty facultyB = new()
        {
            Id = Guid.NewGuid(),
            Name = "Факультет B",
            IsActive = true,
            CreatedAt = now,
        };
        Department departmentA = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = facultyA.Id,
            Name = "Кафедра A",
            SpecialtyCode = "111",
            SpecialtyName = "Спеціальність A",
            StudyForm = "очної форми навчання",
            IsActive = true,
            CreatedAt = now,
        };
        Department departmentB = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = facultyB.Id,
            Name = "Кафедра B",
            SpecialtyCode = "222",
            SpecialtyName = "Спеціальність B",
            StudyForm = "очної форми навчання",
            IsActive = true,
            CreatedAt = now,
        };

        dbContext.Faculties.AddRange(facultyA, facultyB);
        dbContext.Departments.AddRange(departmentA, departmentB);
        await dbContext.SaveChangesAsync();

        HttpContextAccessor httpContextAccessor = (HttpContextAccessor)provider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();

        return new TestContext(
            provider.GetRequiredService<IDepartmentContextService>(),
            authorization,
            departmentA.Id,
            departmentB.Id);
    }

    private sealed record TestContext(
        IDepartmentContextService Service,
        FakeDepartmentAuthorizationService Authorization,
        Guid DepartmentAId,
        Guid DepartmentBId);

    private sealed class TestDepartmentContext : IDepartmentContext
    {
        public Guid? CurrentDepartmentId => null;

        public bool IsSuperAdminImpersonating => false;
    }
}
