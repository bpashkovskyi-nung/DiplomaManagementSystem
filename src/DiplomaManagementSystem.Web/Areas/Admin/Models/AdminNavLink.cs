namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed record AdminNavLink(
    string Text,
    string Controller,
    string Action,
    IReadOnlyDictionary<string, string>? RouteValues = null);
