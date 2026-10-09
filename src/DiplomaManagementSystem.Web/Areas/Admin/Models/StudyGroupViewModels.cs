namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class StudyGroupFormViewModel
{
    public Guid? Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid SpecialtyId { get; set; }

    public string StudyForm { get; set; } = "очної форми навчання";

    public int? Course { get; set; }

    public IReadOnlyList<StudyGroupSpecialtyOptionViewModel> SpecialtyOptions { get; set; } = [];
}

public sealed class StudyGroupSpecialtyOptionViewModel
{
    public Guid Id { get; init; }

    public string Label { get; init; } = string.Empty;
}

public sealed class StudyGroupDeleteViewModel
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StudentCount { get; set; }

    public bool CanDelete => StudentCount == 0;
}
