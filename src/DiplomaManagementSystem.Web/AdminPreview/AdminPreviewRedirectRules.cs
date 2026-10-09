namespace DiplomaManagementSystem.Web.AdminPreview;

internal static class AdminPreviewRedirectRules
{
    public static bool IsReturnUrlValidForMode(string? returnUrl, AdminPreviewMode mode)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        string path = returnUrl.Split('?', 2)[0];
        if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? area = GetAreaFromPath(path);
        if (mode == AdminPreviewMode.Admin)
        {
            return area == "Admin" || IsAppRoot(path);
        }

        if (mode == AdminPreviewMode.SuperAdmin)
        {
            return area == "SuperAdmin" || IsAppRoot(path);
        }

        return AdminPreviewModeRules.IsValidReturnUrlArea(area, mode);
    }

    private static bool IsAppRoot(string path)
    {
        string normalized = path.TrimEnd('/');
        return normalized.Length == 0
               || string.Equals(normalized, "/Home", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetAreaFromPath(string path)
    {
        string[] segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        return segments[0] switch
        {
            "Admin" => "Admin",
            "Student" => "Student",
            "Employee" => "Employee",
            "Secretary" => "Secretary",
            "SuperAdmin" => "SuperAdmin",
            _ => null,
        };
    }
}
