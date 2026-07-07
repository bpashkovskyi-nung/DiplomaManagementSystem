using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

namespace DiplomaManagementSystem.Web.Areas.Employee;

internal static class EmployeeRoleNavigationBuilder
{
    public static EmployeeRoleNavViewModel Build(
        EmployeeHomeDto home,
        EmployeeRoleArea activeRole,
        string currentController,
        string currentAction)
    {
        Dictionary<EmployeeRoleArea, List<EmployeeRoleCardDto>> cardsByArea = GroupCards(home.Roles);
        IReadOnlyList<EmployeeRoleArea> availableRoles = cardsByArea.Keys.OrderBy(area => (int)area).ToList();

        return new EmployeeRoleNavViewModel
        {
            ActiveRole = activeRole,
            AvailableRoles = availableRoles
                .Select(area => CreateSwitcherItem(area, cardsByArea[area], area == activeRole))
                .ToList(),
            Submenu = CreateSubmenu(activeRole, cardsByArea.GetValueOrDefault(activeRole) ?? [], currentController, currentAction),
        };
    }

    public static IReadOnlyList<EmployeeHomeSectionViewModel> BuildHomeSections(IReadOnlyList<EmployeeRoleCardDto> roles)
    {
        Dictionary<EmployeeRoleArea, List<EmployeeRoleCardDto>> cardsByArea = GroupCards(roles);
        List<EmployeeHomeSectionViewModel> sections = [];

        if (cardsByArea.TryGetValue(EmployeeRoleArea.Supervisor, out List<EmployeeRoleCardDto>? supervisorCards))
        {
            sections.Add(CreateSection(EmployeePageTitles.SupervisorRole, supervisorCards));
        }

        if (cardsByArea.TryGetValue(EmployeeRoleArea.Reviewer, out List<EmployeeRoleCardDto>? reviewerCards))
        {
            sections.Add(CreateSection(EmployeePageTitles.ReviewerRole, reviewerCards));
        }

        List<EmployeeRoleCardDto> sessionCards = [];
        AppendSessionCards(cardsByArea, sessionCards, EmployeeRoleArea.DepartmentHead);
        AppendSessionCards(cardsByArea, sessionCards, EmployeeRoleArea.AntiPlagiarism);
        AppendSessionCards(cardsByArea, sessionCards, EmployeeRoleArea.FormattingReview);

        if (sessionCards.Count > 0)
        {
            sections.Add(CreateSection(EmployeePageTitles.SessionRolesSection, sessionCards));
        }

        return sections;
    }

    public static EmployeeRoleArea ResolveArea(string roleKey) => roleKey switch
    {
        "SupervisorStudents" or "SupervisorPendingStudents" or "SupervisorTopicReviews" or "SupervisorFeedback" =>
            EmployeeRoleArea.Supervisor,
        "ReviewerStudents" or "Reviewer" => EmployeeRoleArea.Reviewer,
        "DepartmentHead" => EmployeeRoleArea.DepartmentHead,
        "AntiPlagiarism" => EmployeeRoleArea.AntiPlagiarism,
        "FormattingReview" => EmployeeRoleArea.FormattingReview,
        _ => throw new ArgumentOutOfRangeException(nameof(roleKey), roleKey, "Unknown employee role key."),
    };

    public static string GetAreaDisplay(EmployeeRoleArea area) => area switch
    {
        EmployeeRoleArea.Supervisor => EmployeePageTitles.SupervisorRole,
        EmployeeRoleArea.Reviewer => EmployeePageTitles.ReviewerRole,
        EmployeeRoleArea.DepartmentHead => EmployeePageTitles.DepartmentHeadRole,
        EmployeeRoleArea.AntiPlagiarism => EmployeePageTitles.AntiPlagiarismRole,
        EmployeeRoleArea.FormattingReview => EmployeePageTitles.FormattingReviewRole,
        _ => throw new ArgumentOutOfRangeException(nameof(area), area, null),
    };

    public static string GetSubmenuLabel(string roleKey) => roleKey switch
    {
        "SupervisorStudents" or "ReviewerStudents" => EmployeePageTitles.MyStudentsNav,
        "SupervisorPendingStudents" => EmployeePageTitles.ConfirmStudentRequestNav,
        "SupervisorTopicReviews" => EmployeePageTitles.ApproveTopicNav,
        "SupervisorFeedback" => EmployeePageTitles.SubmitSupervisorFeedbackNav,
        "Reviewer" => EmployeePageTitles.SubmitExternalReviewNav,
        "DepartmentHead" => EmployeePageTitles.ApproveTopicNav,
        "AntiPlagiarism" => EmployeePageTitles.AntiPlagiarismRole,
        "FormattingReview" => EmployeePageTitles.FormattingReviewRole,
        _ => throw new ArgumentOutOfRangeException(nameof(roleKey), roleKey, "Unknown employee role key."),
    };

