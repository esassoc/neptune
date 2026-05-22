using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class vWaterQualityManagementPlanDetaileds
{
    public static List<vWaterQualityManagementPlanDetailed> ListViewableByPerson(NeptuneDbContext dbContext, Person person)
    {
        var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, person);

        //These users can technically see all Jurisdictions, just potentially not the WQMPs inside them
        var waterQualityManagementPlans = dbContext.vWaterQualityManagementPlanDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID));
        if (person.IsAnonymousOrUnassigned())
        {
            var publicWaterQualityManagementPlans = waterQualityManagementPlans.Where(x =>
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Active ||
                (x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Inactive &&
                x.StormwaterJurisdictionPublicWQMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive)).ToList();
            return publicWaterQualityManagementPlans;
        }

        return waterQualityManagementPlans.ToList();
    }

    public static async Task<List<vWaterQualityManagementPlanDetailed>> ListViewableByPersonDtoAsync(NeptuneDbContext dbContext, PersonDto personDto)
    {
        var isAnonymousOrUnassigned = personDto == null || personDto.RoleID == (int)RoleEnum.Unassigned;

        List<int> stormwaterJurisdictionIDsPersonCanView;
        if (personDto != null && personDto.RoleID is (int)RoleEnum.Admin or (int)RoleEnum.SitkaAdmin)
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictionPeople
                .AsNoTracking().Select(x => x.StormwaterJurisdictionID).Distinct().ToListAsync();
        }
        else if (isAnonymousOrUnassigned)
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictions.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionPublicWQMPVisibilityTypeID !=
                            (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.None)
                .Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }
        else
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictionPeople
                .Where(x => x.PersonID == personDto.PersonID)
                .Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }

        var waterQualityManagementPlans = dbContext.vWaterQualityManagementPlanDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID));

        if (isAnonymousOrUnassigned)
        {
            return await waterQualityManagementPlans.Where(x =>
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Active ||
                (x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Inactive &&
                x.StormwaterJurisdictionPublicWQMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive)).ToListAsync();
        }

        return await waterQualityManagementPlans.ToListAsync();
    }

    /// <summary>
    /// Annual-report Approval Summary listing: pushes the reporting-period date range and
    /// optional jurisdiction filter into SQL so the database does the work. -1 means "all
    /// jurisdictions the caller is allowed to see". Used by the WQMP Annual Report SPA.
    /// </summary>
    public static async Task<List<vWaterQualityManagementPlanDetailed>> ListForAnnualReportApprovalSummaryAsync(
        NeptuneDbContext dbContext, PersonDto personDto, DateTime reportingPeriodStart, DateTime reportingPeriodEnd, int stormwaterJurisdictionID)
    {
        var isAnonymousOrUnassigned = personDto == null || personDto.RoleID == (int)RoleEnum.Unassigned;

        List<int> stormwaterJurisdictionIDsPersonCanView;
        if (personDto != null && personDto.RoleID is (int)RoleEnum.Admin or (int)RoleEnum.SitkaAdmin)
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictionPeople
                .AsNoTracking().Select(x => x.StormwaterJurisdictionID).Distinct().ToListAsync();
        }
        else if (isAnonymousOrUnassigned)
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictions.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionPublicWQMPVisibilityTypeID !=
                            (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.None)
                .Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }
        else
        {
            stormwaterJurisdictionIDsPersonCanView = await dbContext.StormwaterJurisdictionPeople
                .Where(x => x.PersonID == personDto.PersonID)
                .Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }

        var query = dbContext.vWaterQualityManagementPlanDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID)
                        && x.ApprovalDate >= reportingPeriodStart
                        && x.ApprovalDate <= reportingPeriodEnd);

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

        return await query.OrderBy(x => x.WaterQualityManagementPlanName).ToListAsync();
    }
}