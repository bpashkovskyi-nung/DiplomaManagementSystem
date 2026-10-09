using System.Security.Claims;

using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Security;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace DiplomaManagementSystem.Web;

internal static class AuthenticationConfiguration
{
    public static IServiceCollection AddGoogleAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? clientId = configuration["Authentication:Google:ClientId"];
        string? clientSecret = configuration["Authentication:Google:ClientSecret"];

        bool googleConfigured = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);

        AuthenticationBuilder authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = googleConfigured
                    ? GoogleDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

        if (googleConfigured)
        {
            authenticationBuilder.AddGoogle(options =>
            {
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.SaveTokens = true;
                options.Events.OnTicketReceived = async context =>
                {
                    EmailDomainValidator validator = context.HttpContext.RequestServices
                        .GetRequiredService<EmailDomainValidator>();

                    string? email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                                    ?? context.Principal?.FindFirstValue("email");

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }

                    email = email.Trim().ToLowerInvariant();

                    if (!validator.IsAllowed(email))
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }

                    UserManager<ApplicationUser> userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<ApplicationUser>>();
                    SignInManager<ApplicationUser> signInManager = context.HttpContext.RequestServices
                        .GetRequiredService<SignInManager<ApplicationUser>>();

                    ApplicationUser? user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }

                    await signInManager.SignInAsync(user, isPersistent: true);
                    context.HandleResponse();
                    context.Response.Redirect("/");
                };
            });
        }

        return services;
    }
}
