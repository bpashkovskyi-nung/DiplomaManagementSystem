using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Areas.Student.Models;
using DiplomaManagementSystem.Web.Mapping;
using DiplomaManagementSystem.Web.Models.Shared;

namespace DiplomaManagementSystem.Web.Tests.Mapping;

public sealed class WorkflowAndDocumentMapperTests
{
    // TC-WEB-MAP-006
    [Fact]
    public void WorkflowProgressMapper_MapsStepsAndMetadata()
    {
        WorkflowProgressViewModel viewModel = WorkflowProgressMapper.Map(MapperTestFixtures.CreateWorkflowProgress());

        Assert.Equal(12, viewModel.ProgressPercent);
        Assert.Equal(1, viewModel.CompletedSteps);
        Assert.Equal(8, viewModel.TotalSteps);
        Assert.Equal("Наступний крок:", viewModel.CurrentStepHintLabel);
        Assert.Equal(2, viewModel.Steps.Count);
        Assert.Equal("completed", viewModel.Steps[0].StateCssClass);
        Assert.Equal("current", viewModel.Steps[1].StateCssClass);
        Assert.Contains("Петро", viewModel.Steps[0].Metadata, StringComparison.Ordinal);
    }

    // TC-WEB-MAP-006b
    [Fact]
    public void WorkflowProgressMapper_UsesCustomHintLabel()
    {
        WorkflowProgressViewModel viewModel = WorkflowProgressMapper.Map(
            MapperTestFixtures.CreateWorkflowProgress(),
            "Поточний етап:");

        Assert.Equal("Поточний етап:", viewModel.CurrentStepHintLabel);
    }

    // TC-WEB-MAP-007
    [Fact]
    public void DiplomaDocumentMapper_WhenNull_ReturnsEmptyViewModel()
    {
        DiplomaDocumentsViewModel viewModel = DiplomaDocumentMapper.Map(null);

        Assert.Empty(viewModel.StudentWorkVersions);
        Assert.Null(viewModel.LatestSupervisorFeedback);
    }

    // TC-WEB-MAP-007b
    [Fact]
    public void DiplomaDocumentMapper_MapsAllDocumentKinds()
    {
        DiplomaDocumentsViewModel viewModel = DiplomaDocumentMapper.Map(MapperTestFixtures.CreateDocumentsBundle());

        Assert.Single(viewModel.StudentWorkVersions);
        Assert.NotNull(viewModel.LatestSupervisorFeedback);
        Assert.NotNull(viewModel.LatestExternalReview);
        Assert.NotNull(viewModel.LatestAntiPlagiarismReport);
        Assert.Equal("Робота студента", viewModel.StudentWorkVersions[0].KindDisplay);
    }

    // TC-WEB-MAP-008
    [Fact]
    public void TopicHistoryMapper_MapsFromTopicVersionItems()
    {
        IReadOnlyList<TopicVersionItemViewModel> versions =
        [
            new()
            {
                VersionNumber = 1,
                Title = "Тема",
                StatusDisplay = "Затверджено",
                SubmittedAt = MapperTestFixtures.Now,
            },
        ];

        IReadOnlyList<TopicHistoryEntryViewModel> history = TopicHistoryMapper.Map(versions);

        TopicHistoryEntryViewModel entry = Assert.Single(history);
        Assert.Equal("Тема", entry.Title);
        Assert.Equal("Затверджено", entry.StatusDisplay);
    }

    // TC-WEB-MAP-008b
    [Fact]
    public void TopicHistoryMapper_MapsFromSecretaryTopicDetails()
    {
        IReadOnlyList<TopicVersionDetailViewModel> versions =
        [
            new()
            {
                VersionNumber = 1,
                Title = "Тема",
                StatusDisplay = "Затверджено",
                SubmittedAtDisplay = "Подано: 15.03.2026",
                SupervisorApprovalLine = "Керівник: Петро",
                HeadApprovalLine = "Завідувач: Олена",
            },
        ];

        IReadOnlyList<TopicHistoryEntryViewModel> history = TopicHistoryMapper.Map(versions);

        TopicHistoryEntryViewModel entry = Assert.Single(history);
        Assert.Equal("Керівник: Петро", entry.SupervisorApprovalLine);
        Assert.Equal("Завідувач: Олена", entry.HeadApprovalLine);
    }
}
