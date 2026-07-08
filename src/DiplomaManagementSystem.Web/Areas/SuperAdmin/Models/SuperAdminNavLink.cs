namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

public sealed record SuperAdminNavLink(
    string Text,
    string Controller,
    string Action,
    IReadOnlyDictionary<string, string>? RouteValues = null);
