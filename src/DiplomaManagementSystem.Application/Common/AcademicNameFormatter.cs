namespace DiplomaManagementSystem.Application.Common;

public static class AcademicNameFormatter
{
    public static string ToShortDisplayName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        string[] parts = fullName
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        string surname = parts[0];
        IEnumerable<string> initials = parts
            .Skip(1)
            .Select(part => $"{char.ToUpperInvariant(part[0])}.");

        return $"{surname} {string.Join(string.Empty, initials)}";
    }
}
