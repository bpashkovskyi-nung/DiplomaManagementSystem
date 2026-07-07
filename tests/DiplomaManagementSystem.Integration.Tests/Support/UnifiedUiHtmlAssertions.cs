namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class UnifiedUiHtmlAssertions
{
    public static void AssertCheckpointQueueTable(string html)
    {
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Студент");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Група");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Тема");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Робота");
        Assert.Contains("text-end text-nowrap", html, StringComparison.Ordinal);
    }

    public static void AssertContainsNavTitles(string html, params string[] titles)
    {
        foreach (string title in titles)
        {
            IntegrationTestHtmlAssertions.AssertContainsText(html, title);
        }
    }
}
