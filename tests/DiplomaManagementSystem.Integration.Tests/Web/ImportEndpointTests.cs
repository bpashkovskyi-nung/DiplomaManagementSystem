using System.Net.Http.Headers;

using DiplomaManagementSystem.Application.Admin.Students.Contracts;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class ImportEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostStudentImport_ReturnsSuccessWithImportedCount()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(setupScope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        string suffix = Guid.NewGuid().ToString("N")[..8];
        await using MemoryStream csv = IntegrationTestCsv.Students(
            ($"Іван Іваненко", $"ivan.http.{suffix}@nung.edu.ua", "КН-41"));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage importPage = await client.GetAsync($"/Admin/Import/Students?defenceSessionId={sessionId}");
        importPage.EnsureSuccessStatusCode();
        string html = await importPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        csv.Position = 0;
        byte[] csvBytes = csv.ToArray();
        MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(sessionId.ToString()), "DefenceSessionId");
        ByteArrayContent fileContent = new(csvBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "File", "students.csv");

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/Import/Students", form);

        postResponse.EnsureSuccessStatusCode();
        string resultHtml = await postResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "імпортовано 1", ignoreCase: true);

        await using AsyncServiceScope verifyScope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(verifyScope.ServiceProvider);
        IStudentAdminService studentAdminService = verifyScope.ServiceProvider.GetRequiredService<IStudentAdminService>();
        Assert.Single(await studentAdminService.GetAllAsync(sessionId, CancellationToken.None));
    }
}
