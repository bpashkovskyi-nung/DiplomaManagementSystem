using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Tests;

public sealed class CheckpointOutcomeRulesTests
{
    // TC-DOM-CHK-001
    [Fact]
    public void IsPassing_Approved_ReturnsTrue()
    {
        Assert.True(CheckpointOutcomeRules.IsPassing(CheckpointOutcome.Approved));
    }

    // TC-DOM-CHK-003
    [Fact]
    public void IsPassing_NotApproved_ReturnsFalse()
    {
        Assert.False(CheckpointOutcomeRules.IsPassing(CheckpointOutcome.NotApproved));
    }

    // TC-DOM-CHK-004
    [Fact]
    public void IsPassing_Null_ReturnsFalse()
    {
        Assert.False(CheckpointOutcomeRules.IsPassing(null));
    }

    // TC-DOM-CHK-005
    [Fact]
    public void RequiresComment_NotApproved_ReturnsTrue()
    {
        Assert.True(CheckpointOutcomeRules.RequiresComment(CheckpointOutcome.NotApproved));
    }

    // TC-DOM-CHK-007
    [Fact]
    public void RequiresComment_Approved_ReturnsFalse()
    {
        Assert.False(CheckpointOutcomeRules.RequiresComment(CheckpointOutcome.Approved));
    }

    [Fact]
    public void RequiresDocument_Approved_ReturnsTrue()
    {
        Assert.True(CheckpointOutcomeRules.RequiresDocument(CheckpointOutcome.Approved));
    }

    [Fact]
    public void RequiresDocument_NotApproved_ReturnsFalse()
    {
        Assert.False(CheckpointOutcomeRules.RequiresDocument(CheckpointOutcome.NotApproved));
    }
}
