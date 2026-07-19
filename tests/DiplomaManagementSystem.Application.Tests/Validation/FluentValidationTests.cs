using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Employee.Validation;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Application.Import.Validation;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Secretary.Validation;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Application.Student.Validation;
using DiplomaManagementSystem.Domain.Enums;

using FluentValidation.Results;

namespace DiplomaManagementSystem.Application.Tests.Validation;

public sealed class FluentValidationTests
{
    private readonly CompleteCheckpointValidator _checkpointValidator = new();
    private readonly SelectSupervisorValidator _selectSupervisorValidator = new();
    private readonly SubmitTopicValidator _submitTopicValidator = new();
    private readonly AssignReviewerValidator _assignReviewerValidator = new();
    private readonly AdmitDiplomaValidator _admitValidator = new();
    private readonly OverrideSupervisorValidator _overrideSupervisorValidator = new();
    private readonly AddCommentValidator _addCommentValidator = new();
    private readonly OverrideAdmissionStepValidator _overrideAdmissionValidator = new();
    private readonly EmployeeImportRowValidator _employeeImportValidator = new();
    private readonly ApproveTopicValidator _approveTopicValidator = new();
    private readonly SetMilestoneProgressValidator _setMilestoneProgressValidator = new();
    private readonly RequestDefenceDateValidator _requestDefenceDateValidator = new();
    private readonly ConfirmDefenceDateValidator _confirmDefenceDateValidator = new();
    private readonly SaveMilestonesValidator _saveMilestonesValidator = new();
    private readonly SaveDefenceDatesValidator _saveDefenceDatesValidator = new();

