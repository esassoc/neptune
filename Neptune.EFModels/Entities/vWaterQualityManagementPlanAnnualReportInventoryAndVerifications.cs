using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class vWaterQualityManagementPlanAnnualReports
{
    public static List<vWaterQualityManagementPlanAnnualReport> ListForStormwaterJurisdictionIDs(NeptuneDbContext dbContext, Person person, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {

        //These users can technically see all Jurisdictions, just potentially not the WQMPs inside them
        var waterQualityManagementPlans = dbContext.vWaterQualityManagementPlanAnnualReports.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID));
        if (person.IsAnonymousOrUnassigned())
        {
            var publicWaterQualityManagementPlans = waterQualityManagementPlans.Where(x =>
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Active ||
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Inactive &&
                x.StormwaterJurisdictionPublicWQMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive).ToList();
            return publicWaterQualityManagementPlans;
        }

        return waterQualityManagementPlans.ToList();
    }

    /// <summary>
    /// Annual-report Post-Construction listing: pushes the verification-date range and
    /// optional jurisdiction filter into SQL so the database does the work. -1 means
    /// "all jurisdictions the caller is allowed to see". Returns one row per verification
    /// in window; the caller groups by WQMP and picks the most recent.
    /// </summary>
    public static async Task<List<vWaterQualityManagementPlanAnnualReport>> ListForAnnualReportPostConstructionAsync(
        NeptuneDbContext dbContext, PersonDto? personDto, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView,
        DateOnly reportingPeriodStart, DateOnly reportingPeriodEnd, int stormwaterJurisdictionID)
    {
        var isAnonymousOrUnassigned = personDto == null || personDto.RoleID == (int)RoleEnum.Unassigned;

        var query = dbContext.vWaterQualityManagementPlanAnnualReports.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID)
                        && x.WaterQualityManagementPlanVerifyVerificationDate >= reportingPeriodStart
                        && x.WaterQualityManagementPlanVerifyVerificationDate <= reportingPeriodEnd);

        if (stormwaterJurisdictionID != -1)
        {
            query = query.Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        }

        if (isAnonymousOrUnassigned)
        {
            query = query.Where(x =>
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Active ||
                (x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Inactive &&
                x.StormwaterJurisdictionPublicWQMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive));
        }

        return await query.ToListAsync();
    }
}