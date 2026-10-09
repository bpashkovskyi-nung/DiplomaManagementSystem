using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class SecretaryDashboardViewModelMapper
{
    public static SecretaryDashboardViewModel Map(SecretaryDashboardDto dashboard) => new()
    {
        SessionId = dashboard.SessionId,
        SessionType = dashboard.SessionType,
        SessionLabel = dashboard.SessionLabel,
        TotalDiplomas = dashboard.TotalDiplomas,
        Buckets = dashboard.Buckets.Select(MapBucket).ToList(),
    };

    private static SecretaryDashboardBucketViewModel MapBucket(SecretaryDashboardBucketDto item) => new()
    {
        LifecycleStatus = item.LifecycleStatus,
        AdmissionStep = item.AdmissionStep,
        Label = item.AdmissionStep is { } step
            ? UkrainianDisplay.FormatAdmissionStep(step)
            : UkrainianDisplay.FormatDiplomaLifecycleStatus(item.LifecycleStatus!.Value),
        BadgeClass = item.AdmissionStep is { } admissionStep
            ? UkrainianDisplay.AdmissionStepBadgeClass(admissionStep)
            : UkrainianDisplay.LifecycleBadgeClass(item.LifecycleStatus!.Value),
        Count = item.Count,
    };
}
