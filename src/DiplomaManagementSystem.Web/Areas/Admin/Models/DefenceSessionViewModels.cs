using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class DefenceSessionListViewModel
{
    public IReadOnlyList<DefenceSessionListItemViewModel> Items { get; set; } = [];
}

public sealed class DefenceSessionListItemViewModel
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public DefenceSessionType Type { get; set; }

    public string TypeDisplay { get; set; } = string.Empty;

    public int? Semester { get; set; }

    public DefenceSessionStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public int GroupCount { get; set; }

    public int DiplomaCount { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public bool IsActive => Status == DefenceSessionStatus.Active;

    public bool CanArchive => Status == DefenceSessionStatus.Active;
}

public sealed class DefenceSessionFormViewModel
{
    public Guid? Id { get; set; }

    public int Year { get; set; }

    public DefenceSessionType Type { get; set; }

    public int? Semester { get; set; }
}

public sealed class DefenceSessionDetailsViewModel
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public DefenceSessionType Type { get; set; }

    public string TypeDisplay { get; set; } = string.Empty;

    public int? Semester { get; set; }

    public DefenceSessionStatus Status { get; set; }

    public string StatusDisplay { get; set; } = string.Empty;

    public IReadOnlyList<StudyGroupListItemViewModel> Groups { get; set; } = [];

    public int DiplomaCount { get; set; }

    public bool CanArchive { get; set; }
}

public sealed class StudyGroupListItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? Course { get; set; }

    public int StudentCount { get; set; }

    public bool CanDelete => StudentCount == 0;
}
