using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain.Enums;
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
    public void EmployeeRoleNavigation_BuildSupervisorPage_ShowsSupervisorSubmenuOnly()
    {
        EmployeeHomeDto home = CreateMultiRoleHome();

        EmployeeRoleNavViewModel nav = EmployeeRoleNavigationBuilder.Build(
            home,
            EmployeeRoleArea.Supervisor,
            "Supervisor",
            "Students");

        Assert.Equal(EmployeeRoleArea.Supervisor, nav.ActiveRole);
        Assert.Equal(2, nav.AvailableRoles.Count);
        Assert.True(nav.AvailableRoles.Single(role => role.Area == EmployeeRoleArea.Supervisor).IsActive);
        Assert.Equal(5, nav.Submenu.Count);
        Assert.Equal(EmployeePageTitles.MyStudentsNav, nav.Submenu[0].Text);
        Assert.True(nav.Submenu[0].IsActive);
        Assert.Contains(nav.Submenu, item => item.Action == "Progress");
        Assert.DoesNotContain(nav.Submenu, item => item.Controller == "AntiPlagiarism");
    }

    [Fact]
    public void EmployeeRoleNavigation_BuildAntiPlagiarismPage_HasNoCrossRoleSubmenu()
    {
        EmployeeHomeDto home = CreateMultiRoleHome();

        EmployeeRoleNavViewModel nav = EmployeeRoleNavigationBuilder.Build(
            home,
            EmployeeRoleArea.AntiPlagiarism,
            "AntiPlagiarism",
            "Pending");

        Assert.Equal(EmployeeRoleArea.AntiPlagiarism, nav.ActiveRole);
        Assert.Empty(nav.Submenu);
        Assert.Equal(2, nav.AvailableRoles.Count);
    }

    [Fact]
    public void EmployeeRoleNavigation_DetailsPage_HighlightsStudentsSubmenu()
    {
        EmployeeHomeDto home = CreateSupervisorOnlyHome();

        EmployeeRoleNavViewModel nav = EmployeeRoleNavigationBuilder.Build(
            home,
            EmployeeRoleArea.Supervisor,
            "Supervisor",
            "Details");

        EmployeeRoleSubmenuItemViewModel students = Assert.Single(nav.Submenu, item => item.Action == "Students");
        Assert.True(students.IsActive);
    }

    [Fact]
    public void EmployeeRoleNavigation_BuildHomeSections_GroupsRolesByArea()
    {
        IReadOnlyList<EmployeeHomeSectionViewModel> sections =
            EmployeeRoleNavigationBuilder.BuildHomeSections(CreateMultiRoleHome().Roles);

        Assert.Equal(2, sections.Count);
        Assert.Equal(EmployeePageTitles.SupervisorRole, sections[0].Title);
        Assert.Equal(4, sections[0].Items.Count);
        Assert.NotNull(sections[0].StudentListLink);
        Assert.Equal(EmployeePageTitles.MyStudentsNav, sections[0].StudentListLink!.Text);
        Assert.Equal(EmployeePageTitles.SessionRolesSection, sections[1].Title);
        Assert.Single(sections[1].Items);
        Assert.Equal(EmployeePageTitles.AntiPlagiarismRole, sections[1].Items[0].RoleDisplay);
    }

    [Fact]
    public void EmployeeRoleNavigation_GetSubmenuLabel_ReturnsShortLabels()
    {
        Assert.Equal(EmployeePageTitles.MyStudentsNav, EmployeeRoleNavigationBuilder.GetSubmenuLabel("SupervisorStudents"));
        Assert.Equal(EmployeePageTitles.ConfirmStudentRequestNav, EmployeeRoleNavigationBuilder.GetSubmenuLabel("SupervisorPendingStudents"));
        Assert.Equal(EmployeePageTitles.SubmitExternalReviewNav, EmployeeRoleNavigationBuilder.GetSubmenuLabel("Reviewer"));
        Assert.Equal(EmployeePageTitles.AntiPlagiarismRole, EmployeeRoleNavigationBuilder.GetSubmenuLabel("AntiPlagiarism"));
        Assert.Equal(EmployeePageTitles.SupervisorProgress, EmployeeRoleNavigationBuilder.GetSubmenuLabel("SupervisorProgress"));
    }

    [Fact]
    public void SecretaryNavigation_DiplomaDetailsBack_ContainsOnlyDiplomasList()
    {
        IReadOnlyList<SecretaryNavLink> links = SecretaryNavigation.DiplomaDetailsBack(DefenceSessionType.Master);

        Assert.Single(links);
        Assert.Equal("Diplomas", links[0].Controller);
        Assert.Equal("Index", links[0].Action);
    }

    [Fact]
    public void AdminNavigation_DefenceSessionsBack_LinksToSessionsList()
    {
        IReadOnlyList<AdminNavLink> links = AdminNavigation.DefenceSessionsBack();

        Assert.Single(links);
        Assert.Equal(AdminPageTitles.DefenceSessions, links[0].Text);
        Assert.Equal("DefenceSessions", links[0].Controller);
        Assert.Equal("Index", links[0].Action);
    }

    [Fact]
    public void AdminNavigation_SessionBack_LinksToSessionDetails()
    {
        var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        IReadOnlyList<AdminNavLink> links = AdminNavigation.SessionBack(sessionId);

        Assert.Single(links);
        Assert.Equal(AdminPageTitles.DefenceSession, links[0].Text);
        Assert.Equal("DefenceSessions", links[0].Controller);
        Assert.Equal("Details", links[0].Action);
        Assert.Equal(sessionId.ToString(), links[0].RouteValues!["id"]);
    }

    [Fact]
    public void AdminNavigation_StudentsBack_LinksToStudents()
    {
        var sessionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        IReadOnlyList<AdminNavLink> links = AdminNavigation.StudentsBack(sessionId);

        Assert.Single(links);
        Assert.Equal("Students", links[0].Controller);
        Assert.Equal(sessionId.ToString(), links[0].RouteValues!["defenceSessionId"]);
    }

    private static EmployeeHomeDto CreateSupervisorOnlyHome() =>
        new(
        [
            new EmployeeRoleCardDto(
                "SupervisorStudents",
                EmployeePageTitles.MyStudents,
                2,
                "Supervisor",
                "Students",
                CountsStudents: true),
            new EmployeeRoleCardDto(
                "SupervisorPendingStudents",
                EmployeePageTitles.ConfirmStudentRequest,
                1,
                "Supervisor",
                "PendingStudents"),
            new EmployeeRoleCardDto(
                "SupervisorProgress",
                EmployeePageTitles.SupervisorProgress,
                0,
                "Supervisor",
                "Progress"),
        ]);

    private static EmployeeHomeDto CreateMultiRoleHome() =>
        new(
        [
            new EmployeeRoleCardDto(
                "SupervisorStudents",
                EmployeePageTitles.MyStudents,
                2,
                "Supervisor",
                "Students",
                CountsStudents: true),
            new EmployeeRoleCardDto(
                "SupervisorPendingStudents",
                EmployeePageTitles.ConfirmStudentRequest,
                1,
                "Supervisor",
                "PendingStudents"),
            new EmployeeRoleCardDto(
                "SupervisorTopicReviews",
                EmployeePageTitles.ApproveTopicAsSupervisor,
                0,
                "Supervisor",
                "TopicReviews"),
            new EmployeeRoleCardDto(
                "SupervisorFeedback",
                EmployeePageTitles.SubmitSupervisorFeedback,
                0,
                "Supervisor",
                "Checkpoints"),
            new EmployeeRoleCardDto(
                "SupervisorProgress",
                EmployeePageTitles.SupervisorProgress,
                0,
                "Supervisor",
                "Progress"),
            new EmployeeRoleCardDto(
                "AntiPlagiarism",
                EmployeePageTitles.AntiPlagiarism,
                3,
                "AntiPlagiarism",
                "Pending"),
        ]);
}
