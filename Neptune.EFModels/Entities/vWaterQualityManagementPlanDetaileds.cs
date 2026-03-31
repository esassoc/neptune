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
}