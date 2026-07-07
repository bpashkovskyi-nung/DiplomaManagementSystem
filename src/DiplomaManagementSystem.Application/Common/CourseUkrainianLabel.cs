namespace DiplomaManagementSystem.Application.Common;

public static class CourseUkrainianLabel
{
    public static string FormatGenitive(int course) => course switch
    {
        1 => "першого",
        2 => "другого",
        3 => "третього",
        4 => "четвертого",
        5 => "п'ятого",
        6 => "шостого",
        _ => $"{course}-го",
    };
}
