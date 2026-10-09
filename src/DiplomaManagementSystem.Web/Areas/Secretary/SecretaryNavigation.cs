using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

namespace DiplomaManagementSystem.Web.Areas.Secretary;

internal static class SecretaryNavigation
{
    public static IReadOnlyList<SecretaryNavLink> DiplomaDetailsBack(DefenceSessionType sessionType) =>
    [
        new(DefenceWorkLabel.PluralCapitalized(sessionType), "Diplomas", "Index"),
    ];
}
