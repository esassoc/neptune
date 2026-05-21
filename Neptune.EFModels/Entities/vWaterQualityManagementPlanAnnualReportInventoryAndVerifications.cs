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

    public static async Task<List<vWaterQualityManagementPlanAnnualReport>> ListForStormwaterJurisdictionIDsDtoAsync(NeptuneDbContext dbContext, PersonDto? personDto, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        var isAnonymousOrUnassigned = personDto == null || personDto.RoleID == (int)RoleEnum.Unassigned;

        var waterQualityManagementPlans = dbContext.vWaterQualityManagementPlanAnnualReports.AsNoTracking()
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
}