using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

namespace DiplomaManagementSystem.Web.Areas.Secretary;

internal static class SecretaryNavigation
{
    public static IReadOnlyList<SecretaryNavLink> DocumentsAndReports() =>
    [
        new(SecretaryPageTitles.Home, "Dashboard", "Index"),
        new(SecretaryPageTitles.TopicOrder, "Documents", "TopicOrder"),
        new(SecretaryPageTitles.AdmittedReport, "Reports", "Admitted"),
    ];

    public static IReadOnlyList<SecretaryNavLink> DiplomaDetailsBack(DefenceSessionType sessionType) =>
    [
        new(DefenceWorkLabel.PluralCapitalized(sessionType), "Diplomas", "Index"),
        new(SecretaryPageTitles.Home, "Dashboard", "Index"),
    ];
}
