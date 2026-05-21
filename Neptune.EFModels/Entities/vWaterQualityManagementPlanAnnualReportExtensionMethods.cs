using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class vWaterQualityManagementPlanAnnualReportExtensionMethods
{
    public static List<WaterQualityManagementPlanPostConstructionVerificationGridDto> BuildPostConstructionGridDtos(IEnumerable<vWaterQualityManagementPlanAnnualReport> rows)
    {
        return rows
            .GroupBy(x => x.WaterQualityManagementPlanID)
            .Select(BuildPostConstructionGridDto)
            .OrderBy(x => x.WaterQualityManagementPlanName)
            .ToList();
    }

    private static WaterQualityManagementPlanPostConstructionVerificationGridDto BuildPostConstructionGridDto(IGrouping<int, vWaterQualityManagementPlanAnnualReport> group)
    {
        var any = group.First();
        var mostRecent = group
            .OrderByDescending(y => y.WaterQualityManagementPlanVerifyVerificationDate)
            .First();

        // Display-layer toggle: if the verification has TreatmentBMPs (count is non-null) use
        // those roll-up columns; otherwise fall back to QuickBMP roll-up. Both sets of columns
        // are always present on the view (LEFT JOINs); the count being non-null is the signal.
        var useTreatmentBMP = mostRecent.WaterQualityManagementPlanVerifyTreatmentBMPCount.HasValue;
        var numberOfBMPs = useTreatmentBMP
            ? mostRecent.WaterQualityManagementPlanVerifyTreatmentBMPCount
            : mostRecent.WaterQualityManagementPlanVerifyQuickBMPCount;
        var bmpsAdequate = useTreatmentBMP
            ? mostRecent.WaterQualityManagementPlanVerifyTreatmentBMPIsAdequateCount
            : mostRecent.WaterQualityManagementPlanVerifyQuickBMPIsAdequateCount;
        var bmpsDeficient = useTreatmentBMP
            ? mostRecent.WaterQualityManagementPlanVerifyTreatmentBMPIsDeficientCount
            : mostRecent.WaterQualityManagementPlanVerifyQuickBMPIsDeficient;

        var bmpNoteComments = useTreatmentBMP
            ? mostRecent.WaterQualityManagementPlanVerifyTreatmentBMPNotes
            : mostRecent.WaterQualityManagementPlanVerifyQuickBMPNotes;
        var separator = string.IsNullOrWhiteSpace(bmpNoteComments) ? string.Empty : "; ";
        var comments = $"{bmpNoteComments}{separator}{mostRecent.EnforcementOrFollowupActions}";

        var statusName = mostRecent.WaterQualityManagementPlanVerifyStatusID.HasValue
            && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(mostRecent.WaterQualityManagementPlanVerifyStatusID.Value, out var status)
                ? status.WaterQualityManagementPlanVerifyStatusDisplayName
                : string.Empty;

        return new WaterQualityManagementPlanPostConstructionVerificationGridDto
        {
            WaterQualityManagementPlanID = group.Key,
            WaterQualityManagementPlanName = any.WaterQualityManagementPlanName,
            WaterQualityManagementPlanVerifyStatusName = statusName,
            NumberOfBMPs = numberOfBMPs,
            NumberOfBMPsAdequate = bmpsAdequate,
            NumberOfBMPsDeficient = bmpsDeficient,
            WQMPVerificationComments = comments,
        };
    }
}
