namespace DiplomaManagementSystem.Application.Tests;

public sealed class DiplomaWorkflowStateTests
{
    [Fact]
    public void ReadOnly_DisablesAllSecretaryActions()
    {
        DiplomaWorkflowSecretaryFlags flags = DiplomaWorkflowSecretaryFlags.ReadOnly;

        Assert.False(flags.ShowOverrideSupervisorSection);
        Assert.False(flags.CanOverrideSupervisor);
        Assert.False(flags.ShowAssignReviewerSection);
        Assert.False(flags.CanAssignReviewer);
        Assert.False(flags.ShowAdmitSection);
        Assert.False(flags.CanAdmit);
        Assert.False(flags.ShowOverrideAdmissionStepSection);
        Assert.False(flags.CanOverrideAdmissionStep);
        Assert.False(flags.ShowAddCommentSection);
        Assert.False(flags.CanAddComment);
    }
}