    private static Dictionary<EmployeeRoleArea, List<EmployeeRoleCardDto>> GroupCards(
        IReadOnlyList<EmployeeRoleCardDto> roles)
    {
        Dictionary<EmployeeRoleArea, List<EmployeeRoleCardDto>> cardsByArea = [];

        foreach (EmployeeRoleCardDto role in roles)
        {
            EmployeeRoleArea area = ResolveArea(role.RoleKey);
            if (!cardsByArea.TryGetValue(area, out List<EmployeeRoleCardDto>? cards))
            {
                cards = [];
                cardsByArea[area] = cards;
            }

            cards.Add(role);
        }

        return cardsByArea;
    }

    private static EmployeeHomeSectionViewModel CreateSection(
        string title,
        IReadOnlyList<EmployeeRoleCardDto> cards)
    {
        EmployeeRoleCardDto? studentListCard = cards.FirstOrDefault(card => card.CountsStudents);
        IReadOnlyList<EmployeeRoleCardDto> queueCards = cards.Where(card => !card.CountsStudents).ToList();

        return new EmployeeHomeSectionViewModel
        {
            Title = title,
            StudentListLink = studentListCard is null
                ? null
                : new EmployeeHomeStudentListLinkViewModel
                {
                    Text = GetSubmenuLabel(studentListCard.RoleKey),
                    Count = studentListCard.PendingCount,
                    Controller = studentListCard.Controller,
                    Action = studentListCard.Action,
                },
            Items = queueCards
                .Select(card => new EmployeeHomeItemViewModel
                {
                    RoleKey = card.RoleKey,
                    RoleDisplay = GetSubmenuLabel(card.RoleKey),
                    PendingCount = card.PendingCount,
                    Controller = card.Controller,
                    Action = card.Action,
                    CountsStudents = false,
                    IsStudentList = false,
                })
                .ToList(),
        };
    }

    private static void AppendSessionCards(
        Dictionary<EmployeeRoleArea, List<EmployeeRoleCardDto>> cardsByArea,
        List<EmployeeRoleCardDto> sessionCards,
        EmployeeRoleArea area)
    {
        if (cardsByArea.TryGetValue(area, out List<EmployeeRoleCardDto>? cards))
        {
            sessionCards.AddRange(cards);
        }
    }

    private static EmployeeRoleSwitcherItemViewModel CreateSwitcherItem(
        EmployeeRoleArea area,
        IReadOnlyList<EmployeeRoleCardDto> cards,
        bool isActive)
    {
        EmployeeRoleCardDto landing = cards[0];
        int pendingTotal = cards.Where(card => !card.CountsStudents).Sum(card => card.PendingCount);

        return new EmployeeRoleSwitcherItemViewModel
        {
            Area = area,
            Display = GetAreaDisplay(area),
            Controller = landing.Controller,
            Action = landing.Action,
            PendingTotal = pendingTotal,
            IsActive = isActive,
        };
    }

    private static IReadOnlyList<EmployeeRoleSubmenuItemViewModel> CreateSubmenu(
        EmployeeRoleArea area,
        IReadOnlyList<EmployeeRoleCardDto> cards,
        string currentController,
        string currentAction)
    {
        if (cards.Count <= 1)
        {
            return [];
        }

        return cards
            .Select(card => new EmployeeRoleSubmenuItemViewModel
            {
                Text = GetSubmenuLabel(card.RoleKey),
                Controller = card.Controller,
                Action = card.Action,
                IsActive = IsSubmenuActive(card, currentController, currentAction),
                PendingCount = card.PendingCount,
                CountsStudents = card.CountsStudents,
            })
            .ToList();
    }

    private static bool IsSubmenuActive(EmployeeRoleCardDto card, string controller, string action)
    {
        if (string.Equals(card.Controller, controller, StringComparison.OrdinalIgnoreCase)
            && string.Equals(card.Action, action, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(controller, "Supervisor", StringComparison.OrdinalIgnoreCase)
               && string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase)
               && string.Equals(card.Controller, "Supervisor", StringComparison.OrdinalIgnoreCase)
               && string.Equals(card.Action, "Students", StringComparison.OrdinalIgnoreCase);
    }
}
