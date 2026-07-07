using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Admin.Models;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class AdminDefenceSessionViewModelMapper
{
    public static DefenceSessionListItemViewModel MapListItem(DefenceSessionListItemDto item) => new()
    {
        Id = item.Id,
        Year = item.Year,
        Type = item.Type,
        TypeDisplay = UkrainianDisplay.FormatDefenceSessionType(item.Type),
        Semester = item.Semester,
        Status = item.Status,
        StatusDisplay = UkrainianDisplay.FormatDefenceSessionStatus(item.Status),
        SessionLabel = SecretarySessionLabel.Format(item.Year, item.Type, item.Semester),
        GroupCount = item.GroupCount,
        DiplomaCount = item.DiplomaCount,
    };

    public static DefenceSessionDetailsViewModel MapDetails(DefenceSessionDetailsDto details) => new()
    {
        Id = details.Id,
        Year = details.Year,
        Type = details.Type,
        TypeDisplay = UkrainianDisplay.FormatDefenceSessionType(details.Type),
        Semester = details.Semester,
        Status = details.Status,
        StatusDisplay = UkrainianDisplay.FormatDefenceSessionStatus(details.Status),
        DiplomaCount = details.DiplomaCount,
        CanArchive = details.Status == DefenceSessionStatus.Active,
        Groups = details.Groups
            .Select(group => new StudyGroupListItemViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Course = group.Course,
                StudentCount = group.StudentCount,
            })
            .ToList(),
    };
}