    // TC-APP-VAL-001
    [Fact]
    public void SelectSupervisor_EmptyDiplomaId_Invalid()
    {
        ValidationResult result = _selectSupervisorValidator.Validate(
            new SelectSupervisorDto(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-002
    [Fact]
    public void SelectSupervisor_EmptySupervisorId_Invalid()
    {
        ValidationResult result = _selectSupervisorValidator.Validate(
            new SelectSupervisorDto(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-003
    [Fact]
    public void SelectSupervisor_Valid_Valid()
    {
        ValidationResult result = _selectSupervisorValidator.Validate(
            new SelectSupervisorDto(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-004
    [Fact]
    public void SubmitTopic_EmptyTitle_Invalid()
    {
        ValidationResult result = _submitTopicValidator.Validate(
            new SubmitTopicDto(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-005
    [Fact]
    public void SubmitTopic_TitleTooLong_Invalid()
    {
        ValidationResult result = _submitTopicValidator.Validate(
            new SubmitTopicDto(Guid.NewGuid(), new string('а', 501)));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-006
    [Fact]
    public void SubmitTopic_Valid_Valid()
    {
        ValidationResult result = _submitTopicValidator.Validate(
            new SubmitTopicDto(Guid.NewGuid(), "Тема роботи"));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-010
    [Fact]
    public void AssignReviewer_EmptyIds_Invalid()
    {
        ValidationResult result = _assignReviewerValidator.Validate(
            new AssignReviewerDto(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-011
    [Fact]
    public void AdmitDiploma_EmptyDiplomaId_Invalid()
    {
        ValidationResult result = _admitValidator.Validate(
            new AdmitDiplomaDto(Guid.Empty));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-012
    [Fact]
    public void OverrideSupervisor_EmptyReason_Invalid()
    {
        ValidationResult result = _overrideSupervisorValidator.Validate(
            new OverrideSupervisorDto(Guid.NewGuid(), Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-013
    [Fact]
    public void AddComment_EmptyBody_Invalid()
    {
        ValidationResult result = _addCommentValidator.Validate(
            new AddCommentDto(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-014
    [Fact]
    public void OverrideAdmissionStep_EmptyComment_WhenNotApproved_Invalid()
    {
        ValidationResult result = _overrideAdmissionValidator.Validate(
            new OverrideAdmissionStepDto(
                Guid.NewGuid(),
                AdmissionStep.SupervisorFeedback,
                CheckpointOutcome.NotApproved,
                ""));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-014b
    [Fact]
    public void OverrideAdmissionStep_ApprovedWithoutComment_Valid()
    {
        ValidationResult result = _overrideAdmissionValidator.Validate(
            new OverrideAdmissionStepDto(
                Guid.NewGuid(),
                AdmissionStep.SupervisorFeedback,
                CheckpointOutcome.Approved,
                null));

        Assert.True(result.IsValid);
    }
    [Fact]
    public void ApproveTopic_EmptyVersionId_Invalid()
    {
        ValidationResult result = _approveTopicValidator.Validate(new ApproveTopicDto(Guid.Empty, null));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-014c
    [Fact]
    public void ApproveTopic_OptionalComment_Valid()
    {
        ValidationResult withoutComment = _approveTopicValidator.Validate(new ApproveTopicDto(Guid.NewGuid(), null));
        ValidationResult withComment = _approveTopicValidator.Validate(
            new ApproveTopicDto(Guid.NewGuid(), "Коментар до погодження"));

        Assert.True(withoutComment.IsValid);
        Assert.True(withComment.IsValid);
    }

    // TC-APP-VAL-014d
    [Fact]
    public void ApproveTopic_TooLongComment_Invalid()
    {
        ValidationResult result = _approveTopicValidator.Validate(
            new ApproveTopicDto(Guid.NewGuid(), new string('а', 1001)));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-015
    [Fact]
    public void SecretaryValidators_Valid_Valid()
    {
        Assert.True(_assignReviewerValidator.Validate(new AssignReviewerDto(Guid.NewGuid(), Guid.NewGuid())).IsValid);
        Assert.True(_admitValidator.Validate(new AdmitDiplomaDto(Guid.NewGuid())).IsValid);
        Assert.True(_overrideSupervisorValidator.Validate(
            new OverrideSupervisorDto(Guid.NewGuid(), Guid.NewGuid(), "Причина")).IsValid);
        Assert.True(_addCommentValidator.Validate(new AddCommentDto(Guid.NewGuid(), "Коментар")).IsValid);
        Assert.True(_overrideAdmissionValidator.Validate(
            new OverrideAdmissionStepDto(
                Guid.NewGuid(),
                AdmissionStep.SupervisorFeedback,
                CheckpointOutcome.Approved,
                "Коментар")).IsValid);
    }

    // TC-APP-VAL-020
    [Fact]
    public void CompleteCheckpoint_NotApprovedWithoutComment_Invalid()
    {
        ValidationResult result = _checkpointValidator.Validate(
            new CompleteCheckpointDto(Guid.NewGuid(), CheckpointOutcome.NotApproved, null));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-021
    [Fact]
    public void CompleteCheckpoint_NotApprovedWithComment_Valid()
    {
        ValidationResult result = _checkpointValidator.Validate(
            new CompleteCheckpointDto(Guid.NewGuid(), CheckpointOutcome.NotApproved, "Причина"));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-022
    [Fact]
    public void CompleteCheckpoint_ApprovedWithoutComment_Valid()
    {
        ValidationResult result = _checkpointValidator.Validate(
            new CompleteCheckpointDto(Guid.NewGuid(), CheckpointOutcome.Approved, null));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-030
    [Fact]
    public void EmployeeImportRow_EmptyName_Invalid()
    {
        ValidationResult result = _employeeImportValidator.Validate(
            new EmployeeImportRow(FullName: "", Email: "a@b.com"));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-031
    [Fact]
    public void EmployeeImportRow_InvalidEmail_Invalid()
    {
        ValidationResult result = _employeeImportValidator.Validate(
            new EmployeeImportRow(FullName: "Name", Email: "not-email"));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-032
    [Fact]
    public void EmployeeImportRow_Valid_Valid()
    {
        ValidationResult result = _employeeImportValidator.Validate(
            new EmployeeImportRow(FullName: "Петро Петренко", Email: "petro@test.local"));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-040
    [Fact]
    public void SetMilestoneProgress_EmptyIds_Invalid()
    {
        ValidationResult result = _setMilestoneProgressValidator.Validate(
            new SetMilestoneProgressDto(Guid.Empty, Guid.Empty, 50));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-041
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SetMilestoneProgress_PercentOutOfRange_Invalid(int percent)
    {
        ValidationResult result = _setMilestoneProgressValidator.Validate(
            new SetMilestoneProgressDto(Guid.NewGuid(), Guid.NewGuid(), percent));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-042
    [Fact]
    public void SetMilestoneProgress_Valid_Valid()
    {
        ValidationResult result = _setMilestoneProgressValidator.Validate(
            new SetMilestoneProgressDto(Guid.NewGuid(), Guid.NewGuid(), 50));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-043
    [Fact]
    public void RequestDefenceDate_EmptyIds_Invalid()
    {
        ValidationResult result = _requestDefenceDateValidator.Validate(
            new RequestDefenceDateDto(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-044
    [Fact]
    public void RequestDefenceDate_Valid_Valid()
    {
        ValidationResult result = _requestDefenceDateValidator.Validate(
            new RequestDefenceDateDto(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-045
    [Fact]
    public void ConfirmDefenceDate_EmptyDiplomaId_Invalid()
    {
        ValidationResult result = _confirmDefenceDateValidator.Validate(
            new ConfirmDefenceDateDto(Guid.Empty, new DateOnly(2026, 6, 20)));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-046
    [Fact]
    public void ConfirmDefenceDate_EmptyDate_Invalid()
    {
        ValidationResult result = _confirmDefenceDateValidator.Validate(
            new ConfirmDefenceDateDto(Guid.NewGuid(), default));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-047
    [Fact]
    public void ConfirmDefenceDate_Valid_Valid()
    {
        ValidationResult result = _confirmDefenceDateValidator.Validate(
            new ConfirmDefenceDateDto(Guid.NewGuid(), new DateOnly(2026, 6, 20)));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-048
    [Fact]
    public void SaveMilestones_WrongCount_Invalid()
    {
        ValidationResult result = _saveMilestonesValidator.Validate(
            new SaveMilestonesDto(
            [
                new SaveMilestoneItemDto(new DateOnly(2026, 3, 1), 30),
                new SaveMilestoneItemDto(new DateOnly(2026, 4, 1), 60),
            ]));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-049
    [Fact]
    public void SaveMilestones_PercentOutOfRange_Invalid()
    {
        ValidationResult result = _saveMilestonesValidator.Validate(
            new SaveMilestonesDto(
            [
                new SaveMilestoneItemDto(new DateOnly(2026, 3, 1), -5),
                new SaveMilestoneItemDto(new DateOnly(2026, 4, 1), 60),
                new SaveMilestoneItemDto(new DateOnly(2026, 5, 1), 100),
            ]));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-050
    [Fact]
    public void SaveMilestones_Valid_Valid()
    {
        ValidationResult result = _saveMilestonesValidator.Validate(
            new SaveMilestonesDto(
            [
                new SaveMilestoneItemDto(new DateOnly(2026, 3, 1), 30),
                new SaveMilestoneItemDto(new DateOnly(2026, 4, 1), 60),
                new SaveMilestoneItemDto(new DateOnly(2026, 5, 1), 100),
            ]));

        Assert.True(result.IsValid);
    }

    // TC-APP-VAL-051
    [Fact]
    public void SaveDefenceDates_EmptyDateInList_Invalid()
    {
        ValidationResult result = _saveDefenceDatesValidator.Validate(
            new SaveDefenceDatesDto([default]));

        Assert.False(result.IsValid);
    }

    // TC-APP-VAL-052
    [Fact]
    public void SaveDefenceDates_Valid_Valid()
    {
        ValidationResult result = _saveDefenceDatesValidator.Validate(
            new SaveDefenceDatesDto([new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21)]));

        Assert.True(result.IsValid);
    }
}
