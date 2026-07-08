using System.Globalization;
using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Infrastructure;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Web;
using DiplomaManagementSystem.Web.AdminPreview;
using DiplomaManagementSystem.Web.Authorization;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Validation;
using DiplomaManagementSystem.Web.Departments;
using FluentValidation;
using DiplomaManagementSystem.Web.Filters;
using DiplomaManagementSystem.Web.Secretary;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddScoped<IDepartmentContext, DepartmentContextAccessor>();
builder.Services.AddScoped<IDepartmentSessionService, DepartmentSessionService>();
builder.Services.AddScoped<IDepartmentContextService, DepartmentContextService>();
builder.Services.AddScoped<AdminDepartmentContextFilter>();
builder.Services.AddScoped<EmployeeDepartmentContextFilter>();
builder.Services.AddScoped<IValidator<OrganizationImportViewModel>, OrganizationImportViewModelValidator>();
builder.Services.AddScoped<IAdminPreviewService, AdminPreviewService>();
builder.Services.AddScoped<AdminPreviewImpersonationFilter>();
builder.Services.AddScoped<IClaimsTransformation, AdminPreviewClaimsTransformation>();
builder.Services.AddScoped<ISecretarySessionService, SecretarySessionService>();
builder.Services.AddScoped<IAuthorizationHandler, SecretaryAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(RoleNames.SuperAdmin, policy => policy.RequireRole(RoleNames.SuperAdmin))
    .AddPolicy(RoleNames.Admin, policy => policy.RequireRole(RoleNames.Admin))
    .AddPolicy(PolicyNames.ExamCommissionSecretary, policy => policy.AddRequirements(new SecretaryRequirement()));
builder.Services.AddGoogleAuthentication(builder.Configuration);
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews(options => { options.Filters.AddService<AdminPreviewImpersonationFilter>(); })
    .AddViewLocalization();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

WebApplication app = builder.Build();

AppLocalizationOptions localizationOptions = app.Services
    .GetRequiredService<IOptions<AppLocalizationOptions>>()
    .Value;

CultureInfo[] supportedCultures = localizationOptions.SupportedCultures
    .Select(culture => new CultureInfo(culture))
    .ToArray();

RequestLocalizationOptions requestLocalizationOptions = new()
{
    DefaultRequestCulture = new RequestCulture(localizationOptions.DefaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
};

app.UseRequestLocalization(requestLocalizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/health");

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
