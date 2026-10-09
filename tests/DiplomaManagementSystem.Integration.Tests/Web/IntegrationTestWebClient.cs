using Microsoft.AspNetCore.Mvc.Testing;

namespace DiplomaManagementSystem.Integration.Tests.Web;

internal static class IntegrationTestWebClient
{
    public static HttpClient CreateClient(
        WebApplicationFactory<Program> factory,
        Guid? userId = null,
        bool allowAutoRedirect = false)
    {
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowAutoRedirect,
            HandleCookies = true,
        };

        HttpClient client = factory.CreateClient(options);

        if (userId.HasValue)
        {
            client.DefaultRequestHeaders.Add(IntegrationTestAuthHandler.UserIdHeaderName, userId.Value.ToString());
        }

        return client;
    }

    public static void SetSecretarySessionCookie(HttpClient client, Guid sessionId)
    {
        client.DefaultRequestHeaders.Add("Cookie", $"SelectedDefenceSessionId={sessionId}");
    }

    public static void SetDepartmentCookie(HttpClient client, Guid departmentId)
    {
        client.DefaultRequestHeaders.Add("Cookie", $"dms.dept={departmentId}");
    }
}
