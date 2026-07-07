using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.Admin;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Areas.Employee;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Areas.Secretary;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

namespace DiplomaManagementSystem.Web.Tests.Navigation;

public sealed class AreaNavigationTests
{
    [Fact]
    public void EmployeeNavigation_SupervisorWorkflow_ContainsExpectedLinksInOrder()
    {
        IReadOnlyList<EmployeeNavLink> links = EmployeeNavigation.SupervisorWorkflow();

        Assert.Equal(5, links.Count);
        Assert.Equal(EmployeePageTitles.MyStudents, links[0].Text);
        Assert.Equal("Supervisor", links[0].Controller);
        Assert.Equal("Students", links[0].Action);
        Assert.Equal(EmployeePageTitles.SubmitSupervisorFeedback, links[3].Text);
        Assert.Equal(EmployeePageTitles.Home, links[4].Text);
    }

    [Fact]
    public void EmployeeNavigation_ReviewerWorkflow_ContainsExpectedLinks()
    {
        IReadOnlyList<EmployeeNavLink> links = EmployeeNavigation.ReviewerWorkflow();

        Assert.Equal(3, links.Count);
        Assert.Equal(EmployeePageTitles.MyReviewStudents, links[0].Text);
        Assert.Equal(EmployeePageTitles.SubmitExternalReview, links[1].Text);
        Assert.Equal(EmployeePageTitles.Home, links[2].Text);
    }

    [Fact]
    public void SecretaryNavigation_DocumentsAndReports_ContainsExpectedLinks()
    {
        IReadOnlyList<SecretaryNavLink> links = SecretaryNavigation.DocumentsAndReports();

        Assert.Equal(3, links.Count);
        Assert.Equal(SecretaryPageTitles.Home, links[0].Text);
        Assert.Equal(SecretaryPageTitles.TopicOrder, links[1].Text);
        Assert.Equal(SecretaryPageTitles.AdmittedReport, links[2].Text);
    }

    [Fact]
    public void AdminNavigation_Global_ContainsExpectedLinks()
    {
        IReadOnlyList<AdminNavLink> links = AdminNavigation.Global();

        Assert.Equal(3, links.Count);
        Assert.Equal(AdminPageTitles.Home, links[0].Text);
        Assert.Equal(AdminPageTitles.DefenceSessions, links[1].Text);
        Assert.Equal(AdminPageTitles.Employees, links[2].Text);
    }

    [Fact]
    public void AdminNavigation_SessionContext_IncludesSessionAndStudentsRoutes()
    {
        Guid sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        IReadOnlyList<AdminNavLink> links = AdminNavigation.SessionContext(sessionId);

        Assert.Equal(4, links.Count);
        Assert.Equal(sessionId.ToString(), links[1].RouteValues!["id"]);
        Assert.Equal(sessionId.ToString(), links[2].RouteValues!["defenceSessionId"]);
        Assert.Equal(AdminPageTitles.Students, links[2].Text);
    }

    [Fact]
    public void AdminNavigation_StudentsBack_LinksToStudentsAndSession()
    {
        Guid sessionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        IReadOnlyList<AdminNavLink> links = AdminNavigation.StudentsBack(sessionId);

        Assert.Equal(3, links.Count);
        Assert.Equal("Students", links[0].Controller);
        Assert.Equal(sessionId.ToString(), links[0].RouteValues!["defenceSessionId"]);
        Assert.Equal("DefenceSessions", links[1].Controller);
        Assert.Equal("Details", links[1].Action);
    }
}
