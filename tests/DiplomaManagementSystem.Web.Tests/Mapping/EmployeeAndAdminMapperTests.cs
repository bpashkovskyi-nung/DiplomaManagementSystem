using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Mapping;

namespace DiplomaManagementSystem.Web.Tests.Mapping;

public sealed class EmployeeAndAdminMapperTests
{
    // TC-WEB-MAP-009
    [Fact]
    public void MapPendingStudent_MapsFields()
    {
        PendingStudentDto dto = new(Guid.NewGuid(), "Іван", "КН-41", MapperTestFixtures.Now);

        SupervisorStudentItemViewModel viewModel = EmployeeViewModelMapper.MapPendingStudent(dto);

        Assert.Equal(dto.DiplomaId, viewModel.DiplomaId);
        Assert.Equal("Іван", viewModel.StudentFullName);
    }

    // TC-WEB-MAP-009b
    [Fact]
    public void MapTopicReviewItem_MapsFields()
    {
        TopicReviewItemDto dto = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Іван",
            "Петро Керівник",
            "Тема",
            2,
            MapperTestFixtures.Now);

        TopicReviewItemViewModel viewModel = EmployeeViewModelMapper.MapTopicReviewItem(dto);

        Assert.Equal(2, viewModel.VersionNumber);
        Assert.Equal("Тема", viewModel.Title);
        Assert.Equal("Петро Керівник", viewModel.SupervisorFullName);
    }

    // TC-WEB-MAP-009c
    [Fact]
    public void MapPendingCheckpoint_MapsFields()
    {
        PendingCheckpointItemDto dto = new(Guid.NewGuid(), "Іван", "КН-41", "Тема");

        PendingCheckpointItemViewModel viewModel = EmployeeViewModelMapper.MapPendingCheckpoint(dto);

        Assert.Equal("Тема", viewModel.TopicTitle);
    }

    [Fact]
    public void MapPendingCheckpoint_MapsStudentWorkLink()
    {
        PendingCheckpointItemDto dto = new(
            Guid.NewGuid(),
            "Іван",
            "КН-41",
            "Тема",
            new PendingStudentWorkLinkDto("Robota.pdf", "/files/view/1", 1));

        PendingCheckpointItemViewModel viewModel = EmployeeViewModelMapper.MapPendingCheckpoint(dto);

        Assert.NotNull(viewModel.LatestStudentWork);
        Assert.Equal("Robota.pdf", viewModel.LatestStudentWork!.FileName);
        Assert.Equal("/files/view/1", viewModel.LatestStudentWork.ViewUrl);
    }

    // TC-WEB-MAP-009d
    [Fact]
    public void MapReviewerAssignment_MapsStudyGroupName()
    {
        ReviewerAssignmentItemDto dto = new(
            Guid.NewGuid(),
            "Іван",
            "КН-41",
            "Тема",
            ReviewAssignmentStatus.Assigned);

        PendingCheckpointItemViewModel viewModel = EmployeeViewModelMapper.MapReviewerAssignment(dto);

        Assert.Equal("КН-41", viewModel.StudyGroupName);
        Assert.Equal("Тема", viewModel.TopicTitle);
    }

    // TC-WEB-MAP-009e
    [Fact]
    public void MapRoleCard_MapsNavigation()
    {
        EmployeeRoleCardDto dto = new("supervisor", "Керівник", 3, "Supervisor", "PendingStudents");

        EmployeeRoleCardViewModel viewModel = EmployeeViewModelMapper.MapRoleCard(dto);

        Assert.Equal("Supervisor", viewModel.Controller);
        Assert.Equal(3, viewModel.PendingCount);
    }

    // TC-WEB-MAP-010
    [Fact]
    public void MapDefenceSessionListItem_MapsLabelAndCounts()
    {
        DefenceSessionListItemDto dto = new(
            Guid.NewGuid(),
            2026,
            DefenceSessionType.Bachelor,
            2,
            DefenceSessionStatus.Active,
            4,
            120);

        DefenceSessionListItemViewModel viewModel = AdminDefenceSessionViewModelMapper.MapListItem(dto);

        Assert.Equal("Бакалавр", viewModel.TypeDisplay);
        Assert.Contains("2026", viewModel.SessionLabel, StringComparison.Ordinal);
        Assert.Equal(120, viewModel.DiplomaCount);
    }

    // TC-WEB-MAP-010b
    [Fact]
    public void MapDefenceSessionDetails_SetsCanArchiveForActiveSession()
    {
        DefenceSessionDetailsDto dto = new(
            Guid.NewGuid(),
            2026,
            DefenceSessionType.Master,
            null,
            DefenceSessionStatus.Active,
            [new StudyGroupItemDto(Guid.NewGuid(), "КН-41", 4, 25)],
            10);

        DefenceSessionDetailsViewModel viewModel = AdminDefenceSessionViewModelMapper.MapDetails(dto);

        Assert.True(viewModel.CanArchive);
        Assert.Single(viewModel.Groups);
        Assert.Equal(25, viewModel.Groups[0].StudentCount);
        Assert.Equal(4, viewModel.Groups[0].Course);
    }
}
