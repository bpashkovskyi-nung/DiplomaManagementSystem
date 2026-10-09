using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Mapping;

namespace DiplomaManagementSystem.Web.Tests.Mapping;

public sealed class SecretaryListAndDashboardMapperTests
{
    // TC-WEB-MAP-003
    [Fact]
    public void MapListItem_MapsStudentAndProgressFields()
    {
        DiplomaListItemDto item = new(
            Guid.NewGuid(),
            "Іван Іваненко",
            "ivan@test.local",
            "КН-41",
            "Петро Петренко",
            "Тема",
            DiplomaLifecycleStatus.DocumentsInProgress,
            DiplomaAdmissionStatus.NotAdmitted,
            AdmissionStep.FormattingReview,
            2,
            4);

        DiplomaListItemViewModel viewModel = SecretaryListViewModelMapper.MapListItem(item);

        Assert.Equal(item.Id, viewModel.Id);
        Assert.Equal("Іван Іваненко", viewModel.StudentFullName);
        Assert.NotNull(viewModel.LifecycleDisplay);
        Assert.Equal("Нормоконтроль", viewModel.CurrentAdmissionStepDisplay);
        Assert.Equal(2, viewModel.OutcomeStepsCompleted);
        Assert.Equal(4, viewModel.OutcomeStepsTotal);
    }

    // TC-WEB-MAP-003b
    [Fact]
    public void MapIndex_BuildsFilterSelectLists()
    {
        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        DiplomaListPageDto page = new(
            sessionId,
            DefenceSessionType.Bachelor,
            [],
            new DiplomaListFilterDto(null, null, null, null, groupId, null),
            [new StudyGroupFilterOptionDto(groupId, "КН-41")]);

        DiplomaListViewModel viewModel = SecretaryListViewModelMapper.MapIndex(
            page,
            page.Filter);

        Assert.Equal(sessionId, viewModel.SessionId);
        Assert.NotEmpty(viewModel.Filter.LifecycleStatuses);
        Assert.NotEmpty(viewModel.Filter.AdmissionSteps);
        Assert.Equal(2, viewModel.Filter.StudyGroups.Count);
        Assert.Equal("Усі групи", viewModel.Filter.StudyGroups[0].Text);
    }

    // TC-WEB-MAP-004
    [Fact]
    public void MapDashboard_MapsBucketsWithLabels()
    {
        SecretaryDashboardDto dashboard = new(
            Guid.NewGuid(),
            DefenceSessionType.Bachelor,
            "2026 — Бакалавр",
            [
                new SecretaryDashboardBucketDto(DiplomaLifecycleStatus.AwaitingSupervisor, null, 3),
                new SecretaryDashboardBucketDto(null, AdmissionStep.ExternalReview, 2),
            ],
            5);

        SecretaryDashboardViewModel viewModel = SecretaryDashboardViewModelMapper.Map(dashboard);

        Assert.Equal(5, viewModel.TotalDiplomas);
        Assert.Equal(2, viewModel.Buckets.Count);
        Assert.Contains("реценз", viewModel.Buckets[1].Label, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(viewModel.Buckets[0].BadgeClass);
    }

    // TC-WEB-MAP-005
    [Fact]
    public void MapReport_MapsAdmittedItems()
    {
        AdmittedReportDto report = new(
            Guid.NewGuid(),
            "2026 — Бакалавр",
            [
                new AdmittedReportItemDto(
                    "Іван Іваненко",
                    "КН-41",
                    "Тема",
                    "Петро Петренко",
                    "Олена Коваленко",
                    new DateOnly(2026, 6, 20)),
            ]);

        AdmittedReportViewModel viewModel = SecretaryReportsViewModelMapper.Map(report);

        Assert.Equal(report.SessionLabel, viewModel.SessionLabel);
        AdmittedReportItemViewModel item = Assert.Single(viewModel.Items);
        Assert.Equal("Іван Іваненко", item.StudentFullName);
        Assert.Equal(new DateOnly(2026, 6, 20), item.DefenceDate);
    }
}
