using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class SecretaryListViewModelMapper
{
    public static SupervisorStudentsListViewModel MapSupervisorStudents(
        SupervisorDiplomaListPageDto page,
        DiplomaListFilterDto filter,
        bool showSupervisorColumn,
        bool showDetailsLink) => new()
        {
            Items = page.Items.Select(SecretaryListViewModelMapper.MapListItem).ToList(),
            ShowSupervisorColumn = showSupervisorColumn,
            ShowDetailsLink = showDetailsLink,
            Filter = new DiplomaListFilterViewModel
            {
                LifecycleStatus = filter.LifecycleStatus,
                CurrentAdmissionStep = filter.CurrentAdmissionStep,
                StudyGroupId = filter.StudyGroupId,
                Search = filter.Search,
                LifecycleStatuses = BuildLifecycleSelectList(filter.LifecycleStatus),
                AdmissionSteps = BuildAdmissionStepSelectList(filter.CurrentAdmissionStep),
                StudyGroups = page.StudyGroups
                .Select(group => new SelectListItem(group.Name, group.Id.ToString(), group.Id == filter.StudyGroupId))
                .Prepend(new SelectListItem("Усі групи", string.Empty))
                .ToList(),
            },
        };

    public static SupervisorStudentsListViewModel MapReviewerStudents(
        ReviewerDiplomaListPageDto page,
        DiplomaListFilterDto filter,
        bool showSupervisorColumn,
        bool showDetailsLink) => new()
        {
            Items = page.Items.Select(SecretaryListViewModelMapper.MapListItem).ToList(),
            ShowSupervisorColumn = showSupervisorColumn,
            ShowDetailsLink = showDetailsLink,
            Filter = new DiplomaListFilterViewModel
            {
                LifecycleStatus = filter.LifecycleStatus,
                CurrentAdmissionStep = filter.CurrentAdmissionStep,
                StudyGroupId = filter.StudyGroupId,
                Search = filter.Search,
                LifecycleStatuses = BuildLifecycleSelectList(filter.LifecycleStatus),
                AdmissionSteps = BuildAdmissionStepSelectList(filter.CurrentAdmissionStep),
                StudyGroups = page.StudyGroups
                    .Select(group => new SelectListItem(group.Name, group.Id.ToString(), group.Id == filter.StudyGroupId))
                    .Prepend(new SelectListItem("Усі групи", string.Empty))
                    .ToList(),
            },
        };

    public static DiplomaListViewModel MapIndex(DiplomaListPageDto page, DiplomaListFilterDto filter) => new()
    {
        SessionId = page.SessionId,
        SessionType = page.SessionType,
        Items = page.Items.Select(MapListItem).ToList(),
        Filter = new DiplomaListFilterViewModel
        {
            LifecycleStatus = filter.LifecycleStatus,
            CurrentAdmissionStep = filter.CurrentAdmissionStep,
            StudyGroupId = filter.StudyGroupId,
            Search = filter.Search,
            LifecycleStatuses = BuildLifecycleSelectList(filter.LifecycleStatus),
            AdmissionSteps = BuildAdmissionStepSelectList(filter.CurrentAdmissionStep),
            StudyGroups = page.StudyGroups
                .Select(group => new SelectListItem(group.Name, group.Id.ToString(), group.Id == filter.StudyGroupId))
                .Prepend(new SelectListItem("Усі групи", string.Empty))
                .ToList(),
        },
    };

    public static DiplomaListItemViewModel MapListItem(DiplomaListItemDto item) => new()
    {
        Id = item.Id,
        StudentFullName = item.StudentFullName,
        StudentEmail = item.StudentEmail,
        StudyGroupName = item.StudyGroupName,
        SupervisorName = item.SupervisorName,
        TopicTitle = item.TopicTitle,
        LifecycleStatus = item.LifecycleStatus,
        LifecycleDisplay = UkrainianDisplay.FormatSecretaryWorkflowStatus(
            item.LifecycleStatus,
            item.CurrentAdmissionStep),
        AdmissionStatus = item.AdmissionStatus,
        CurrentAdmissionStep = item.CurrentAdmissionStep,
        CurrentAdmissionStepDisplay = item.CurrentAdmissionStep.HasValue
            ? UkrainianDisplay.FormatAdmissionStep(item.CurrentAdmissionStep.Value)
            : null,
        OutcomeStepsCompleted = item.OutcomeStepsCompleted,
        OutcomeStepsTotal = item.OutcomeStepsTotal,
    };

    private static List<SelectListItem> BuildLifecycleSelectList(DiplomaLifecycleStatus? selected)
    {
        return Enum.GetValues<DiplomaLifecycleStatus>()
            .Where(status => status is not (
                DiplomaLifecycleStatus.TopicApproved
                or DiplomaLifecycleStatus.DocumentsInProgress))
            .Select(status => new SelectListItem(
                UkrainianDisplay.FormatDiplomaLifecycleStatus(status),
                status.ToString(),
                status == selected))
            .Prepend(new SelectListItem("Усі статуси", string.Empty))
            .ToList();
    }

    private static List<SelectListItem> BuildAdmissionStepSelectList(AdmissionStep? selected)
    {
        return Enum.GetValues<AdmissionStep>()
            .Select(step => new SelectListItem(
                UkrainianDisplay.FormatAdmissionStep(step),
                step.ToString(),
                step == selected))
            .Prepend(new SelectListItem("Усі кроки", string.Empty))
            .ToList();
    }
}
